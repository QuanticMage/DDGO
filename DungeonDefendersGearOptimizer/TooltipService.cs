using Microsoft.JSInterop;

public interface ITooltipService
{
	event Action<string, double, double> OnShow;
	event Action OnHide;
	Task InitializeAsync();
}

public class TooltipService : ITooltipService
{
	private readonly IJSRuntime _js;
	public event Action<string, double, double>? OnShow;
	public event Action? OnHide;

	public TooltipService(IJSRuntime js) => _js = js;

	public async Task InitializeAsync()
	{
		// This passes a reference of THIS service to JS so it can call our methods
		var dotNetHelper = DotNetObjectReference.Create(this);
		await _js.InvokeVoidAsync("setupGlobalTooltips", dotNetHelper);
	}

	[JSInvokable("ShowGlobalTooltip")]
	public void Show(string text, double x, double y) => OnShow?.Invoke(text, x, y);

	[JSInvokable("HideGlobalTooltip")]
	public void Hide() => OnHide?.Invoke();
}