using LumexUI.Motion.Tests.TestHelpers;
using LumexUI.Motion.Types;

using Microsoft.AspNetCore.Components;

namespace LumexUI.Motion.Tests.Components;

public class AnimatePresenceTests : MotionTestContext
{
    private static readonly object EnterProps = new { opacity = 1 };
    private static readonly object ExitProps = new { opacity = 0 };

    // Motion registers itself with the PresenceContext while AnimatePresence renders (a side effect of
    // BuildRenderTree), and AnimatePresence renders the registered children itself. These tests pin that down.

    [Fact]
    public void ShouldRenderItsMotionChildren()
    {
        var cut = Render<PresenceHost>( p => p.Add( h => h.Items, ["a", "b"] ) );

        cut.FindAll( ".item" ).Select( e => e.TextContent ).Should().Equal( "a", "b" );
    }

    [Fact]
    public void ShouldRenderNothingWithoutChildren()
    {
        var cut = Render<PresenceHost>();

        cut.MarkupMatches( string.Empty );
    }

    [Fact]
    public void ShouldAllowExitOnItsChildren()
    {
        var action = () => Render<PresenceHost>( p => p
            .Add( h => h.Items, ["a"] )
            .Add( h => h.Exit, ExitProps ) );

        action.Should().NotThrow();
    }

    [Fact]
    public void ShouldAnimateEnterForEachChild()
    {
        Module.SetupVoid( "motion.animateEnter", _ => true ).SetVoidResult();

        Render<PresenceHost>( p => p
            .Add( h => h.Items, ["a", "b"] )
            .Add( h => h.Enter, EnterProps ) );

        Module.VerifyInvoke( "motion.animateEnter", calledTimes: 2 );
    }

    [Fact]
    public void ShouldRenderAndAnimateAnAddedChild()
    {
        Module.SetupVoid( "motion.animateEnter", _ => true ).SetVoidResult();
        var cut = Render<PresenceHost>( p => p
            .Add( h => h.Items, ["a"] )
            .Add( h => h.Enter, EnterProps ) );

        cut.Render( p => p.Add( h => h.Items, ["a", "b"] ) );

        cut.WaitForAssertion( () =>
            cut.FindAll( ".item" ).Select( e => e.TextContent ).Should().Equal( "a", "b" ) );
        Module.VerifyInvoke( "motion.animateEnter", calledTimes: 2 );
    }

    [Fact]
    public void ShouldKeepARemovedChildUntilItsExitAnimationFinishes()
    {
        var exit = Module.SetupVoid( "motion.animateExit", _ => true );
        var cut = Render<PresenceHost>( p => p
            .Add( h => h.Items, ["a", "b"] )
            .Add( h => h.Exit, ExitProps ) );

        cut.Render( p => p.Add( h => h.Items, ["a"] ) );

        // The exit animation is still running, so "b" is still on the page.
        var invocation = Module.VerifyInvoke( "motion.animateExit" );
        invocation.Arguments[0].Should().BeOfType<ElementReference>();
        invocation.Arguments[1].Should().Be( new MotionProps { Exit = ExitProps } );
        cut.FindAll( ".item" ).Select( e => e.TextContent ).Should().Equal( "a", "b" );

        exit.SetVoidResult();

        cut.WaitForAssertion( () =>
            cut.FindAll( ".item" ).Select( e => e.TextContent ).Should().Equal( "a" ) );
    }

    [Fact]
    public void ShouldRemoveAChildWithoutExitRightAway()
    {
        var cut = Render<PresenceHost>( p => p.Add( h => h.Items, ["a", "b"] ) );

        cut.Render( p => p.Add( h => h.Items, ["a"] ) );

        cut.WaitForAssertion( () =>
            cut.FindAll( ".item" ).Select( e => e.TextContent ).Should().Equal( "a" ) );
        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void ShouldRemoveAllChildrenAfterTheirExitAnimations()
    {
        Module.SetupVoid( "motion.animateExit", _ => true ).SetVoidResult();
        var cut = Render<PresenceHost>( p => p
            .Add( h => h.Items, ["a", "b"] )
            .Add( h => h.Exit, ExitProps ) );

        cut.Render( p => p.Add( h => h.Items, Array.Empty<string>() ) );

        cut.WaitForAssertion( () => cut.FindAll( ".item" ).Should().BeEmpty() );
        Module.VerifyInvoke( "motion.animateExit", calledTimes: 2 );
    }
}
