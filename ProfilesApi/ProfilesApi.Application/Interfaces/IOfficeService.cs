using ProfilesApi.Application.Dto.Offices;
using ProfilesApi.Application.Dto.Shared;

namespace ProfilesApi.Application.Interfaces;

public interface IOfficeService
{
    Task<OfficeDto> CreateOfficeAsync(CreateOfficeDto createOfficeDto, CancellationToken ct = default);

    Task<IEnumerable<OfficeDto>> GetOfficeListAsync(SearchQueryDto? searchQueryDto = null, CancellationToken ct = default);
    Task<PagedResult<OfficeDto>> GetOfficeListPagedAsync(SearchPagedOfficeDto pagedOfficeDto, CancellationToken ct = default);
    Task<OfficeDto> GetOfficeByIdAsync(Guid id, CancellationToken ct = default);

    Task DeleteOfficeAsync(Guid id, CancellationToken ct = default);

    Task EditOfficeAsync(EditOfficeInformationDto editOfficeInformationDto, CancellationToken ct = default);
}