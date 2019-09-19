using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace TestTask_GZipArchiver.Core.Models
{
    // Utility class for GZipBlockStream
    public class GZipBlocksMap
    {
        public int BlockSize { get; }
        public long UnzippedLength { get; }
        public long[] BlocksMap { get; }

        public GZipBlocksMap(string path, int blockSize)
        {
            BlockSize = blockSize;

            var blocksMap = new List<long>();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (var gzips = new GZipStream(fs, CompressionMode.Decompress))
                {
                    var buffer = new byte[blockSize];
                    long read;

                    do
                    {
                        blocksMap.Add(gzips.BaseStream.Position);

                        read = gzips.Read(buffer);
                        UnzippedLength += read;
                    }
                    while (read != 0);
                }
            }

            BlocksMap = blocksMap
                .OrderBy(x => x)
                // The last item is the end of file, it is use less
                .SkipLast(1)
                .ToArray();
        }
    }
}
