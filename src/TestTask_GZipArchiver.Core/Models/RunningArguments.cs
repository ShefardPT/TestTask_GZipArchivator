using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using TestTask_GZipArchiver.Core.Services.Interfaces;

namespace TestTask_GZipArchiver.Core.Models
{
    // Class stores runtime arguments
    public class RunningArguments
    {
        private static RunningArguments _instance = new RunningArguments();
        private static bool _isInitialized;

        public CompressionMode CompressionMode { get; private set; }
        public string InputPath { get; private set; }
        public string OutputPath { get; private set; }
        public bool DoShowHelp { get; private set; }
        
        // Sets runtime arguments from string[]
        // According the specification format is "<appname.exe> compress|decompress <input file path> [output file path]"
        public static void Set(string[] args)
        {
            // Adjustment args to lowercase
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = args[i].ToLowerInvariant();
            }

            var helpFlags = new[]
            {
                "-h",
                "--help"
            };

            var helpFlagsInArgs = args.Join(helpFlags, arg => arg, hf => hf, (arg, hf) => arg);

            if (args.Length == 0 ||
                helpFlagsInArgs.Any())
            {
                _instance = new RunningArguments{ DoShowHelp = true };
                return;
            }

            // Check is user needs help has been passed. The args format is being checked now.
            if (args.Length < 2 || 3 < args.Length)
            {
                throw new ArgumentException
                    ("Input parameters are invalid. Use \"gziparchiver -h\" to get help.");
            }

            var isMode = Enum.TryParse<CompressionMode>(args[0], true, out var mode);

            if (!isMode)
            {
                throw new ArgumentException
                    ("Compression mode was set is invalid. Use \"gziparchiver -h\" to get help.");
            }

            // TODO The question is should be paths checked here or in special class.
            var inputFile = new FileInfo(args[1]);
            if (!inputFile.Exists)
            {
                throw new ArgumentException
                    ($"Specified input file \"{inputFile.FullName}\" wasn't found, " +
                     "please type valid file name. Use \"gziparchiver -h\" to get help.");
            }

            FileInfo outputFile;

            if (args.Length == 3)
            {
                outputFile = new FileInfo(args[2]); 
            }
            else
            {
                outputFile = new FileInfo($"{inputFile.FullName}.gz");
            }

            if (outputFile.Exists)
            {
                throw new ArgumentException
                    ($"Specified output file \"{outputFile.FullName}\" is already existing, " +
                     "please type valid file name. Use \"gziparchiver -h\" to get help.");
            }
            
            _instance = new RunningArguments()
            {
                DoShowHelp = false,
                CompressionMode = mode,
                InputPath = inputFile.FullName,
                OutputPath = outputFile.FullName
            };
        }

        private RunningArguments()
        {
            Init();
        }
        
        public static RunningArguments Current
        {
            get
            {
                if (!_isInitialized)
                {
                    _instance = new RunningArguments();
                }

                return _instance;
            }
        }

        // Sets initialization state to "initialized"
        private static void Init()
        {
            _isInitialized = true;
        }
    }
}
