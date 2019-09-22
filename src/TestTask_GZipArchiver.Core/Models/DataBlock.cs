using System;
using System.Collections.Generic;
using System.Text;

namespace TestTask_GZipArchiver.Core.Models
{
    public class DataBlock
    {
        public byte[] Data { get; private set; }
        public long StreamPosition { get; private set; }

        public DataBlock(byte[] data, long streamPosition)
        {
            Data = data;
            StreamPosition = streamPosition;
        }
    }
}
