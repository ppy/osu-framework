// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.MathUtils;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Colour;
using osu.Framework.Platform;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Bindings;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.UserInterface
{
    public class TextBox : TabbableContainer, IHasCurrentValue<string>
    {
        protected FillFlowContainer TextFlow;
        protected Box Background;
        protected Drawable Caret;
        protected Container TextContainer;

        public override bool HandleKeyboardInput => HasFocus;

        /// <summary>
        /// Padding to be used within the TextContainer. Requires special handling due to the sideways scrolling of text content.
        /// </summary>
        protected virtual float LeftRightPadding => 5;

        private const float caret_move_time = 60;

        public int? LengthLimit;

        public virtual bool AllowClipboardExport => true;

        //represents the left/right selection coordinates of the word double clicked on when dragging
        private int[] doubleClickWord;

        private AudioManager audio;

        /// <summary>
        /// Whether this TextBox should accept left and right arrow keys for navigation.
        /// </summary>
        public virtual bool HandleLeftRightArrows => true;

        protected virtual Color4 BackgroundCommit => new Color4(249, 90, 255, 200);
        protected virtual Color4 BackgroundFocused => new Color4(100, 100, 100, 255);
        protected virtual Color4 BackgroundUnfocused => new Color4(100, 100, 100, 120);

        public bool ReadOnly;

        public bool ReleaseFocusOnCommit = true;

        public override bool CanBeTabbedTo => !ReadOnly;

        private ITextInputSource textInput;
        private Clipboard clipboard;

        public delegate void OnCommitHandler(TextBox sender, bool newText);

        public OnCommitHandler OnCommit;

        private readonly Scheduler textUpdateScheduler = new Scheduler();

        public TextBox()
        {
            Masking = true;
            CornerRadius = 3;

            Children = new Drawable[]
            {
                Background = new Box
                {
                    Colour = BackgroundUnfocused,
                    RelativeSizeAxes = Axes.Both,
                },
                TextContainer = new TextBoxPlatformBindingHandler(this)
                {
                    AutoSizeAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Position = new Vector2(LeftRightPadding, 0),
                    Children = new[]
                    {
                        Placeholder = CreatePlaceholder(),
                        Caret = new DrawableCaret(),
                        TextFlow = new FillFlowContainer
                        {
                            Direction = FillDirection.Horizontal,
                            AutoSizeAxes = Axes.X,
                            RelativeSizeAxes = Axes.Y,
                        },
                    },
                },
            };

            Current.ValueChanged += newValue => { Text = newValue; };
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host, AudioManager audio)
        {
            this.audio = audio;

            textInput = host.GetTextInput();
            clipboard = host.GetClipboard();

            if (textInput != null)
            {
                textInput.OnNewImeComposition += delegate(string s)
                {
                    textUpdateScheduler.Add(() => onImeComposition(s));
                    cursorAndLayout.Invalidate();
                };
                textInput.OnNewImeResult += delegate
                {
                    textUpdateScheduler.Add(onImeResult);
                    cursorAndLayout.Invalidate();
                };
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            textUpdateScheduler.SetCurrentThread(MainThread);
        }

        internal override void UpdateClock(IFrameBasedClock clock)
        {
            base.UpdateClock(clock);
            textUpdateScheduler.UpdateClock(Clock);
        }

        private void resetSelection()
        {
            selectionStart = selectionEnd;
            cursorAndLayout.Invalidate();
        }

        protected override void Dispose(bool isDisposing)
        {
            OnCommit = null;

            unbindInput();

            base.Dispose(isDisposing);
        }

        private float textContainerPosX;

        private string textAtLastLayout = string.Empty;

        private void updateCursorAndLayout()
        {
            const float cursor_width = 3;

            Placeholder.TextSize = CalculatedTextSize;

            textUpdateScheduler.Update();

            float caretWidth = cursor_width;

            Vector2 cursorPos = Vector2.Zero;
            if (text.Length > 0)
                cursorPos.X = getPositionAt(selectionLeft) - cursor_width / 2;

            float cursorPosEnd = getPositionAt(selectionEnd);

            if (selectionLength > 0)
                caretWidth = getPositionAt(selectionRight) - cursorPos.X;

            float cursorRelativePositionAxesInBox = (cursorPosEnd - textContainerPosX) / DrawWidth;

            //we only want to reposition the view when the cursor reaches near the extremities.
            if (cursorRelativePositionAxesInBox < 0.1 || cursorRelativePositionAxesInBox > 0.9)
            {
                textContainerPosX = cursorPosEnd - DrawWidth / 2 + LeftRightPadding * 2;
            }

            textContainerPosX = MathHelper.Clamp(textContainerPosX, 0, Math.Max(0, TextFlow.DrawWidth - DrawWidth + LeftRightPadding * 2));

            TextContainer.MoveToX(LeftRightPadding - textContainerPosX, 300, Easing.OutExpo);

            if (HasFocus)
            {
                Caret.ClearTransforms();
                Caret.MoveTo(cursorPos, 60, Easing.Out);
                Caret.ResizeWidthTo(caretWidth, caret_move_time, Easing.Out);

                if (selectionLength > 0)
                    Caret
                        .FadeTo(0.5f, 200, Easing.Out)
                        .FadeColour(new Color4(249, 90, 255, 255), 200, Easing.Out);
                else
                    Caret
                        .FadeColour(Color4.White, 200, Easing.Out)
                        .Loop(c => c.FadeTo(0.7f).FadeTo(0.4f, 500, Easing.InOutSine));
            }

            if (textAtLastLayout != text)
                Current.Value = text;
            if (textAtLastLayout.Length == 0 || text.Length == 0)
                Placeholder.FadeTo(text.Length == 0 ? 1 : 0, 200);

            textAtLastLayout = text;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            //have to run this after children flow
            if (!cursorAndLayout.IsValid)
            {
                updateCursorAndLayout();
                cursorAndLayout.Validate();
            }
        }

        private float getPositionAt(int index)
        {
            if (index > 0)
            {
                if (index < text.Length)
                    return TextFlow.Children[index].DrawPosition.X + TextFlow.DrawPosition.X;
                var d = TextFlow.Children[index - 1];
                return d.DrawPosition.X + d.DrawSize.X + TextFlow.Spacing.X + TextFlow.DrawPosition.X;
            }

            return 0;
        }

        private int getCharacterClosestTo(Vector2 pos)
        {
            pos = Parent.ToSpaceOfOtherDrawable(pos, TextFlow);

            int i = 0;
            foreach (Drawable d in TextFlow.Children)
            {
                if (d.DrawPosition.X + d.DrawSize.X / 2 > pos.X)
                    break;
                i++;
            }

            return i;
        }

        private int selectionStart;
        private int selectionEnd;

        private int selectionLength => Math.Abs(selectionEnd - selectionStart);

        private int selectionLeft => Math.Min(selectionStart, selectionEnd);
        private int selectionRight => Math.Max(selectionStart, selectionEnd);

        private Cached cursorAndLayout = new Cached();

        private bool handleAction(PlatformAction action)
        {
            int? amount = null;

            if (!HandleLeftRightArrows &&
                action.ActionMethod == PlatformActionMethod.Move &&
                (action.ActionType == PlatformActionType.CharNext || action.ActionType == PlatformActionType.CharPrevious))
                return false;

            switch (action.ActionType)
            {
                // Clipboard
                case PlatformActionType.Cut:
                case PlatformActionType.Copy:
                    if (string.IsNullOrEmpty(SelectedText) || !AllowClipboardExport) return true;

                    clipboard?.SetText(SelectedText);
                    if (action.ActionType == PlatformActionType.Cut)
                        removeCharacterOrSelection();
                    return true;

                case PlatformActionType.Paste:
                    //the text may get pasted into the hidden textbox, so we don't need any direct clipboard interaction here.
                    string pending = textInput?.GetPendingText();

                    if (string.IsNullOrEmpty(pending))
                        pending = clipboard?.GetText();

                    insertString(pending);
                    return true;

                case PlatformActionType.SelectAll:
                    selectionStart = 0;
                    selectionEnd = text.Length;
                    cursorAndLayout.Invalidate();
                    return true;

                // Cursor Manipulation
                case PlatformActionType.CharNext:
                    amount = 1;
                    break;

                case PlatformActionType.CharPrevious:
                    amount = -1;
                    break;

                case PlatformActionType.LineEnd:
                    amount = text.Length;
                    break;

                case PlatformActionType.LineStart:
                    amount = -text.Length;
                    break;

                case PlatformActionType.WordNext:
                    int searchNext = MathHelper.Clamp(selectionEnd, 0, Text.Length - 1);
                    while (searchNext < Text.Length && text[searchNext] == ' ')
                        searchNext++;
                    int nextSpace = text.IndexOf(' ', searchNext);
                    amount = (nextSpace >= 0 ? nextSpace : text.Length) - selectionEnd;
                    break;

                case PlatformActionType.WordPrevious:
                    int searchPrev = MathHelper.Clamp(selectionEnd - 2, 0, Text.Length - 1);
                    while (searchPrev > 0 && text[searchPrev] == ' ')
                        searchPrev--;
                    int lastSpace = text.LastIndexOf(' ', searchPrev);
                    amount = lastSpace > 0 ? -(selectionEnd - lastSpace - 1) : -selectionEnd;
                    break;
            }

            if (amount.HasValue)
            {
                switch (action.ActionMethod)
                {
                    case PlatformActionMethod.Move:
                        resetSelection();
                        moveSelection(amount.Value, false);
                        break;

                    case PlatformActionMethod.Select:
                        moveSelection(amount.Value, true);
                        break;

                    case PlatformActionMethod.Delete:
                        if (selectionLength == 0)
                            selectionEnd = MathHelper.Clamp(selectionStart + amount.Value, 0, text.Length);
                        if (selectionLength > 0)
                            removeCharacterOrSelection();
                        break;
                }

                return true;
            }

            return false;
        }

        private void moveSelection(int offset, bool expand)
        {
            if (textInput?.ImeActive == true) return;

            int oldStart = selectionStart;
            int oldEnd = selectionEnd;

            if (expand)
                selectionEnd = MathHelper.Clamp(selectionEnd + offset, 0, text.Length);
            else
            {
                if (selectionLength > 0 && Math.Abs(offset) <= 1)
                {
                    //we don't want to move the location when "removing" an existing selection, just set the new location.
                    if (offset > 0)
                        selectionEnd = selectionStart = selectionRight;
                    else
                        selectionEnd = selectionStart = selectionLeft;
                }
                else
                    selectionEnd = selectionStart = MathHelper.Clamp((offset > 0 ? selectionRight : selectionLeft) + offset, 0, text.Length);
            }

            if (oldStart != selectionStart || oldEnd != selectionEnd)
            {
                audio.Sample.Get(@"Keyboard/key-movement")?.Play();
                cursorAndLayout.Invalidate();
            }
        }

        private bool removeCharacterOrSelection(bool sound = true)
        {
            if (Current.Disabled)
                return false;

            if (text.Length == 0) return false;
            if (selectionLength == 0 && selectionLeft == 0) return false;

            int count = MathHelper.Clamp(selectionLength, 1, text.Length);
            int start = MathHelper.Clamp(selectionLength > 0 ? selectionLeft : selectionLeft - 1, 0, text.Length - count);

            if (count == 0) return false;

            if (sound)
                audio.Sample.Get(@"Keyboard/key-delete")?.Play();

            foreach (var d in TextFlow.Children.Skip(start).Take(count).ToArray()) //ToArray since we are removing items from the children in this block.
            {
                TextFlow.Remove(d);

                TextContainer.Add(d);
                d.FadeOut(200);
                d.MoveToY(d.DrawSize.Y, 200, Easing.InExpo);
                d.Expire();
            }

            text = text.Remove(start, count);

            if (selectionLength > 0)
                selectionStart = selectionEnd = selectionLeft;
            else
                selectionStart = selectionEnd = selectionLeft - 1;

            cursorAndLayout.Invalidate();
            return true;
        }

        protected virtual Drawable GetDrawableCharacter(char c) => new SpriteText { Text = c.ToString(), TextSize = CalculatedTextSize };

        protected virtual Drawable AddCharacterToFlow(char c)
        {
            // Remove all characters to the right and store them in a local list,
            // such that their depth can be updated.
            List<Drawable> charsRight = new List<Drawable>();
            foreach (Drawable d in TextFlow.Children.Skip(selectionLeft))
                charsRight.Add(d);
            TextFlow.RemoveRange(charsRight);

            // Update their depth to make room for the to-be inserted character.
            int i = -selectionLeft;
            foreach (Drawable d in charsRight)
                d.Depth = --i;

            // Add the character
            Drawable ch = GetDrawableCharacter(c);
            ch.Depth = -selectionLeft;

            TextFlow.Add(ch);

            ch.FadeColour(Color4.Transparent)
              .FadeColour(ColourInfo.GradientHorizontal(Color4.White, Color4.Transparent), caret_move_time / 2).Then()
              .FadeColour(Color4.White, caret_move_time / 2);

            // Add back all the previously removed characters
            TextFlow.AddRange(charsRight);

            return ch;
        }

        protected float CalculatedTextSize => TextFlow.DrawSize.Y - (TextFlow.Padding.Top + TextFlow.Padding.Bottom);

        /// <summary>
        /// Insert an arbitrary string into the text at the current position.
        /// </summary>
        /// <param name="addText"></param>
        private void insertString(string addText)
        {
            if (string.IsNullOrEmpty(addText)) return;

            foreach (char c in addText)
                addCharacter(c);
        }

        private Drawable addCharacter(char c)
        {
            if (Current.Disabled)
                return null;

            if (char.IsControl(c)) return null;

            if (selectionLength > 0)
                removeCharacterOrSelection();

            if (text.Length + 1 > LengthLimit)
            {
                if (Background.Alpha > 0)
                    Background.FlashColour(Color4.Red, 200);
                else
                    TextFlow.FlashColour(Color4.Red, 200);
                return null;
            }

            Drawable ch = AddCharacterToFlow(c);

            text = text.Insert(selectionLeft, c.ToString());
            selectionStart = selectionEnd = selectionLeft + 1;

            cursorAndLayout.Invalidate();

            return ch;
        }

        protected virtual SpriteText CreatePlaceholder() => new SpriteText
        {
            Colour = Color4.Gray,
        };

        protected SpriteText Placeholder;

        public string PlaceholderText
        {
            get { return Placeholder.Text; }
            set { Placeholder.Text = value; }
        }

        public Bindable<string> Current { get; } = new Bindable<string>();

        private string text = string.Empty;

        public virtual string Text
        {
            get { return text; }
            set
            {
                if (Current.Disabled)
                    return;

                if (value == text)
                    return;

                value = value ?? string.Empty;

                Placeholder.FadeTo(value.Length == 0 ? 1 : 0);

                if (!IsLoaded)
                    Current.Value = text = value;

                textUpdateScheduler.Add(delegate
                {
                    int startBefore = selectionStart;
                    selectionStart = selectionEnd = 0;
                    TextFlow?.Clear();
                    text = string.Empty;

                    foreach (char c in value)
                        addCharacter(c);

                    selectionStart = MathHelper.Clamp(startBefore, 0, text.Length);
                });

                cursorAndLayout.Invalidate();
            }
        }

        public string SelectedText => selectionLength > 0 ? Text.Substring(selectionLeft, selectionLength) : string.Empty;

        protected bool HandlePendingText(InputState state)
        {
            string str = textInput?.GetPendingText();
            if (string.IsNullOrEmpty(str) || ReadOnly)
                return false;

            if (state.Keyboard.ShiftPressed)
                audio.Sample.Get(@"Keyboard/key-caps")?.Play();
            else
                audio.Sample.Get($@"Keyboard/key-press-{RNG.Next(1, 5)}")?.Play();
            insertString(str);

            // as we are grabbing *all* waiting text, we may receive two or more characters.
            // each of these characters will still fire OnKeyDown events (which we want to block) so we need to
            // store our "handled" state until all keys are released.
            handledUserTextInput = true;
            return true;
        }

        private bool handledUserTextInput;

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (textInput?.ImeActive == true) return true;

            if (args.Key <= Key.F35)
                return false;

            if (HandlePendingText(state))
                return true;

            if (ReadOnly) return true;

            if (state.Keyboard.AltPressed || state.Keyboard.ControlPressed || state.Keyboard.SuperPressed)
                return false;

            switch (args.Key)
            {
                case Key.Escape:
                    GetContainingInputManager().ChangeFocus(null);
                    return true;

                case Key.Tab:
                    return base.OnKeyDown(state, args);

                case Key.KeypadEnter:
                case Key.Enter:
                    if (ReleaseFocusOnCommit)
                        GetContainingInputManager().ChangeFocus(null);

                    Background.Colour = ReleaseFocusOnCommit ? BackgroundUnfocused : BackgroundFocused;
                    Background.ClearTransforms();
                    Background.FlashColour(BackgroundCommit, 400);

                    audio.Sample.Get(@"Keyboard/key-confirm")?.Play();
                    OnCommit?.Invoke(this, true);
                    return true;
            }

            return handledUserTextInput;
        }

        protected override bool OnKeyUp(InputState state, KeyUpEventArgs args)
        {
            HandlePendingText(state);

            handledUserTextInput &= state.Keyboard.Keys.Any();

            return base.OnKeyUp(state, args);
        }

        protected override bool OnDrag(InputState state)
        {
            //if (textInput?.ImeActive == true) return true;

            if (doubleClickWord != null)
            {
                //select words at a time
                if (getCharacterClosestTo(state.Mouse.Position) > doubleClickWord[1])
                {
                    selectionStart = doubleClickWord[0];
                    selectionEnd = findSeparatorIndex(text, getCharacterClosestTo(state.Mouse.Position) - 1, 1);
                    selectionEnd = selectionEnd >= 0 ? selectionEnd : text.Length;
                }
                else if (getCharacterClosestTo(state.Mouse.Position) < doubleClickWord[0])
                {
                    selectionStart = doubleClickWord[1];
                    selectionEnd = findSeparatorIndex(text, getCharacterClosestTo(state.Mouse.Position), -1);
                    selectionEnd = selectionEnd >= 0 ? selectionEnd + 1 : 0;
                }
                else
                {
                    //in the middle
                    selectionStart = doubleClickWord[0];
                    selectionEnd = doubleClickWord[1];
                }

                cursorAndLayout.Invalidate();
            }
            else
            {
                if (text.Length == 0) return true;

                selectionEnd = getCharacterClosestTo(state.Mouse.Position);
                if (selectionLength > 0)
                    GetContainingInputManager().ChangeFocus(this);

                cursorAndLayout.Invalidate();
            }

            return true;
        }

        protected override bool OnDragStart(InputState state)
        {
            if (HasFocus) return true;

            if (!state.Mouse.PositionMouseDown.HasValue)
                throw new ArgumentNullException(nameof(state.Mouse.PositionMouseDown));

            Vector2 posDiff = state.Mouse.PositionMouseDown.Value - state.Mouse.Position;

            return Math.Abs(posDiff.X) > Math.Abs(posDiff.Y);
        }

        protected override bool OnDoubleClick(InputState state)
        {
            if (textInput?.ImeActive == true) return true;

            if (text.Length == 0) return true;

            if (AllowClipboardExport)
            {
                int hover = Math.Min(text.Length - 1, getCharacterClosestTo(state.Mouse.Position));

                int lastSeparator = findSeparatorIndex(text, hover, -1);
                int nextSeparator = findSeparatorIndex(text, hover, 1);

                selectionStart = lastSeparator >= 0 ? lastSeparator + 1 : 0;
                selectionEnd = nextSeparator >= 0 ? nextSeparator : text.Length;
            }
            else
            {
                selectionStart = 0;
                selectionEnd = text.Length;
            }

            //in order to keep the home word selected
            doubleClickWord = new[] { selectionStart, selectionEnd };

            cursorAndLayout.Invalidate();
            return true;
        }

        private static int findSeparatorIndex(string input, int searchPos, int direction)
        {
            bool isLetterOrDigit = char.IsLetterOrDigit(input[searchPos]);

            for (int i = searchPos; i >= 0 && i < input.Length; i += direction)
            {
                if (char.IsLetterOrDigit(input[i]) != isLetterOrDigit)
                    return i;
            }

            return -1;
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            if (textInput?.ImeActive == true) return true;

            selectionStart = selectionEnd = getCharacterClosestTo(state.Mouse.Position);

            cursorAndLayout.Invalidate();

            return false;
        }

        protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
        {
            doubleClickWord = null;
            return true;
        }

        protected override void OnFocusLost(InputState state)
        {
            unbindInput();

            Caret.ClearTransforms();
            Caret.FadeOut(200);


            Background.ClearTransforms();
            Background.FadeColour(BackgroundUnfocused, 200, Easing.OutExpo);

            cursorAndLayout.Invalidate();
        }

        public override bool AcceptsFocus => true;

        protected override bool OnClick(InputState state) => !ReadOnly;

        protected override void OnFocus(InputState state)
        {
            bindInput();

            Background.ClearTransforms();
            Background.FadeColour(BackgroundFocused, 200, Easing.Out);

            cursorAndLayout.Invalidate();
        }

        #region Native TextBox handling (winform specific)

        private void unbindInput()
        {
            textInput?.Deactivate(this);
        }

        private void bindInput()
        {
            textInput?.Activate(this);
        }

        private void onImeResult()
        {
            //we only succeeded if there is pending data in the textbox
            if (imeDrawables.Count > 0)
            {
                foreach (Drawable d in imeDrawables)
                {
                    d.Colour = Color4.White;
                    d.FadeTo(1, 200, Easing.Out);
                }
            }

            imeDrawables.Clear();
        }

        private readonly List<Drawable> imeDrawables = new List<Drawable>();

        private void onImeComposition(string s)
        {
            //search for unchanged characters..
            int matchCount = 0;
            bool matching = true;
            bool didDelete = false;

            int searchStart = text.Length - imeDrawables.Count;

            //we want to keep processing to the end of the longest string (the current displayed or the new composition).
            int maxLength = Math.Max(imeDrawables.Count, s.Length);

            for (int i = 0; i < maxLength; i++)
            {
                if (matching && searchStart + i < text.Length && i < s.Length && text[searchStart + i] == s[i])
                {
                    matchCount = i + 1;
                    continue;
                }

                matching = false;

                if (matchCount < imeDrawables.Count)
                {
                    //if we are no longer matching, we want to remove all further characters.
                    removeCharacterOrSelection(false);
                    imeDrawables.RemoveAt(matchCount);
                    didDelete = true;
                }
            }

            if (matchCount == s.Length)
            {
                //in the case of backspacing (or a NOP), we can exit early here.
                if (didDelete)
                    audio.Sample.Get(@"Keyboard/key-delete")?.Play();
                return;
            }

            //add any new or changed characters
            for (int i = matchCount; i < s.Length; i++)
            {
                Drawable dr = addCharacter(s[i]);
                if (dr != null)
                {
                    dr.Colour = Color4.Aqua;
                    dr.Alpha = 0.6f;
                    imeDrawables.Add(dr);
                }
            }

            audio.Sample.Get($@"Keyboard/key-press-{RNG.Next(1, 5)}")?.Play();
        }

        #endregion

        private class DrawableCaret : CompositeDrawable
        {
            public DrawableCaret()
            {
                RelativeSizeAxes = Axes.Y;
                Size = new Vector2(1, 0.9f);
                Alpha = 0;
                Colour = Color4.Transparent;
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;

                Masking = true;
                CornerRadius = 1;

                InternalChild = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                };
            }
        }

        private class TextBoxPlatformBindingHandler : Container, IKeyBindingHandler<PlatformAction>
        {
            private readonly TextBox textBox;

            public TextBoxPlatformBindingHandler(TextBox textBox)
            {
                this.textBox = textBox;
            }

            public bool OnPressed(PlatformAction action) => textBox.HasFocus && textBox.handleAction(action);

            public bool OnReleased(PlatformAction action) => false;
        }
    }
}
