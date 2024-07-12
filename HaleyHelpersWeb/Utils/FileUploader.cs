using Haley.Models;
using System.IO;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;

namespace Haley.Utils {
    public class FileUploader {
        public async Task<FileUploadSummary> UploadFileAsync(Stream fileStream, string contentType) {
            var fileCount = 0;
            long totalSizeInBytes = 0;
            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(contentType));
            var multipartReader = new MultipartReader(boundary, fileStream);
            var section = await multipartReader.ReadNextSectionAsync();

            var filePaths = new List<string>();
            var notUploadedFiles = new List<string>();

            while (section != null) {
                var fileSection = section.AsFileSection();
                if (fileSection != null) {
                    totalSizeInBytes += await SaveFileAsync(fileSection, filePaths, notUploadedFiles);
                    fileCount++;
                }

                section = await multipartReader.ReadNextSectionAsync();
            }

            return new FileUploadSummary {
                TotalFilesUploaded = fileCount,
                TotalSizeUploaded = ConvertSizeToString(totalSizeInBytes),
                FilePaths = filePaths,
                NotUploadedFiles = notUploadedFiles
            };
        }
    }
}
