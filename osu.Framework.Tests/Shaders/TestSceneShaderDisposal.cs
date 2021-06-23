// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics.Shaders;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Shaders
{
    public class TestSceneShaderDisposal : FrameworkTestScene
    {
        private ShaderManager manager;
        private Shader shader;

        private WeakReference<IShader> shaderRef;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("setup manager", () =>
            {
                manager = new TestShaderManager(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Game).Assembly), @"Resources/Shaders"));
                shader = (Shader)manager.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
                shaderRef = new WeakReference<IShader>(shader);

                shader.Compile();
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
                : base(store)
            {
            }

            internal override Shader CreateShader(string name, List<ShaderPart> parts) => new TestShader(name, parts);

            private class TestShader : Shader
            {
                internal TestShader(string name, List<ShaderPart> parts)
                    : base(name, parts)
                {
                }

                protected override int CreateProgram() => 1337;

                protected override bool CompileInternal() => true;

                protected override void SetupUniforms()
                {
                    Uniforms.Add("test", new Uniform<int>(this, "test", 1));
                }

                protected override string GetProgramLog() => string.Empty;

                protected override void DeleteProgram(int id)
                {
                }
            }
        }
    }
}
