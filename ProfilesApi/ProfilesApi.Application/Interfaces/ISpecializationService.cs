using ProfilesApi.Application.Dto.Shared;
using ProfilesApi.Application.Dto.Specializations;

namespace ProfilesApi.Application.Interfaces;

public interface ISpecializationService
{
    Task<SpecializationDto> CreateSpecializationAsync(CreateSpecializationDto createSpecializationDto, CancellationToken ct = default);

    Task<IEnumerable<SpecializationDto>> GetSpecializationsAsync(SearchQueryDto? searchQueryDto = null, CancellationToken ct = default);
    Task<SpecializationDto> GetSpecializationByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<SpecializationDto>> GetSpecializationsPagedAsync(SearchPagedSpecializationDto searchPagedPatientDto, CancellationToken ct = default);

    Task DeleteSpecializationAsync(Guid id, CancellationToken ct = default);

    Task EditSpecializationAsync(EditSpecializationInformationDto editSpecializationInformationDto, CancellationToken ct = default);
}