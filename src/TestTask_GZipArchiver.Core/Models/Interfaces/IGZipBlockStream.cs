using System;
using System.Collections.Generic;
using System.Text;

namespace TestTask_GZipArchiver.Core.Models.Interfaces
{
    public interface IGZipBlockStream : IBlockStream
    {
        DataBlock GetBlockBytes();
    }
}
