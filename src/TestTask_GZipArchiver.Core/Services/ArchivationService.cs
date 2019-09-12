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
        private class ObjectToProceed
        {
            public FileStream InputFileStream { get; set; }
            public FileStream OutFileStream { get; set; }
            public GZipStream GZipStream { get; set; }
            public int BlockNumber { get; set; }
            public QueueSynchronizer QueueSynchronizer { get; set; }
            public Semaphore Semaphore { get; set; }
            public CountdownEvent CountdownEvent { get; set; }
        }

        private int _processorsNumber;
        private ApplicationSettings _settings;
        private string _instanceId;
        private Semaphore _semaphore;
        private object _locker = new object();

        public ArchivationService()
        {
            _processorsNumber = Environment.ProcessorCount;
            //_processorsNumber = 1;
            _settings = ApplicationSettings.Current;
            _instanceId = Guid.NewGuid().ToString("N");
            _semaphore = new Semaphore(_processorsNumber, _processorsNumber, _instanceId);
        }

        public void CompressFile(string input, string output)
        {
            var inputFileStream = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.None);
            var outputFileStream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var gzipStream = new GZipStream(outputFileStream, CompressionMode.Compress);

            int blocksCount = (int)(inputFileStream.Length / _settings.BlockSize + 1);

            var queueSynchronizer = new QueueSynchronizer();
            var countdownEvent = new CountdownEvent(blocksCount);

            for (int i = 0; i < blocksCount; i++)
            {
                var blockNumber = i;

                var obj = new ObjectToProceed()
                {
                    InputFileStream = inputFileStream,
                    OutFileStream = outputFileStream,
                    GZipStream = gzipStream,
                    BlockNumber = blockNumber,
                    QueueSynchronizer = queueSynchronizer,
                    Semaphore = _semaphore,
                    CountdownEvent = countdownEvent
                };

                var thread = new Thread((data) => CompressBlock((ObjectToProceed) data));
                //{
                //    _semaphore.WaitOne();
                    
                //    var streamPos = (long)_settings.BlockSize * blockNumber;
                //    inputFileStream.Seek(streamPos, SeekOrigin.Begin);

                //    var bytesToReadCount = GetBytesToReadCount(inputFileStream.Length, inputFileStream.Position);
                //    var dataBlock = new byte[bytesToReadCount];
                //    inputFileStream.Read(dataBlock);

                //    queueSynchronizer.GetInQueue(blockNumber);

                //    gzipStream.Write(dataBlock);

                //    queueSynchronizer.LeaveQueue();
                //    countdownEvent.Signal();
                //    _semaphore.Release();
                //});

                thread.Start(obj);
            }

            countdownEvent.Wait();

            gzipStream.Dispose();
            outputFileStream.Dispose();
            inputFileStream.Dispose();
        }

        private void CompressBlock(ObjectToProceed obj)
        {
             obj.Semaphore.WaitOne();

            var streamPos = (long)_settings.BlockSize * obj.BlockNumber;
            var bytesToReadCount = GetBytesToReadCount(obj.InputFileStream.Length, obj.InputFileStream.Position);

            byte[] dataBlock;

            lock (_locker)
            {
                obj.InputFileStream.Seek(streamPos, SeekOrigin.Begin);
                dataBlock = new byte[bytesToReadCount];
            }

            obj.InputFileStream.Read(dataBlock);

            obj.QueueSynchronizer.GetInQueue(obj.BlockNumber);

            obj.GZipStream.Write(dataBlock);

            obj.QueueSynchronizer.LeaveQueue();
            obj.CountdownEvent.Signal();
            _semaphore.Release();
        }

        public void DecompressFile(string input, string output)
        {
            var inputFileStream = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.None);
            var outputFileStream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var gzipStream = new GZipStream(inputFileStream, CompressionMode.Decompress);
            
            int blocksCount = (int)(inputFileStream.Length / _settings.BlockSize + 1);

            var queueSynchronizer = new QueueSynchronizer();
            var countdownEvent = new CountdownEvent(blocksCount);

            for (int i = 0; i < blocksCount; i++)
            {
                var blockNumber = i;

                var thread = new Thread(() =>
                {
                    
                    var dataBlock = GetDataBlock(inputFileStream, blockNumber);

                    queueSynchronizer.GetInQueue(blockNumber);

                        //WriteBlock(decompressedBlock, outputFileStream);

                        queueSynchronizer.LeaveQueue();

                        countdownEvent.Signal();
                  

                    _semaphore.Release();
                });

                thread.Start();
            }


            countdownEvent.Wait();

            gzipStream.Dispose();
            outputFileStream.Dispose();
            inputFileStream.Dispose();
        }

        private byte[] GetDataBlock(FileStream fileStream, int blockNumber)
        {
            var streamPos = (long)_settings.BlockSize * blockNumber;

            fileStream.Seek(streamPos, SeekOrigin.Begin);

            var bytesToReadCount = GetBytesToReadCount(fileStream.Length, fileStream.Position);
            var buffer = new byte[bytesToReadCount];

            fileStream.Read(buffer);

            return buffer;
        }

        private byte[] DecompressBlock(FileStream inputFile)
        {
            var result = new MemoryStream();

            var bytesToReadCount = GetBytesToReadCount(inputFile.Length, inputFile.Position);
            var buffer = new byte[bytesToReadCount];
            inputFile.Read(buffer, 0, bytesToReadCount);

            using (var originalBlockStream = new MemoryStream(buffer))
            {
                using (var decompressedData = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(originalBlockStream, CompressionMode.Decompress))
                    {
                        gzipStream.CopyTo(decompressedData);
                        decompressedData.Seek(0, SeekOrigin.Begin);
                        decompressedData.CopyTo(result);
                    }
                }
            }

            return result.ToArray();
        }

        private byte[] CompressBlock(FileStream inputFile)
        {
            var result = new MemoryStream();

            var bytesToReadCount = GetBytesToReadCount(inputFile.Length, inputFile.Position);
            var buffer = new byte[bytesToReadCount];
            inputFile.Read(buffer, 0, bytesToReadCount);

            using (var originalBlockStream = new MemoryStream(buffer))
            {
                using (var compressedData = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(compressedData, CompressionMode.Compress))
                    {
                        originalBlockStream.CopyTo(gzipStream);
                        compressedData.Seek(0, SeekOrigin.Begin);
                        compressedData.CopyTo(result);
                    }
                }
            }

            result.Seek(0, SeekOrigin.Begin);

            return result.ToArray();
        }

        private void WriteBlock(MemoryStream dataBlock, FileStream outputFile)
        {
            // Length of dataBlock would never overflow Int32 value in theory as BlockSize has Int32 type
            outputFile.Write(dataBlock.ToArray(), 0, (int)dataBlock.Length);
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
