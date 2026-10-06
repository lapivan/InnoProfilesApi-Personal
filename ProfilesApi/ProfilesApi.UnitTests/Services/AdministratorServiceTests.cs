using System.Linq.Expressions;
using AutoMapper;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using ProfilesApi.Application.Dto.Administrators;
using ProfilesApi.Application.Interfaces;
using ProfilesApi.Application.Services;
using ProfilesApi.Domain.Entities;
using ProfilesApi.Domain.Exceptions;
using Xunit;

namespace ProfilesApi.UnitTests.Services;

public class AdministratorServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<AdministratorService>> _loggerMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly AdministratorService _adminService;

    public AdministratorServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork> { DefaultValue = DefaultValue.Mock };
        _mapperMock = new Mock<IMapper>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = new Mock<ILogger<AdministratorService>>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();

        _adminService = new AdministratorService(
            _mapperMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object,
            _publishEndpointMock.Object);
    }

    private static T CreateEntity<T>() where T : class
    {
        return (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
    }

    [Fact]
    public async Task CreateAdministratorAsync_EmailExists_ThrowsConflictException()
    {
        var dto = new CreateAdministratorDto { Email = "test@test.com" };

        _unitOfWorkMock.Setup(u => u.Accounts.ExistsAsync(It.IsAny<Expression<Func<Account, bool>>>(), default))
                       .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _adminService.CreateAdministratorAsync(dto, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAdministratorAsync_OfficeNotFound_ThrowsNotFoundException()
    {
        var dto = new CreateAdministratorDto { Email = "new@test.com", PhoneNumber = "123", OfficeId = Guid.NewGuid() };
        _unitOfWorkMock.Setup(u => u.Accounts.ExistsAsync(It.IsAny<Expression<Func<Account, bool>>>(), default)).ReturnsAsync(false);
        _unitOfWorkMock.Setup(u => u.Offices.ExistsAsync(It.IsAny<Expression<Func<Office, bool>>>(), default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _adminService.CreateAdministratorAsync(dto, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAdministratorAsync_NotFound_ThrowsNotFoundException()
    {
        _unitOfWorkMock.Setup(u => u.Administrators.GetWithDetailsAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Administrator?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _adminService.DeleteAdministratorAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAdministratorAsync_LastAdmin_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var admin = CreateEntity<Administrator>();
        admin.Id = id;
        _unitOfWorkMock.Setup(u => u.Administrators.GetWithDetailsAsync(id, default)).ReturnsAsync(admin);
        _unitOfWorkMock.Setup(u => u.Administrators.ExistsAsync(It.IsAny<Expression<Func<Administrator, bool>>>(), default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<ConflictException>(() => _adminService.DeleteAdministratorAsync(id));
    }

    [Fact]
    public async Task DeleteAdministratorAsync_Success_DeletesAdminAndAccount()
    {
        var id = Guid.NewGuid();
        var admin = CreateEntity<Administrator>();
        admin.Id = id;
        admin.Account = CreateEntity<Account>();

        _unitOfWorkMock.Setup(u => u.Administrators.GetWithDetailsAsync(id, default)).ReturnsAsync(admin);
        _unitOfWorkMock.Setup(u => u.Administrators.ExistsAsync(It.IsAny<Expression<Func<Administrator, bool>>>(), default)).ReturnsAsync(true);

        await _adminService.DeleteAdministratorAsync(id);

        _unitOfWorkMock.Verify(u => u.Administrators.Delete(admin), Times.Once);
        _unitOfWorkMock.Verify(u => u.Accounts.Delete(admin.Account), Times.Once);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetAdministratorAsync_NotFound_ThrowsNotFoundException()
    {
        _unitOfWorkMock.Setup(u => u.Administrators.GetWithDetailsAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Administrator?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _adminService.GetAdministratorAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAdministratorAsync_Success_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var admin = CreateEntity<Administrator>();
        admin.Id = id;
        _unitOfWorkMock.Setup(u => u.Administrators.GetWithDetailsAsync(id, default)).ReturnsAsync(admin);
        _mapperMock.Setup(m => m.Map<AdministratorDto>(admin)).Returns(new AdministratorDto { Id = id });

        var result = await _adminService.GetAdministratorAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }
}