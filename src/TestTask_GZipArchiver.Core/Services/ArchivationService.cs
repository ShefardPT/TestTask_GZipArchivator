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
    public class ArchivationService : IArchivationService
    {
        private ApplicationSettings _settings;
        private string _instanceId;
        private Semaphore _semaphore;
        private ValidationService _validationSrv;

        public ArchivationService()
        {
            _settings = ApplicationSettings.Current;
            _instanceId = Guid.NewGuid().ToString("N");
            _semaphore = new Semaphore(_settings.ThreadsCount, _settings.ThreadsCount, _instanceId);
            _validationSrv = new ValidationService();
        }

        // Compresses any non-gzip file to GZip archive
        public void CompressFile(string input, string output)
        {
            var inputFileCheck = _validationSrv.IsFileGZipArchive(input);
            if (inputFileCheck.IsValid)
            {
                throw new ArgumentException("The specified input file is GZip archive already.");
            }

            var inputFileStream = new FileBlockStream(input, FileMode.Open, FileAccess.Read, FileShare.None, _settings.BlockSize);
            var outputFileStream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var gzipStream = new GZipStream(outputFileStream, CompressionMode.Compress);

            int blocksCount = inputFileStream.BlocksCount;

            var queueSynchronizer = new QueueSynchronizer();
            var countdownEvent = new CountdownEvent(blocksCount);

            for (int i = 0; i < blocksCount; i++)
            {
                var blockNumber = i;

                _semaphore.WaitOne();

                var thread = new Thread(() =>
                {
                    var dataBlock = inputFileStream.GetBlockBytes(blockNumber);

                    queueSynchronizer.GetInQueue(blockNumber);

                    gzipStream.Write(dataBlock);

                    queueSynchronizer.LeaveQueue();
                    _semaphore.Release();
                    countdownEvent.Signal();
                });

                thread.Start();
            }

            countdownEvent.Wait();

            gzipStream.Dispose();
            outputFileStream.Dispose();
            inputFileStream.Dispose();
        }

        // Decompress GZip file
        public void DecompressFile(string input, string output)
        {
            var inputFileCheck = _validationSrv.IsFileGZipArchive(input);
            if (!inputFileCheck.IsValid)
            {
                throw new ArgumentException("The specified input file is not GZip archive.");
            }

            Console.WriteLine("Getting info about the input archive.");

            var blocksMap = new GZipBlocksMap(input, _settings.BlockSize);

            Console.WriteLine($"Info has been read. Size of unzipped file is {blocksMap.UnzippedLength}.");

            var inputFileStream = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read);
            var outputFileStream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);

            var gzipStream = new GZipBlockStream(inputFileStream, CompressionMode.Decompress, blocksMap);

            int blocksCount = gzipStream.BlocksCount;

            Console.WriteLine($"{blocksCount} are awaiting to be proceeded.");

            var queueSynchronizer = new QueueSynchronizer();
            var countdownEvent = new CountdownEvent(blocksCount);

            for (int i = 0; i < blocksCount; i++)
            {
                var blockNumber = i;

                _semaphore.WaitOne();

                var thread = new Thread(() =>
                {
                    var dataBlock = gzipStream.GetBlockBytes(blockNumber);

                    queueSynchronizer.GetInQueue(blockNumber);

                    outputFileStream.Write(dataBlock);

                    queueSynchronizer.LeaveQueue();
                    _semaphore.Release();
                    countdownEvent.Signal();

                    Console.Write($"\r {blockNumber} of {blocksCount} has been proceeded.");
                });

                thread.Start();
            }

            countdownEvent.Wait();

            Console.Write("\n");

            gzipStream.Dispose();
            outputFileStream.Dispose();
            inputFileStream.Dispose();
        }
    }
}
