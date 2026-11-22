using LumexUI.Motion.Types;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LumexUI.Motion.Tests.Interop;

public class MotionJsInteropTests
{
    private readonly FakeJSRuntime _runtime = new();
    private readonly MotionJsInterop _interop;

    public MotionJsInteropTests()
    {
        _interop = new MotionJsInterop( _runtime );
    }

    [Fact]
    public void ShouldNotImportTheModuleUntilItIsUsed()
    {
        _runtime.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldImportTheModuleOnceFromTheStaticWebAssetPath()
    {
        var props = new MotionProps { Enter = new { opacity = 1 } };

        await _interop.AnimateEnterAsync( default, props );
        await _interop.AnimateExitAsync( default, props );

        _runtime.Invocations.Should().ContainSingle()
            .Which.Should().BeEquivalentTo( ("import", new object?[] { "./_content/LumexUI.Motion/motion.js" }) );
    }

    [Fact]
    public async Task ShouldCallAnimateEnter()
    {
        var element = new ElementReference( "el-1" );
        var props = new MotionProps { Enter = new { opacity = 1 } };

        await _interop.AnimateEnterAsync( element, props );

        _runtime.Module.Invocations.Should().ContainSingle()
            .Which.Should().BeEquivalentTo( ("motion.animateEnter", new object?[] { element, props }) );
    }

    [Fact]
    public async Task ShouldCallAnimateExit()
    {
        var element = new ElementReference( "el-1" );
        var props = new MotionProps { Exit = new { opacity = 0 } };

        await _interop.AnimateExitAsync( element, props );

        _runtime.Module.Invocations.Should().ContainSingle()
            .Which.Should().BeEquivalentTo( ("motion.animateExit", new object?[] { element, props }) );
    }

    [Fact]
    public async Task ShouldCallAnimateLayoutId()
    {
        var element = new ElementReference( "el-1" );

        await _interop.AnimateLayoutIdAsync( element, null, "underline" );

        _runtime.Module.Invocations.Should().ContainSingle()
            .Which.Should().BeEquivalentTo( ("motion.animateLayoutId", new object?[] { element, null, "underline" }) );
    }

    [Fact]
    public async Task ShouldNotImportTheModuleWhenDisposedUnused()
    {
        await _interop.DisposeAsync();

        _runtime.Invocations.Should().BeEmpty();
        _runtime.Module.DisposeCount.Should().Be( 0 );
    }

    [Fact]
    public async Task ShouldDisposeTheModuleWhenDisposed()
    {
        await _interop.AnimateEnterAsync( default, new MotionProps() );

        await _interop.DisposeAsync();

        _runtime.Module.DisposeCount.Should().Be( 1 );
    }

    // On Blazor Server, every JS call throws JSDisconnectedException once the circuit is gone. Exit animations and
    // disposal run during teardown, so they hit it routinely.

    public static TheoryData<string> AnimateMethods => ["enter", "exit", "layoutId"];

    private Task AnimateAsync( string method ) => method switch
    {
        "enter" => _interop.AnimateEnterAsync( default, new MotionProps() ),
        "exit" => _interop.AnimateExitAsync( default, new MotionProps() ),
        "layoutId" => _interop.AnimateLayoutIdAsync( default, null, "underline" ),
        _ => throw new ArgumentOutOfRangeException( nameof( method ) ),
    };

    [Theory]
    [MemberData( nameof( AnimateMethods ) )]
    public async Task ShouldIgnoreADisconnectWhileAnimating( string method )
    {
        _runtime.Module.InvokeException = new JSDisconnectedException( "The circuit is gone." );

        var action = () => AnimateAsync( method );

        await action.Should().NotThrowAsync();
    }

    [Theory]
    [MemberData( nameof( AnimateMethods ) )]
    public async Task ShouldIgnoreADisconnectWhileImportingTheModule( string method )
    {
        _runtime.ImportException = new JSDisconnectedException( "The circuit is gone." );

        var action = () => AnimateAsync( method );

        await action.Should().NotThrowAsync();
    }

    [Theory]
    [MemberData( nameof( AnimateMethods ) )]
    public async Task ShouldStillThrowOtherJSErrorsWhileAnimating( string method )
    {
        _runtime.Module.InvokeException = new JSException( "Something else broke." );

        var action = () => AnimateAsync( method );

        await action.Should().ThrowAsync<JSException>();
    }

    [Fact]
    public async Task ShouldIgnoreADisconnectWhileDisposingTheModule()
    {
        await _interop.AnimateEnterAsync( default, new MotionProps() );
        _runtime.Module.DisposeException = new JSDisconnectedException( "The circuit is gone." );

        var action = async () => await _interop.DisposeAsync();

        await action.Should().NotThrowAsync();
        _runtime.Module.DisposeCount.Should().Be( 1 );
    }

    [Fact]
    public async Task ShouldIgnoreADisconnectedImportWhenDisposing()
    {
        _runtime.ImportException = new JSDisconnectedException( "The circuit is gone." );
        await _interop.AnimateEnterAsync( default, new MotionProps() );

        var action = async () => await _interop.DisposeAsync();

        await action.Should().NotThrowAsync();
        _runtime.Module.DisposeCount.Should().Be( 0 );
    }

    [Fact]
    public async Task ShouldStillThrowOtherJSErrorsWhileDisposing()
    {
        await _interop.AnimateEnterAsync( default, new MotionProps() );
        _runtime.Module.DisposeException = new JSException( "Something else broke." );

        var action = async () => await _interop.DisposeAsync();

        await action.Should().ThrowAsync<JSException>();
    }
}
