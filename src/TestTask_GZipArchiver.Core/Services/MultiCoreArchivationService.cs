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
    public class MultiCoreArchivationService : IArchivationService
    {
        private ApplicationSettings _settings;
        private string _instanceId;
        private Semaphore _semaphore;
        private ValidationService _validationSrv;

        public MultiCoreArchivationService()
        {
            _settings = ApplicationSettings.Current;
            _instanceId = Guid.NewGuid().ToString("N");
            _semaphore = new Semaphore(_settings.ThreadsCount, _settings.ThreadsCount, _instanceId);
            _validationSrv = new ValidationService();
        }

        ~MultiCoreArchivationService()
        {
            _semaphore.Dispose();
        }

        // Compresses any non-gzip file to GZip archive
        public void CompressFile(string input, string output)
        {
            var inputFileCheck = _validationSrv.IsFileGZipArchive(input);
            if (inputFileCheck.IsValid)
            {
                throw new ArgumentException("The specified input file is GZip archive already.");
            }

            using (var inputFS = new FileBlockStream(input, FileMode.Open, FileAccess.Read, FileShare.None, _settings.BlockSize))
            {
                using (var outputFS = new FileStream(output, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    using (var gzips = new GZipStream(outputFS, CompressionMode.Compress))
                    {
                        byte[] readData = new byte[0];
                        byte[] dataToWrite = new byte[0];

                        var writeLocker = new AutoResetEvent(true);
                        var readLocker = new AutoResetEvent(true);
                        var workIsDoneLocker = new ManualResetEvent(false);

                        var writingThread = new Thread(() =>
                        {
                            var isLastBlock = false;

                            while (!isLastBlock)
                            {
                                writeLocker.WaitOne();
                                dataToWrite = (byte[])readData.Clone();
                                readLocker.Set();
                                gzips.Write(dataToWrite);

                                if (dataToWrite.Length < inputFS.BlockSize)
                                {
                                    isLastBlock = true;
                                }
                            }

                            workIsDoneLocker.Set();
                        });

                        while (true)
                        {
                            readLocker.WaitOne();
                            readData = inputFS.GetBytesBlock();

                            if (readData.Length <= 0)
                            {
                                break;
                            }

                            writeLocker.Set();

                            if (!writingThread.IsAlive)
                            {
                                writingThread.Start();
                            }
                        };

                        workIsDoneLocker.WaitOne();
                        workIsDoneLocker.Dispose();
                        readLocker.Dispose();
                        writeLocker.Dispose();
                    }
                }
            }
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

            Console.WriteLine($"Info has been read. Size of unzipped file is {blocksMap.UnzippedLength} bytes.");

            var inputFileStream = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read);
            var outputFileStream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var gzipStream = new GZipBlockStream(inputFileStream, CompressionMode.Decompress, blocksMap);

            int blocksCount = gzipStream.BlocksCount;

            Console.WriteLine($"{blocksCount} blocks are awaiting to be proceeded.");

            var queueSynchronizer = new QueueSynchronizer();
            var countdownEvent = new CountdownEvent(blocksCount);

            for (int i = 0; i < blocksCount; i++)
            {
                _semaphore.WaitOne();

                var thread = new Thread(() =>
                {
                    var dataBlock = gzipStream.GetBlockBytes();

                    queueSynchronizer.GetInQueue(dataBlock.BlockNumber);

                    outputFileStream.Write(dataBlock.Data);

                    queueSynchronizer.LeaveQueue(dataBlock.BlockNumber);
                    _semaphore.Release();

                    Console.Write($"\r{dataBlock.BlockNumber + 1} of {blocksCount} blocks have been proceeded.");

                    countdownEvent.Signal();
                });

                thread.Start();
            }

            countdownEvent.Wait();

            Console.Write("\n");

            countdownEvent.Dispose();
            queueSynchronizer.Dispose();
            gzipStream.Dispose();
            outputFileStream.Dispose();
            inputFileStream.Dispose();
        }
    }
}
