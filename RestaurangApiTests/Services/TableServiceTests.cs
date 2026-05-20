using Moq;
using ResturangDB_API.Data.Repos.IRepos;
using ResturangDB_API.Models;
using ResturangDB_API.Models.DTOs.Table;
using ResturangDB_API.Services;

namespace RestaurangDB_APITests.Services;

public class TableServiceTests
{
    private readonly Mock<ITableRepo> _tableRepoMock;
    private readonly Mock<IBookingRepo> _bookingRepoMock;
    private readonly TableService _service;

    public TableServiceTests()
    {
        _tableRepoMock = new Mock<ITableRepo>(MockBehavior.Strict);
        _bookingRepoMock = new Mock<IBookingRepo>(MockBehavior.Strict);
        _service = new TableService(_tableRepoMock.Object, _bookingRepoMock.Object);
    }

    [Fact]
    public async Task AddTableAsync_MapsDto_AndCallsRepo()
    {
        var dto = new TableCreateDTO { TableSeats = 4, IsAvailable = true };

        _tableRepoMock.Setup(r => r.AddTableAsync(It.Is<Table>(t =>
            t.TableSeats == dto.TableSeats &&
            t.IsAvailable == dto.IsAvailable))).Returns(Task.CompletedTask);

        await _service.AddTableAsync(dto);

        _tableRepoMock.VerifyAll();
    }

    [Fact]
    public async Task GetTableByIdAsync_NotFound_ReturnsNull()
    {
        _tableRepoMock.Setup(r => r.GetTableByIDAsync(1)).ReturnsAsync((Table?)null);

        var result = await _service.GetTableByIdAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTableAsync_NotFound_ReturnsFalse()
    {
        var dto = new TableUpdateDTO { TableID = 1, TableSeats = 2, IsAvailable = false };
        _tableRepoMock.Setup(r => r.GetTableByIDAsync(1)).ReturnsAsync((Table?)null);

        var result = await _service.UpdateTableAsync(dto);

        Assert.False(result);
        _tableRepoMock.Verify(r => r.UpdateTableAsync(It.IsAny<Table>()), Times.Never);
    }

    [Fact]
    public async Task GetAvailableTablesAsync_OverlappingBooking_MarksBookedTableUnavailable()
    {
        var time = DateTime.UtcNow.AddDays(1);
        var timeEnd = time.AddHours(1);

        var table1 = new Table { TableID = 1, TableSeats = 4, IsAvailable = true };
        var table2 = new Table { TableID = 2, TableSeats = 2, IsAvailable = true };

        var booking = new Booking
        {
            BookingID = 1,
            FK_TableID = table1.TableID,
            Time = time.AddMinutes(30),
            TimeEnd = time.AddMinutes(45),
            Table = table1,
            Name = "Name",
            Email = "Email",
            PhoneNumber = "Phone"
        };

        _tableRepoMock.Setup(r => r.GetAllTablesAsync()).ReturnsAsync(new List<Table> { table1, table2 });
        _bookingRepoMock.Setup(r => r.GetAllBookingsAsync()).ReturnsAsync(new List<Booking> { booking });

        var result = (await _service.GetAvailableTablesAsync(time, timeEnd)).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].TableID);
    }
}
