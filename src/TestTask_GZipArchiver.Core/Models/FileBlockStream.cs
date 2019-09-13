using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;
using TestTask_GZipArchiver.Core.Models.Interfaces;

namespace TestTask_GZipArchiver.Core.Models
{
    // IBlockStream implementation above FileStream class
    public class FileBlockStream : FileStream, IBlockStream
    {
        public FileBlockStream(SafeFileHandle handle, FileAccess access, int blockSize) 
            : base(handle, access)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(SafeFileHandle handle, FileAccess access, int bufferSize, int blockSize) :
            base(handle, access, bufferSize)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync, int blockSize) :
            base(handle, access, bufferSize, isAsync)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(IntPtr handle, FileAccess access, int blockSize) : 
            base(handle, access)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(IntPtr handle, FileAccess access, bool ownsHandle, int blockSize) : 
            base(handle, access, ownsHandle)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize, int blockSize) : 
            base(handle, access, ownsHandle, bufferSize)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize, bool isAsync, int blockSize) :
            base(handle, access, ownsHandle, bufferSize, isAsync)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(string path, FileMode mode, int blockSize) : 
            base(path, mode)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(string path, FileMode mode, FileAccess access, int blockSize) : 
            base(path, mode, access)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(string path, FileMode mode, FileAccess access, FileShare share, int blockSize) : 
            base(path, mode, access, share)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, int blockSize) :
            base(path, mode, access, share, bufferSize)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync, int blockSize) :
            base(path, mode, access, share, bufferSize, useAsync)
        {
            InitBlockStream(blockSize);
        }

        public FileBlockStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, int blockSize) : 
            base(path, mode, access, share, bufferSize, options)
        {
            InitBlockStream(blockSize);
        }

        private int _bytesForLastBlockCount;
        private object _locker = new object();

        public int BlockSize { get; private set; }
        public int BlocksCount { get; private set; }

        public byte[] GetBlockBytes(int blockNumber)
        {
            var blockSize = BlocksCount == blockNumber + 1
                ? _bytesForLastBlockCount
                : BlockSize;

            byte[] result = new byte[blockSize];

            var pos = (long)BlockSize * blockNumber;

            lock (_locker)
            {
                this.Seek(pos, SeekOrigin.Begin);
                this.Read(result, 0, blockSize);
            }

            return result;
        }

        private void InitBlockStream(int blockSize)
        {
            BlockSize = blockSize;
            // BlockSize of 1Mb size will be sufficient fo files of 2^22 GB
            // So casting exception is unlikely
            BlocksCount = (int) (this.Length / BlockSize + 1);

            // This field's value will not overflow int as blocksize is int.
            _bytesForLastBlockCount = (int) (this.Length % BlockSize);
        }
    }
}
