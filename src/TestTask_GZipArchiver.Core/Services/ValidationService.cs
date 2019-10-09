using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TestTask_GZipArchiver.Core.Models;

namespace TestTask_GZipArchiver.Core.Services
{
    public class ValidationService
    {
        // Checks if specified file is GZip archive.
        public ValidationResult IsFileTTGZipArchive(string path)
        {
            var file = new FileInfo(path);

            if (!file.Exists)
            {
                throw new ArgumentException("Specified file does not exists.");
            }

            using (var fs = file.OpenRead())
            {
                // Try to extract ttgzip header
                byte[] signatureHeader = new byte[4];
                try
                {
                    fs.Read(signatureHeader, 0, signatureHeader.Length);
                }
                catch (Exception ex)
                {
                    throw;
                }
                finally
                {
                    fs.Close();
                }

                var signature = BitConverter.ToInt32(signatureHeader);

                if (ApplicationSettings.Current.TTGZipFormatSignature == signature)
                {
                    return new ValidationResult(true);
                }
                else
                {
                    return new ValidationResult(false, "Specified file is not .ttgz archive.");
                }
            }
        }
    }
}
