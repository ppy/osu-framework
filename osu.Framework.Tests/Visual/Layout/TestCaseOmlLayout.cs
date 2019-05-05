// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.MarkupLanguage;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Layout
{
    [System.ComponentModel.Description("Loads a layout using osu! markup language")]
    public class TestCaseOmlLayout : TestCase
    {
        private const string simple_layout = @"
Layout:
  Children:
    - Extends: Box
      Width: 200
      Height: 100
      Colour: Red
    - Extends: Box
      Width: 100
      Height: 200
      Colour: '#00F7'
";

        private const string event_layout = @"
Layout:
  Children:
    - Extends: HoverBox
      Width: 100
      Height: 100
      Position: 50, 50
      Colour: HotPink

      Events:
        - Name: BoxHover
          AliasOf: OnBoxHover
        - Name: BoxUnHover
          AliasOf: OnBoxUnHover
      Transitions:
        - Name: BoxHover
          State:
            Colour: Orange
        - Name: BoxUnHover
          State:
            Colour: HotPink

    - Extends: Button
      Width: 100
      Height: 25
      Text: Test button
      Position: 50, 175
      BackgroundColour: Purple

      Events:
        - Name: ButtonClick
          AliasOf: Action
          Transitions:
            -
              State:
                Alpha: 0.5
              Duration: 500
              Easing: EasingTypes.Out
";

        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(OmlReader), typeof(OmlObject) };
        private OmlReader reader;

        [SetUp]
        public void SetUp() => Schedule(() => Child = new Container());

        [TestCase]
        public void LoadSimpleLayout()
        {
            AddStep("Create OmlReader", () => reader = new OmlReader(new Dictionary<string, Type>
            {
                { "Box", typeof(Box) },
            }));

            AddStep("Load layout", () => Child = reader.Load("Layout", new StringReader(simple_layout)));
        }

        [TestCase]
        public void LoadEventLayout()
        {
            AddStep("Create OmlReader", () => reader = new OmlReader(new Dictionary<string, Type>
            {
                { "Box", typeof(Box) },
                { "HoverBox", typeof(HoverBox) },
                { "Button", typeof(Button) },
            }));

            AddStep("Load layout", () => Child = reader.Load("Layout", new StringReader(event_layout)));
        }

        private class HoverBox : Box
        {
            public event Action<HoverEvent> OnBoxHover;
            public event Action<HoverLostEvent> OnBoxUnHover;

            protected override bool OnHover(HoverEvent e)
            {
                OnBoxHover?.Invoke(e);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                OnBoxUnHover?.Invoke(e);
                base.OnHoverLost(e);
            }
        }
    }
}
