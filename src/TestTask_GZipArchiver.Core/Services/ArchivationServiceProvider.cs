using System;
using System.Collections.Generic;
using System.Text;
using TestTask_GZipArchiver.Core.Models;
using TestTask_GZipArchiver.Core.Services.Interfaces;

namespace TestTask_GZipArchiver.Core.Services
{
    public class ArchivationServiceProvider
    {
        public IArchivationService GetArchivationService()
        {
            if (ApplicationSettings.Current.ThreadsCount == 1)
            {
                return new MonoCoreArchivationService();
            }
            else
            {
                return new MultiCoreArchivationService();
            }
        }
    }
}
