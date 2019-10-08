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
        private ValidationService _validationSrv;

        public MultiCoreArchivationService()
        {
            _settings = ApplicationSettings.Current;
            _instanceId = Guid.NewGuid().ToString("N");
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

            using (var inputFS = new FileBlockStream(input, FileMode.Open, FileAccess.Read, FileShare.None, _settings.BlockSize))
            {
                using (var outputFS = new FileStream(output, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    using (var gzips = new GZipStream(outputFS, CompressionMode.Compress))
                    {
                        var workIsDoneCountdown = new CountdownEvent(_settings.ThreadsCount);
                        var queueSync = new QueueSynchronizer();
                        var readLocker = new AutoResetEvent(true);
                        var posCounter = 0;
                        var fileIsRead = false;

                        var threadPool = new Thread[_settings.ThreadsCount];

                        for (int i = 0; i < threadPool.Length; i++)
                        {
                            threadPool[i] = new Thread(() =>
                            {
                                while (!fileIsRead)
                                {
                                    // Protecting concurrency read operation
                                    readLocker.WaitOne();
                                    var dataBlock = new DataBlock(inputFS.GetBytesBlock(), inputFS.Position, posCounter);
                                    posCounter++;
                                    readLocker.Set();

                                    if (dataBlock.Data.Length < inputFS.BlockSize)
                                    {
                                        fileIsRead = true;

                                        if (dataBlock.Data.Length == 0)
                                        {
                                            break;
                                        }
                                    }

                                    queueSync.GetInQueue(dataBlock.BlockNumber);
                                    gzips.Write(dataBlock.Data);
                                    Console.Write($"\r{dataBlock.StreamPosition * 100 / inputFS.Length}% were compressed.");
                                    queueSync.LeaveQueue(dataBlock.BlockNumber); 
                                }

                                workIsDoneCountdown.Signal();
                            });
                        }

                        foreach (var thread in threadPool)
                        {
                            thread.Start();
                        }

                        workIsDoneCountdown.Wait();
                        workIsDoneCountdown.Dispose();
                        queueSync.Dispose();
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

            using (var inputFS = new FileStream(input, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                using (var outputFS = new FileStream(output, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    using (var gzips = new GZipBlockStream(inputFS, CompressionMode.Decompress, _settings.BlockSize))
                    {
                        var data = new Queue<DataBlock>();

                        var writeLocker = new AutoResetEvent(false);
                        var readLocker = new AutoResetEvent(true);
                        var workIsDoneLocker = new ManualResetEvent(false);

                        var writingThread = new Thread(() =>
                        {
                            var isLastBlock = false;

                            while (!isLastBlock)
                            {
                                if (data.Count == 0)
                                {
                                    writeLocker.WaitOne();
                                }

                                var dataToWrite = data.Dequeue();
                                writeLocker.Reset();
                                readLocker.Set();

                                outputFS.Write(dataToWrite.Data);

                                Console.Write($"\r{dataToWrite.StreamPosition * 100 / inputFS.Length}% were decompressed.");

                                if (dataToWrite.Data.Length < _settings.BlockSize)
                                {
                                    isLastBlock = true;
                                }
                            }

                            Console.Write("\n");
                            workIsDoneLocker.Set();
                        });

                        while (true)
                        {
                            if (data.Count > 1)
                            {
                                readLocker.WaitOne();
                            }

                            var readData = gzips.GetBytesBlock();

                            if (readData.Length <= 0)
                            {
                                break;
                            }

                            data.Enqueue(new DataBlock(readData, inputFS.Position, 0));
                            readLocker.Reset();
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
    }
}
