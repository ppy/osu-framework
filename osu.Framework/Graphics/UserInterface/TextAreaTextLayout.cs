// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public partial class TextAreaTextLayout : FillFlowContainer<TextAreaTextLayout.TextAreaWord>
    {
        public partial class TextAreaWord : SpriteText
        {
            public bool IsNewLine;

            public override bool IsPresent => true;
        }

        internal LineInfo[] Lines { get; private set; } = Array.Empty<LineInfo>();

        public override IEnumerable<Drawable> FlowingChildren => AliveInternalChildren.OrderBy(d => d.ChildID);

        internal bool TryGetLineInfo(int index, [NotNullWhen(true)] out LineInfo? line)
        {
            if (index < 0 || index >= Lines.Length)
            {
                line = null;
                return false;
            }

            line = Lines[index];
            return true;
        }

        protected override bool ForceNewRow(Drawable drawable) => ((TextAreaWord)drawable).IsNewLine;

        protected override void OnLayoutComputed(Drawable[] children, Vector2[] layoutPositions, int[] rowIndices)
        {
            base.OnLayoutComputed(children, layoutPositions, rowIndices);

            if (children.Length == 0)
            {
                Lines = new[]
                {
                    new LineInfo(default, 0, 0, 0, 0)
                };
                return;
            }

            // rowIndices may be larger than children.Length
            Lines = new LineInfo[rowIndices.Take(children.Length).Max() + 1];

            RectangleF currentLineBounds = default;

            int lastRowIndex = 0;
            int positionInString = 0;
            int lineStartPositionInString = 0;

            int firstDrawableInLine = 0;

            for (int i = 0; i < children.Length; i++)
            {
                var child = (SpriteText)children[i];
                var position = layoutPositions[i];
                var childSize = child.BoundingBox.Size;
                int rowIndex = rowIndices[i];

                var childBounds = new RectangleF(position, childSize);

                if (rowIndex != lastRowIndex)
                {
                    Lines[lastRowIndex] = new LineInfo(currentLineBounds, lineStartPositionInString, positionInString, firstDrawableInLine, i - firstDrawableInLine);

                    currentLineBounds = childBounds;
                    lineStartPositionInString = positionInString;
                    firstDrawableInLine = i;
                }
                else
                {
                    currentLineBounds = RectangleF.Union(currentLineBounds, childBounds);
                }

                positionInString += child.Characters.Count;
                lastRowIndex = rowIndex;

                if (i == children.Length - 1)
                {
                    // we can navigate 1 character beyond the string's length so we add an extra character here
                    Lines[rowIndex] = new LineInfo(currentLineBounds, lineStartPositionInString, positionInString + 1, firstDrawableInLine, i - firstDrawableInLine + 1);
                }
            }
        }

        public int TextPositionToIndex(TextPosition position)
        {
            var line = Lines[position.Row];

            return line.StartOffset + position.Column;
        }

        public TextPosition IndexToTextPosition(int index)
        {
            if (Lines.Length == 0)
                return new TextPosition();

            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i];

                if (index < line.EndOffset)
                {
                    return new TextPosition(i, index - line.StartOffset);
                }
            }

            return new TextPosition(Lines.Length - 1, Lines[^1].EndOffset);
        }

        public SpriteText? GetDrawableAt(int index)
        {
            var position = IndexToTextPosition(index);

            var line = Lines[position.Row];

            var flowingChildren = FlowingChildren.ToArray();

            if (flowingChildren.Length == 0)
                return null;

            int charactersSoFar = 0;

            if (position.Row == Lines.Length - 1 && position.Column >= line.CharacterCount - 1)
            {
                return (SpriteText)flowingChildren[line.LastDrawableIndex];
            }

            if (position.Column >= line.CharacterCount)
            {
                return (SpriteText)flowingChildren[line.LastDrawableIndex];
            }

            for (int i = line.FirstDrawableIndex; i <= line.LastDrawableIndex; i++)
            {
                var spriteText = (SpriteText)flowingChildren[i];

                if (spriteText.Characters.Count == 0)
                    continue;

                if (charactersSoFar + spriteText.Characters.Count <= position.Column)
                {
                    charactersSoFar += spriteText.Characters.Count;
                    continue;
                }

                return spriteText;
            }

            return (SpriteText)flowingChildren[^1];
        }

        public Vector2 GetCharacterPosition(int index)
        {
            var position = IndexToTextPosition(index);

            var line = Lines[position.Row];

            var flowingChildren = FlowingChildren.ToArray();

            if (flowingChildren.Length == 0)
                return new Vector2();

            int charactersSoFar = 0;

            if (position.Row == Lines.Length - 1 && position.Column >= line.CharacterCount - 1)
            {
                // Special case for the last line, since it's true length is shorter by a character
                var spriteText = (SpriteText)flowingChildren[line.LastDrawableIndex];

                return spriteText.BoundingBox.TopRight;
            }

            if (position.Column >= line.CharacterCount)
            {
                var spriteText = (SpriteText)flowingChildren[line.LastDrawableIndex];

                return spriteText.BoundingBox.TopRight;
            }

            for (int i = line.FirstDrawableIndex; i <= line.LastDrawableIndex; i++)
            {
                var spriteText = (SpriteText)flowingChildren[i];

                if (spriteText.Characters.Count == 0)
                    continue;

                if (charactersSoFar + spriteText.Characters.Count <= position.Column)
                {
                    charactersSoFar += spriteText.Characters.Count;
                    continue;
                }

                var glyph = spriteText.Characters[position.Column - charactersSoFar];

                float glyphPosition = glyph.DrawRectangle.Left - glyph.XOffset;

                return spriteText.BoundingBox.TopLeft + new Vector2(glyphPosition, 0);
            }

            return flowingChildren[^1].BoundingBox.TopRight;
        }

        internal TextPosition GetClosestTextPosition(Vector2 position)
        {
            for (int lineIndex = 0; lineIndex < Lines.Length; lineIndex++)
            {
                var line = Lines[lineIndex];

                if (line.Bounds.Bottom < position.Y)
                    continue;

                return new TextPosition(
                    lineIndex,
                    GetClosestCharacterForLine(line, position.X)
                );
            }

            // if we land here it means that the position is below the last line
            return new TextPosition(Lines.Length - 1, GetClosestCharacterForLine(Lines[^1], position.X));
        }

        internal int GetClosestCharacterForLine(LineInfo line, float xOffset)
        {
            var flowingChildren = FlowingChildren.ToArray();

            int charactersSoFar = 0;

            for (int i = line.FirstDrawableIndex; i <= line.LastDrawableIndex; i++)
            {
                var spriteText = (SpriteText)flowingChildren[i];

                var textBounds = spriteText.BoundingBox;

                if (xOffset < textBounds.Left)
                {
                    // Since we're going left-to-right this always means that the position is before the line's first character
                    return 0;
                }

                if (xOffset > textBounds.Right)
                {
                    // we have not reached the character yet
                    charactersSoFar += spriteText.Characters.Count;
                    continue;
                }

                for (int j = 0; j < spriteText.Characters.Count; j++)
                {
                    var glyph = spriteText.Characters[j];

                    float glyphPosition = glyph.DrawRectangle.Centre.X - glyph.XOffset;

                    if (xOffset < textBounds.Left + glyphPosition)
                        return charactersSoFar + j;
                }

                return charactersSoFar + spriteText.Characters.Count;
            }

            return line.CharacterCount - 1;
        }

        internal record LineInfo(RectangleF Bounds, int StartOffset, int EndOffset, int FirstDrawableIndex, int DrawableCount)
        {
            public int CharacterCount => EndOffset - StartOffset;

            public int LastDrawableIndex => FirstDrawableIndex + DrawableCount - 1;
        }
    }
}
