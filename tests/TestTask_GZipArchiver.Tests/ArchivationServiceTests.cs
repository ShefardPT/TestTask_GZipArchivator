using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using TestTask_GZipArchiver.Core.Models;
using TestTask_GZipArchiver.Core.Services;

namespace TestTask_GZipArchiver.Tests
{
    [TestFixture]
    public class ArchivationServiceTests
    {
        private ArchivationService _sut;

        [SetUp]
        public void Init()
        {
            _sut = new ArchivationService();
        }

        [Test]
        public void Should_compress_file()
        {
            var inputFile = new FileInfo("..\\..\\..\\files\\file_to_compress.pdf");
            var outputFile = new FileInfo("..\\..\\..\\files\\file_to_compress.pdf.gz");

            if (outputFile.Exists)
            {
                outputFile.Delete();
            }

            var args = new[]
            {
                "compress",
                inputFile.FullName,
                outputFile.FullName
            };

            RunningArguments.Set(args);

            _sut.CompressFile(RunningArguments.Current.InputPath, RunningArguments.Current.OutputPath);

            outputFile = new FileInfo("..\\..\\..\\files\\file_to_compress.pdf.gz");

            Assert.That(outputFile.Exists);
            Assert.That(outputFile.Length < inputFile.Length);
        }

        [Test]
        public void Should_decompress_file()
        {
            var inputFile = new FileInfo("..\\..\\..\\files\\file_to_decompress.pdf.gz");
            var outputFile = new FileInfo("..\\..\\..\\files\\file_to_decompress.pdf");

            if (outputFile.Exists)
            {
                outputFile.Delete();
            }

            var args = new[]
            {
                "decompress",
                inputFile.FullName,
                outputFile.FullName
            };

            RunningArguments.Set(args);

            _sut.DecompressFile(RunningArguments.Current.InputPath, RunningArguments.Current.OutputPath);

            outputFile = new FileInfo("..\\..\\..\\files\\file_to_decompress.pdf");

            Assert.That(outputFile.Exists);
            Assert.That(outputFile.Length > inputFile.Length);
        }
    }
}