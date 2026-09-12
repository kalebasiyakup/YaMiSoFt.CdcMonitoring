using CdcMonitoring.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CdcMonitoring.Infrastructure.Security;

public class HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public string GetCurrentUserNameOrDefault(string fallback = "system") =>
        httpContextAccessor.HttpContext?.User?.Identity?.Name ?? fallback;
}
