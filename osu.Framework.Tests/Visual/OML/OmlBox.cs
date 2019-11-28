// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.OML;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.OML
{
    public class OmlBox : TestScene
    {
        private const string test_data = "<oml>" +
                                         "    <Box width=\"250\" height=\"250\" colour=\"#FF0000\"             position=\"0,0\"    />" +
                                         "    <Box width=\"250\" height=\"250\" colour=\"green\"               position=\"100,100\"/>" +
                                         "    <Box width=\"250\" height=\"250\" colour=\"rgba(0, 0, 255, .4)\" position=\"200,200\"/>" +
                                         "</oml>";

        [Test]
        public void TestBox()
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
