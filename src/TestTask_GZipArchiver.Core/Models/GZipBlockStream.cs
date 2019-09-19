using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using TestTask_GZipArchiver.Core.Models.Interfaces;
using System.Linq;

namespace TestTask_GZipArchiver.Core.Models
{
    // IBlockStream implementation above GZipStream class
    public class GZipBlockStream : GZipStream, IGZipBlockStream
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
        private Dictionary<long, int> _blocksMap;

        public int BlockSize { get; private set; }
        public int BlocksCount { get; private set; }

        public DataBlock GetBlockBytes()
        {
            DataBlock result;

            lock (_locker)
            {
                var blockNumber = _blocksMap[this.BaseStream.Position];

                var blockSize = blockNumber == BlocksCount - 1
                    ? _bytesForLastBlockCount
                    : BlockSize;

                result = new DataBlock(new byte[blockSize], blockNumber);

                this.Read(result.Data);
            }

            return result;
        }

        private void InitBlockStream(GZipBlocksMap gZipBlocksMap)
        {
            BlockSize = gZipBlocksMap.BlockSize;
            _lengthOfUnzipped = gZipBlocksMap.UnzippedLength;
            _blocksMap = new Dictionary<long, int>();

            for (int i = 0; i < gZipBlocksMap.BlocksMap.Length; i++)
            {
                _blocksMap.Add(gZipBlocksMap.BlocksMap[i], i);
            }

            // BlockSize of 1Mb size will be sufficient fo files of 2^22 GB
            // So casting exception is unlikely
            BlocksCount = gZipBlocksMap.BlocksMap.Length;

            // This field's value will not overflow int as blocksize is int.
            _bytesForLastBlockCount = (int)(_lengthOfUnzipped % BlockSize);
        }
    }
}
