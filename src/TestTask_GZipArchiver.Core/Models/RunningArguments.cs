using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        }

        public static RunningArguments Current
        {
            get
            {
                if (!_isInitialized)
                {
                    _instance = new RunningArguments();
                    _isInitialized = true;
                }

                return _instance;
            }
        }

        public static void Bind(string[] args)
        {
            throw new NotImplementedException();
        }
    }
}
