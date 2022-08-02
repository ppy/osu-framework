// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    /// <summary>
    /// An <see cref="INativeTexture"/> that does nothing. May be used for tests that don't have a visual output.
    /// </summary>
    internal class DummyNativeTexture : INativeTexture
    {
        public IRenderer Renderer { get; }

        public string Identifier => string.Empty;
        public int MaxSize => 4096; // Sane default for testing purposes.
        public int Width { get; set; } = 1;
        public int Height { get; set; } = 1;
        public bool Available => true;
        public bool BypassTextureUploadQueueing { get; set; }
        public bool UploadComplete => true;
        public bool IsQueuedForUpload { get; set; }

        public DummyNativeTexture(IRenderer renderer)
        {
            Renderer = renderer;
        }

        public void FlushUploads()
        {
        }

        public void SetData(ITextureUpload upload)
        {
        }

        public bool Upload() => true;

        public bool Bind(int unit, WrapMode wrapModeS, WrapMode wrapModeT) => true;

        public int GetByteSize() => 0;

        public ulong BindCount => 0;

        public void Dispose()
        {
        }
    }
}
