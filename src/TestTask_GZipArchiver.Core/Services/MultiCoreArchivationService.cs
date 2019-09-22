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
                        byte[] readData = new byte[0];
                        byte[] dataToWrite = new byte[0];

                        var writeLocker = new AutoResetEvent(true);
                        var readLocker = new AutoResetEvent(true);
                        var workIsDoneLocker = new ManualResetEvent(false);

                        var writingThread = new Thread(() =>
                        {
                            var isLastBlock = false;
                            long bytesProceeded = 0;

                            while (!isLastBlock)
                            {
                                writeLocker.WaitOne();
                                dataToWrite = (byte[])readData.Clone();
                                bytesProceeded = inputFS.Position;
                                readLocker.Set();
                                gzips.Write(dataToWrite);

                                Console.Write($"\r{bytesProceeded * 100 / inputFS.Length}% were compressed.");

                                if (dataToWrite.Length < inputFS.BlockSize)
                                {
                                    isLastBlock = true;
                                }
                            }

                            Console.Write("\n");
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

            using (var inputFS = new FileStream(input, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                using (var outputFS = new FileStream(output, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    using (var gzips = new GZipBlockStream(inputFS, CompressionMode.Decompress, _settings.BlockSize))
                    {
                        byte[] readData = new byte[0];
                        byte[] dataToWrite = new byte[0];

                        var writeLocker = new AutoResetEvent(true);
                        var readLocker = new AutoResetEvent(true);
                        var workIsDoneLocker = new ManualResetEvent(false);

                        var writingThread = new Thread(() =>
                        {
                            var isLastBlock = false;
                            long bytesProceeded = 0;

                            while (!isLastBlock)
                            {
                                writeLocker.WaitOne();
                                dataToWrite = (byte[])readData.Clone();
                                bytesProceeded = inputFS.Position;
                                readLocker.Set();
                                outputFS.Write(dataToWrite);

                                Console.Write($"\r{bytesProceeded * 100 / inputFS.Length}% were decompressed.");

                                if (dataToWrite.Length < _settings.BlockSize)
                                {
                                    isLastBlock = true;
                                }
                            }

                            Console.Write("\n");
                            workIsDoneLocker.Set();
                        });

                        while (true)
                        {
                            readLocker.WaitOne();
                            readData = gzips.GetBytesBlock();

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
    }
}
