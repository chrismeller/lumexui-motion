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

    /// <summary>When set, "import" throws this, the way it does once a Blazor Server circuit is gone.</summary>
    public Exception? ImportException { get; set; }

    public ValueTask<TValue> InvokeAsync<TValue>( string identifier, object?[]? args )
        => InvokeAsync<TValue>( identifier, CancellationToken.None, args );

    public ValueTask<TValue> InvokeAsync<TValue>( string identifier, CancellationToken cancellationToken, object?[]? args )
    {
        Invocations.Add( (identifier, args) );
        if( ImportException is not null )
        {
            return ValueTask.FromException<TValue>( ImportException );
        }
        return ValueTask.FromResult( (TValue)(object)Module );
    }
}

internal sealed class FakeJSModule : IJSObjectReference
{
    public List<(string Identifier, object?[]? Args)> Invocations { get; } = [];

    public int DisposeCount { get; private set; }

    /// <summary>When set, calls into the module throw this.</summary>
    public Exception? InvokeException { get; set; }

    /// <summary>When set, disposing the module throws this.</summary>
    public Exception? DisposeException { get; set; }

    public ValueTask<TValue> InvokeAsync<TValue>( string identifier, object?[]? args )
        => InvokeAsync<TValue>( identifier, CancellationToken.None, args );

    public ValueTask<TValue> InvokeAsync<TValue>( string identifier, CancellationToken cancellationToken, object?[]? args )
    {
        Invocations.Add( (identifier, args) );
        if( InvokeException is not null )
        {
            return ValueTask.FromException<TValue>( InvokeException );
        }
        return ValueTask.FromResult( default( TValue )! );
    }

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        if( DisposeException is not null )
        {
            return ValueTask.FromException( DisposeException );
        }
        return ValueTask.CompletedTask;
    }
}
