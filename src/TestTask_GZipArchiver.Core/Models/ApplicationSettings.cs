using System;
using System.Collections.Generic;
using System.Text;

namespace TestTask_GZipArchiver.Core.Models
{
    public class ApplicationSettings
    {
        private static ApplicationSettings _instance = new ApplicationSettings();
        private static bool _isInitialized;

        public int BlockSize { get; private set; }

        private ApplicationSettings()
        {
            BlockSize = 1 * 1024 * 1024 * 4;
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
