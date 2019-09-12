using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using TestTask_GZipArchiver.Core.Models.Interfaces;

namespace TestTask_GZipArchiver.Core.Models
{
    public class GZipBlockStream : GZipStream, IBlockStream
    {
        public GZipBlockStream(Stream stream, CompressionLevel compressionLevel, GZipBlocksMap gZipBlocksMap)
            : base(stream, compressionLevel)
        {
            InitBlockStream(gZipBlocksMap);
        }

        public GZipBlockStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen, GZipBlocksMap gZipBlocksMap)
            : base(stream, compressionLevel, leaveOpen)
        {
            InitBlockStream(gZipBlocksMap);
        }

        public GZipBlockStream(Stream stream, CompressionMode mode, GZipBlocksMap gZipBlocksMap)
            : base(stream, mode)
        {
            InitBlockStream(gZipBlocksMap);
        }

        public GZipBlockStream(Stream stream, CompressionMode mode, bool leaveOpen, GZipBlocksMap gZipBlocksMap)
            : base(stream, mode, leaveOpen)
        {
            InitBlockStream(gZipBlocksMap);
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

        private void InitBlockStream(GZipBlocksMap gZipBlocksMap)
        {
            BlockSize = gZipBlocksMap.BlockSize;
            _lengthOfUnzipped = gZipBlocksMap.UnzippedLength;
            _blocksMap = gZipBlocksMap.BlocksMap;

            // BlockSize of 1Mb size will be sufficient fo files of 2^22 GB
            // So casting exception is unlikely
            BlocksCount = (int)(_lengthOfUnzipped / BlockSize + 1);

            // This field's value will not overflow int as blocksize is int.
            _bytesForLastBlockCount = (int)(_lengthOfUnzipped % BlockSize);
        }
    }
}
