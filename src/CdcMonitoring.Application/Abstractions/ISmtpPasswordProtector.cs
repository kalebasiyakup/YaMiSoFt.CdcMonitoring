namespace CdcMonitoring.Application.Abstractions;

public interface ISmtpPasswordProtector
{
    string Protect(string plaintextPassword);
    string Unprotect(string encryptedPassword);
}
