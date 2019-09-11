﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using TestTask_GZipArchiver.Core.Services.Interfaces;

namespace TestTask_GZipArchiver.Core.Models
{
    public class RunningArguments
    {
        private static RunningArguments _instance = new RunningArguments();
        private static bool _isInitialized;

        public CompressionMode CompressionMode { get; private set; }
        public string InputPath { get; private set; }
        public string OutputPath { get; private set; }
        public bool DoShowHelp { get; private set; }

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

        private static void Init()
        {
            _isInitialized = true;
        }

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

            var isMode = Enum.TryParse<CompressionMode>(args[0], true, out var mode);

            if (!isMode)
            {
                throw new ArgumentException
                    ("Compression mode was set is invalid. Use \"gziparchiver -h\" to get help.");
            }

            var inputFile = new FileInfo(args[1]);
            if (!inputFile.Exists)
            {
                throw new ArgumentException
                    ("Specified input file wasn't found, please type valid path. Use \"gziparchiver -h\" to get help.");
            }

            var outputFile = new FileInfo(args[2]);
            if (outputFile.Exists)
            {
                throw new ArgumentException
                    ("Specified output file is already existing, please type valid path. Use \"gziparchiver -h\" to get help.");
            }
            
            _instance = new RunningArguments()
            {
                DoShowHelp = false,
                CompressionMode = mode,
                InputPath = inputFile.FullName,
                OutputPath = outputFile.FullName
            };
        }
    }
}
