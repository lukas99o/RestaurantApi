using Microsoft.EntityFrameworkCore;
using ResturangDB_API.Data;
using ResturangDB_API.Services.IServices;

namespace ResturangDB_API.Services
{
    public class MenuImageBootstrapper : IMenuImageBootstrapper
    {
        private static readonly Dictionary<int, string> SeedImageSources = new()
        {
            { 1, "https://www.recipetineats.com/tachyon/2018/07/Spaghetti-Bolognese.jpg" },
            { 2, "https://eu-central-1.linodeobjects.com/tasteline/2018/06/pizza-margherita-foto-kerstin-eriksson-original-2048x2048.jpg" },
            { 3, "https://cdn.kronfagel.se/app/uploads/2019/07/Caesarsallad.jpg" },
            { 4, "https://www.cookinwithmima.com/wp-content/uploads/2021/06/Grilled-BBQ-Chicken.jpg" },
            { 5, "https://www.allrecipes.com/thmb/_emMPu4gpcuCOoC0kfjRWIdHlmc=/1500x0/filters:no_upscale():max_bytes(150000):strip_icc()/53729-fish-tacos-DDMFS-4x3-b5547c67c6f0432da06ad8f905e82c1e.jpg" },
            { 6, "https://www.barossafinefoods.com.au/glide-cache/containers/main/2020_bff_porkribs_bbq_website-2.jpg/03d880f2ca84b83fdeb147548e7d9b12.jpg" },
            { 7, "https://www.recipetineats.com/tachyon/2018/01/Beef-Stroganoff_2-1-1.jpg" },
            { 8, "https://natashaskitchen.com/wp-content/uploads/2020/08/Vegetable-Stir-Fry-2.jpg" },
            { 9, "https://static01.nyt.com/images/2021/02/14/dining/carbonara-horizontal/carbonara-horizontal-square640-v2.jpg" },
            { 10, "https://thecozycook.com/wp-content/uploads/2022/04/Lasagna-Recipe-f.jpg" }
        };

        private readonly ResturangContext _context;
        private readonly IImageFileStorageService _imageFileStorageService;
        private readonly ILogger<MenuImageBootstrapper> _logger;

        public MenuImageBootstrapper(
            ResturangContext context,
            IImageFileStorageService imageFileStorageService,
            ILogger<MenuImageBootstrapper> logger)
        {
            _context = context;
            _imageFileStorageService = imageFileStorageService;
            _logger = logger;
        }

        public async Task EnsureSeedImagesDownloadedAsync(CancellationToken cancellationToken = default)
        {
            var menuItemIds = SeedImageSources.Keys.ToList();
            var items = await _context.MenuItems
                .Where(item => menuItemIds.Contains(item.MenuItemID))
                .ToListAsync(cancellationToken);

            var hasChanges = false;

            foreach (var menuItemId in menuItemIds)
            {
                var menuItem = items.FirstOrDefault(item => item.MenuItemID == menuItemId);
                if (menuItem == null)
                {
                    _logger.LogWarning("Menu item with id {MenuItemId} was not found during image bootstrap.", menuItemId);
                    continue;
                }

                if (_imageFileStorageService.IsLocalMenuImagePath(menuItem.ImgUrl)
                    && await _imageFileStorageService.PublicPathExistsAsync(menuItem.ImgUrl!, cancellationToken))
                {
                    continue;
                }

                var sourceUrl = ResolveSourceUrl(menuItemId, menuItem.ImgUrl);
                if (string.IsNullOrWhiteSpace(sourceUrl))
                {
                    _logger.LogWarning("No valid source URL available for menu item id {MenuItemId}.", menuItem.MenuItemID);
                    continue;
                }

                try
                {
                    var localPath = await _imageFileStorageService.DownloadMenuImageAsync(
                        menuItem.MenuItemID,
                        menuItem.Name,
                        sourceUrl,
                        cancellationToken);

                    if (string.IsNullOrWhiteSpace(localPath))
                    {
                        _logger.LogWarning("Image download failed for menu item id {MenuItemId}.", menuItem.MenuItemID);
                        continue;
                    }

                    if (!string.Equals(menuItem.ImgUrl, localPath, StringComparison.OrdinalIgnoreCase))
                    {
                        menuItem.ImgUrl = localPath;
                        hasChanges = true;
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Image bootstrap failed for menu item id {MenuItemId}.", menuItem.MenuItemID);
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private static string? ResolveSourceUrl(int menuItemId, string? currentImageUrl)
        {
            if (!string.IsNullOrWhiteSpace(currentImageUrl)
                && currentImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return currentImageUrl;
            }

            return SeedImageSources.GetValueOrDefault(menuItemId);
        }
    }
}
