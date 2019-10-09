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

                                using (var ms = new MemoryStream())
                                {
                                    using (var gzips = new GZipStream(ms, CompressionMode.Compress))
                                    {
                                        gzips.Write(dataBlock.Data);
                                    }

                                    queueSync.GetInQueue(dataBlock.BlockNumber);
                                    outputFS.Write(ms.ToArray());
                                    Console.Write($"\r{dataBlock.StreamPosition * 100 / inputFS.Length}% were compressed.");
                                    queueSync.LeaveQueue(dataBlock.BlockNumber);
                                }
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
                    readLocker.Dispose();
                    queueSync.Dispose();
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

            using (var inputFS = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.None, _settings.BlockSize))
            {
                using (var outputFS = new FileStream(output, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
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
                                DataBlock dataBlock;

                                using (var gzips = new GZipBlockStream(inputFS, CompressionMode.Decompress, true,
                                    _settings.BlockSize))
                                {
                                    // Protecting concurrency read operation
                                    readLocker.WaitOne();
                                    dataBlock = new DataBlock(gzips.GetBytesBlock(), inputFS.Position, posCounter);
                                    posCounter++;
                                    readLocker.Set();

                                    if (dataBlock.Data.Length < gzips.BlockSize)
                                    {
                                        fileIsRead = true;

                                        if (dataBlock.Data.Length == 0)
                                        {
                                            break;
                                        }
                                    }
                                }

                                queueSync.GetInQueue(dataBlock.BlockNumber);
                                outputFS.Write(dataBlock.Data);
                                Console.Write($"\r{dataBlock.StreamPosition * 100 / inputFS.Length}% were compressed.");
                                queueSync.LeaveQueue(dataBlock.BlockNumber);

                                workIsDoneCountdown.Signal();
                            }
                        });
                    }

                    foreach (var thread in threadPool)
                    {
                        thread.Start();
                    }

                    workIsDoneCountdown.Wait();
                    workIsDoneCountdown.Dispose();
                    readLocker.Dispose();
                    queueSync.Dispose();
                }
            }
        }
    }
}
