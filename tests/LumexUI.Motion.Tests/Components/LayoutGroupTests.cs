using LumexUI.Motion.Tests.TestHelpers;

namespace LumexUI.Motion.Tests.Components;

public class LayoutGroupTests : MotionTestContext
{
    [Fact]
    public void ShouldRenderOnlyItsChildContent()
    {
        var cut = Render<LayoutGroup>( p => p
            .Add( g => g.Id, "tabs" )
            .AddChildContent( "<p>Content</p>" ) );

        cut.MarkupMatches( "<p>Content</p>" );
    }

    [Fact]
    public void ShouldPrefixTheLayoutIdOfItsMotionChildrenWithItsId()
    {
        Module.SetupVoid( "motion.animateLayoutId", _ => true ).SetVoidResult();

        Render<LayoutGroup>( p => p
            .Add( g => g.Id, "tabs" )
            .AddChildContent<Motion>( m => m.Add( x => x.LayoutId, "underline" ) ) );

        var invocation = Module.VerifyInvoke( "motion.animateLayoutId" );
        invocation.Arguments[2].Should().Be( "tabs-underline" );
    }

    [Fact]
    public void ShouldKeepTheSameLayoutIdApartInDifferentGroups()
    {
        Module.SetupVoid( "motion.animateLayoutId", _ => true ).SetVoidResult();

        Render<TwoLayoutGroups>();

        Module.Invocations
            .Where( i => i.Identifier == "motion.animateLayoutId" )
            .Select( i => i.Arguments[2] )
            .Should().BeEquivalentTo( ["first-underline", "second-underline"] );
    }
}
