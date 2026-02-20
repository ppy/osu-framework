// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Text;
using osu.Framework.Utils;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public partial class TextArea : CompositeDrawable, IKeyBindingHandler<PlatformAction>
    {
        public FontUsage Font { get; init; } = FontUsage.Default;

        public TextInputProperties InputProperties { get; init; }

        private string text = string.Empty;

        public string Text
        {
            get => text;
            set
            {
                if (value == text)
                    return;

                text = value;
                textBacking.Invalidate();
            }
        }

        protected readonly Container TextContainer;

        private readonly TextAreaTextLayout textLayout;
        private readonly TextAreaCaret caret;
        private readonly TextSelection selection;

        private readonly ScrollContainer<Drawable> scroll;

        // Keeps track of the caret's x position of the most recent horizontal navigation
        private readonly Cached<float> caretXPosition = new Cached<float>();

        private readonly Cached textBacking = new Cached();

        public TextArea()
        {
            Masking = true;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.2f,
                },
                scroll = new BasicScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = TextContainer = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding(8),
                        Children = new Drawable[]
                        {
                            textLayout = new TextAreaTextLayout
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                            },
                            caret = new TextAreaCaret
                            {
                                BypassAutoSizeAxes = Axes.Both,
                            }
                        }
                    }
                }
            };

            selection = new TextAreaSelection(() => text, textLayout);

            selection.SelectionChanged += direction =>
            {
                caretBacking.Invalidate();
                if (direction != Direction.Vertical)
                    caretXPosition.Invalidate();

                Logger.Log($"Selection changed [{selection.SelectionStart}, {selection.SelectionEnd}]");
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            caret.Hide();
        }

        protected override void Update()
        {
            base.Update();

            if (!textBacking.IsValid)
            {
                rebuildText();
                textBacking.Validate();
            }
        }

        private readonly Cached caretBacking = new Cached();

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!caretBacking.IsValid)
            {
                updateCaret();

                caretBacking.Validate();
            }
        }

        private void updateCaret()
        {
            if (selection.HasSelection)
            {
                var selectionStart = textLayout.IndexToTextPosition(selection.SelectionLeft);
                var selectionEnd = textLayout.IndexToTextPosition(selection.SelectionRight);

                var selectionRects = new List<RectangleF>();

                for (int line = selectionStart.Row; line <= selectionEnd.Row; line++)
                {
                    var selectionRect = textLayout.Lines[line].Bounds;

                    if (line == selectionStart.Row)
                    {
                        var position = textLayout.GetCharacterPosition(selection.SelectionLeft);

                        selectionRect.Width -= position.X - selectionRect.X;
                        selectionRect.X = position.X;
                    }

                    if (line == selectionEnd.Row)
                    {
                        var position = textLayout.GetCharacterPosition(selection.SelectionRight);

                        selectionRect.Width = position.X - selectionRect.Left;
                    }

                    if (selectionStart.Row != selectionEnd.Row && Precision.AlmostEquals(selectionRect.Width, 0f))
                        continue;

                    selectionRects.Add(selectionRect);
                }

                var caretPos = textLayout.GetCharacterPosition(selection.SelectionEnd);

                var drawable = textLayout.GetDrawableAt(selection.SelectionEnd);

                if (drawable != null)
                    scroll.ScrollIntoView(drawable);

                if (!caretXPosition.IsValid)
                {
                    caretXPosition.Value = caretPos.X;
                }

                caret.DisplayRange(selectionRects);
            }
            else
            {
                var position = textLayout.GetCharacterPosition(selection.SelectionStart);

                caret.DisplayAt(position, Font.Size);

                if (!caretXPosition.IsValid)
                    caretXPosition.Value = position.X;

                var drawable = textLayout.GetDrawableAt(selection.SelectionEnd);

                if (drawable != null)
                    scroll.ScrollIntoView(drawable);
            }
        }

        private void rebuildText()
        {
            textLayout.Clear();

            var words = splitWords(text);

            Logger.Log(words.ToString());

            foreach (var word in words)
            {
                textLayout.Add(new TextAreaTextLayout.TextAreaWord
                {
                    Text = word.Text,
                    IsNewLine = word.IsNewLine,
                });
            }
        }

        private readonly record struct Word(string Text, bool IsNewLine);

        private Word[] splitWords(string text)
        {
            var words = new List<Word>();
            var builder = new StringBuilder();

            bool lastWordWasNewline = false;

            for (int i = 0; i < text.Length; i++)
            {
                if (i == 0
                    || char.IsSeparator(text[i - 1])
                    || char.IsControl(text[i - 1])
                    || char.GetUnicodeCategory(text[i - 1]) == UnicodeCategory.DashPunctuation
                    || text[i - 1] == '/'
                    || text[i - 1] == '\\'
                    || (isCjkCharacter(text[i - 1]) && !char.IsPunctuation(text[i])))
                {
                    words.Add(new Word(builder.ToString(), lastWordWasNewline));
                    builder.Clear();

                    lastWordWasNewline = i > 0 && text[i - 1] == '\n';
                }

                bool isNewLine = text[i] == '\n';

                builder.Append(isNewLine ? ' ' : text[i]);
            }

            if (builder.Length > 0)
                words.Add(new Word(builder.ToString(), false));

            return words.ToArray();

            bool isCjkCharacter(char c) => c >= '\x2E80' && c <= '\x9FFF';
        }

        protected void MoveCursorVertically(int amount)
        {
            float? xPosition =
                caretXPosition.IsValid
                    ? caretXPosition.Value
                    : textLayout.GetCharacterPosition(selection.SelectionStart).X;

            var position = textLayout.IndexToTextPosition(selection.SelectionStart);

            int lineIndex = int.Clamp(position.Row + amount, 0, textLayout.Lines.Length - 1);

            int index = textLayout.TextPositionToIndex(new TextPosition(lineIndex, textLayout.GetClosestCharacterForLine(textLayout.Lines[lineIndex], xPosition.Value)));

            selection.MoveCursorTo(index, Direction.Vertical);
            caretBacking.Invalidate();
        }

        private void insertText(string text)
        {
            deleteSelection();
            this.text = this.text.Insert(selection.SelectionStart, text);
            textBacking.Invalidate();
            selection.MoveForwardChar();
        }

        private void deleteSelection()
        {
            if (!selection.HasSelection)
                return;

            text = text.Remove(selection.SelectionLeft, selection.SelectionLength);
            selection.MoveCursorTo(selection.SelectionLeft);

            textBacking.Invalidate();
        }

        protected void ExpandSelectionVertically(int amount)
        {
            float? xPosition =
                caretXPosition.IsValid
                    ? caretXPosition.Value
                    : textLayout.GetCharacterPosition(selection.SelectionEnd).X;

            var position = textLayout.IndexToTextPosition(selection.SelectionEnd);

            if (position.Row + amount < 0)
            {
                selection.SetSelection(selection.SelectionStart, 0, Direction.Vertical);
                return;
            }

            if (position.Row + amount >= textLayout.Lines.Length)
            {
                selection.SetSelection(selection.SelectionStart, text.Length, Direction.Vertical);
                return;
            }

            int lineIndex = position.Row + amount;

            int index = textLayout.TextPositionToIndex(new TextPosition(lineIndex, textLayout.GetClosestCharacterForLine(textLayout.Lines[lineIndex], xPosition.Value)));

            selection.SetSelection(selection.SelectionStart, index, Direction.Vertical);
            caretBacking.Invalidate();
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (!HasFocus)
                return false;

            switch (e.Key)
            {
                case Key.Up:
                    if (e.ShiftPressed)
                        ExpandSelectionVertically(-1);
                    else
                        MoveCursorVertically(-1);
                    return true;

                case Key.Down:
                    if (e.ShiftPressed)
                        ExpandSelectionVertically(1);
                    else
                        MoveCursorVertically(1);
                    return true;

                case Key.Enter:
                    insertText("\n");
                    return true;
            }

            return base.OnKeyDown(e);
        }

        public bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
        {
            if (!HasFocus)
                return false;

            switch (e.Action)
            {
                case PlatformAction.SelectAll:
                    selection.SelectAll();
                    return true;

                case PlatformAction.MoveForwardChar:
                    selection.MoveForwardChar();
                    return true;

                case PlatformAction.MoveBackwardChar:
                    selection.MoveBackwardChar();
                    return true;

                case PlatformAction.MoveBackwardWord:
                    selection.MoveBackwardWord();
                    return true;

                case PlatformAction.MoveForwardWord:
                    selection.MoveForwardWord();
                    return true;

                case PlatformAction.MoveForwardLine:
                    selection.MoveForwardLine();
                    return true;

                case PlatformAction.MoveBackwardLine:
                    selection.MoveBackwardLine();
                    return true;

                case PlatformAction.SelectBackwardChar:
                    selection.SelectBackwardChar();
                    return true;

                case PlatformAction.SelectForwardChar:
                    selection.SelectForwardChar();
                    return true;

                case PlatformAction.SelectBackwardWord:
                    selection.SelectBackwardWord();
                    return true;

                case PlatformAction.SelectForwardWord:
                    selection.SelectForwardWord();
                    return true;

                case PlatformAction.SelectBackwardLine:
                    selection.SelectBackwardLine();
                    return true;

                case PlatformAction.SelectForwardLine:
                    selection.SelectForwardLine();
                    return true;

                case PlatformAction.DeleteBackwardChar:
                    if (!selection.HasSelection)
                        selection.SelectBackwardChar();

                    deleteSelection();
                    return true;

                case PlatformAction.DeleteForwardChar:
                    if (!selection.HasSelection)
                        selection.SelectForwardChar();

                    deleteSelection();
                    return true;

                case PlatformAction.DeleteBackwardWord:
                    if (!selection.HasSelection)
                        selection.SelectBackwardWord();

                    deleteSelection();
                    return true;

                case PlatformAction.DeleteForwardWord:
                    if (!selection.HasSelection)
                        selection.SelectForwardWord();

                    deleteSelection();
                    return true;

                case PlatformAction.DeleteBackwardLine:
                    if (!selection.HasSelection)
                        selection.SelectBackwardLine();

                    deleteSelection();
                    return true;

                case PlatformAction.DeleteForwardLine:
                    if (!selection.HasSelection)
                        selection.SelectForwardLine();

                    deleteSelection();
                    selection.MoveCursorTo(selection.SelectionLeft);
                    return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
        {
        }

        protected override bool OnClick(ClickEvent e)
        {
            var position = textLayout.GetClosestTextPosition(textLayout.ToLocalSpace(e.ScreenSpaceMousePosition));

            selection.MoveCursorTo(textLayout.TextPositionToIndex(position));

            return true;
        }

        protected override bool OnDoubleClick(DoubleClickEvent e)
        {
            var position = textLayout.GetClosestTextPosition(textLayout.ToLocalSpace(e.ScreenSpaceMousePosition));

            selection.SelectWord(textLayout.TextPositionToIndex(position));

            return true;
        }

        // TODO: see if there's a better way of preventing the scroll container from accepting mouse input.
        protected override bool OnMouseDown(MouseDownEvent e) => e.Button == MouseButton.Left;

        protected override bool OnDragStart(DragStartEvent e)
        {
            var position = textLayout.GetClosestTextPosition(textLayout.ToLocalSpace(e.ScreenSpaceMouseDownPosition));

            selection.MoveCursorTo(textLayout.TextPositionToIndex(position));

            handleDrag(e);

            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            base.OnDrag(e);

            handleDrag(e);
        }

        private void handleDrag(MouseButtonEvent e)
        {
            var position = textLayout.GetClosestTextPosition(textLayout.ToLocalSpace(e.ScreenSpaceMousePosition));

            selection.SetSelection(selection.SelectionStart, textLayout.TextPositionToIndex(position));
        }

        internal override bool BuildPositionalInputQueue(Vector2 screenSpacePos, List<Drawable> queue)
        {
            if (!base.BuildPositionalInputQueue(screenSpacePos, queue))
                return false;

            queue.Remove(this);
            queue.Add(this);

            return true;
        }

        internal override bool BuildNonPositionalInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (!base.BuildNonPositionalInputQueue(queue, allowBlocking))
                return false;

            queue.Remove(this);
            queue.Add(this);

            return true;
        }

        [Resolved]
        private TextInputSource textInput { get; set; } = null!;

        /// <summary>
        /// Whether <see cref="textInput"/> has been activated and bound to.
        /// </summary>
        private bool textInputBound;

        private void bindInput()
        {
            if (textInputBound)
            {
                textInput.EnsureActivated(InputProperties);
                return;
            }

            textInput.Activate(InputProperties, ScreenSpaceDrawQuad.AABBFloat);
            textInput.OnTextInput += insertText;

            textInputBound = true;
        }

        private void unbindInput()
        {
            if (!textInputBound)
                return;

            textInput.Deactivate();
            textInput.OnTextInput -= insertText;

            textInputBound = false;
        }

        public override bool AcceptsFocus => true;

        protected override void OnFocus(FocusEvent e)
        {
            base.OnFocus(e);

            bindInput();
            caret.Show();
        }

        protected override void OnFocusLost(FocusLostEvent e)
        {
            base.OnFocusLost(e);

            unbindInput();
            caret.Hide();
        }
    }
}
