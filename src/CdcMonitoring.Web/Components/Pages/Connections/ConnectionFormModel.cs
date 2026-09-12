using System.ComponentModel.DataAnnotations;
using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Web.Components.Pages.Connections;

public class ConnectionFormModel
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [MaxLength(200, ErrorMessage = "Ad en fazla 200 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Host zorunludur.")]
    [MaxLength(255, ErrorMessage = "Host en fazla 255 karakter olabilir.")]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Port 1-65535 aralığında olmalıdır.")]
    public int Port { get; set; } = 5432;

    [Required(ErrorMessage = "Veritabanı adı zorunludur.")]
    [MaxLength(200, ErrorMessage = "Veritabanı adı en fazla 200 karakter olabilir.")]
    public string DatabaseName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [MaxLength(200, ErrorMessage = "Kullanıcı adı en fazla 200 karakter olabilir.")]
    public string Username { get; set; } = string.Empty;

    public string? Password { get; set; }

    public PgSslMode SslMode { get; set; } = PgSslMode.Prefer;

    public bool TrustServerCertificate { get; set; }

    [Required(ErrorMessage = "Ortam/DC etiketi zorunludur.")]
    [MaxLength(100, ErrorMessage = "Ortam/DC etiketi en fazla 100 karakter olabilir.")]
    public string EnvironmentTag { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
