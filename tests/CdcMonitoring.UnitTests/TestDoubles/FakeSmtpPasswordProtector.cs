using CdcMonitoring.Application.Abstractions;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class FakeSmtpPasswordProtector : ISmtpPasswordProtector
{
    private const string Prefix = "enc:";

    public string Protect(string plaintextPassword) => Prefix + plaintextPassword;

    public string Unprotect(string encryptedPassword) => encryptedPassword.StartsWith(Prefix)
        ? encryptedPassword[Prefix.Length..]
        : throw new InvalidOperationException("Beklenmeyen şifreli parola formatı.");
}
