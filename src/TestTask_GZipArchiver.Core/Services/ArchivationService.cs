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
    // Methods CompressFile(string, string) and DecompressFile(string, string) are almost similar
    // excepting one line, but I'm not sure abut should them be united somehow
    public class ArchivationService : IArchivationService
    {
        private int _processorsNumber;
        private ApplicationSettings _settings;
        private string _instanceId;
        private Semaphore _semaphore;
        private object _locker = new object();

        public ArchivationService()
        {
            _processorsNumber = Environment.ProcessorCount;
            _settings = ApplicationSettings.Current;
            _instanceId = Guid.NewGuid().ToString("N");
            _semaphore = new Semaphore(_processorsNumber, _processorsNumber, _instanceId);
        }

        public void CompressFile(string input, string output)
        {
            var inputFileStream =
                new FileStream
                    (input,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None,
                    4096,
                    FileOptions.Asynchronous);

            var outputFileStream =
                new FileStream
                    (output,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.Asynchronous);

            var blocksCount = inputFileStream.Length / _settings.BlockSize + 1;

            for (int i = 0; i < blocksCount; i++)
            {
                var blockNumber = i;

                var thread = new Thread(() =>
                {
                    _semaphore.WaitOne();

                    // TODO SOLVE SYNCHRONIZATION ISSUE
                    using (var compressedBlock = CompressBlock(inputFileStream, blockNumber))
                    {
                        WriteBlock(compressedBlock, outputFileStream, blockNumber);
                    }

                    _semaphore.Release();
                });

                thread.Start();
            }

            inputFileStream.Dispose();
            outputFileStream.Dispose();
        }

        public void DecompressFile(string input, string output)
        {
            var inputFileStream = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.None,
                4096, FileOptions.Asynchronous);

            var outputFileStream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous);

            var blocksCount = inputFileStream.Length / _settings.BlockSize + 1;

            for (int i = 0; i < blocksCount; i++)
            {
                var blockNumber = i;

                var thread = new Thread(() =>
                {
                    _semaphore.WaitOne();

                    // TODO SOLVE SYNCHRONIZATION ISSUE
                    using (var decompressedBlock = DecompressBlock(inputFileStream, blockNumber))
                    {
                        WriteBlock(decompressedBlock, outputFileStream, blockNumber);
                    }

                    _semaphore.Release();
                });

                thread.Start();
            }

            inputFileStream.Dispose();
            outputFileStream.Dispose();
        }

        private Stream DecompressBlock(FileStream inputFile, int blockNumber)
        {
            var result = new MemoryStream();

            var bytesToReadCount = GetBytesToReadCount(inputFile.Length, inputFile.Position);
            var buffer = new byte[bytesToReadCount];
            inputFile.Read(buffer, 0, bytesToReadCount);

            using (var originalBlockStream = new MemoryStream(buffer))
            {
                using (var gzipStream = new GZipStream(originalBlockStream, CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(result);
                }
            }

            return result;
        }

        private Stream CompressBlock(FileStream inputFile, int blockNumber)
        {
            var result = new MemoryStream();

            var bytesToReadCount = GetBytesToReadCount(inputFile.Length, inputFile.Position);
            var buffer = new byte[bytesToReadCount];
            inputFile.Read(buffer, 0, bytesToReadCount);

            using (var originalBlockStream = new MemoryStream(buffer))
            {
                using (var gzipStream = new GZipStream(originalBlockStream, CompressionMode.Compress))
                {
                    gzipStream.CopyTo(result);
                }
            }

            return result;
        }

        private void WriteBlock(Stream dataBlock, FileStream outputFile, int blockNumber)
        {
            throw new NotImplementedException();
        }

        private int GetBytesToReadCount(long streamLength, long streamPosition)
        {
            // Result would never overflow Int32 value in theory as BlockSize has Int32 type
            var result = (int)(streamLength < streamPosition + _settings.BlockSize
                ? streamLength - streamPosition
                : _settings.BlockSize);

            return result;
        }
    }
}
