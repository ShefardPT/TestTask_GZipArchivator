using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using TestTask_GZipArchiver.Core.Models.Interfaces;

namespace TestTask_GZipArchiver.Core.Models
{
    public class GZipBlockStream: GZipStream, IBlockStream
    {
        public GZipBlockStream(Stream stream, CompressionLevel compressionLevel, int blockSize) 
            : base(stream, compressionLevel)
        {
            InitBlockStream(blockSize);
        }

        public GZipBlockStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen, int blockSize) 
            : base(stream, compressionLevel, leaveOpen)
        {
            InitBlockStream(blockSize);
        }

        public GZipBlockStream(Stream stream, CompressionMode mode, int blockSize) 
            : base(stream, mode)
        {
            InitBlockStream(blockSize);
        }

        public GZipBlockStream(Stream stream, CompressionMode mode, bool leaveOpen, int blockSize) 
            : base(stream, mode, leaveOpen)
        {
            InitBlockStream(blockSize);
        }

        private int _bytesForLastBlockCount;
        private object _locker = new object();

        public int BlockSize { get; private set; }
        public long BlocksCount { get; private set; }

        public byte[] GetBlockBytes(int blockNumber)
        {
            var blockSize = BlocksCount == blockNumber
                ? _bytesForLastBlockCount
                : BlockSize;

            byte[] result = new byte[blockSize];

            var pos = BlockSize * blockNumber;

            lock (_locker)
            {
                this.BaseStream.Seek(pos, SeekOrigin.Begin);
                this.Read(result, 0, blockSize);
            }

            return result;
        }

        private void InitBlockStream(int blockSize)
        {
            BlockSize = blockSize;
            BlocksCount = this.BaseStream.Length / BlockSize + 1;

            // This field's value will not overflow int as blocksize is int.
            _bytesForLastBlockCount = (int)(this.BaseStream.Length % BlockSize);
        }
    }
}
