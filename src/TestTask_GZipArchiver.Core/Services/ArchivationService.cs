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

            int blocksCount = (int) (inputFileStream.Length / _settings.BlockSize + 1);

            var queueSynchronizer = new QueueSynchronizer();
            var countdownEvent = new CountdownEvent(blocksCount);

            for (int i = 0; i < blocksCount; i++)
            {
                var blockNumber = i;

                var thread = new Thread(() =>
                {
                    _semaphore.WaitOne();

                    using (var compressedBlock = CompressBlock(inputFileStream))
                    {
                        queueSynchronizer.GetInQueue(blockNumber);

                        WriteBlock(compressedBlock, outputFileStream);

                        queueSynchronizer.LeaveQueue();

                        countdownEvent.Signal();
                    }

                    _semaphore.Release();
                });

                thread.Start();
            }

            countdownEvent.Wait();

            inputFileStream.Dispose();
            outputFileStream.Dispose();
        }

        public void DecompressFile(string input, string output)
        {
            var inputFileStream = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.None,
                4096, FileOptions.Asynchronous);

            var outputFileStream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous);

            int blocksCount = (int) (inputFileStream.Length / _settings.BlockSize + 1);

            var queueSynchronizer = new QueueSynchronizer();
            var countdownEvent = new CountdownEvent(blocksCount);

            for (int i = 0; i < blocksCount; i++)
            {
                var blockNumber = i;

                var thread = new Thread(() =>
                {
                    _semaphore.WaitOne();

                    using (var decompressedBlock = DecompressBlock(inputFileStream))
                    {
                        queueSynchronizer.GetInQueue(blockNumber);

                        WriteBlock(decompressedBlock, outputFileStream);

                        queueSynchronizer.LeaveQueue();

                        countdownEvent.Signal();
                    }

                    _semaphore.Release();
                });

                thread.Start();
            }


            countdownEvent.Wait();

            inputFileStream.Dispose();
            outputFileStream.Dispose();
        }

        private MemoryStream DecompressBlock(FileStream inputFile)
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

        private MemoryStream CompressBlock(FileStream inputFile)
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

        private void WriteBlock(MemoryStream dataBlock, FileStream outputFile)
        {
            // Length of dataBlock would never overflow Int32 value in theory as BlockSize has Int32 type
            outputFile.Write(dataBlock.ToArray(), 0, (int) dataBlock.Length);
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
