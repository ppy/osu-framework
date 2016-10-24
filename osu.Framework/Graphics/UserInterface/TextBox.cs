// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using osu.Framework.MathUtils;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public class TextBox : Container
    {
        private FlowContainer textFlow;
        private Box background;
        private Box cursor;
        private Container textContainer;

        public int? LengthLimit;

        public bool AllowClipboardExport => true;

        //represents the left/right selection coordinates of the word double clicked on when dragging
        private int[] doubleClickWord = null;

        BaseGame game;

        /// <summary>
        /// Should this TextBox accept arrow keys for navigation?
        /// </summary>
        public bool HandleLeftRightArrows = true;

        protected virtual Color4 BackgroundCommit => new Color4(249, 90, 255, 200);
        protected virtual Color4 BackgroundFocused => new Color4(100, 100, 100, 255);
        protected virtual Color4 BackgroundUnfocused => new Color4(100, 100, 100, 120);

        public bool ReadOnly;

        public delegate void OnCommitHandler(TextBox sender, bool newText);

        public event OnCommitHandler OnCommit;
        public event OnCommitHandler OnChange;

        private Scheduler textUpdateScheduler = new Scheduler();

        public override void Load(BaseGame game)
        {
            base.Load(game);

            this.game = game;

            Masking = true;
            CornerRadius = 3;

            Add(background = new Box
            {
                Colour = BackgroundUnfocused,
                RelativeSizeAxes = Axes.Both,
            });

            Add(textContainer = new Container
            {
                RelativeSizeAxes = Axes.Both
            });

            textFlow = new FlowContainer
            {
                Direction = FlowDirection.HorizontalOnly,
                AutoSizeAxes = Axes.Both,
            };

            cursor = new Box
            {
                Depth = float.MinValue,
                Size = Vector2.One,
                Colour = Color4.Transparent,
                RelativeSizeAxes = Axes.Y,
                Alpha = 0,
            };

            textContainer.Add(cursor);
            textContainer.Add(textFlow);
        }

        private void resetSelection()
        {
            selectionStart = selectionEnd;
            cursorAndLayout.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            OnChange = null;
            OnCommit = null;

            base.Dispose(disposing);
        }

        private float textContainerPosX;

        private string textAtLastLayout = string.Empty;

        protected override void UpdateLayout()
        {
            base.UpdateLayout();

            //have to run this after children flow
            cursorAndLayout.Refresh(delegate
            {
                textUpdateScheduler.Update();

                Vector2 cursorPos = Vector2.Zero;
                if (text?.Length > 0)
                    cursorPos.X = getPositionAt(selectionLeft);

                float cursorPosEnd = getPositionAt(selectionEnd);

                float cursorWidth = 2;

                if (selectionLength > 0)
                    cursorWidth = getPositionAt(selectionRight) - cursorPos.X;

                float cursorRelativePositionAxesInBox = (cursorPosEnd - textContainerPosX) / DrawWidth;

                //we only want to reposition the view when the cursor reaches near the extremities.
                if (cursorRelativePositionAxesInBox < 0.1 || cursorRelativePositionAxesInBox > 0.9)
                {
                    textContainerPosX = cursorPosEnd - DrawWidth / 2;
                }

                textContainerPosX = MathHelper.Clamp(textContainerPosX, 0, Math.Max(0, textFlow.DrawWidth - DrawWidth));

                textContainer.MoveToX(-textContainerPosX, 300, EasingTypes.OutExpo);

                if (HasFocus)
                {
                    cursor.ClearTransformations();
                    cursor.MoveTo(cursorPos, 60, EasingTypes.Out);
                    cursor.ScaleTo(new Vector2(cursorWidth, 1), 60, EasingTypes.Out);

                    if (selectionLength > 0)
                    {
                        cursor.FadeTo(0.5f, 200, EasingTypes.Out);
                        cursor.FadeColour(new Color4(249, 90, 255, 255), 200, EasingTypes.Out);
                    }
                    else
                    {
                        cursor.FadeTo(0.5f, 200, EasingTypes.Out);
                        cursor.FadeColour(Color4.White, 200, EasingTypes.Out);
                        cursor.Transforms.Add(new TransformAlpha(Clock)
                              {
                                  StartValue = 0.5f,
                                  EndValue = 0.2f,
                                  StartTime = Time,
                                  EndTime = Time + 500,
                                  Easing = EasingTypes.InOutSine,
                                  LoopCount = -1,
                              });
                    }
                }

                OnChange?.Invoke(this, textAtLastLayout != text);
                textAtLastLayout = text;

                return cursorPos;
            });
        }

        private float getPositionAt(int index)
        {
            if (index > 0)
            {
                if (index < text.Length)
                    return textFlow.Children.ElementAt(index).DrawPosition.X + textFlow.DrawPosition.X;
                var d = textFlow.Children.ElementAt(index - 1);
                return d.DrawPosition.X + d.DrawSize.X + textFlow.Spacing.X + textFlow.DrawPosition.X;
            }
            return 0;
        }

        private int getCharacterClosestTo(Vector2 pos)
        {
            pos = textFlow.GetLocalPosition(pos * DrawInfo.Matrix);

            int i = 0;
            foreach (Drawable d in textFlow.Children)
            {
                if (d.DrawPosition.X + d.DrawSize.X / 2 > pos.X)
                    break;
                i++;
            }

            return i;
        }

        int selectionStart;
        int selectionEnd;

        int selectionLength => Math.Abs(selectionEnd - selectionStart);

        int selectionLeft => Math.Min(selectionStart, selectionEnd);
        int selectionRight => Math.Max(selectionStart, selectionEnd);

        Cached<Vector2> cursorAndLayout = new Cached<Vector2>();

        private void moveSelection(int offset, bool expand)
        {
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
                game.Audio.Sample.Get(@"Keyboard/key-movement")?.Play();
                cursorAndLayout.Invalidate();
            }
        }

        private bool removeCharacterOrSelection(bool sound = true)
        {
            if (text.Length == 0) return false;
            if (selectionLength == 0 && selectionLeft == 0) return false;

            int count = MathHelper.Clamp(selectionLength, 1, text.Length);
            int start = MathHelper.Clamp(selectionLength > 0 ? selectionLeft : selectionLeft - 1, 0, text.Length - count);

            if (count == 0) return false;

            if (sound)
                game.Audio.Sample.Get(@"Keyboard/key-delete")?.Play();

            foreach (var d in textFlow.Children.Skip(start).Take(count).ToArray()) //ToArray since we are removing items from the children in this block.
            {
                textFlow.Remove(d);

                textContainer.Add(d);
                d.FadeOut(200);
                d.MoveToY(d.DrawSize.Y, 200, EasingTypes.InExpo);
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

        protected virtual Drawable AddCharacterToFlow(char c)
        {
            int i = selectionLeft;
            foreach (Drawable dd in textFlow.Children.Skip(selectionLeft).Take(text.Length - selectionLeft))
                dd.Depth = i + 1;

            Drawable ch;

            textFlow.Add(ch = new SpriteText
            {
                Text = c.ToString(),
                TextSize = DrawSize.Y,
                Depth = selectionLeft,
            });

            return ch;
        }

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
            if (char.IsControl(c)) return null;

            if (selectionLength > 0)
                removeCharacterOrSelection();

            if (text.Length + 1 > LengthLimit)
            {
                if (background.Alpha > 0)
                    background.FlashColour(Color4.Red, 200);
                else
                    textFlow.FlashColour(Color4.Red, 200);
                return null;
            }

            Drawable ch = AddCharacterToFlow(c);

            ch.Position = new Vector2(0, DrawSize.Y);
            ch.MoveToY(0, 200, EasingTypes.OutExpo);

            text = text.Insert(selectionLeft, c.ToString());
            selectionStart = selectionEnd = selectionLeft + 1;

            cursorAndLayout.Invalidate();

            return ch;
        }

        private string text = string.Empty;

        public virtual string Text
        {
            get { return text; }
            set
            {
                Debug.Assert(value != null);

                if (value == text)
                    return;

                textUpdateScheduler.Add(delegate
                {
                    int startBefore = selectionStart;
                    selectionStart = selectionEnd = 0;
                    textFlow?.Clear();
                    text = string.Empty;

                    foreach (char c in value)
                        addCharacter(c);

                    selectionStart = MathHelper.Clamp(startBefore, 0, text.Length);
                });

                cursorAndLayout.Invalidate();
            }
        }

        public string SelectedText => selectionLength > 0 ? Text.Substring(selectionLeft, selectionLength) : string.Empty;

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (!HasFocus)
                return false;

            switch (args.Key)
            {
                case Key.Tab:
                    return false;
                case Key.End:
                    moveSelection(text.Length, state.Keyboard.ShiftPressed);
                    return true;
                case Key.Home:
                    moveSelection(-text.Length, state.Keyboard.ShiftPressed);
                    return true;
                case Key.Left:
                {
                    if (!HandleLeftRightArrows) return false;

                    if (selectionEnd == 0)
                    {
                        //we only clear if you aren't holding shift
                        if (!state.Keyboard.ShiftPressed)
                            resetSelection();
                        return true;
                    }

                    int amount = 1;
                    if (state.Keyboard.ControlPressed)
                    {
                        int lastSpace = text.LastIndexOf(' ', Math.Max(0, selectionEnd - 2));
                        if (lastSpace >= 0)
                        {
                            //if you have something selected and shift is not held down
                            //A selection reset is required to select a word inside the current selection
                            if(!state.Keyboard.ShiftPressed)
                                resetSelection();
                            amount = selectionEnd - lastSpace - 1;
                        }
                         else
                            amount = selectionEnd;
                    }

                    moveSelection(-amount, state.Keyboard.ShiftPressed);
                    return true;
                }
                case Key.Right:
                {
                    if (!HandleLeftRightArrows) return false;

                    if (selectionEnd == text.Length)
                    {
                        if (!state.Keyboard.ShiftPressed)
                            resetSelection();
                        return true;
                    }

                    int amount = 1;
                    if (state.Keyboard.ControlPressed)
                    {
                        int nextSpace = text.IndexOf(' ', selectionEnd + 1);
                        if (nextSpace >= 0)
                        {
                            if (!state.Keyboard.ShiftPressed)
                                resetSelection();
                            amount = nextSpace - selectionEnd;
                        }
                        else
                            amount = text.Length - selectionEnd;
                    }

                    moveSelection(amount, state.Keyboard.ShiftPressed);
                    return true;
                }    
                case Key.Enter:
                    selectionStart = selectionEnd = 0;
                    TriggerFocusLost(state);
                    return true;
                case Key.Delete:
                    if (selectionLength == 0)
                    {
                        if (text.Length == selectionStart)
                            return true;

                        if (state.Keyboard.ControlPressed)
                        {
                            int spacePos = selectionStart;
                            while (text[spacePos] == ' ' && spacePos < text.Length)
                                spacePos++;

                            spacePos = MathHelper.Clamp(text.IndexOf(' ', spacePos), 0, text.Length);
                            selectionEnd = spacePos;

                            if (selectionStart == 0 && spacePos == 0)
                                selectionEnd = text.Length;

                            if (selectionLength == 0)
                                return true;
                        }
                        else
                        {
                            //we're deleting in front of the cursor, so move the cursor forward once first
                            selectionStart = selectionEnd = selectionStart + 1;
                        }
                    }

                    removeCharacterOrSelection();
                    return true;
                case Key.Back:
                    if (selectionLength == 0 && state.Keyboard.ControlPressed)
                    {
                        int spacePos = selectionLeft >= 2 ? Math.Max(0, text.LastIndexOf(' ', selectionLeft - 2) + 1) : 0;
                        selectionStart = spacePos;
                    }

                    removeCharacterOrSelection();
                    return true;
            }

            if (state.Keyboard.ControlPressed)
            {
                //handling of function keys
                switch (args.Key)
                {
                    case Key.A:
                        selectionStart = 0;
                        selectionEnd = text.Length;
                        cursorAndLayout.Invalidate();
                        return true;
                    case Key.C:
                        if (string.IsNullOrEmpty(SelectedText) || !AllowClipboardExport) return true;
                        //System.Windows.Forms.Clipboard.SetText(SelectedText);
                        return true;
                    case Key.X:
                        if (string.IsNullOrEmpty(SelectedText)) return true;

                        //if (AllowClipboardExport)
                        //    System.Windows.Forms.Clipboard.SetText(SelectedText);
                        removeCharacterOrSelection();
                        return true;
                    case Key.V:
                        // TODO: clipboard
                        return true;
                }

                return false;
            }

            return false;
        }
        
        protected override bool OnCharacterInput(char c)
        {
            game.Audio.Sample.Get($@"Keyboard/key-press-{RNG.Next(1, 5)}")?.Play();
            insertString(c.ToString());
            return true;
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
                    selectionEnd = selectionEnd >= 0 ? (selectionEnd+1) : 0;
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
                    TriggerFocus();

                cursorAndLayout.Invalidate();
            }
            return true;
        }

        protected override bool OnDragStart(InputState state)
        {
            //need to handle this so we get onDrag events.
            return true;
        }

        protected override bool OnDoubleClick(InputState state)
        {
            if (text.Length == 0) return true;

            int hover = Math.Min(text.Length - 1, getCharacterClosestTo(state.Mouse.Position));

            int lastSeparator = findSeparatorIndex(text, hover, -1);
            int nextSeparator = findSeparatorIndex(text, hover, 1);

            selectionStart = lastSeparator >= 0 ? lastSeparator + 1 : 0;
            selectionEnd = nextSeparator >= 0 ? nextSeparator : text.Length;

            //in order to keep the home word selected
            doubleClickWord = new int[] { selectionStart, selectionEnd };

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
            selectionStart = selectionEnd = getCharacterClosestTo(state.Mouse.Position);

            cursorAndLayout.Invalidate();

            return true;
        }

        protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
        {
            doubleClickWord = null;
            return true;
        }

        protected override void OnFocusLost(InputState state)
        {
            cursor.ClearTransformations();
            cursor.FadeOut(200);

            if (state.Keyboard.Keys.Contains(Key.Enter))
            {
                background.Colour = BackgroundUnfocused;
                background.ClearTransformations();
                background.FlashColour(BackgroundCommit, 400);

                game.Audio.Sample.Get(@"Keyboard/key-confirm")?.Play();
                OnCommit?.Invoke(this, true);
            }
            else
            {
                background.ClearTransformations();
                background.FadeColour(BackgroundUnfocused, 200, EasingTypes.OutExpo);
            }

            cursorAndLayout.Invalidate();
        }

        protected override bool OnFocus(InputState state)
        {
            if (ReadOnly) return false;

            background.ClearTransformations();
            background.FadeColour(BackgroundFocused, 200, EasingTypes.Out);

            cursorAndLayout.Invalidate();
            return true;
        }
    }
}
