using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Console.WriteLine("Program has been started.");

            try
            {
                RunningArguments.Set(args);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            Console.WriteLine("Arguments have been set.");

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

                var watch = new Stopwatch();
                watch.Start();

                try
                {
                    archiverAction.Invoke(RunningArguments.Current.InputPath, RunningArguments.Current.OutputPath);
                }
                catch(ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);

                    var outputFile = new FileInfo(RunningArguments.Current.OutputPath);
                    if (outputFile.Exists)
                    {
                        outputFile.Delete();
                    }

                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("The unhandled exception has been thrown. Please create bug ticket on " +
                                      "https://github.com/ShefardPT/TestTask_GZipArchiver/issues");

                    var outputFile = new FileInfo(RunningArguments.Current.OutputPath);
                    if (outputFile.Exists)
                    {
                        outputFile.Delete();
                    }

                    return;
                }

                watch.Stop();

                Console.WriteLine("Done!");
                Console.WriteLine($"It had taken {watch.ElapsedMilliseconds / 1000}.{watch.ElapsedMilliseconds % 1000} seconds to do job.");
                Console.WriteLine("Press ENTER to finish.");
                Console.ReadLine();
            }
        }
    }
}
