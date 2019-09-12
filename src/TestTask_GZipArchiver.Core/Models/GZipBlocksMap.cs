using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace TestTask_GZipArchiver.Core.Models
{
    public class GZipBlocksMap
    {
        public int BlockSize { get; }
        public long UnzippedLength { get; }
        public long[] BlocksMap { get; }

        public GZipBlocksMap(string path, int blockSize)
        {
            BlockSize = blockSize;

            var blocksMap = new List<long>() { 0 };

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (var gzips = new GZipStream(fs, CompressionMode.Decompress))
                {
                    var buffer = new byte[blockSize];
                    long read;

                    while ((read = gzips.Read(buffer)) > 0)
                    {
                        UnzippedLength += read;
                        blocksMap.Add(gzips.BaseStream.Position);
                    }
                }
            }

            BlocksMap = blocksMap.ToArray();
        }
    }
}
