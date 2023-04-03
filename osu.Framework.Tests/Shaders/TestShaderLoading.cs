// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Shaders;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests.Shaders
{
    public class TestShaderLoading
    {
        [Test]
        public void TestFetchNonExistentShader()
        {
            var manager = new TestShaderManager(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Game).Assembly), @"Resources/Shaders"));

            // fetch non-existent shader throws an arbitrary exception type.
            // not sure this is the best way to have things (other stores return null instead), but is what it is for now so let's ensure we don't change behaviour.
            Assert.That(() => manager.Load("unknown", "unknown"), Throws.Exception.TypeOf<FileNotFoundException>());
        }

        [Test]
        public void TestFetchExistentShader()
        {
            IShader lookup1;

            var manager = new TestShaderManager(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Game).Assembly), @"Resources/Shaders"));

            // fetch existent shader.
            Assert.That(lookup1 = manager.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE), Is.Not.Null);

            // ensure cached
            Assert.That(manager.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE), Is.SameAs(lookup1));
        }

        private class TestShaderManager : ShaderManager
        {
            public TestShaderManager(IResourceStore<byte[]> store)
                : base(new GLRenderer(), store)
            {
            }

            internal override IShader CreateShader(IRenderer renderer, string name, params IShaderPart[] parts)
                => new TestGLShader((GLRenderer)renderer, name, parts.Cast<GLShaderPart>().ToArray());

            private class TestGLShader : GLShader
            {
                internal TestGLShader(GLRenderer renderer, string name, GLShaderPart[] parts)
                    : base(renderer, name, parts, null)
                {
                }

                private protected override int CreateProgram() => 1337;

                private protected override bool CompileInternal() => true;

                public override void BindUniformBlock(string blockName, IUniformBuffer buffer)
                {
                }

                private protected override string GetProgramLog() => string.Empty;

                private protected override void DeleteProgram(int id)
                {
                }
            }
        }
    }
}
