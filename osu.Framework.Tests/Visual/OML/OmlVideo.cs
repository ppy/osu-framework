// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.OML;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.OML
{
    public class OmlVideo : TestScene
    {
        private const string test_data = "<oml>"
                                         + "    <video src=\"https://cdn.discordapp.com/attachments/418775953343250432/649510831012446218/wp.mp4\""
                                         + "        anchor=\"TopLeft\""
                                         + "        origin=\"Centre\""
                                         + "        margin=\"0,0,0,200\""
                                         + "        loop=\"true\""
                                         + "     />" +
                                         "</oml>";

        [Test]
        public void TestVideo()
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
