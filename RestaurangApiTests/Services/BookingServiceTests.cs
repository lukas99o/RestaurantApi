using Moq;
using ResturangDB_API.Data.Repos.IRepos;
using ResturangDB_API.Models;
using ResturangDB_API.Models.DTOs.Booking;
using ResturangDB_API.Services;

namespace RestaurangDB_APITests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepo> _bookingRepoMock;
    private readonly Mock<ITableRepo> _tableRepoMock;
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _bookingRepoMock = new Mock<IBookingRepo>(MockBehavior.Strict);
        _tableRepoMock = new Mock<ITableRepo>(MockBehavior.Strict);
        _service = new BookingService(_bookingRepoMock.Object, _tableRepoMock.Object);
    }

    [Fact]
    public async Task AddBookingAsync_MapsDto_AndCallsRepo()
    {
        var time = DateTime.UtcNow.AddDays(1);
        var timeEnd = time.AddHours(1);

        var dto = new BookingCreateDTO
        {
            TableID = 3,
            Time = time,
            TimeEnd = timeEnd,
            Name = "N",
            Email = "e",
            Phone = "p"
        };

        _tableRepoMock.Setup(r => r.GetTableByIDAsync(dto.TableID)).ReturnsAsync(new Table
        {
            TableID = dto.TableID,
            TableSeats = 4,
            IsAvailable = true
        });

        _bookingRepoMock.Setup(r => r.GetAllBookingsAsync()).ReturnsAsync(new List<Booking>());

        _bookingRepoMock.Setup(r => r.AddBookingAsync(It.Is<Booking>(b =>
            b.FK_TableID == dto.TableID &&
            b.Time == dto.Time &&
            b.TimeEnd == dto.TimeEnd &&
            b.Name == dto.Name &&
            b.Email == dto.Email &&
            b.PhoneNumber == dto.Phone))).Returns(Task.CompletedTask);

        await _service.AddBookingAsync(dto);

        _bookingRepoMock.VerifyAll();
    }

    [Fact]
    public async Task GetBookingByIdAsync_NotFound_ReturnsNull()
    {
        _bookingRepoMock.Setup(r => r.GetBookingByIDAsync(1)).ReturnsAsync((Booking?)null);

        var result = await _service.GetBookingByIdAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateBookingAsync_NotFound_ReturnsFalse()
    {
        var dto = new BookingUpdateDTO { BookingID = 5, TableID = 1, Name = "N", Email = "E", Phone = "P" };
        _bookingRepoMock.Setup(r => r.GetBookingByIDAsync(5)).ReturnsAsync((Booking?)null);

        var result = await _service.UpdateBookingAsync(dto);

        Assert.False(result);
        _bookingRepoMock.Verify(r => r.UpdateBookingAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task DeleteBookingAsync_Found_DeletesAndReturnsTrue()
    {
        var existing = new Booking { BookingID = 7, Name = "Name", Email = "Email", PhoneNumber = "Phone" };
        _bookingRepoMock.Setup(r => r.GetBookingByIDAsync(7)).ReturnsAsync(existing);
        _bookingRepoMock.Setup(r => r.DeleteBookingAsync(existing)).Returns(Task.CompletedTask);

        var result = await _service.DeleteBookingAsync(7);

        Assert.True(result);
        _bookingRepoMock.VerifyAll();
    }
}
