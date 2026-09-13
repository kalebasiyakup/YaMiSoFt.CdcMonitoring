using Microsoft.JSInterop;

namespace CdcMonitoring.Web.Services;

/// <summary>
/// UTC zaman damgalarını tarayıcının (kullanıcının makinesinin) saat dilimine çevirir.
/// Circuit başladığında sunucu yerel saatiyle başlar; ilk render sonrası JS interop ile
/// tarayıcının IANA saat dilimi kimliği alınıp <see cref="Changed"/> tetiklenir, sayfalar
/// bunu dinleyip görüntülenen saatleri günceller. UI dili (tr-TR/en-US) seçimi bu değeri
/// etkilemez — kültür yalnızca biçimi belirler, saat dilimini değil.
/// </summary>
public sealed class ClientTimeZoneService
{
    private bool _initializing;

    public TimeZoneInfo TimeZone { get; private set; } = TimeZoneInfo.Local;

    public bool IsResolved { get; private set; }

    public event Action? Changed;

    public async Task EnsureInitializedAsync(IJSRuntime js)
    {
        if (IsResolved || _initializing)
        {
            return;
        }

        _initializing = true;
        try
        {
            await using var module = await js.InvokeAsync<IJSObjectReference>("import", "./js/timezone.js");
            var ianaId = await module.InvokeAsync<string>("getTimeZoneId");
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(ianaId);
        }
        catch
        {
            // Tarayıcı saat dilimi alınamazsa sunucu yerel saatinde kalınır.
        }
        finally
        {
            IsResolved = true;
            _initializing = false;
            Changed?.Invoke();
        }
    }

    public DateTimeOffset ToLocal(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, TimeZone);
}
