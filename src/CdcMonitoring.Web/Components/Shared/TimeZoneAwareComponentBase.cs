using CdcMonitoring.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CdcMonitoring.Web.Components.Shared;

/// <summary>
/// UTC zaman damgalarını tarayıcının saat dilimine göre gösteren sayfalar için taban sınıf.
/// İlk render sonrası tarayıcının saat dilimi JS interop ile alınır; sonuç geldiğinde
/// (veya değiştiğinde) sayfa otomatik yeniden render edilir.
/// </summary>
public abstract class TimeZoneAwareComponentBase : ComponentBase, IDisposable
{
    [Inject]
    protected ClientTimeZoneService ClientTimeZone { get; set; } = default!;

    [Inject]
    protected IJSRuntime JsRuntime { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ClientTimeZone.Changed += OnTimeZoneResolved;
            await ClientTimeZone.EnsureInitializedAsync(JsRuntime);
        }
    }

    private void OnTimeZoneResolved() => InvokeAsync(StateHasChanged);

    protected string FormatLocal(DateTimeOffset utc, string format = "g") =>
        ClientTimeZone.ToLocal(utc).ToString(format);

    protected string FormatLocal(DateTimeOffset? utc, string format = "g", string fallback = "-") =>
        utc is null ? fallback : FormatLocal(utc.Value, format);

    public virtual void Dispose() => ClientTimeZone.Changed -= OnTimeZoneResolved;
}
