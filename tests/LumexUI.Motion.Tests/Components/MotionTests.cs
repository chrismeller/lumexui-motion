using LumexUI.Motion.Tests.TestHelpers;
using LumexUI.Motion.Types;

using Microsoft.AspNetCore.Components;

namespace LumexUI.Motion.Tests.Components;

public class MotionTests : MotionTestContext
{
    [Fact]
    public void ShouldRenderTheElementWithItsAttributesAndContent()
    {
        var cut = Render<Motion>( p => p
            .Add( m => m.As, "section" )
            .AddUnmatched( "class", "box" )
            .AddUnmatched( "data-test", "value" )
            .AddChildContent( "<span>Hello</span>" ) );

        cut.MarkupMatches( """<section class="box" data-test="value"><span>Hello</span></section>""" );
    }

    [Fact]
    public void ShouldRenderADivByDefault()
    {
        var cut = Render<Motion>();

        cut.MarkupMatches( "<div></div>" );
    }

    [Fact]
    public void ShouldThrowWhenExitIsSetOutsideAnimatePresence()
    {
        var action = () => Render<Motion>( p => p.Add( m => m.Exit, new { opacity = 0 } ) );

        action.Should().Throw<InvalidOperationException>()
            .WithMessage( "*must be wrapped within the 'AnimatePresence' component*'Exit'*" );
    }

    [Fact]
    public void ShouldAnimateEnterAfterTheFirstRender()
    {
        var enter = new { opacity = 1 };
        var transition = new { duration = 0.2 };
        Module.SetupVoid( "motion.animateEnter", _ => true ).SetVoidResult();

        Render<Motion>( p => p
            .Add( m => m.Enter, enter )
            .Add( m => m.Transition, transition ) );

        var invocation = Module.VerifyInvoke( "motion.animateEnter" );
        invocation.Arguments.Should().HaveCount( 2 );
        invocation.Arguments[0].Should().BeOfType<ElementReference>()
            .Which.Id.Should().NotBeNullOrEmpty();
        invocation.Arguments[1].Should().Be( new MotionProps { Enter = enter, Transition = transition } );
    }

    [Fact]
    public void ShouldAnimateEnterOnlyOnce()
    {
        Module.SetupVoid( "motion.animateEnter", _ => true ).SetVoidResult();
        var cut = Render<Motion>( p => p.Add( m => m.Enter, new { opacity = 1 } ) );

        cut.Render( p => p.Add( m => m.Enter, new { opacity = 0.5 } ) );

        Module.VerifyInvoke( "motion.animateEnter", calledTimes: 1 );
    }

    [Fact]
    public void ShouldNotCallTheModuleWithoutAnimationProps()
    {
        Render<Motion>( p => p.AddChildContent( "Static" ) );

        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotCallTheModuleWhenOnlyATransitionIsSet()
    {
        Render<Motion>( p => p.Add( m => m.Transition, new { duration = 1 } ) );

        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void ShouldAnimateTheLayoutIdInsteadOfEnter()
    {
        var enter = new { opacity = 1 };
        Module.SetupVoid( "motion.animateLayoutId", _ => true ).SetVoidResult();

        Render<Motion>( p => p
            .Add( m => m.LayoutId, "underline" )
            .Add( m => m.Enter, enter ) );

        var invocation = Module.VerifyInvoke( "motion.animateLayoutId" );
        invocation.Arguments[0].Should().BeOfType<ElementReference>();
        invocation.Arguments[1].Should().Be( new MotionProps { Enter = enter } );
        invocation.Arguments[2].Should().Be( "underline" );
        Module.VerifyNotInvoke( "motion.animateEnter" );
    }

    [Fact]
    public void ShouldPassNullPropsForALayoutIdWithoutAnimationProps()
    {
        Module.SetupVoid( "motion.animateLayoutId", _ => true ).SetVoidResult();

        Render<Motion>( p => p.Add( m => m.LayoutId, "underline" ) );

        var invocation = Module.VerifyInvoke( "motion.animateLayoutId" );
        invocation.Arguments[1].Should().BeNull();
        invocation.Arguments[2].Should().Be( "underline" );
    }
}
