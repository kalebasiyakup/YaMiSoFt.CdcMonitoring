using CdcMonitoring.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace CdcMonitoring.Infrastructure.Security;

public class DataProtectionSmtpPasswordProtector : ISmtpPasswordProtector
{
    private const string Purpose = "CdcMonitoring.Smtp.Password.v1";
    private readonly IDataProtector _protector;

    public DataProtectionSmtpPasswordProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintextPassword) => _protector.Protect(plaintextPassword);

    public string Unprotect(string encryptedPassword) => _protector.Unprotect(encryptedPassword);
}
