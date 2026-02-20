// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Graphics;

namespace osu.Framework.Text
{
    public class TextSelection
    {
        private readonly Func<string> getText;

        public int SelectionStart { get; protected set; }
        public int SelectionEnd { get; protected set; }

        public virtual bool AllowWordNavigation { get; set; } = true;

        public int SelectionLeft => int.Min(SelectionStart, SelectionEnd);
        public int SelectionRight => int.Max(SelectionStart, SelectionEnd);

        public int SelectionLength => SelectionRight - SelectionLeft;

        public delegate void SelectionChangedHandler(Direction direction);

        public event SelectionChangedHandler? SelectionChanged;

        public TextSelection(Func<string> getText)
        {
            this.getText = getText;
        }

        public bool HasSelection => SelectionLength > 0;

        private string text => getText();

        public void SetSelection(int start, int end, Direction direction = Direction.Horizontal)
        {
            SelectionStart = int.Clamp(start, 0, text.Length + 1);
            SelectionEnd = int.Clamp(end, 0, text.Length + 1);
            SelectionChanged?.Invoke(direction);
        }

        public void MoveBackwardChar()
        {
            if (HasSelection)
                MoveCursorBy(SelectionLeft - SelectionEnd);
            else
                MoveCursorBy(-1);
        }

        public void MoveForwardChar()
        {
            if (HasSelection)
                MoveCursorBy(SelectionRight - SelectionEnd);
            else
                MoveCursorBy(1);
        }

        public void MoveBackwardWord()
        {
            if (HasSelection)
            {
                MoveCursorBy(SelectionLeft - SelectionEnd);
            }
            else
            {
                MoveCursorBy(GetBackwardWordAmount());
            }
        }

        public void MoveForwardWord()
        {
            if (HasSelection)
            {
                MoveCursorBy(SelectionRight - SelectionEnd);
            }
            else
            {
                MoveCursorBy(GetForwardWordAmount());
            }
        }

        public void MoveBackwardLine()
        {
            MoveCursorTo(GetLineStart(SelectionLeft));
        }

        public void MoveForwardLine()
        {
            MoveCursorTo(GetLineEnd(SelectionRight));
        }

        public void SelectBackwardChar()
        {
            ExpandSelectionBy(-1);
        }

        public void SelectForwardChar()
        {
            ExpandSelectionBy(1);
        }

        public void SelectBackwardWord()
        {
            ExpandSelectionBy(GetBackwardWordAmount());
        }

        public void SelectForwardWord()
        {
            ExpandSelectionBy(GetForwardWordAmount());
        }

        public void SelectBackwardLine()
        {
            ExpandSelectionBy(GetBackwardLineAmount());
        }

        public void SelectForwardLine()
        {
            ExpandSelectionBy(GetForwardLineAmount());
        }

        protected void ExpandSelectionBy(int amount)
        {
            moveSelection(amount, true);
        }

        public void MoveCursorTo(int position, Direction direction = Direction.Horizontal)
        {
            SelectionStart = SelectionEnd = position;
            SelectionChanged?.Invoke(direction);
        }

        public void MoveCursorBy(int amount)
        {
            SelectionStart = SelectionEnd;
            moveSelection(amount, false);
            SelectionChanged?.Invoke(Direction.Horizontal);
        }

        private void moveSelection(int offset, bool expand)
        {
            int oldStart = SelectionStart;
            int oldEnd = SelectionEnd;

            if (expand)
                SelectionEnd = Math.Clamp(SelectionEnd + offset, 0, text.Length);
            else
            {
                if (HasSelection && Math.Abs(offset) <= 1)
                {
                    //we don't want to move the location when "removing" an existing selection, just set the new location.
                    if (offset > 0)
                        SelectionEnd = SelectionStart = SelectionRight;
                    else
                        SelectionEnd = SelectionStart = SelectionLeft;
                }
                else
                    SelectionEnd = SelectionStart = Math.Clamp((offset > 0 ? SelectionRight : SelectionLeft) + offset, 0, text.Length);
            }

            if (oldStart != SelectionStart || oldEnd != SelectionEnd)
                SelectionChanged?.Invoke(Direction.Horizontal);
        }

        public int GetBackwardWordAmount()
        {
            if (!AllowWordNavigation)
                return -1;

            return findNextWord(text, SelectionEnd, -1) - SelectionEnd;
        }

        public int GetForwardWordAmount()
        {
            if (!AllowWordNavigation)
                return 1;

            return findNextWord(text, SelectionEnd, 1) - SelectionEnd;
        }

        private static int findNextWord(string text, int position, int direction)
        {
            Debug.Assert(direction == -1 || direction == 1);

            // When going backwards, the initial position will always be the index of the first character in the next word,
            // but it should be the index of the character in the last word.
            if (direction == -1)
                position -= 1;

            WordTraversalStep currentStep = WordTraversalStep.Whitespace;

            while (true)
            {
                if (position < 0)
                    return 0;

                if (position >= text.Length)
                    return text.Length;

                char character = text[position];

                switch (currentStep)
                {
                    case WordTraversalStep.Whitespace:
                        if (char.IsWhiteSpace(character))
                            position += direction;
                        else if (char.IsLetterOrDigit(character))
                            currentStep = WordTraversalStep.LetterOrDigit;
                        else
                            currentStep = WordTraversalStep.Symbol;

                        continue;

                    case WordTraversalStep.Symbol:
                        if (char.IsLetterOrDigit(character))
                            currentStep = WordTraversalStep.LetterOrDigit;
                        else if (char.IsWhiteSpace(character))
                            break;

                        position += direction;
                        continue;

                    case WordTraversalStep.LetterOrDigit:
                        if (char.IsLetterOrDigit(character))
                        {
                            position += direction;
                            continue;
                        }

                        break;
                }

                break;
            }

            // When going backwards, the final position will always be the the index of the last character of the previous word,
            // but it should be the index of the first character in the next word.
            if (direction == -1)
                position += 1;

            return position;
        }

        public virtual int GetLineStart(int position) => 0;

        public virtual int GetLineEnd(int position) => text.Length;

        public int GetBackwardLineAmount()
        {
            return GetLineStart(SelectionEnd) - SelectionEnd;
        }

        public int GetForwardLineAmount()
        {
            return GetLineEnd(SelectionEnd) - SelectionEnd;
        }

        public void SelectWord(int position)
        {
            SelectionEnd = position;

            SelectionStart = position + GetBackwardWordAmount();
            SelectionEnd = position + GetForwardWordAmount();
            SelectionChanged?.Invoke(Direction.Horizontal);
        }

        public void SelectAll()
        {
            SelectionStart = 0;
            SelectionEnd = text.Length;
            SelectionChanged?.Invoke(Direction.Horizontal);
        }

        private enum WordTraversalStep
        {
            Whitespace,
            LetterOrDigit,
            Symbol,
        }
    }
}
