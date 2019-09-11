using System;
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

            var args = new[]
            {
                "compress",
                inputFile.FullName,
                outputFile.FullName
            };

            RunningArguments.Bind(args);

            _sut.CompressFile(RunningArguments.Current.InputPath, RunningArguments.Current.OutputPath);

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
            var inputFile = new FileInfo("..\\..\\..\\files\\file_to_decompress.pdf.gz");
            var outputFile = new FileInfo("..\\..\\..\\files\\file_to_decompress.pdf");

            var args = new[]
            {
                "decompress",
                inputFile.FullName,
                outputFile.FullName
            };

            RunningArguments.Bind(args);

            _sut.DecompressFile(RunningArguments.Current.InputPath, RunningArguments.Current.OutputPath);

            Assert.That(outputFile.Exists);
            Assert.That(outputFile.Length < inputFile.Length);

            if (outputFile.Exists)
            {
                outputFile.Delete();
            }
        }
    }
}