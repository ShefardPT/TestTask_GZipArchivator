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

        private object _locker = new object();

        public int BlockSize { get; private set; }

        public byte[] GetBytesBlock()
        {
            byte[] data = new byte[BlockSize];
            var readCount = this.Read(data);

            if (readCount < BlockSize)
            {
                var buffer = new byte[readCount];
                Array.Copy(data, buffer, readCount);
                data = buffer;
            }

            return data;
        }

        private void InitBlockStream(int blockSize)
        {
            BlockSize = blockSize;
        }
    }
}
