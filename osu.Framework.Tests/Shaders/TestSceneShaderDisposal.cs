// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Shaders;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Shaders
{
    [HeadlessTest]
    public class TestSceneShaderDisposal : FrameworkTestScene
    {
        private ShaderManager manager;
        private GLShader shader;

        private WeakReference<IShader> shaderRef;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("setup manager", () =>
            {
                manager = new TestShaderManager(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Game).Assembly), @"Resources/Shaders"));
                shader = (GLShader)manager.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
                shaderRef = new WeakReference<IShader>(shader);

                shader.EnsureShaderCompiled();
            });

            AddUntilStep("wait for load", () => shader.IsLoaded);
        }

        [Test]
        public void TestShadersLoseReferencesOnManagerDisposal()
        {
            AddStep("remove local reference", () => shader = null);

            AddStep("dispose manager", () =>
            {
                manager.Dispose();
                manager = null;
            });

            AddUntilStep("reference lost", () =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return !shaderRef.TryGetTarget(out _);
            });
        }

        private class TestShaderManager : ShaderManager
        {
            public TestShaderManager(IResourceStore<byte[]> store)
                : base(new GLRenderer(), store)
            {
            }

            internal override IShader CreateShader(IRenderer renderer, string name, params IShaderPart[] parts)
                => new TestGLShader(renderer, name, parts.Cast<GLShaderPart>().ToArray());

            private class TestGLShader : GLShader
            {
                private readonly IRenderer renderer;

                internal TestGLShader(IRenderer renderer, string name, GLShaderPart[] parts)
                    : base(renderer, name, parts)
                {
                    this.renderer = renderer;
                }

                private protected override int CreateProgram() => 1337;

                private protected override bool CompileInternal() => true;

                private protected override void SetupUniforms()
                {
                    Uniforms.Add("test", new Uniform<int>(renderer, this, "test", 1));
                }

                private protected override string GetProgramLog() => string.Empty;

                private protected override void DeleteProgram(int id)
                {
                }
            }
        }
    }
}
