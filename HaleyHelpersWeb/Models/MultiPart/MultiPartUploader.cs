using Haley.Models;
using Microsoft.AspNetCore.WebUtilities;

//using System.Net.Http.Headers;
using Haley.Abstractions;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Features;
using Azure.Core;
using System.Text;
using Haley.Utils;
using Microsoft.Extensions.Primitives;

namespace Haley.Models {
    public class MultiPartUploader
    {
        private readonly FormOptions _defaultFormOptions = new FormOptions();

        Func<IObjectUploadRequest, Task<ObjectCreateResponse>> _fileHandler;
        Func<Dictionary<string,StringValues>, Task<bool>> _dataHandler; 

        public MultiPartUploader(Func<IObjectUploadRequest, Task<ObjectCreateResponse>> fileSectionHandler, Func<Dictionary<string, StringValues>, Task<bool>> dataSectionHandler) {
            _fileHandler = fileSectionHandler;
            _dataHandler = dataSectionHandler;
        }

        public async Task<MultipartUploadSummary> UploadFileAsync(HttpRequest request, IObjectUploadRequest upRequest)
        {
            return await UploadFileAsync(request.Body, request.ContentType, upRequest);
        }

        public async Task<MultipartUploadSummary> UploadFileAsync(Stream fileStream, string contentType, IObjectUploadRequest upRequest)
        {
            if (!IsMultipartContentType(contentType))
            {
                throw new Exception($"Expected a multipart request, but got {contentType}");
            }

            if (upRequest == null) throw new Exception("Object upload request cannot be empty");

            if (upRequest.BufferSize < 1024) upRequest.BufferSize = 1024;

            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(contentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
            var multipartReader = new MultipartReader(boundary, fileStream, upRequest.BufferSize);
            var section = await multipartReader.ReadNextSectionAsync();

            var formAccumulator = new KeyValueAccumulator();
            MultipartUploadSummary result = new MultipartUploadSummary();
            long sizeUploadedInBytes = 0;

            while (section != null)
            {
                var hasDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                if (hasDispositionHeader && contentDisposition != null)
                {
                    //Check if it is file disposition or data disposition
                    if (HasFileContentDisposition(contentDisposition))
                    {
                        if (_fileHandler == null) throw new ArgumentException("Multipart form has File content. FileSectionHandler is mandatory.");

                        var fsection = section.AsFileSection();
                        if (fsection != null) {
                            //DO NOT SEND SAME REQUEST AGAIN AND AGAIN. 
                            // CLONE AND SEND.
                            var reqClone = upRequest.Clone() as IObjectUploadRequest;
                            if (reqClone == null) throw new ArgumentException($@"Unable to successfully clone the {nameof(IObjectUploadRequest)} object.");
                            //We fill the input request object.
                            //PathMaker is reference type.
                            reqClone.FileStream = fsection.FileStream;
                            reqClone.ObjectRawName = fsection.FileName;
                            reqClone.ObjectId = fsection.Name;

                            var saveSummary = await _fileHandler(reqClone);
                            if (saveSummary != null && saveSummary.Status) {
                                result.Passed++;
                                sizeUploadedInBytes += saveSummary.Size;
                                result.PassedObjects.Add(saveSummary);
                            } else {
                                result.Failed++;
                                result.FailedObjects.Add(saveSummary);
                            }
                        }
                    }
                    else if (HasDataContentDisposition(contentDisposition))
                    {
                        if (_fileHandler == null) {
                            throw new ArgumentException("Multipart form has data content. DataSectionHandler is mandatory.");
                        }
                        //handle the normal content data.
                        var formParam = await FormParameterHandlerInternal(section, contentDisposition);
                        if (!string.IsNullOrWhiteSpace(formParam.key))
                        {
                            formAccumulator.Append(formParam.key, formParam.value);
                        }
                    }
                }
                section = await multipartReader.ReadNextSectionAsync();
            }
            if (formAccumulator.KeyCount < 1) result.Status = true; //need not worry about data handling.
            if (formAccumulator.KeyCount > 0)
            {
                result.Status = await _dataHandler.Invoke(formAccumulator.GetResults());
            }
            result.TotalSizeUploaded = sizeUploadedInBytes.ToFileSize(false);
            return result;
        }

        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec says 70 characters is a reasonable limit.
        string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary);
            if (string.IsNullOrWhiteSpace(boundary.Value))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            if (boundary.Length > lengthLimit)
            {
                throw new InvalidDataException(
                    $"Multipart boundary length limit {lengthLimit} exceeded.");
            }

            return boundary.Value;
        }

        Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }

        async Task<(string key, string value)> FormParameterHandlerInternal(MultipartSection section, ContentDispositionHeaderValue contentDisposition)
        {
            string key = string.Empty, value = string.Empty;
            // Don't limit the key name length because the
            // multipart headers length limit is already in effect.
            if (contentDisposition.Name == null) return (key, value);
            key = HeaderUtilities.RemoveQuotes(contentDisposition.Name!).Value;
            var encoding = GetEncoding(section);
            if (encoding == null)
            {
                throw new ArgumentException($@"Unable to fetch the encoding for the parameter {key}");
            }

            using (var streamReader = new StreamReader(section.Body, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                // The value length limit is enforced by
                // MultipartBodyLengthLimit
                value = await streamReader.ReadToEndAsync();

                if (string.Equals(value, "undefined",
                    StringComparison.OrdinalIgnoreCase))
                {
                    value = string.Empty; //In case we are receiving input from
                }
                return (key, value);
            }
        }

        bool HasDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="key";
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && string.IsNullOrEmpty(contentDisposition.FileName.Value)
                   && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
        }

        bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                       || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }

        bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

    }
}