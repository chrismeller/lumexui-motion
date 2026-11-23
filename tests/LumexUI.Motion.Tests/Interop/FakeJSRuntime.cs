using Microsoft.JSInterop;

namespace LumexUI.Motion.Tests.Interop;

/// <summary>
/// Records calls into the JS runtime and hands out a <see cref="FakeJSModule"/> for "import", so the tests can see
/// whether <see cref="MotionJsInterop"/> loaded and disposed its module.
/// </summary>
internal sealed class FakeJSRuntime : IJSRuntime
{
    public FakeJSModule Module { get; } = new();

    public List<(string Identifier, object?[]? Args)> Invocations { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>( string identifier, object?[]? args )
        => InvokeAsync<TValue>( identifier, CancellationToken.None, args );

    public ValueTask<TValue> InvokeAsync<TValue>( string identifier, CancellationToken cancellationToken, object?[]? args )
    {
        Invocations.Add( (identifier, args) );
        return ValueTask.FromResult( (TValue)(object)Module );
    }
}

internal sealed class FakeJSModule : IJSObjectReference
{
    public List<(string Identifier, object?[]? Args)> Invocations { get; } = [];

    public int DisposeCount { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>( string identifier, object?[]? args )
        => InvokeAsync<TValue>( identifier, CancellationToken.None, args );

    public ValueTask<TValue> InvokeAsync<TValue>( string identifier, CancellationToken cancellationToken, object?[]? args )
    {
        Invocations.Add( (identifier, args) );
        return ValueTask.FromResult( default( TValue )! );
    }

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return ValueTask.CompletedTask;
    }
}
