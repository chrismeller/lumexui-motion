using LumexUI.Motion.Extensions;

namespace LumexUI.Motion.Tests.TestHelpers;

/// <summary>
/// A bunit context with <see cref="MotionJsInterop"/> registered the way consumers register it, and the motion JS
/// module set up in strict mode, so an unexpected call into the module fails the test.
/// </summary>
public abstract class MotionTestContext : BunitContext
{
    protected const string ModulePath = "./_content/LumexUI.Motion/motion.js";

    protected MotionTestContext()
    {
        Services.AddLumexMotion();
        Module = JSInterop.SetupModule( ModulePath );
    }

    protected BunitJSModuleInterop Module { get; }
}
