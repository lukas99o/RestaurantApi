using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ResturangDB_API.Data;
using ResturangDB_API.Models;
using ResturangDB_API.Services;
using ResturangDB_API.Services.IServices;

namespace RestaurangDB_APITests.Services;

public class MenuImageBootstrapperTests
{
    [Fact]
    public async Task EnsureSeedImagesDownloadedAsync_ExternalImage_UpdatesToLocalPath()
    {
        var context = BuildContext(nameof(EnsureSeedImagesDownloadedAsync_ExternalImage_UpdatesToLocalPath));
        context.MenuItems.Add(new MenuItem
        {
            MenuItemID = 1,
            Name = "Bolognese",
            Price = 100,
            IsAvailable = true,
            FK_MenuID = 1,
            ImgUrl = "https://example.com/image.jpg",
            Description = "desc"
        });
        await context.SaveChangesAsync();

        var storageMock = new Mock<IImageFileStorageService>(MockBehavior.Strict);
        storageMock.Setup(s => s.IsLocalMenuImagePath("https://example.com/image.jpg")).Returns(false);
        storageMock
            .Setup(s => s.DownloadMenuImageAsync(1, "Bolognese", "https://example.com/image.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/images/menuitems/1-bolognese.jpg");

        var loggerMock = new Mock<ILogger<MenuImageBootstrapper>>();
        var sut = new MenuImageBootstrapper(context, storageMock.Object, loggerMock.Object);

        await sut.EnsureSeedImagesDownloadedAsync();

        var updated = await context.MenuItems.SingleAsync(m => m.MenuItemID == 1);
        Assert.Equal("/images/menuitems/1-bolognese.jpg", updated.ImgUrl);
        storageMock.VerifyAll();
    }

    [Fact]
    public async Task EnsureSeedImagesDownloadedAsync_LocalExistingImage_SkipsDownload()
    {
        var context = BuildContext(nameof(EnsureSeedImagesDownloadedAsync_LocalExistingImage_SkipsDownload));
        context.MenuItems.Add(new MenuItem
        {
            MenuItemID = 2,
            Name = "Pizza",
            Price = 120,
            IsAvailable = true,
            FK_MenuID = 1,
            ImgUrl = "/images/menuitems/2-pizza.jpg",
            Description = "desc"
        });
        await context.SaveChangesAsync();

        var storageMock = new Mock<IImageFileStorageService>(MockBehavior.Strict);
        storageMock.Setup(s => s.IsLocalMenuImagePath("/images/menuitems/2-pizza.jpg")).Returns(true);
        storageMock
            .Setup(s => s.PublicPathExistsAsync("/images/menuitems/2-pizza.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<MenuImageBootstrapper>>();
        var sut = new MenuImageBootstrapper(context, storageMock.Object, loggerMock.Object);

        await sut.EnsureSeedImagesDownloadedAsync();

        var unchanged = await context.MenuItems.SingleAsync(m => m.MenuItemID == 2);
        Assert.Equal("/images/menuitems/2-pizza.jpg", unchanged.ImgUrl);
        storageMock.Verify(s => s.DownloadMenuImageAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        storageMock.VerifyAll();
    }

    private static ResturangContext BuildContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ResturangContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ResturangContext(options);
    }
}
