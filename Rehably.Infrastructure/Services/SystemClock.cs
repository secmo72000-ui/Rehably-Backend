using Rehably.Application.Interfaces;

namespace Rehably.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
