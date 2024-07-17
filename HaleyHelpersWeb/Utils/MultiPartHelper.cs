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

        public static async Task<StorageResponse> UploadFileAsync(HttpRequest request, VaultRequestHelper mpuInput) { 
            return await UploadFileAsync(request.Body,request.ContentType,mpuInput);
        }

        public static async Task<StorageResponse> UploadFileAsync(Stream fileStream, string contentType,VaultRequestHelper mpuInput) {
            if (mpuInput == null) throw new ArgumentException(nameof(VaultRequestHelper));
            if (mpuInput.Service == null) throw new ArgumentNullException(nameof(VaultRequestHelper.Service));

            if (!IsMultipartContentType(contentType)) {
                throw new Exception($"Expected a multipart request, but got {contentType}");
            }

            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(contentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
            var multipartReader = new MultipartReader(boundary, fileStream,mpuInput.BufferSize);
            var section = await multipartReader.ReadNextSectionAsync();

            var formAccumulator = new KeyValueAccumulator();
            StorageResponse result = new StorageResponse();
            long sizeUploadedInBytes = 0;

            while (section != null) {

                var hasDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                if (hasDispositionHeader && contentDisposition != null) {
                    //Check if it is file disposition or data disposition
                    if (HasFileContentDisposition(contentDisposition)) {
                        var saveSummary = await StoreFileAsync(section,mpuInput);
                        if (saveSummary == null) continue;
                        if (saveSummary.Status) {
                            result.Passed++;
                            sizeUploadedInBytes += saveSummary.Size;
                            result.StoredFilesInfo.TryAdd(saveSummary.FileName, saveSummary); //what if the extensions differ for different files?
                        } else {
                            result.FailedCount++;
                            result.FailedFilesInfo.Add(saveSummary.FileName, saveSummary);
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
                if (mpuInput.DataHandler == null) throw new ArgumentNullException($@"When parameters are present, {nameof(VaultRequestHelper.DataHandler)} cannot be null");
                result.Status = mpuInput.DataHandler.Invoke(formAccumulator);
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

            static async Task<FileStorageSummary> StoreFileAsync(MultipartSection section,VaultRequestHelper mpuInput) {
            var fileSection = section.AsFileSection();
            if (fileSection != null) {
                //If we are dealing with file, then we need a valid storage service.
                if (mpuInput.Service == null) throw new ArgumentNullException(nameof(VaultRequestHelper.Service));

                StorageRequest input = new StorageRequest() { PreferNumericName = mpuInput.PreferId, FileName = fileSection.FileName, RootDirName = mpuInput.RootDir };

                if (mpuInput.PreferId && input.Id < 1) {
                    //PREFERENCE 1 : If we have  a parse from Name, try to use it.
                    var fname = Path.GetFileNameWithoutExtension(fileSection.Name); //Name obviously will not contain extension. Only FileName has extension.
                    if (long.TryParse(fname, out long fnameId)) {
                        input.Id = fnameId; //id is obtained.
                    }

                    //PREFERENCE 2 : Since we are creating the Storage input inside this method, obviously, the input id is less than 1.
                    if (input.Id < 1) {
                        if (mpuInput.IdGenerator == null) throw new ArgumentNullException($@"When {nameof(VaultRequestHelper.PreferId)} is true, {nameof(VaultRequestHelper.IdGenerator)} cannot be null");
                        input.Id = mpuInput.IdGenerator?.Invoke(fileSection.Name, fileSection.FileName) ?? 0;
                    }
                }
                
                var saveSummary = await mpuInput.Service!.Upload(input, fileSection.FileStream, mpuInput.BufferSize);
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
