using Microsoft.Extensions.Options;
using ResturangDB_API.Services.IServices;
using System.Text;

namespace ResturangDB_API.Services
{
    public class ImageFileStorageService : IImageFileStorageService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly FileStorageOptions _fileStorageOptions;

        public ImageFileStorageService(
            HttpClient httpClient,
            IWebHostEnvironment webHostEnvironment,
            IOptions<FileStorageOptions> options)
        {
            _httpClient = httpClient;
            _webHostEnvironment = webHostEnvironment;
            _fileStorageOptions = options.Value;
            _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _fileStorageOptions.RequestTimeoutSeconds));
        }

        public bool IsLocalMenuImagePath(string? imagePath)
        {
            return !string.IsNullOrWhiteSpace(imagePath)
                && imagePath.StartsWith("/images/menuitems/", StringComparison.OrdinalIgnoreCase);
        }

        public Task<bool> PublicPathExistsAsync(string publicPath, CancellationToken cancellationToken = default)
        {
            var physicalPath = PublicPathToPhysicalPath(publicPath);
            return Task.FromResult(File.Exists(physicalPath));
        }

        public async Task<string?> DownloadMenuImageAsync(int menuItemId, string menuItemName, string sourceUrl, CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceUri))
            {
                return null;
            }

            var extension = ResolveExtension(sourceUri);
            var fileName = BuildStableFileName(menuItemId, menuItemName, extension);
            var folderPhysicalPath = GetPhysicalFolderPath();
            Directory.CreateDirectory(folderPhysicalPath);

            var physicalPath = Path.Combine(folderPhysicalPath, fileName);
            var publicPath = BuildPublicPath(fileName);

            if (File.Exists(physicalPath))
            {
                return publicPath;
            }

            using var response = await _httpClient.GetAsync(sourceUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var maxBytes = _fileStorageOptions.MaxDownloadBytes;
            await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destinationStream = File.Create(physicalPath);

            var buffer = new byte[81920];
            long totalBytes = 0;
            int bytesRead;
            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                totalBytes += bytesRead;
                if (totalBytes > maxBytes)
                {
                    destinationStream.Close();
                    File.Delete(physicalPath);
                    return null;
                }

                await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            return publicPath;
        }

        private string GetPhysicalFolderPath()
        {
            var webRoot = string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath)
                ? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot")
                : _webHostEnvironment.WebRootPath;

            var normalizedFolder = _fileStorageOptions.MenuImageFolder
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            return Path.Combine(webRoot, normalizedFolder);
        }

        private string PublicPathToPhysicalPath(string publicPath)
        {
            var relativePath = publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var webRoot = string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath)
                ? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot")
                : _webHostEnvironment.WebRootPath;

            return Path.Combine(webRoot, relativePath);
        }

        private string BuildPublicPath(string fileName)
        {
            var normalizedFolder = _fileStorageOptions.MenuImageFolder.Trim('/');
            return $"/{normalizedFolder}/{fileName}";
        }

        private static string ResolveExtension(Uri uri)
        {
            var extension = Path.GetExtension(uri.AbsolutePath);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                return ".jpg";
            }

            return extension.ToLowerInvariant();
        }

        private static string BuildStableFileName(int menuItemId, string menuItemName, string extension)
        {
            var slugBuilder = new StringBuilder();
            foreach (var character in menuItemName.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    slugBuilder.Append(character);
                }
                else if (slugBuilder.Length == 0 || slugBuilder[^1] != '-')
                {
                    slugBuilder.Append('-');
                }
            }

            var slug = slugBuilder.ToString().Trim('-');
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = "menu-item";
            }

            return $"{menuItemId}-{slug}{extension}";
        }
    }
}
