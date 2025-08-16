using Azure.Core;
//using System.Net.Http.Headers;
using Haley.Abstractions;
using Haley.Models;
using Haley.Utils;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using static System.Collections.Specialized.BitVector32;

//TRY :

//if (HasFileContentDisposition(cd)) {
//    var fileStream = new MemoryStream();
//    await section.Body.CopyToAsync(fileStream);
//    fileStream.Position = 0;

//    fileSections.Add(new MultipartSection {
//        Body = fileStream,
//        ContentDisposition = section.ContentDisposition,
//        Headers = section.Headers
//    });
//}

//SET TEMP PATH:

//var tempPath = Path.GetTempFileName();
//using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write);
//await section.Body.CopyToAsync(fs);
//fs.Position = 0;
// Store tempPath or wrap it in a stream for later use

//MultipartReader.ReadNextSectionAsync() is forward-only.

//You can process data sections as they arrive.

//For file sections, you can either:

//    Buffer them to memory/disk immediately and store references.

//    Or stream them directly when you reach Phase 2 (if you don’t need to rewind

namespace Haley.Models {
    public class MultiPartUploader
    {
        private readonly FormOptions _defaultFormOptions = new FormOptions();

        Func<IOSSWrite, Dictionary<string,StringValues>?, Task<IOSSResponse>> _fileHandler;
        Func<Dictionary<string,StringValues>, Task<bool>> _dataHandler; 

        public MultiPartUploader(Func<IOSSWrite, Dictionary<string, StringValues>?, Task<IOSSResponse>> fileSectionHandler, Func<Dictionary<string, StringValues>, Task<bool>> dataSectionHandler) {
            _fileHandler = fileSectionHandler;
            _dataHandler = dataSectionHandler;
        }

        public async Task<MultipartUploadSummary> UploadFileAsync(HttpRequest request, IOSSWrite upRequest)
        {
             return await UploadFileAsync(request.Body, request.ContentType, upRequest);
        }

        public async Task<MultipartUploadSummary> UploadFileAsync(Stream stream, string contentType, IOSSWrite upRequest)
        {
            if (!IsMultipartContentType(contentType))
            {
                throw new Exception($"Expected a multipart request, but got {contentType}");
            }

            if (upRequest == null) throw new Exception("Object upload request cannot be empty");

            if (upRequest.BufferSize < 1024) upRequest.BufferSize = 1024;

            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(contentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
            var multipartReader = new MultipartReader(boundary, stream, upRequest.BufferSize);

            var formAccumulator = new KeyValueAccumulator();
            MultipartUploadSummary result = new MultipartUploadSummary();
            long sizeUploadedInBytes = 0;

            var dataSections = new List<MultipartSection>();
            var fileSections = new List<MultipartSection>();

            var section = await multipartReader.ReadNextSectionAsync();

            //Accumulate all sections, so that we can first fetch the information.
            while (section != null) {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition,out var cd) && cd != null) {
                    if (HasDataContentDisposition(cd)) {
                        dataSections.Add(section);
                    }else if (HasFileContentDisposition(cd)) {
                        fileSections.Add(section);
                    }
                }
                section = await multipartReader.ReadNextSectionAsync();
            }

            //PHASE 1 : Handle data content first
            foreach (var dataSection in dataSections) {
                var contentDisposition = ContentDispositionHeaderValue.Parse(dataSection.ContentDisposition);
                //handle the normal content data.
                var formParam = await FormParameterHandlerInternal(dataSection, contentDisposition);
                if (!string.IsNullOrWhiteSpace(formParam.key)) {
                    formAccumulator.Append(formParam.key, formParam.value);
                }
            }

            //PHASE 1-A: INVOKE THE DATA HANDLER FOR PROCESSING.
            if (formAccumulator.KeyCount > 0 && _dataHandler != null) {
                result.Status = await _dataHandler.Invoke(formAccumulator.GetResults());
            } else {
                result.Status = true; // No data to handle
            }

            //PHASE 2 : Handle File Sections

            foreach (var fileSection in fileSections) {
                if (_fileHandler == null) throw new ArgumentException("Multipart form has File content. FileSectionHandler is mandatory.");

                var fsection = fileSection.AsFileSection();
                if (fsection != null) {
                    //DO NOT SEND SAME REQUEST AGAIN AND AGAIN. 
                    // CLONE AND SEND.
                    var reqClone = upRequest.Clone() as IOSSWrite;
                    reqClone?.GenerateCallId(); //We generate a new call id here itself for the request.
                    if (reqClone == null) throw new ArgumentException($@"Unable to successfully clone the {nameof(IOSSWrite)} object.");
                    //We fill the input request object.
                    //PathMaker is reference type.
                    //Lets make a deep clone of the PathMaker as it is not a primitive type and clone might not work properly
                    reqClone.FileStream = fsection.FileStream;
                    reqClone.FileOriginalName = fsection.FileName; //Not sure what to do with this.
                    reqClone.SetTargetName(fsection.Name); //For repo mode, this becomes the path.

                    IOSSResponse saveSummary = new OSSResponse() { Status = false };
                    try {
                        
                        saveSummary = await _fileHandler(reqClone, formAccumulator.KeyCount > 0 ? formAccumulator.GetResults() : null);
                    } catch (Exception ex) {
                        saveSummary.Message = ex.Message;

                    }
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
            try {
                
                string key = string.Empty, value = string.Empty;
                // Don't limit the key name length because the
                // multipart headers length limit is already in effect.
                if (contentDisposition.Name == null) return (key, value);
                key = HeaderUtilities.RemoveQuotes(contentDisposition.Name!).Value;
                var encoding = GetEncoding(section) ?? Encoding.UTF8;
                if (encoding == null) {
                    throw new ArgumentException($@"Unable to fetch the encoding for the parameter {key}");
                }
                Stream source = section.Body;
                //if the stream is already at end, then we buffer it ourselves
                if (section.Body.Position != 0) {
                    if (section.Body.CanSeek) {
                        section.Body.Position = 0; //can seek, so we set it ourselves.
                    } else {
                        source = new MemoryStream();
                        await section.Body.CopyToAsync(source);
                        source.Position = 0;
                    }
                }

                using (var streamReader = new StreamReader(source, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true)) {
                    // The value length limit is enforced by
                    // MultipartBodyLengthLimit
                    value = await streamReader.ReadToEndAsync();

                    if (string.Equals(value, "undefined",
                        StringComparison.OrdinalIgnoreCase)) {
                        value = string.Empty; //In case we are receiving input from
                    }
                    return (key, value);
                }
            } catch (Exception ex) {
                return (null, null);
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