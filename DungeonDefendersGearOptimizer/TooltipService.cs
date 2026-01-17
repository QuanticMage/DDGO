using Microsoft.JSInterop;

public interface ITooltipService : IAsyncDisposable
{
	event Action<string, double, double> OnShow;
	event Action OnHide;
	Task InitializeAsync();
}

public sealed class TooltipService : ITooltipService
{
	private readonly IJSRuntime _js;

	// Keep this alive for the life of the app/service
	private DotNetObjectReference<TooltipService>? _dotNetRef;
	private bool _initialized;

	public event Action<string, double, double>? OnShow;
	public event Action? OnHide;

	public TooltipService(IJSRuntime js) => _js = js;

	public async Task InitializeAsync()
	{
		if (_initialized) return;
		_initialized = true;

		_dotNetRef = DotNetObjectReference.Create(this);
		await _js.InvokeVoidAsync("setupGlobalTooltips", _dotNetRef);
	}

	[JSInvokable("ShowGlobalTooltip")]
	public void ShowGlobalTooltip(string text, double x, double y)
		=> OnShow?.Invoke(text, x, y);

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
