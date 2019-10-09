using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using TestTask_GZipArchiver.Core.Models;
using TestTask_GZipArchiver.Core.Services;

namespace TestTask_GZipArchiver.Tests
{
    [TestFixture]
    public class ArchivationServiceTests
    {
        private ArchivationService _sut;

        private DirectoryInfo _testFilesDir;
        private FileInfo _fileToCompress;
        private FileInfo _fileToDecompress;

        [SetUp]
        public void Init()
        {
            _sut = new ArchivationService();
            _testFilesDir = new DirectoryInfo("..\\..\\..\\files");

            if (!_testFilesDir.Exists)
            {
                _testFilesDir.Create();
            }

            _fileToCompress = new FileInfo(_testFilesDir.FullName + "\\file_to_compress.pdf");
            _fileToDecompress = new FileInfo(_testFilesDir.FullName + "\\file_to_decompress.pdf.gz");
        }

        [Test]
        public void Should_compress_file()
        {
            var inputFile = _fileToCompress;
            var outputFile = new FileInfo(inputFile.FullName + ".gz");
            
            var args = new[]
            {
                "compress",
                inputFile.FullName,
                outputFile.FullName
            };

            RunningArguments.Set(args);

            _sut.CompressFile(RunningArguments.Current.InputPath, RunningArguments.Current.OutputPath);

            outputFile = new FileInfo(outputFile.FullName);

            Assert.That(outputFile.Exists);
            Assert.That(outputFile.Length < inputFile.Length);

            if (outputFile.Exists)
            {
                outputFile.Delete(); 
            }
        }

        [Test]
        public void Should_decompress_file()
        {
            var inputFile = _fileToDecompress;
            var outputFile = new FileInfo(inputFile.FullName.Remove(inputFile.FullName.Length - 3));
            
            var args = new[]
            {
                "decompress",
                inputFile.FullName,
                outputFile.FullName
            };

            RunningArguments.Set(args);

            _sut.DecompressFile(RunningArguments.Current.InputPath, RunningArguments.Current.OutputPath);

            outputFile = new FileInfo(outputFile.FullName);

            Assert.That(outputFile.Exists);
            Assert.That(outputFile.Length > inputFile.Length);

            if (outputFile.Exists)
            {
                outputFile.Delete();
            }
        }
    }
}