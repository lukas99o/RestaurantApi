namespace ResturangDB_API.Services.IServices
{
    public interface IImageFileStorageService
    {
        bool IsLocalMenuImagePath(string? imagePath);
        Task<bool> PublicPathExistsAsync(string publicPath, CancellationToken cancellationToken = default);
        Task<string?> DownloadMenuImageAsync(int menuItemId, string menuItemName, string sourceUrl, CancellationToken cancellationToken = default);
    }
}
