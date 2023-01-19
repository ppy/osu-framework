// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Shaders
{
    public class UniformBlock : IUniform
    {
        public IShader Owner { get; }
        public string Name { get; }
        public int Location { get; }

        private readonly IRenderer renderer;

        public UniformBlock(IRenderer renderer, IShader owner, string name, int blockIndex)
        {
            this.renderer = renderer;
            Owner = owner;
            Name = name;
            Location = blockIndex;
        }

        public void Update()
        {
        }
    }
}
