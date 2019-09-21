namespace TestTask_GZipArchiver.Core.Services.Interfaces
{
    public interface IArchivationService
    {
        void CompressFile(string inputFile, string outputFile);
        void DecompressFile(string inputFile, string outputFile);
    }
}
