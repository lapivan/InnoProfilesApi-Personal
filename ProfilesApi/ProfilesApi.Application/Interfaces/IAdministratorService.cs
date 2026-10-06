using ProfilesApi.Application.Dto.Administrators;
using ProfilesApi.Application.Dto.Shared;

namespace ProfilesApi.Application.Interfaces;

public interface IAdministratorService
{
    Task<AdministratorDto> CreateAdministratorAsync(CreateAdministratorDto createAdministratorDto, Guid createdBy, CancellationToken ct = default);

    Task DeleteAdministratorAsync(Guid id, CancellationToken ct = default);

    Task<AdministratorDto> EditAdministratorProfileAsync(EditAdministratorProfileDto editAdministratorProfileDto, Guid editedById, CancellationToken ct = default);

    Task<AdministratorDto> GetAdministratorAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<AdministratorDto>> GetAdministratorsAsync(SearchFilteredAdministratorListDto filteredAdministratorListDto, CancellationToken ct = default);
    Task<PagedResult<AdministratorDto>> GetAdministratorsPagedAsync(SearchPagedAdministratorDto searchPagedAdministratorDto, CancellationToken ct = default);
    Task<AdministratorDto> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);
}