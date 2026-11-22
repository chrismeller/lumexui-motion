using LumexUI.Motion.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace LumexUI.Motion.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLumexMotionShouldRegisterMotionJsInteropAsScoped()
    {
        var services = new ServiceCollection();

        services.AddLumexMotion();

        services.Should().ContainSingle( d => d.ServiceType == typeof( MotionJsInterop ) )
            .Which.Lifetime.Should().Be( ServiceLifetime.Scoped );
    }

    [Fact]
    public async Task AddLumexMotionShouldResolveMotionJsInterop()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>( new Interop.FakeJSRuntime() );
        services.AddLumexMotion();

        // MotionJsInterop is only IAsyncDisposable, so the container has to be disposed asynchronously.
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<MotionJsInterop>().Should().NotBeNull();
    }
}
