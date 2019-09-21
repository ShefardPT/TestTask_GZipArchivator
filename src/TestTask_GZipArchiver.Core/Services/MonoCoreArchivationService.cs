using System;
using System.Collections.Generic;
using System.Text;
using TestTask_GZipArchiver.Core.Services.Interfaces;

namespace TestTask_GZipArchiver.Core.Services
{
    public class MonoCoreArchivationService : IArchivationService
    {
        public void CompressFile(string inputFile, string outputFile)
        {
            throw new NotImplementedException();
        }

        public void DecompressFile(string inputFile, string outputFile)
        {
            throw new NotImplementedException();
        }
    }
}
