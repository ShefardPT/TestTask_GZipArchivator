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
    public class MultiCoreArchivationServiceTests
    {
        private MultiCoreArchivationService _sut;

        [SetUp]
        public void Init()
        {
            _sut = new MultiCoreArchivationService();
        }

        [Test]
        public void Should_copy_file()
        {
            var inputFile = new FileInfo("..\\..\\..\\files\\file_to_compress.pdf");
            var outputFile = new FileInfo("..\\..\\..\\files\\file_to_compress1.pdf");

            using (var inputFS = inputFile.OpenRead())
            {
                using (var outputFS = outputFile.OpenWrite())
                {
                    var bytesCount = 1024;
                    var data = new byte[bytesCount];

                    while (inputFS.Read(data) > 0)
                    {
                        if (inputFS.Position > inputFS.Length - bytesCount)
                        {

                        }

                        outputFS.Write(data);
                    }
                }
            }

            Assert.Pass();
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