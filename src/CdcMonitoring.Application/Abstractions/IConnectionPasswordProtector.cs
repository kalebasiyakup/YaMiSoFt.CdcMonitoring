namespace CdcMonitoring.Application.Abstractions;

public interface IConnectionPasswordProtector
{
    string Protect(string plaintextPassword);
    string Unprotect(string encryptedPassword);
}
