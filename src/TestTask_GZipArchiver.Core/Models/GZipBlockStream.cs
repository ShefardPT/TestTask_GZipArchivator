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
        private long _lengthOfUnzipped;
        private long[] _blocksMap;

        public int BlockSize { get; private set; }
        public int BlocksCount { get; private set; }

        public byte[] GetBlockBytes(int blockNumber)
        {
            var blockSize = BlocksCount == blockNumber + 1
                ? _bytesForLastBlockCount
                : BlockSize;

            byte[] result = new byte[blockSize];

            var pos = _blocksMap[blockNumber];

            lock (_locker)
            {
                this.BaseStream.Seek(pos, SeekOrigin.Begin);
                this.Read(result);
            }

            return result;
        }

        private void InitBlockStream(int blockSize)
        {
            BlockSize = blockSize;

            _lengthOfUnzipped = 0;

            var buffer = new byte[BlockSize];
            var lengthRead = 0;
            var blocksMap = new List<long>() { 0 };

            while ((lengthRead = this.Read(buffer)) > 0)
            {
                blocksMap.Add(this.BaseStream.Position);
                _lengthOfUnzipped += lengthRead;
            }

            this.BaseStream.Seek(0, SeekOrigin.Begin);

            _blocksMap = blocksMap.ToArray();

            BlocksCount = (int) (_lengthOfUnzipped / BlockSize + 1);

            // This field's value will not overflow int as blocksize is int.
            _bytesForLastBlockCount = (int)(_lengthOfUnzipped % BlockSize);
        }
    }
}
