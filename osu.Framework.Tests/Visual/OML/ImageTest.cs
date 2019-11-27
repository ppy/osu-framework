// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.OML;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.OML
{
    public class ImageTest : TestScene
    {
        private const string test_data = "<oml>" +
                                         "<img src=\"https://a.ppy.sh/10291354\" width=\"250\" height=\"250\"/>" +
                                         "</oml>";

        [Test]
        public void TestImage()
        {
            AddStep("Parse OML", performParse);
        }

        private void performParse()
        {
            var parser = new OmlParser(test_data);
            var display = new OmlDisplayContainer(parser);

            Child = display;
        }
    }
}
