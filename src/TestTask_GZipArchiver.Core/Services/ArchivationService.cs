﻿using System;
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
        private ValidationService _validationSrv;

        public ArchivationService()
        {
            _settings = ApplicationSettings.Current;
            _instanceId = Guid.NewGuid().ToString("N");
            _validationSrv = new ValidationService();
        }

        // Compresses any non-gzip file to GZip archive
        public void CompressFile(string input, string output)
        {
            var inputFileCheck = _validationSrv.IsFileTTGZipArchive(input);
            if (inputFileCheck.IsValid)
            {
                throw new ArgumentException("The specified input file is TTGZip archive already.");
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

                    // That is needed to decompression and validation
                    // TODO Headers processing may be possibly improved
                    int blocksCount = (int)(inputFS.Length / inputFS.BlockSize + 1);
                    var header = new byte[4 + 4 + 8 * blocksCount];
                    var blocksMap = new List<long>();

                    outputFS.Write(header);

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
                                    using (var gzips = new GZipStream(ms, CompressionMode.Compress, true))
                                    {
                                        gzips.Write(dataBlock.Data);
                                    }

                                    queueSync.GetInQueue(dataBlock.BlockNumber);
                                    outputFS.Write(ms.ToArray());
                                    blocksMap.Add(ms.Length);
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

                    // TODO Headers processing may be possibly improved
                    outputFS.Seek(0, SeekOrigin.Begin);
                    outputFS.Write(BitConverter.GetBytes(_settings.TTGZipFormatSignature));
                    outputFS.Write(BitConverter.GetBytes(blocksCount));

                    for (int i = 0; i < blocksMap.Count; i++)
                    {
                        outputFS.Write(BitConverter.GetBytes(blocksMap[i]));
                    }

                    workIsDoneCountdown.Dispose();
                    readLocker.Dispose();
                    queueSync.Dispose();
                }
            }
        }

        // Decompress GZip file
        public void DecompressFile(string input, string output)
        {
            var inputFileCheck = _validationSrv.IsFileTTGZipArchive(input);
            if (!inputFileCheck.IsValid)
            {
                throw new ArgumentException("The specified input file is not TTGZip archive.");
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

                    // TODO Headers processing may be possibly improved
                    var blocksCountHeader = new byte[4];
                    inputFS.Seek(4, SeekOrigin.Begin);
                    inputFS.Read(blocksCountHeader);
                    var blocksCount = BitConverter.ToInt32(blocksCountHeader);

                    var blocksMap = new long[blocksCount];
                    for (int i = 0; i < blocksCount; i++)
                    {
                        var blockSizeHeader = new byte[8];
                        inputFS.Read(blockSizeHeader);
                        blocksMap[i] = BitConverter.ToInt64(blockSizeHeader);
                    }

                    var threadPool = new Thread[_settings.ThreadsCount];

                    for (int i = 0; i < threadPool.Length; i++)
                    {
                        threadPool[i] = new Thread(() =>
                        {
                            while (!fileIsRead)
                            {
                                readLocker.WaitOne();

                                DataBlock dataBlock;

                                if (posCounter < blocksCount)
                                {
                                    var data = new byte[blocksMap[posCounter]];
                                    inputFS.Read(data);
                                    dataBlock = new DataBlock(data, inputFS.Position, posCounter);
                                    posCounter++;
                                }
                                else
                                {
                                    fileIsRead = true;
                                    break;
                                }

                                readLocker.Set();

                                // TODO Think about could decompressedMS be replaced by bytes array
                                using (var decompressedDataMS = new MemoryStream())
                                {
                                    using (var compressedDataMS = new MemoryStream(dataBlock.Data))
                                    {
                                        using (var gzips = new GZipStream(compressedDataMS, CompressionMode.Decompress))
                                        {
                                            gzips.CopyTo(decompressedDataMS);
                                        }
                                    }

                                    queueSync.GetInQueue(dataBlock.BlockNumber);
                                    outputFS.Write(decompressedDataMS.ToArray());
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
    }
}
