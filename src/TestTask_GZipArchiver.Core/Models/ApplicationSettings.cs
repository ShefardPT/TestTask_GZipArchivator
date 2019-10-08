using System;
using System.Collections.Generic;
using System.Text;

namespace TestTask_GZipArchiver.Core.Models
{
    // Class stores application settings
    public class ApplicationSettings
    {
        private static ApplicationSettings _instance = new ApplicationSettings();
        private static bool _isInitialized;

        public int ThreadsCount { get; set; }
        // Size of block lower than 1 MB strictly NOT recommended
        public int BlockSize { get; set; }

        private ApplicationSettings()
        {
            BlockSize = 1024 * 1024 * 16;
            ThreadsCount = Environment.ProcessorCount;
        }

        public static ApplicationSettings Current
        {
            get
            {
                if (!_isInitialized)
                {
                    _instance = new ApplicationSettings();
                    _isInitialized = true;
                }

                return _instance;
            }
        }
    }
}
