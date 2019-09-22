using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using TestTask_GZipArchiver.Core.Models;
using TestTask_GZipArchiver.Core.Services.Interfaces;

namespace TestTask_GZipArchiver.Core.Services
{
    public class MonoCoreArchivationService : IArchivationService
    {
        private ValidationService _validationSrv;

        public MonoCoreArchivationService()
        {
            _validationSrv = new ValidationService();
        }

        public void CompressFile(string input, string output)
        {
            var inputFileCheck = _validationSrv.IsFileGZipArchive(input);
            if (inputFileCheck.IsValid)
            {
                throw new ArgumentException("The specified input file is GZip archive already.");
            }

            using (var inputFS = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (var outputFS = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (var gzips = new GZipStream(outputFS, CompressionMode.Compress))
                    {
                        inputFS.CopyTo(gzips);
                    }
                }
            }
        }

        public void DecompressFile(string input, string output)
        {
            var inputFileCheck = _validationSrv.IsFileGZipArchive(input);
            if (!inputFileCheck.IsValid)
            {
                throw new ArgumentException("The specified input file is not GZip archive.");
            }

            using (var inputFS = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (var outputFS = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (var gzips = new GZipStream(inputFS, CompressionMode.Decompress))
                    {
                        gzips.CopyTo(outputFS);
                    }
                }
            }
        }
    }
}
