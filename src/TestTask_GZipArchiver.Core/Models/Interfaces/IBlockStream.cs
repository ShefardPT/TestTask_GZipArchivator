using System;
using System.Collections.Generic;
using System.Text;

namespace TestTask_GZipArchiver.Core.Models.Interfaces
{
    public interface IBlockStream
    {
        int BlockSize { get; }
        long BlocksCount { get; }

        byte[] GetBlockBytes(int blockNumber);
    }
}
