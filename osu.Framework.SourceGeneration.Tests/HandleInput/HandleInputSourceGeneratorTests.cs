// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = osu.Framework.SourceGeneration.Tests.Verifiers.CSharpSourceGeneratorVerifier<osu.Framework.SourceGeneration.Generators.HandleInput.HandleInputSourceGenerator>;

namespace osu.Framework.SourceGeneration.Tests.HandleInput
{
    public class HandleInputSourceGeneratorTests : AbstractGeneratorTests
    {
        protected override string ResourceNamespace => "HandleInput";

        [Theory]
        [InlineData("HandleMethod")]
        [InlineData("OnMouseMoveMethod")]
        [InlineData("OnHoverMethod")]
        [InlineData("OnHoverLostMethod")]
        [InlineData("OnMouseDownMethod")]
        [InlineData("OnMouseUpMethod")]
        [InlineData("OnClickMethod")]
        [InlineData("OnDoubleClickMethod")]
        [InlineData("OnDragStartMethod")]
        [InlineData("OnDragMethod")]
        [InlineData("OnDragEndMethod")]
        [InlineData("OnScrollMethod")]
        [InlineData("OnFocusMethod")]
        [InlineData("OnFocusLostMethod")]
        [InlineData("OnTouchDownMethod")]
        [InlineData("OnTouchMoveMethod")]
        [InlineData("OnTouchUpMethod")]
        [InlineData("OnTabletPenButtonPressMethod")]
        [InlineData("OnTabletPenButtonReleaseMethod")]
        [InlineData("OnKeyDownMethod")]
        [InlineData("OnKeyUpMethod")]
        [InlineData("OnJoystickPressMethod")]
        [InlineData("OnJoystickReleaseMethod")]
        [InlineData("OnJoystickAxisMoveMethod")]
        [InlineData("OnTabletAuxiliaryButtonPressMethod")]
        [InlineData("OnTabletAuxiliaryButtonReleaseMethod")]
        [InlineData("OnMidiDownMethod")]
        [InlineData("OnMidiUpMethod")]
        [InlineData("HandlePositionalInputProperty")]
        [InlineData("HandleNonPositionalInputProperty")]
        [InlineData("AcceptsFocusProperty")]
        [InlineData("IHasTooltipInterface")]
        [InlineData("IHasCustomTooltipInterface")]
        [InlineData("IHasContextMenuInterface")]
        [InlineData("IHasPopoverInterface")]
        [InlineData("IKeyBindingHandlerInterface")]
        [InlineData("IntermediateNonPartial")]
        public Task Check(string name)
        {
            GetTestSources(name,
                out (string filename, string content)[] commonSourceFiles,
                out (string filename, string content)[] sourceFiles,
                out (string filename, string content)[] commonGeneratedFiles,
                out (string filename, string content)[] generatedFiles
            );

            return VerifyCS.VerifyAsync(commonSourceFiles, sourceFiles, commonGeneratedFiles, generatedFiles);
        }
    }
}
