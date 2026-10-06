using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using ProfilesApi.Application.Dto.Offices;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Application.Services;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Exceptions;
using Xunit;

namespace ProfilesApi.UnitTests.Services;

public class OfficeServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<OfficeService>> _loggerMock;
    private readonly OfficeService _officeService;

    public OfficeServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork> { DefaultValue = DefaultValue.Mock };
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<OfficeService>>();

        _officeService = new OfficeService(_mapperMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
    }

    private static T CreateEntity<T>() where T : class
    {
        return (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
    }

    [Fact]
    public async Task CreateOfficeAsync_Success_ReturnsOfficeDto()
    {
        var dto = new CreateOfficeDto { Address = "Test", PhoneNumber = "123" };
        var office = CreateEntity<Office>();
        office.Id = Guid.NewGuid();
        var officeDto = new OfficeDto { Id = office.Id, Address = "Test" };

        _mapperMock.Setup(m => m.Map<Office>(dto)).Returns(office);
        _unitOfWorkMock.Setup(u => u.Offices.ExistsAsync(It.IsAny<Expression<Func<Office, bool>>>(), default)).ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<OfficeDto>(office)).Returns(officeDto);

        var result = await _officeService.CreateOfficeAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(office.Id, result.Id);
        _unitOfWorkMock.Verify(u => u.Offices.Add(office), Times.Once);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateOfficeAsync_AlreadyExists_ThrowsConflictException()
    {
        var dto = new CreateOfficeDto { Address = "Test", PhoneNumber = "123" };
        _mapperMock.Setup(m => m.Map<Office>(dto)).Returns(CreateEntity<Office>());
        _unitOfWorkMock.Setup(u => u.Offices.ExistsAsync(It.IsAny<Expression<Func<Office, bool>>>(), default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _officeService.CreateOfficeAsync(dto));
    }

    [Fact]
    public async Task GetOfficeByIdAsync_Success_ReturnsOfficeDto()
    {
        var id = Guid.NewGuid();
        var office = CreateEntity<Office>();
        office.Id = id;
        var officeDto = new OfficeDto { Id = id };

        _unitOfWorkMock.Setup(u => u.Offices.GetByIdAsync(id, default)).ReturnsAsync(office);
        _mapperMock.Setup(m => m.Map<OfficeDto>(office)).Returns(officeDto);

        var result = await _officeService.GetOfficeByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetOfficeByIdAsync_NotFound_ThrowsConflictException()
    {
        _unitOfWorkMock.Setup(u => u.Offices.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Office?)null);

        await Assert.ThrowsAsync<ConflictException>(() => _officeService.GetOfficeByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteOfficeAsync_Success_DeletesOffice()
    {
        var id = Guid.NewGuid();
        var office = CreateEntity<Office>();
        office.Id = id;

        _unitOfWorkMock.Setup(u => u.Offices.GetByIdAsync(id, default)).ReturnsAsync(office);
        _unitOfWorkMock.Setup(u => u.Doctors.ExistsAsync(It.IsAny<Expression<Func<Doctor, bool>>>(), default)).ReturnsAsync(false);
        _unitOfWorkMock.Setup(u => u.Administrators.ExistsAsync(It.IsAny<Expression<Func<Administrator, bool>>>(), default)).ReturnsAsync(false);

        await _officeService.DeleteOfficeAsync(id);

        _unitOfWorkMock.Verify(u => u.Offices.Delete(office), Times.Once);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteOfficeAsync_NotFound_ThrowsNotFoundException()
    {
        _unitOfWorkMock.Setup(u => u.Offices.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Office?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _officeService.DeleteOfficeAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteOfficeAsync_HasAssignedDoctors_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var office = CreateEntity<Office>();
        office.Id = id;

        _unitOfWorkMock.Setup(u => u.Offices.GetByIdAsync(id, default)).ReturnsAsync(office);
        _unitOfWorkMock.Setup(u => u.Doctors.ExistsAsync(It.IsAny<Expression<Func<Doctor, bool>>>(), default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _officeService.DeleteOfficeAsync(id));
    }

    [Fact]
    public async Task DeleteOfficeAsync_HasAssignedAdmins_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var office = CreateEntity<Office>();
        office.Id = id;

        _unitOfWorkMock.Setup(u => u.Offices.GetByIdAsync(id, default)).ReturnsAsync(office);
        _unitOfWorkMock.Setup(u => u.Doctors.ExistsAsync(It.IsAny<Expression<Func<Doctor, bool>>>(), default)).ReturnsAsync(false);
        _unitOfWorkMock.Setup(u => u.Administrators.ExistsAsync(It.IsAny<Expression<Func<Administrator, bool>>>(), default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _officeService.DeleteOfficeAsync(id));
    }
}