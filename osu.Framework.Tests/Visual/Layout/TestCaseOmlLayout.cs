// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using osu.Framework.Graphics.Shapes;
using osu.Framework.MarkupLanguage;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Layout
{
    [System.ComponentModel.Description("Loads a layout using osu! markup language")]
    public class TestCaseOmlLayout : TestCase
    {
        private const string simple_layout = @"
Layout:
  Extends: Box
  Width: 200
  Height: 100
  Colour: Red";

        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(OmlReader), typeof(OmlObject) };

        [TestCase]
        public void LoadSimpleLayout()
        {
            var reader = new OmlReader(new Dictionary<string, Type>
            {
                { "Box", typeof(Box) },
            });

            var obj = reader.Load("Layout", new StringReader(simple_layout));
            Child = obj;
        }
    }
}
