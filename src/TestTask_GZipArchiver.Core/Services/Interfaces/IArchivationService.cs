namespace TestTask_GZipArchiver.Core.Services.Interfaces
{
    public interface IArchivationService
    {
        void CompressFile(string input, string output);
        void DecompressFile(string input, string output);
    }
}
