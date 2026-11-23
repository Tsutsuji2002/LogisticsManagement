using Logistics.Server.Application.Common.Interfaces;

namespace Logistics.Server.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
