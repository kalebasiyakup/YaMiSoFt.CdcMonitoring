using System.ComponentModel.DataAnnotations;

namespace CdcMonitoring.Web.Components.Pages.Connections;

public class ConnectionFormModel
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Host zorunludur.")]
    [MaxLength(255)]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Port 1-65535 aralığında olmalıdır.")]
    public int Port { get; set; } = 5432;

    [Required(ErrorMessage = "Veritabanı adı zorunludur.")]
    [MaxLength(200)]
    public string DatabaseName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [MaxLength(200)]
    public string Username { get; set; } = string.Empty;

    public string? Password { get; set; }

    [Required(ErrorMessage = "Ortam/DC etiketi zorunludur.")]
    [MaxLength(100)]
    public string EnvironmentTag { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
