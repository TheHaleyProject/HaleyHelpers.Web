using Haley.Models;
using Microsoft.AspNetCore.WebUtilities;
//using System.Net.Http.Headers;
using Haley.Abstractions;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Features;
using Azure.Core;
using static System.Collections.Specialized.BitVector32;

namespace Haley.Utils {
    public static class MultiPartHelper {
        static readonly FormOptions _defaultFormOptions = new FormOptions();

        public static async Task<StorageOutput> UploadFileAsync(HttpRequest request, MultiPartUploadInput mpuInput) { 
            return await UploadFileAsync(request.Body,request.ContentType,mpuInput);
        }

        public static async Task<StorageOutput> UploadFileAsync(Stream fileStream, string contentType,MultiPartUploadInput mpuInput) {
            if (mpuInput == null) throw new ArgumentException(nameof(MultiPartUploadInput));
            if (mpuInput.StorageService == null) throw new ArgumentNullException(nameof(MultiPartUploadInput.StorageService));

            if (!IsMultipartContentType(contentType)) {
                throw new Exception($"Expected a multipart request, but got {contentType}");
            }

            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(contentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
            var multipartReader = new MultipartReader(boundary, fileStream,mpuInput.BufferSize);
            var section = await multipartReader.ReadNextSectionAsync();

            StorageOutput result = new StorageOutput();
            long sizeUploadedInBytes = 0;

            while (section != null) {

                var hasDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                if (hasDispositionHeader && contentDisposition != null) {
                    //Check if it is file disposition or data disposition
                    if (HasFileContentDisposition(contentDisposition)) {

                        var saveSummary = await StoreFileAsync(section,mpuInput);
                        if (saveSummary == null) continue;
                        if (saveSummary.Status) {
                            result.StoredCount++;
                            sizeUploadedInBytes += saveSummary.Size;
                            result.StoredFilesInfo.TryAdd(saveSummary.FileName, saveSummary); //what if the extensions differ for different files?
                        } else {
                            result.FailedCount++;
                            result.FailedFilesInfo.Add(saveSummary.FileName, saveSummary);
                        }

                    } else if (HasDataContentDisposition(contentDisposition)){

                    }
                }
                section = await multipartReader.ReadNextSectionAsync();
            }
            result.TotalSizeUploaded = sizeUploadedInBytes.ToFileSize(false);
            return result;
        }

        static async Task<FileSaveSummary> StoreFileAsync(MultipartSection section,MultiPartUploadInput mpuInput) {
            var fileSection = section.AsFileSection();
            if (fileSection != null) {
                StorageInput input = new StorageInput() { PreferId = mpuInput.PreferId, FileName = fileSection.FileName };
                if (mpuInput.ParseNameAsId && input.Id < 1) {
                    var fname = Path.GetFileNameWithoutExtension(fileSection.Name);
                    if (long.TryParse(fname, out long fnameId)) {
                        input.Id = fnameId; //id is obtained.
                    }
                }

                //If id is still less than zero and we don't have name
                if (mpuInput.FallBackToName && input.Id < 1) input.PreferId = false; //so, we go ahead and prefer name.

                if ( mpuInput.StorageService == null) throw new ArgumentNullException(nameof(MultiPartUploadInput.StorageService));
                var saveSummary = await mpuInput.StorageService!.Store(input, fileSection.FileStream, mpuInput.BufferSize);
                return saveSummary;
            }
            return null; //don't return a value.
        }

        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec says 70 characters is a reasonable limit.
        public static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit) {
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

        public static bool IsMultipartContentType(string contentType) {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool HasDataContentDisposition(ContentDispositionHeaderValue contentDisposition) {
            // Content-Disposition: form-data; name="key";
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && string.IsNullOrEmpty(contentDisposition.FileName.Value)
                   && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
        }

        public static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition) {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                       || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }

    }
}
