using LumexUI.Motion.Types;

using Microsoft.AspNetCore.Components;

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
}
