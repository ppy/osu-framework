// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Text.RegularExpressions;
using NUnit.Framework;
using osu.Framework.Graphics.OpenGL.Shaders;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class ShaderRegexTest
    {
        private readonly Regex shaderAttributeRegex = new Regex(GLShaderPart.SHADER_ATTRIBUTE_PATTERN);

        [Test]
        public void TestComment()
        {
            const string test_string = "// The following implementation using mix and step may be faster, but stackoverflow indicates it is in fact a lot slower on some GPUs.";
            performInvalidAttributeTest(test_string);
        }

        [Test]
        public void TestNonAttribute()
        {
            const string test_string = "varying vec3 name;";
            performInvalidAttributeTest(test_string);
        }

        [Test]
        public void TestValidAttribute()
        {
            const string test_string = "attribute lowp float name;";
            performValidAttributeTest(test_string);
        }

        [Test]
        public void TestSpacedAttribute()
        {
            const string test_string = "    attribute    float    name   ;";
            performValidAttributeTest(test_string);
        }

        [Test]
        public void TestNoPrecisionQualifier()
        {
            const string test_string = "attribute float name;";
            performValidAttributeTest(test_string);
        }

        private void performValidAttributeTest(string testString)
        {
            var match = shaderAttributeRegex.Match(testString);

            Assert.IsTrue(match.Success);
            Assert.AreEqual("name", match.Groups[1].Value.Trim());
        }

        private void performInvalidAttributeTest(string testString)
        {
            var match = shaderAttributeRegex.Match(testString);

            Assert.IsFalse(match.Success);
        }
    }
}
