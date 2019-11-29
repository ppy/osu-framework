// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.OML;

namespace osu.Framework.Tests.Visual.OML
{
    public class TestSceneOmlBox : FrameworkTestScene
    {
        private const string test_data = "<oml>" +
                                         "    <Box width=\"250px\" height=\"250px\" colour=\"#FF0000\" position=\"0,0\"/>" +
                                         "    <Box width=\"6.6145cm\" height=\"6.6145cm\" colour=\"green\" position=\"100,100\"/>" +
                                         "    <Box width=\"2.6041in\" height=\"2.6041in\" colour=\"rgba(0, 0, 255, .4)\" position=\"200,200\"/>" +
                                         "    <Box width=\"187.5pt\" height=\"187.5pt\" colour=\"rgba(0, 255, 255, .6)\" position=\"300,300\"/>" +
                                         "    <Box width=\"15.625pc\" height=\"15.625pc\" colour=\"rgba(255, 0, 255, .8)\" position=\"400,400\"/>" +
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
