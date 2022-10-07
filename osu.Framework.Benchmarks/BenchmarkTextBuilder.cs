// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Text;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkTextBuilder
    {
        private readonly ITexturedGlyphLookupStore store = new TestStore();

        private TextBuilder textBuilder = null!;

        [Benchmark]
        public void AddCharacters() => initialiseBuilder(false);

        [Benchmark]
        public void AddCharactersWithDifferentBaselines() => initialiseBuilder(true);

        [Benchmark]
        public void RemoveLastCharacter()
        {
            initialiseBuilder(false);
            textBuilder.RemoveLastCharacter();
        }

        [Benchmark]
        public void RemoveLastCharacterWithDifferentBaselines()
        {
            initialiseBuilder(true);
            textBuilder.RemoveLastCharacter();
        }

        private void initialiseBuilder(bool withDifferentBaselines)
        {
            textBuilder = new TextBuilder(store, FontUsage.Default);

            char different = 'B';

            for (int i = 0; i < 100; i++)
                textBuilder.AddCharacter(withDifferentBaselines && (i % 10 == 0) ? different++ : 'A');
        }

        private class TestStore : ITexturedGlyphLookupStore
        {
            public ITexturedCharacterGlyph Get(string fontName, char character) => new TexturedCharacterGlyph(
                new CharacterGlyph(character, character, character, character, character, null),
                new DummyRenderer().CreateTexture(1, 1));

            public Task<ITexturedCharacterGlyph> GetAsync(string fontName, char character) => Task.Run(() => Get(fontName, character));
        }
    }
}
