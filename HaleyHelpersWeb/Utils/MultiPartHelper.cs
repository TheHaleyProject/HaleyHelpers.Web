using Haley.Models;
using Microsoft.AspNetCore.WebUtilities;
//using System.Net.Http.Headers;
using Haley.Abstractions;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Features;
using Azure.Core;
using System.Text;

namespace Haley.Utils {
    public static class MultiPartHelper {
        static readonly FormOptions _defaultFormOptions = new FormOptions();

        public static async Task<StorageResponse> UploadFileAsync(HttpRequest request, VaultWriteWrapper wrapper) { 
            return await UploadFileAsync(request.Body,request.ContentType,wrapper);
        }

        public static async Task<StorageResponse> UploadFileAsync(Stream fileStream, string contentType, VaultWriteWrapper wrapper) {
            if (wrapper == null) throw new ArgumentException(nameof(VaultWriteWrapper));
            if (wrapper.Service == null) throw new ArgumentNullException(nameof(VaultWriteWrapper.Service));

            if (!IsMultipartContentType(contentType)) {
                throw new Exception($"Expected a multipart request, but got {contentType}");
            }

            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(contentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
            var multipartReader = new MultipartReader(boundary, fileStream,wrapper.BufferSize);
            var section = await multipartReader.ReadNextSectionAsync();

            var formAccumulator = new KeyValueAccumulator();
            StorageResponse result = new StorageResponse();
            long sizeUploadedInBytes = 0;

            while (section != null) {

                var hasDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                if (hasDispositionHeader && contentDisposition != null) {
                    //Check if it is file disposition or data disposition
                    if (HasFileContentDisposition(contentDisposition)) {
                        var saveSummary = await StoreFileAsync(section,wrapper);
                        if (saveSummary == null) continue;
                        if (saveSummary.Status) {
                            result.Passed++;
                            sizeUploadedInBytes += saveSummary.Size;
                            result.PassedSummary.TryAdd(saveSummary.RawName , saveSummary); //what if the extensions differ for different files?
                        } else {
                            result.Failed++;
                            result.FailedSummary.Add(saveSummary.RawName, saveSummary);
                        }

                    } else if (HasDataContentDisposition(contentDisposition)){
                        //handle the normal content data.
                        var formParam = await HandleFormParameter(section, contentDisposition);
                        if (!string.IsNullOrWhiteSpace(formParam.key)) {
                            formAccumulator.Append(formParam.key, formParam.value);
                        }
                    }
                }
                section = await multipartReader.ReadNextSectionAsync();
            }
            if (formAccumulator.KeyCount < 1) result.Status = true; //need not worry about data handling. 
            if (formAccumulator.KeyCount > 0) {
                if (wrapper.DataHandler == null) throw new ArgumentNullException($@"When parameters are present, {nameof(wrapper.DataHandler)} cannot be null");
                result.Status = wrapper.DataHandler.Invoke(formAccumulator);
            }
            result.TotalSizeUploaded = sizeUploadedInBytes.ToFileSize(false);
            return result;
        }

        static async Task<(string key, string value)> HandleFormParameter(MultipartSection section, ContentDispositionHeaderValue contentDisposition) {
            string key = string.Empty, value = string.Empty;
            // Don't limit the key name length because the 
            // multipart headers length limit is already in effect.
            key = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value;
            var encoding = GetEncoding(section);
            if (encoding == null) {
                throw new ArgumentException($@"Unable to fetch the encoding for the parameter {key}");
            }

            using (var streamReader = new StreamReader(section.Body,encoding,detectEncodingFromByteOrderMarks: true,bufferSize: 1024,leaveOpen: true)) {
                // The value length limit is enforced by 
                // MultipartBodyLengthLimit
                value = await streamReader.ReadToEndAsync();

                if (string.Equals(value, "undefined",
                    StringComparison.OrdinalIgnoreCase)) {
                    value = string.Empty; //In case we are receiving input from 
                }
                return (key, value);
            }
        }

            static async Task<FileStorageSummary> StoreFileAsync(MultipartSection section, VaultWriteWrapper wrapper) {
            var fileSection = section.AsFileSection();
            if (fileSection != null) {
                //If we are dealing with file, then we need a valid storage service.
                if (wrapper.Service == null) throw new ArgumentNullException(nameof(VaultWriteWrapper.Service));

                StorageRequest input = new StorageRequest();
                wrapper.MapProperties(input); //Fill rootdir and other basic properties
                input.Id = fileSection.Name;
                input.RawName = fileSection.FileName;
                input.IsFolder = false; //as we are uploading a file.

                //PREFERENCE : if the file name generator is present, then it means, we need to override the default mechanism.
                if (wrapper.FileNameGenerator != null) {
                    input.TargetName = wrapper.FileNameGenerator.Invoke((input.Id, input.RawName, wrapper));
                }
                
                var saveSummary = await wrapper.Service!.Upload(input, fileSection.FileStream, wrapper.BufferSize);
                return saveSummary;
            }
            return null; //don't return a value.
        }

        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec says 70 characters is a reasonable limit.
        static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit) {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary);
            if (string.IsNullOrWhiteSpace(boundary.Value)) {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            if (boundary.Length > lengthLimit) {
                throw new InvalidDataException(
                    $"Multipart boundary length limit {lengthLimit} exceeded.");
            }

            return boundary.Value;
        }

        static bool IsMultipartContentType(string contentType) {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static Encoding GetEncoding(MultipartSection section) {
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding)) {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }

        static bool HasDataContentDisposition(ContentDispositionHeaderValue contentDisposition) {
            // Content-Disposition: form-data; name="key";
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && string.IsNullOrEmpty(contentDisposition.FileName.Value)
                   && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
        }

        static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition) {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                       || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }

    }
}
