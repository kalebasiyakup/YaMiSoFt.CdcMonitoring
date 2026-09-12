using CdcMonitoring.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace CdcMonitoring.Infrastructure.Security;

public class DataProtectionPasswordProtector : IConnectionPasswordProtector
{
    private const string Purpose = "CdcMonitoring.PgConnection.Password.v1";
    private readonly IDataProtector _protector;

    public DataProtectionPasswordProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintextPassword) => _protector.Protect(plaintextPassword);

    public string Unprotect(string encryptedPassword) => _protector.Unprotect(encryptedPassword);
}
