using Haley.Models;
using Microsoft.AspNetCore.WebUtilities;

//using System.Net.Http.Headers;
using Haley.Abstractions;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Features;
using Azure.Core;
using System.Text;

namespace Haley.Utils {

    public static class MultiPartUploadHelper {
        private static readonly FormOptions _defaultFormOptions = new FormOptions();

        public static async Task<SummaryResponse> UploadFileAsync(HttpRequest request, VaultRequestWrapper wrapper) {
            return await UploadFileAsync(request.Body, request.ContentType, wrapper);
        }

        public static async Task<SummaryResponse> UploadFileAsync(Stream fileStream, string contentType, VaultRequestWrapper wrapper) {
            if (wrapper == null) throw new ArgumentException(nameof(VaultRequestWrapper));
            if (wrapper.Service == null) throw new ArgumentNullException(nameof(VaultRequestWrapper.Service));

            if (!IsMultipartContentType(contentType)) {
                throw new Exception($"Expected a multipart request, but got {contentType}");
            }

            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(contentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
            var multipartReader = new MultipartReader(boundary, fileStream, wrapper.BufferSize);
            var section = await multipartReader.ReadNextSectionAsync();

            var formAccumulator = new KeyValueAccumulator();
            SummaryResponse result = new SummaryResponse();
            long sizeUploadedInBytes = 0;

            while (section != null) {
                var hasDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                if (hasDispositionHeader && contentDisposition != null) {
                    //Check if it is file disposition or data disposition
                    if (HasFileContentDisposition(contentDisposition)) {
                        var saveSummary = await StoreFileAsync(section, wrapper);
                        if (saveSummary != null && saveSummary.Status) {
                            result.Passed++;
                            sizeUploadedInBytes += saveSummary.Size;
                            if (!string.IsNullOrWhiteSpace(saveSummary.ObjectRawName)) {
                                result.PassedSummary.TryAdd(saveSummary.ObjectRawName, saveSummary); //what if the extensions differ for different files?
                            }
                        } else {
                            result.Failed++;
                            if (!string.IsNullOrWhiteSpace(saveSummary?.ObjectRawName)) {
                                result.FailedSummary.Add(saveSummary.ObjectRawName, saveSummary);
                            }
                        }
                    } else if (HasDataContentDisposition(contentDisposition)) {
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

        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec says 70 characters is a reasonable limit.
        private static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit) {
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

        private static Encoding GetEncoding(MultipartSection section) {
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding)) {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }

        private static async Task<(string key, string value)> HandleFormParameter(MultipartSection section, ContentDispositionHeaderValue contentDisposition) {
            string key = string.Empty, value = string.Empty;
            // Don't limit the key name length because the
            // multipart headers length limit is already in effect.
            key = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value;
            var encoding = GetEncoding(section);
            if (encoding == null) {
                throw new ArgumentException($@"Unable to fetch the encoding for the parameter {key}");
            }

            using (var streamReader = new StreamReader(section.Body, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true)) {
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

        private static bool HasDataContentDisposition(ContentDispositionHeaderValue contentDisposition) {
            // Content-Disposition: form-data; name="key";
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && string.IsNullOrEmpty(contentDisposition.FileName.Value)
                   && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
        }

        private static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition) {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                       || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }

        private static bool IsMultipartContentType(string contentType) {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string SanitizeStoragePath(this string input) {
            if (input == "/" || input == "\\") input = string.Empty;
            if (input.StartsWith("/")) input = input.Substring(1);
            return input;
        }

        private static async Task<FileStorageSummary> StoreFileAsync(MultipartSection section, VaultRequestWrapper wrapper) {
            var fileSection = section.AsFileSection();
            if (fileSection != null) {
                //If we are dealing with file, then we need a valid storage service.
                if (wrapper.Service == null) throw new ArgumentNullException(nameof(VaultRequestWrapper.Service));

                FileStorageSummary saveSummary = new FileStorageSummary() { Status = false};
                if (!wrapper.RepoMode) {
                    ObjectWriteRequest input = new ObjectWriteRequest();
                    var vaultWrite = (wrapper.Request as VaultWrite);
                    if (vaultWrite == null) throw new ArgumentNullException($@"For non-repo mode, Wrapper needs a valid {nameof(VaultWrite)} object");
                    vaultWrite.MapProperties(input);
                    //wrapper.MapProperties(input); //Fill rootdir and other basic properties
                    input.Id = fileSection.Name;
                    input.RawName = fileSection.FileName;

                    //PREFERENCE : if the file name generator is present, then it means, we need to override the default mechanism.
                    if (wrapper.FileNameGenerator != null) {
                        input.Name = wrapper.FileNameGenerator.Invoke((input.Id, input.RawName, wrapper.Request));
                    }
                    //return new FileStorageSummary() { Status = false };
                    saveSummary = await wrapper.Service!.Upload(input, fileSection.FileStream, wrapper.BufferSize);
                } else {
                    var repoWrite = (wrapper.Request as RepoWrite);
                    if (repoWrite == null) throw new ArgumentNullException($@"For repo mode, Wrapper needs a valid {nameof(RepoWrite)} object");
                    //Upload to repository mode.
                    RepoStorageRequest rinput = new RepoStorageRequest();
                    rinput.RepoInfo.Container = repoWrite.RootDir;
                    rinput.RepoInfo.ObjectSavedName = repoWrite.RepoName; //Repository Target Name
                    rinput.ResolveMode = repoWrite.ResolveMode;
                    rinput.Path = fileSection.Name; //this is path.
                    rinput.Path = rinput.Path.SanitizeStoragePath();
                    rinput.Name = fileSection.FileName; //this is the file to save.
                    
                    saveSummary = await wrapper.Service!.UploadToRepo(rinput, fileSection.FileStream, wrapper.BufferSize);
                }
                if (string.IsNullOrWhiteSpace(saveSummary.ObjectRawName)) {
                    saveSummary.ObjectRawName = fileSection.FileName;
                }
                return saveSummary;
            }
            return null; //don't return a value.
        }
    }
}