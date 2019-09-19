using System;
using System.Collections.Generic;
using System.Text;

namespace TestTask_GZipArchiver.Core.Models
{
    public class DataBlock
    {
        public byte[] Data { get; private set; }
        public int BlockNumber { get; private set; }

        public DataBlock(byte[] data, int blockNumber)
        {
            Data = data;
            BlockNumber = blockNumber;
        }
    }
}
