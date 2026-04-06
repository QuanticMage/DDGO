using Microsoft.JSInterop;

public interface ITooltipService : IAsyncDisposable
{
	event Action<string, double, double, double> OnShow;
	event Action OnHide;
	Task InitializeAsync();
}

public sealed class TooltipService : ITooltipService
{
	private readonly IJSRuntime _js;

	// Keep this alive for the life of the app/service
	private DotNetObjectReference<TooltipService>? _dotNetRef;
	private bool _initialized;

	public event Action<string, double, double, double>? OnShow;
	public event Action? OnHide;

	public TooltipService(IJSRuntime js) => _js = js;

	public async Task InitializeAsync()
	{
		if (_initialized) return;
		_initialized = true;

		_dotNetRef = DotNetObjectReference.Create(this);
		try
		{
			await _js.InvokeVoidAsync("setupGlobalTooltips", _dotNetRef);
		}
		catch
		{
			// Allow retry on next render if JS call failed (e.g. slow load, extension interference)
			_initialized = false;
			_dotNetRef?.Dispose();
			_dotNetRef = null;
		}
	}

	[JSInvokable("ShowGlobalTooltip")]
	public void ShowGlobalTooltip(string text, double x, double y, double windowWidth)
		=> OnShow?.Invoke(text, x, y, windowWidth);

	[JSInvokable("HideGlobalTooltip")]
	public void HideGlobalTooltip()
		=> OnHide?.Invoke();

	public ValueTask DisposeAsync()
	{
		_dotNetRef?.Dispose();
		_dotNetRef = null;
		return ValueTask.CompletedTask;
	}
}
