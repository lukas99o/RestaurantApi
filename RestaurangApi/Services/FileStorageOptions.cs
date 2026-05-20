namespace ResturangDB_API.Services
{
    public class FileStorageOptions
    {
        public const string SectionName = "FileStorage";

        public string MenuImageFolder { get; set; } = "images/menuitems";

        public long MaxDownloadBytes { get; set; } = 5 * 1024 * 1024;

        public int RequestTimeoutSeconds { get; set; } = 20;
    }
}
