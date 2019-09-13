using System;
using System.Collections.Generic;
using System.Text;
using TestTask_GZipArchiver.Core.Services.Interfaces;

namespace TestTask_GZipArchiver.Core.Services
{
    // Shows help
    public class HelpShower : IHelpShower
    {
        public void ShowHelp()
        {
            var helpTips = new[]
            {
                "Usage: gziparchiver <mode(compress|decompress)> <input_file_path> [output_file_path]",
                "   mode                    Mode of the application. Valid values are \"compress\" and \"decompress\".",
                "   input_file_path         Path to the input file.",
                "   output_file_path        Path to the output file. Optional. It is setting to \"input_file_path.gz\" automatically, if it wasn't set."
            };

            foreach (var tip in helpTips)
            {
                Console.WriteLine(tip);
            }
        }
    }
}
