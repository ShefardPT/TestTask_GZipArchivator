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
        public ValidationResult IsFileGZipArchive(string path)
        {
            var file = new FileInfo(path);

            if (!file.Exists)
            {
                throw new ArgumentException("Specified file does not exists.");
            }

            using (var fs = file.OpenRead())
            {
                // Try to extract gzip header
                // More info at http://tools.ietf.org/html/rfc1952
                byte[] header = new byte[3];
                try
                {
                    fs.Read(header, 0, 3);
                }
                catch (Exception ex)
                {
                    throw;
                }
                finally
                {
                    fs.Close();
                }
                
                if (header[0] == 31 && header[1] == 139 && header[2] == 8) //If magic numbers are 31 and 139 and the deflation id is 8 then it's OK
                {
                    return new ValidationResult(true);
                }
                else
                {
                    return new ValidationResult(false, "Specified file is not .gz archive.");
                }
            }
        }
    }
}
