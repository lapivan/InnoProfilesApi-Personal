using ProfilesApi.Domain.Entities;
using ProfilesApi.Infrastructure.Repositories;
using Xunit;

namespace ProfilesApi.IntegrationTests;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

[Collection("Database collection")]
public class RepositoryIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public RepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.DbContext.ChangeTracker.Clear();
    }

    private Account CreateValidAccount()
    {
        var account = DatabaseFixture.CreateEntity<Account>();
        account.Id = Guid.NewGuid();
        account.Firstname = "John";
        account.Lastname = "Doe";
        account.Email = $"test{Guid.NewGuid()}@domain.com";
        account.PhoneNumber = $"+12345{Guid.NewGuid().ToString().Substring(0, 5)}";
        account.PasswordHash = "hash";
        return account;
    }

    private Office CreateValidOffice()
    {
        var office = DatabaseFixture.CreateEntity<Office>();
        office.Id = Guid.NewGuid();
        // office.Address = $"Wall Street {Guid.NewGuid().ToString().Substring(0, 5)}";
        office.PhoneNumber = $"+12345{Guid.NewGuid().ToString().Substring(0, 5)}";
        return office;
    }

    [Fact]
    public async Task Add_And_GetByIdAsync_ShouldWriteAndReadFromDatabase()
    {
        var repo = new OfficeRepository(_fixture.DbContext);
        var office = CreateValidOffice();

        repo.Add(office);
        await _fixture.DbContext.SaveChangesAsync();

        var result = await repo.GetByIdAsync(office.Id);

        Assert.NotNull(result);
        Assert.Equal(office.Address, result.Address);
    }

    [Fact]
    public async Task Delete_ShouldRemoveEntityFromDatabase()
    {
        var repo = new OfficeRepository(_fixture.DbContext);
        var office = CreateValidOffice();

        repo.Add(office);
        await _fixture.DbContext.SaveChangesAsync();

        repo.Delete(office);
        await _fixture.DbContext.SaveChangesAsync();

        var result = await repo.GetByIdAsync(office.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueWhenEntityExists()
    {
        var repo = new OfficeRepository(_fixture.DbContext);
        var office = CreateValidOffice();
        office.Address = "Unique Address";

        repo.Add(office);
        await _fixture.DbContext.SaveChangesAsync();

        var exists = await repo.ExistsAsync(o => o.Address == "Unique Address");

        Assert.True(exists);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectPaginationAndTotalCount()
    {
        var repo = new OfficeRepository(_fixture.DbContext);

        for (int i = 0; i < 5; i++)
        {
            var office = CreateValidOffice();
            repo.Add(office);
        }
        await _fixture.DbContext.SaveChangesAsync();

        var (items, totalCount) = await repo.GetPagedAsync(pageNumber: 1, pageSize: 2);

        Assert.Equal(2, items.Count());
        Assert.True(totalCount >= 5);
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnCorrectAccount()
    {
        var repo = new AccountRepository(_fixture.DbContext);
        var account = CreateValidAccount();
        account.Email = "target@domain.com";

        repo.Add(account);
        await _fixture.DbContext.SaveChangesAsync();

        var result = await repo.GetByEmail("target@domain.com");

        Assert.NotNull(result);
        Assert.Equal(account.Id, result.Id);
    }

    [Fact]
    public async Task GetByPhoneNumber_ShouldReturnCorrectAccount()
    {
        var repo = new AccountRepository(_fixture.DbContext);
        var account = CreateValidAccount();
        account.PhoneNumber = "+9999999999";

        repo.Add(account);
        await _fixture.DbContext.SaveChangesAsync();

        var result = await repo.GetByPhoneNumber("+9999999999");

        Assert.NotNull(result);
        Assert.Equal(account.Id, result.Id);
    }

    [Fact]
    public async Task SearchByTerm_ShouldMatchCombinedFirstnameAndLastname()
    {
        var repo = new AccountRepository(_fixture.DbContext);
        var account = CreateValidAccount();
        account.Firstname = "James";
        account.Lastname = "Bond";

        repo.Add(account);
        await _fixture.DbContext.SaveChangesAsync();

        var results = await repo.SearchByTerm("James Bond");

        Assert.Contains(results, a => a.Id == account.Id);
    }

    [Fact]
    public async Task GetByName_ShouldMatchExactFirstOrLastName()
    {
        var repo = new AccountRepository(_fixture.DbContext);
        var account = CreateValidAccount();
        account.Firstname = "Alice";
        account.Lastname = "Smith";

        repo.Add(account);
        await _fixture.DbContext.SaveChangesAsync();

        var results = await repo.GetByName("Smith");

        Assert.Contains(results, a => a.Id == account.Id);
    }

    [Fact]
    public async Task SearchByTerm_Specialization_ShouldReturnMatchesCaseInsensitive()
    {
        var repo = new SpecializationRepository(_fixture.DbContext);
        var spec = DatabaseFixture.CreateEntity<Specialization>();
        spec.Id = Guid.NewGuid();
        spec.Name = "Neurology";

        repo.Add(spec);
        await _fixture.DbContext.SaveChangesAsync();

        var results = await repo.SearchByTerm("neuro");

        Assert.Contains(results, s => s.Id == spec.Id);
    }

    [Fact]
    public async Task GetAllAsync_WithIncludes_ShouldLoadRelatedData()
    {
        var repo = new AccountRepository(_fixture.DbContext);
        var account = CreateValidAccount();

        var photo = DatabaseFixture.CreateEntity<Photo>();
        photo.Id = Guid.NewGuid();
        photo.Url = "http://test.com/photo.png";
        account.Photo = photo;

        repo.Add(account);
        await _fixture.DbContext.SaveChangesAsync();

        var results = await repo.GetAllAsync(
            filter: a => a.Id == account.Id,
            cancellationToken: default,
            includesProperties: a => a.Photo!);

        var loadedAccount = results.First();
        Assert.NotNull(loadedAccount.Photo);
        Assert.Equal("http://test.com/photo.png", loadedAccount.Photo.Url);
    }
}