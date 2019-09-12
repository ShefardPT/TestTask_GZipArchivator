using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using TestTask_GZipArchiver.Core.Models;
using TestTask_GZipArchiver.Core.Services;
using TestTask_GZipArchiver.Core.Services.Interfaces;

namespace TestTask_GZipArchiver
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                RunningArguments.Set(args);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            if (RunningArguments.Current.DoShowHelp)
            {
                var helpShower = new HelpShower();
                helpShower.ShowHelp();
            }
            else
            {
                var archivationSrv = new ArchivationService();

                var actionDict = new Dictionary<CompressionMode, Action<string, string>>()
                {
                    { CompressionMode.Decompress, (input, output) => archivationSrv.DecompressFile(input, output) },
                    { CompressionMode.Compress, (input, output) => archivationSrv.CompressFile(input, output) },
                };

                var archiverAction = actionDict[RunningArguments.Current.CompressionMode];

                try
                {
                    archiverAction.Invoke(RunningArguments.Current.InputPath, RunningArguments.Current.OutputPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("The unhandled exception has been thrown. Please create bug ticket on " +
                                      "https://github.com/ShefardPT/TestTask_GZipArchiver/issues");
                }
            }
        }
    }
}
