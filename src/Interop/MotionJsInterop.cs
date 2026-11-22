using LumexUI.Motion.Types;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LumexUI.Motion;

public class MotionJsInterop : IAsyncDisposable
{
    private const string JavaScriptPrefix = "motion";

    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public MotionJsInterop( IJSRuntime jsRuntime )
    {
        _moduleTask = new( () => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/LumexUI.Motion/motion.js" ).AsTask() );
    }

    public Task AnimateEnterAsync( ElementReference @ref, MotionProps props )
        => InvokeAsync( "animateEnter", @ref, props );

    public Task AnimateExitAsync( ElementReference @ref, MotionProps props )
        => InvokeAsync( "animateExit", @ref, props );

    public Task AnimateLayoutIdAsync( ElementReference @ref, MotionProps? props, string layoutId )
        => InvokeAsync( "animateLayoutId", @ref, props, layoutId );

    public async ValueTask DisposeAsync()
    {
        if( _moduleTask.IsValueCreated )
        {
            try
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
            catch( JSDisconnectedException )
            {
                // The Blazor Server circuit is gone, and the module with it. There's nothing left to dispose.
            }
        }
    }

    private async Task InvokeAsync( string function, params object?[] args )
    {
        try
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync( $"{JavaScriptPrefix}.{function}", args );
        }
        catch( JSDisconnectedException )
        {
            // The Blazor Server circuit is gone, usually because the page is being torn down while an enter or exit
            // animation is starting. There's no page left to animate.
        }
    }
}
