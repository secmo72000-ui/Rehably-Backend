using Rehably.Domain.Entities.Identity;

namespace Rehably.Application.Repositories;

public interface IOtpPasswordResetTokenRepository : IRepository<OtpPasswordResetToken>
{
    Task<OtpPasswordResetToken?> GetByTokenHashAsync(string tokenHash);
    Task<OtpPasswordResetToken?> GetByTokenHashWithUserAsync(string tokenHash);
}
