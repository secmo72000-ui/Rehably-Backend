using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Enums;

namespace Rehably.Application.Repositories;

public interface IOtpCodeRepository : IRepository<OtpCode>
{
    Task<OtpCode?> GetLatestUnusedAsync(string contact, OtpPurpose purpose);
    Task<OtpCode?> GetLatestAsync(string contact, OtpPurpose purpose);
    Task<IEnumerable<OtpCode>> GetUnusedByContactAsync(string contact, OtpPurpose purpose);
}
