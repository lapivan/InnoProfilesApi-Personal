using ProfilesApi.Application.Dto.Doctors;
using ProfilesApi.Application.Dto.Shared;

namespace ProfilesApi.Application.Interfaces;

public interface IDoctorService
{
    Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto createDoctorDto, Guid? createdById = null, CancellationToken ct = default);

    Task DeleteDoctorAsync(Guid id, CancellationToken ct = default);

    Task<DoctorDto> EditDoctorProfileAsync(EditDoctorProfileDto editDoctorProfileDto, Guid? editdById = null, CancellationToken ct = default);

    Task<DoctorDto> GetDoctorAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<DoctorDto>> GetDoctorsAsync(SearchFilteredDoctorListDto filteredDoctorListDto, CancellationToken ct = default);
    Task<PagedResult<DoctorDto>> GetDoctorsPagedAsync(SearchPagedDoctorDto searchPagedDoctorDto, CancellationToken ct = default);
    Task<DoctorDto> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);
}