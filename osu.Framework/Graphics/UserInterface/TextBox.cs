// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Development;
using osu.Framework.Extensions.PlatformActionExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class TextBox : TabbableContainer, IHasCurrentValue<string>, IKeyBindingHandler<PlatformAction>
    {
        protected FillFlowContainer TextFlow { get; private set; }
        protected Container TextContainer { get; private set; }

        public override bool HandleNonPositionalInput => HasFocus;

        /// <summary>
        /// Padding to be used within the TextContainer. Requires special handling due to the sideways scrolling of text content.
        /// </summary>
        protected virtual float LeftRightPadding => 5;

        /// <summary>
        /// Maximum allowed length of text.
        /// </summary>
        /// <remarks>Any input beyond this limit will be dropped and then <see cref="NotifyInputError"/> will be called.</remarks>
        public int? LengthLimit;

        /// <summary>
        /// Whether clipboard copying functionality is allowed.
        /// </summary>
        protected virtual bool AllowClipboardExport => true;

        /// <summary>
        /// Whether seeking to word boundaries is allowed.
        /// </summary>
        protected virtual bool AllowWordNavigation => true;

        /// <summary>
        /// Represents the left/right selection coordinates of the word double clicked on when dragging.
        /// </summary>
        private int[] doubleClickWord;

        /// <summary>
        /// Whether this TextBox should accept left and right arrow keys for navigation.
        /// </summary>
        public virtual bool HandleLeftRightArrows => true;

        /// <summary>
        /// Whether to allow IME input when this text box has input focus.
        /// </summary>
        /// <remarks>
        /// This is just a hint to the native implementation, some might respect this,
        /// while others will ignore and always have the IME (dis)allowed.
        /// </remarks>
        /// <example>
        /// Useful for situations where IME input is not wanted, such as for passwords, numbers, or romanised text.
        /// </example>
        protected virtual bool AllowIme => true;

        /// <summary>
        /// Check if a character can be added to this TextBox.
        /// </summary>
        /// <param name="character">The pending character.</param>
        /// <returns>Whether the character is allowed to be added.</returns>
        protected virtual bool CanAddCharacter(char character) => true;

        /// <summary>
        /// Private helper for <see cref="CanAddCharacter"/>, additionally requiring that the character is not a control character.
        /// </summary>
        private bool canAddCharacter(char character) => !char.IsControl(character) && CanAddCharacter(character);

        private bool readOnly;

        public bool ReadOnly
        {
            get => readOnly;
            set
            {
                readOnly = value;

                if (readOnly)
                    KillFocus();
            }
        }

        /// <summary>
        /// Whether the textbox should rescind focus on commit.
        /// </summary>
        public bool ReleaseFocusOnCommit { get; set; } = true;

        /// <summary>
        /// Whether a commit should be triggered whenever the textbox loses focus.
        /// </summary>
        public bool CommitOnFocusLost { get; set; }

        public override bool CanBeTabbedTo => !ReadOnly;

        [Resolved]
        private TextInputSource textInput { get; set; }

        private Clipboard clipboard;

        /// <summary>
        /// Whether the <see cref="GameHost"/> is active (has keyboard focus).
        /// </summary>
        private IBindable<bool> isActive;

        private readonly Caret caret;

        public delegate void OnCommitHandler(TextBox sender, bool newText);

        /// <summary>
        /// Fired whenever text is committed via a user action.
        /// This usually happens on pressing enter, but can also be triggered on focus loss automatically, via <see cref="CommitOnFocusLost"/>.
        /// </summary>
        public event OnCommitHandler OnCommit;

        /// <summary>
        /// Scheduler used for scheduling text input events coming from <see cref="textInput"/>.
        /// </summary>
        /// <remarks>
        /// Used for scheduling text events so that the <see cref="Text"/> is updated on the update thread.
        /// This scheduler is updated in two places / at two points in time:
        ///  - Early in the update frame, in <see cref="OnKeyDown"/>, so that the key event is blocked. We assume a key event that comes right after a
        ///    text event is associated with that text event and therefore should be blocked. In other words: to ensure consistent UX, if a user
        ///    presses a key to input text then no other action (eg. from a keyboard shortcut) should be taken by the game, so we block it.
        ///  - Later in the same update frame, in <see cref="Update"/>. In case there was no associated key event. This is mostly required for mobile platforms.
        /// </remarks>
        private readonly Scheduler textInputScheduler = new Scheduler(() => ThreadSafety.IsUpdateThread, null);

        /// <summary>
        /// Scheduler used for scheduling IME composition and result events coming from <see cref="textInput"/>.
        /// </summary>
        private readonly Scheduler imeCompositionScheduler = new Scheduler(() => ThreadSafety.IsUpdateThread, null);

        protected TextBox()
        {
            Masking = true;

            Children = new Drawable[]
            {
                TextContainer = new Container
                {
                    AutoSizeAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Position = new Vector2(LeftRightPadding, 0),
                    Children = new Drawable[]
                    {
                        Placeholder = CreatePlaceholder(),
                        caret = CreateCaret(),
                        TextFlow = new FillFlowContainer
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Direction = FillDirection.Horizontal,
                            AutoSizeAxes = Axes.X,
                            RelativeSizeAxes = Axes.Y,
                        },
                    },
                },
            };

            Current.ValueChanged += e =>
            {
                // we generally want Text and Current to be synchronised at all times.
                // a change to Text will trigger a Current set, and potentially cause a feedback loop which isn't always desirable
                // (could lead to no animations playing out, etc.)
                // the following guard is supposed to not allow that feedback loop to close.
                if (Text != e.NewValue)
                    Text = e.NewValue;
            };

            caretVisible = false;
            caret.Hide();
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            clipboard = host.GetClipboard();
            isActive = host.IsActive.GetBoundCopy();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            isActive.BindValueChanged(_ => Scheduler.AddOnce(updateCaretVisibility));
            Current.BindDisabledChanged(disabled =>
            {
                if (disabled)
                {
                    // disabling Current means that the textbox shouldn't accept any more user input.
                    // if there is currently an ongoing composition, we want to finalize it and reset the user's IME
                    // so that the user understands that compositing is done and that further input won't be accepted.
                    FinalizeImeComposition(false);
                }
            });

            setText(Text);
        }

        public virtual bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
        {
            if (!HasFocus)
                return false;

            if (!HandleLeftRightArrows && (e.Action == PlatformAction.MoveBackwardChar || e.Action == PlatformAction.MoveForwardChar))
                return false;

            if (e.Action.IsCommonTextEditingAction() && ImeCompositionActive)
                return true;

            var lastSelectionBounds = getTextSelectionBounds();

            switch (e.Action)
            {
                // Clipboard
                case PlatformAction.Cut:
                case PlatformAction.Copy:
                    if (string.IsNullOrEmpty(SelectedText) || !AllowClipboardExport) return true;

                    clipboard?.SetText(SelectedText);

                    if (e.Action == PlatformAction.Cut)
                        DeleteBy(0);

                    return true;

                case PlatformAction.Paste:
                    if (textInputBlocking)
                        // TextInputSource received text while this action got activated.
                        // This is an indicator that text has already been pasted at an OS level
                        // and has been received here through the TextInputSource flow.
                        //
                        // This is currently only happening on iOS since it relies on a hidden UITextField for software keyboard.
                        return true;

                    InsertString(clipboard?.GetText());
                    return true;

                case PlatformAction.SelectAll:
                    selectionStart = 0;
                    selectionEnd = text.Length;
                    cursorAndLayout.Invalidate();
                    onTextSelectionChanged(TextSelectionType.All, lastSelectionBounds);
                    return true;

                // Cursor Manipulation
                case PlatformAction.MoveBackwardChar:
                    MoveCursorBy(-1);
                    return true;

                case PlatformAction.MoveForwardChar:
                    MoveCursorBy(1);
                    return true;

                case PlatformAction.MoveBackwardWord:
                    MoveCursorBy(GetBackwardWordAmount());
                    return true;

                case PlatformAction.MoveForwardWord:
                    MoveCursorBy(GetForwardWordAmount());
                    return true;

                case PlatformAction.MoveBackwardLine:
                    MoveCursorBy(GetBackwardLineAmount());
                    return true;

                case PlatformAction.MoveForwardLine:
                    MoveCursorBy(GetForwardLineAmount());
                    return true;

                // Deletion
                case PlatformAction.DeleteBackwardChar:
                    DeleteBy(-1);
                    return true;

                case PlatformAction.DeleteForwardChar:
                    DeleteBy(1);
                    return true;

                case PlatformAction.DeleteBackwardWord:
                    DeleteBy(GetBackwardWordAmount());
                    return true;

                case PlatformAction.DeleteForwardWord:
                    DeleteBy(GetForwardWordAmount());
                    return true;

                case PlatformAction.DeleteBackwardLine:
                    DeleteBy(GetBackwardLineAmount());
                    return true;

                case PlatformAction.DeleteForwardLine:
                    DeleteBy(GetForwardLineAmount());
                    return true;

                // Expand selection
                case PlatformAction.SelectBackwardChar:
                    ExpandSelectionBy(-1);
                    onTextSelectionChanged(TextSelectionType.Character, lastSelectionBounds);
                    return true;

                case PlatformAction.SelectForwardChar:
                    ExpandSelectionBy(1);
                    onTextSelectionChanged(TextSelectionType.Character, lastSelectionBounds);
                    return true;

                case PlatformAction.SelectBackwardWord:
                    ExpandSelectionBy(GetBackwardWordAmount());
                    onTextSelectionChanged(TextSelectionType.Word, lastSelectionBounds);
                    return true;

                case PlatformAction.SelectForwardWord:
                    ExpandSelectionBy(GetForwardWordAmount());
                    onTextSelectionChanged(TextSelectionType.Word, lastSelectionBounds);
                    return true;

                case PlatformAction.SelectBackwardLine:
                    ExpandSelectionBy(GetBackwardLineAmount());
                    // TODO: Differentiate 'line' and 'all' selection types if/when multi-line support is added
                    onTextSelectionChanged(TextSelectionType.All, lastSelectionBounds);
                    return true;

                case PlatformAction.SelectForwardLine:
                    ExpandSelectionBy(GetForwardLineAmount());
                    // TODO: Differentiate 'line' and 'all' selection types if/when multi-line support is added
                    onTextSelectionChanged(TextSelectionType.All, lastSelectionBounds);
                    return true;
            }

            return false;
        }

        public virtual void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
        {
        }

        /// <summary>
        /// Find the word boundary in the backward direction, then return the negative amount of characters.
        /// </summary>
        protected int GetBackwardWordAmount()
        {
            if (!AllowWordNavigation)
                return -1;

            int searchPrev = Math.Clamp(selectionEnd - 1, 0, Math.Max(0, Text.Length - 1));
            while (searchPrev > 0 && text[searchPrev] == ' ')
                searchPrev--;
            int lastSpace = text.LastIndexOf(' ', searchPrev);
            return lastSpace > 0 ? -(selectionEnd - lastSpace - 1) : -selectionEnd;
        }

        /// <summary>
        /// Find the word boundary in the forward direction, then return the positive amount of characters.
        /// </summary>
        protected int GetForwardWordAmount()
        {
            if (!AllowWordNavigation)
                return 1;

            int searchNext = Math.Clamp(selectionEnd, 0, Math.Max(0, Text.Length - 1));
            while (searchNext < Text.Length && text[searchNext] == ' ')
                searchNext++;
            int nextSpace = text.IndexOf(' ', searchNext);
            return (nextSpace >= 0 ? nextSpace : text.Length) - selectionEnd;
        }

        // Currently only single line is supported and line length and text length are the same.
        protected int GetBackwardLineAmount() => -text.Length;

        protected int GetForwardLineAmount() => text.Length;

        /// <summary>
        /// Move the current cursor by the signed <paramref name="amount"/>.
        /// </summary>
        protected void MoveCursorBy(int amount)
        {
            var lastSelectionBounds = getTextSelectionBounds();
            selectionStart = selectionEnd;
            cursorAndLayout.Invalidate();
            moveSelection(amount, false);
            onTextDeselected(lastSelectionBounds);
        }

        /// <summary>
        /// Expand the current selection by the signed <paramref name="amount"/>.
        /// </summary>
        protected void ExpandSelectionBy(int amount)
        {
            moveSelection(amount, true);
        }

        /// <summary>
        /// If there is a selection, delete the selected text.
        /// Otherwise, delete characters from the cursor position by the signed <paramref name="amount"/>.
        /// A negative amount represents a backward deletion, and a positive amount represents a forward deletion.
        /// </summary>
        protected void DeleteBy(int amount)
        {
            if (selectionLength == 0)
                selectionEnd = Math.Clamp(selectionStart + amount, 0, text.Length);

            if (selectionLength > 0)
            {
                string removedText = removeSelection();
                OnUserTextRemoved(removedText);
            }
        }

        /// <summary>
        /// Finalize the current IME composition if one is active.
        /// </summary>
        /// <param name="userEvent">
        /// Whether this was invoked from a user action.
        /// Set to <c>true</c> to have <see cref="OnImeResult"/> invoked.
        /// </param>
        /// <remarks>Must only be called from the update thread.</remarks>
        protected void FinalizeImeComposition(bool userEvent)
        {
            // do nothing if there isn't an active composition.
            // importantly, if there are pending tasks, we should finish those off regardless
            // and then call `onImeResult()`.
            // the composition being inactive and having scheduled tasks shouldn't happen,
            // but the check is here to cover that improbable edge case.
            if (!ImeCompositionActive && !imeCompositionScheduler.HasPendingTasks)
                return;

            imeCompositionScheduler.Add(() => onImeResult(userEvent, false));

            if (textInputBound)
                textInput.ResetIme();

            // importantly, we want to force-update all pending composition events,
            // so that when we return control to the caller, those events won't mutate text and/or caret position.
            imeCompositionScheduler.Update();
        }

        /// <summary>
        /// Cancels the current IME composition, removing it from the <see cref="Text"/>.
        /// </summary>
        /// <remarks>Must only be called from the update thread.</remarks>
        protected void CancelImeComposition()
        {
            // same rationale as above, in `FinalizeImeComposition()`
            if (!ImeCompositionActive && !imeCompositionScheduler.HasPendingTasks)
                return;

            if (textInputBound)
                textInput.ResetIme();

            imeCompositionScheduler.Add(() => onImeComposition(string.Empty, 0, 0, false));
            imeCompositionScheduler.Update(); // same rationale as above, in `FinalizeImeComposition()`
        }

        protected override void Dispose(bool isDisposing)
        {
            OnCommit = null;

            unbindInput(false);

            base.Dispose(isDisposing);
        }

        private float textContainerPosX;

        private string textAtLastLayout = string.Empty;

        private void updateCursorAndLayout()
        {
            Placeholder.Font = Placeholder.Font.With(size: CalculatedTextSize);

            float cursorPos = 0;
            if (text.Length > 0)
                cursorPos = getPositionAt(selectionLeft);

            float cursorPosEnd = getPositionAt(selectionEnd);

            float? selectionWidth = null;
            if (selectionLength > 0)
                selectionWidth = getPositionAt(selectionRight) - cursorPos;

            float cursorRelativePositionAxesInBox = (cursorPosEnd - textContainerPosX) / (DrawWidth - 2 * LeftRightPadding);

            //we only want to reposition the view when the cursor reaches near the extremities.
            if (cursorRelativePositionAxesInBox < 0.1 || cursorRelativePositionAxesInBox > 0.9)
            {
                textContainerPosX = cursorPosEnd - DrawWidth / 2 + LeftRightPadding * 2;
            }

            textContainerPosX = Math.Clamp(textContainerPosX, 0, Math.Max(0, TextFlow.DrawWidth - DrawWidth + LeftRightPadding * 2));

            TextContainer.MoveToX(LeftRightPadding - textContainerPosX, 300, Easing.OutExpo);

            if (caretVisible)
                caret.DisplayAt(new Vector2(cursorPos, 0), selectionWidth);

            if (textAtLastLayout.Length == 0 || text.Length == 0)
            {
                if (text.Length == 0)
                    Placeholder.Show();
                else
                    Placeholder.Hide();
            }

            textAtLastLayout = text;
        }

        protected override void Update()
        {
            base.Update();

            // update the schedulers before updating children as it might mutate TextFlow.
            // we want the character drawables to be up-to date for further calculations in `updateCursorAndLayout()`.
            textInputScheduler.Update();
            imeCompositionScheduler.Update();
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            // have to run this after children flow
            if (!cursorAndLayout.IsValid)
            {
                // update in case selection length has changed.
                updateCaretVisibility();

                updateCursorAndLayout();
                cursorAndLayout.Validate();

                // keep the IME window up-to date with the current selection / composition string.
                updateImeWindowPosition();
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

        private readonly Cached cursorAndLayout = new Cached();

        private void moveSelection(int offset, bool expand)
        {
            if (textInput.ImeActive) return;

            int oldStart = selectionStart;
            int oldEnd = selectionEnd;

            if (expand)
                selectionEnd = Math.Clamp(selectionEnd + offset, 0, text.Length);
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
                    selectionEnd = selectionStart = Math.Clamp((offset > 0 ? selectionRight : selectionLeft) + offset, 0, text.Length);
            }

            if (oldStart != selectionStart || oldEnd != selectionEnd)
            {
                OnCaretMoved(expand);
                cursorAndLayout.Invalidate();
            }
        }

        /// <summary>
        /// Indicates whether a complex change operation to <see cref="Text"/> has begun.
        /// This is relevant because, for example, an insertion operation with text selected is really a removal of the selection and an insertion.
        /// We want to ensure that <see cref="Text"/> is transferred out to <see cref="Current"/> only at the end of such an operation chain.
        /// </summary>
        private bool textChanging;

        /// <summary>
        /// Starts a text change operation.
        /// </summary>
        /// <returns>Whether this call has initiated a text change.</returns>
        private bool beginTextChange()
        {
            if (textChanging)
                return false;

            return textChanging = true;
        }

        /// <summary>
        /// Ends a text change operation.
        /// This causes <see cref="Text"/> to be transferred out to <see cref="Current"/>.
        /// </summary>
        /// <param name="started">The return value of a corresponding <see cref="beginTextChange"/> call should be passed here.</param>
        private void endTextChange(bool started)
        {
            if (!started)
                return;

            if (Current.Value != Text)
                Current.Value = Text;

            textChanging = false;
        }

        /// <summary>
        /// Removes the selected text if a selection persists.
        /// </summary>
        private string removeSelection() => removeCharacters(selectionLength);

        /// <summary>
        /// Removes a specified <paramref name="number"/> of characters left side of the current position.
        /// </summary>
        /// <remarks>
        /// If a selection persists, <see cref="removeSelection"/> must be called instead.
        /// </remarks>
        /// <returns>A string of the removed characters.</returns>
        private string removeCharacters(int number = 1)
        {
            if (Current.Disabled || text.Length == 0)
                return string.Empty;

            int removeStart = Math.Clamp(selectionRight - number, 0, selectionRight);
            int removeCount = selectionRight - removeStart;

            if (removeCount == 0)
                return string.Empty;

            Debug.Assert(selectionLength == 0 || removeCount == selectionLength);

            bool beganChange = beginTextChange();

            foreach (var d in TextFlow.Children.Skip(removeStart).Take(removeCount).ToArray()) //ToArray since we are removing items from the children in this block.
            {
                TextFlow.Remove(d);

                TextContainer.Add(d);

                // account for potentially altered height of textbox
                d.Y = TextFlow.BoundingBox.Y;

                d.Hide();
                d.Expire();
            }

            string removedText = text.Substring(removeStart, removeCount);

            text = text.Remove(removeStart, removeCount);

            // Reorder characters depth after removal to avoid ordering issues with newly added characters.
            for (int i = removeStart; i < TextFlow.Count; i++)
                TextFlow.ChangeChildDepth(TextFlow[i], getDepthForCharacterIndex(i));

            selectionStart = selectionEnd = removeStart;

            endTextChange(beganChange);
            cursorAndLayout.Invalidate();

            return removedText;
        }

        /// <summary>
        /// Creates a single character. Override <see cref="Drawable.Show"/> and <see cref="Drawable.Hide"/> for custom behavior.
        /// </summary>
        /// <param name="c">The character that this <see cref="Drawable"/> should represent.</param>
        /// <returns>A <see cref="Drawable"/> that represents the character <paramref name="c"/> </returns>
        protected virtual Drawable GetDrawableCharacter(char c) => new SpriteText { Text = c.ToString(), Font = new FontUsage(size: CalculatedTextSize) };

        protected virtual Drawable AddCharacterToFlow(char c)
        {
            // Remove all characters to the right and store them in a local list,
            // such that their depth can be updated.
            List<Drawable> charsRight = new List<Drawable>();
            foreach (Drawable d in TextFlow.Children.Skip(selectionLeft))
                charsRight.Add(d);
            TextFlow.RemoveRange(charsRight);

            // Update their depth to make room for the to-be inserted character.
            int i = selectionLeft;
            foreach (Drawable d in charsRight)
                d.Depth = getDepthForCharacterIndex(i++);

            // Add the character
            Drawable ch = GetDrawableCharacter(c);
            ch.Depth = getDepthForCharacterIndex(selectionLeft);

            TextFlow.Add(ch);

            // Add back all the previously removed characters
            TextFlow.AddRange(charsRight);

            return ch;
        }

        private float getDepthForCharacterIndex(int index) => -index;

        protected float CalculatedTextSize => TextFlow.DrawSize.Y - (TextFlow.Padding.Top + TextFlow.Padding.Bottom);

        protected void InsertString(string value)
        {
            // inserting text could insert it in the middle of an active composition, leading to an invalid state.
            // so finalize the composition before adding text.
            FinalizeImeComposition(false);

            insertString(value);
        }

        private void insertString(string value, Action<Drawable> drawableCreationParameters = null)
        {
            if (string.IsNullOrEmpty(value)) return;

            if (Current.Disabled)
            {
                NotifyInputError();
                return;
            }

            bool beganChange = beginTextChange();

            foreach (char c in value)
            {
                if (!canAddCharacter(c))
                {
                    NotifyInputError();
                    continue;
                }

                if (selectionLength > 0)
                    removeSelection();

                if (text.Length + 1 > LengthLimit)
                {
                    NotifyInputError();
                    break;
                }

                Drawable drawable = AddCharacterToFlow(c);

                drawable.Show();
                drawableCreationParameters?.Invoke(drawable);

                text = text.Insert(selectionLeft, c.ToString());
                selectionStart = selectionEnd = selectionLeft + 1;

                cursorAndLayout.Invalidate();
            }

            endTextChange(beganChange);
        }

        /// <summary>
        /// Called whenever an invalid character has been entered
        /// </summary>
        protected abstract void NotifyInputError();

        /// <summary>
        /// Invoked when new text is added via user input.
        /// </summary>
        /// <param name="added">The text which was added.</param>
        protected virtual void OnUserTextAdded(string added)
        {
        }

        /// <summary>
        /// Invoked when text is removed via user input.
        /// </summary>
        /// <param name="removed">The text which was removed.</param>
        protected virtual void OnUserTextRemoved(string removed)
        {
        }

        /// <summary>
        /// Invoked whenever a text string has been committed to the textbox.
        /// </summary>
        /// <param name="textChanged">Whether the current text string is different than the last committed.</param>
        protected virtual void OnTextCommitted(bool textChanged)
        {
        }

        /// <summary>
        /// Invoked whenever the caret has moved from its position.
        /// </summary>
        /// <param name="selecting">Whether the caret is selecting text while moving.</param>
        protected virtual void OnCaretMoved(bool selecting)
        {
        }

        /// <summary>
        /// Invoked whenever text selection changes. For deselection, see <seealso cref="OnTextDeselected"/>.
        /// </summary>
        /// <param name="selectionType">The type of selection change that occured.</param>
        protected virtual void OnTextSelectionChanged(TextSelectionType selectionType)
        {
        }

        /// <summary>
        /// Invoked whenever selected text is deselected. For selection, see <seealso cref="OnTextSelectionChanged"/>.
        /// </summary>
        protected virtual void OnTextDeselected()
        {
        }

        private void onTextSelectionChanged(TextSelectionType selectionType, (int start, int end) lastSelectionBounds)
        {
            if (lastSelectionBounds.start == selectionStart && lastSelectionBounds.end == selectionEnd)
                return;

            if (selectionLength > 0)
                OnTextSelectionChanged(selectionType);
            else
                onTextDeselected(lastSelectionBounds);
        }

        private void onTextDeselected((int start, int end) lastSelectionBounds)
        {
            if (lastSelectionBounds.start == selectionStart && lastSelectionBounds.end == selectionEnd)
                return;

            if (lastSelectionBounds.start != lastSelectionBounds.end)
                OnTextDeselected();
        }

        private (int start, int end) getTextSelectionBounds() => (selectionStart, selectionEnd);

        /// <summary>
        /// Invoked whenever the IME composition has changed.
        /// </summary>
        /// <param name="newComposition">The current text of the composition.</param>
        /// <param name="removedTextLength">The number of characters that have been replaced by new ones.</param>
        /// <param name="addedTextLength">The number of characters that have replaced the old ones.</param>
        /// <param name="selectionMoved">Whether the selection/caret has moved.</param>
        protected virtual void OnImeComposition(string newComposition, int removedTextLength, int addedTextLength, bool selectionMoved)
        {
        }

        /// <summary>
        /// Invoked when the IME has finished compositing.
        /// </summary>
        /// <param name="result">The result of the composition.</param>
        /// <param name="successful">
        /// Whether this composition was finished trough normal means (eg. user normally finished compositing trough the IME).
        /// <c>false</c> if ended prematurely.
        /// </param>
        protected virtual void OnImeResult(string result, bool successful)
        {
        }

        /// <summary>
        /// Creates a placeholder that shows whenever the textbox is empty. Override <see cref="Drawable.Show"/> or <see cref="Drawable.Hide"/> for custom behavior.
        /// </summary>
        /// <returns>The placeholder</returns>
        protected abstract SpriteText CreatePlaceholder();

        protected SpriteText Placeholder;

        public LocalisableString PlaceholderText
        {
            get => Placeholder.Text;
            set => Placeholder.Text = value;
        }

        protected abstract Caret CreateCaret();

        /// <summary>
        /// Whether the <see cref="caret"/> should be visible.
        /// </summary>
        private bool caretVisible;

        private void updateCaretVisibility()
        {
            // a blinking cursor signals to the user that keyboard input will appear at that cursor,
            // hide the caret when we don't have keyboard focus to conform with that expectation.
            // importantly, we want the caret to remain visible when there is a selection.
            bool newVisibility = HasFocus && (isActive.Value || selectionLength != 0);

            if (caretVisible != newVisibility)
            {
                caretVisible = newVisibility;

                if (caretVisible)
                    caret.Show();
                else
                    caret.Hide();

                cursorAndLayout.Invalidate();
            }
        }

        private readonly BindableWithCurrent<string> current = new BindableWithCurrent<string>(string.Empty);

        public Bindable<string> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private string text = string.Empty;

        public virtual string Text
        {
            get => text;
            set
            {
                if (Current.Disabled)
                    return;

                if (value == text)
                    return;

                lastCommitText = value ??= string.Empty;

                if (value.Length == 0)
                    Placeholder.Show();
                else
                    Placeholder.Hide();

                setText(value);
            }
        }

        private void setText(string value)
        {
            bool beganChange = beginTextChange();

            // finalize and cleanup the IME composition (if one is active) so we get a clean slate for pending text changes and future IME composition.
            // `IsLoaded` check is required because `Text` could be set in the initializer / before the drawable loaded.
            // `FinalizeImeComposition()` crashes if textbox isn't fully loaded.
            if (IsLoaded) FinalizeImeComposition(false);

            selectionStart = selectionEnd = 0;

            TextFlow?.Clear();
            text = string.Empty;

            // insert string and fast forward any transforms (generally when replacing the full content of a textbox we don't want any kind of fade etc.).
            insertString(value, d => d.FinishTransforms());

            endTextChange(beganChange);
            cursorAndLayout.Invalidate();
        }

        public string SelectedText => selectionLength > 0 ? Text.Substring(selectionLeft, selectionLength) : string.Empty;

        /// <summary>
        /// Whether <see cref="KeyDownEvent"/>s should be blocked because of recent text input from a <see cref="TextInputSource"/>.
        /// </summary>
        /// <remarks>
        /// Blocking starts when a text events occurs and ends when all keys are released (or on the next frame if no keys are pressed).
        ///
        /// We currently eagerly block keydown events, blocking all key events until all keys are released.
        /// This means that some key events will be (erroneously) blocked, even if they weren't associated with a text event.
        /// This simplified logic is used because trying to associate each key event with a text event is error prone.
        /// Some reasons as to why:
        ///  - Text and key repeat rate are inherently different, since text repeat is handled by the OS, while <see cref="InputManager"/> handles key repeat.
        ///  - The ordering of keydown and text events can vary between platforms.
        ///  - The ordering of the events can vary even more because these events are propagated to the textbox differently:
        ///     - Key events are propagated by <see cref="UserInputManager"/> at the beginning of each update frame.
        ///     - Text events are propagated immediately when they're received, and are handled either by the <see cref="Update"/> call or <see cref="OnKeyDown"/>,
        ///       whichever comes first. (Check usages of <see cref="textInputScheduler"/>.<see cref="Scheduler.Update"/> for specifics.)
        ///     - This is especially problematic if the key and text events arrive in between the <see cref="UserInputManager"/> and <see cref="TextBox"/> updates.
        ///
        /// So we catch the first key that produced text and block until we get back to a sane state (all keys released).
        /// </remarks>
        private bool textInputBlocking;

        /// <summary>
        /// Whether there is a ongoing IME composition.
        /// </summary>
        /// <remarks>
        /// The IME should take full input priority, as a lot of the common text editing keys/shortcuts
        /// are used internally in the IME for compositing.
        /// Full data about the composition events is processed by <see cref="handleImeComposition"/> "passively"
        /// so we shouldn't take any action on key events we receive.
        /// </remarks>
        protected bool ImeCompositionActive => (textInputBound && textInput.ImeActive) || imeCompositionLength > 0;

        #region Input event handling

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (readOnly)
                return true;

            if (ImeCompositionActive)
                return true;

            switch (e.Key)
            {
                case Key.Escape:
                    // if keypress is repeating, the IME was probably closed with the first, non-repeating keypress
                    // so don't kill focus unless the user has explicitly released and pressed the key again.
                    if (!e.Repeat)
                        KillFocus();
                    return true;

                case Key.KeypadEnter:
                case Key.Enter:
                    // alt-enter is commonly used to toggle fullscreen.
                    if (e.AltPressed)
                        return false;

                    // same rationale as comment in case statement above.
                    if (!e.Repeat)
                        Commit();
                    return true;

                // avoid blocking certain keys which we need propagated to a PlatformActionContainer,
                // so that we can get them as appropriate `PlatformAction`s in OnPressed(KeyBindingPressEvent<PlatformAction>).
                case Key.BackSpace:
                case Key.Delete:
                    return false;
            }

            // check for any pending text input.
            // updating here will set `textInputBlocking` accordingly.
            textInputScheduler.Update();

            // block on recent text input *after* handling the above keys so those keys can be used during text input.
            return base.OnKeyDown(e) || textInputBlocking;
        }

        /// <summary>
        /// Removes focus from this <see cref="TextBox"/> if it currently has focus.
        /// </summary>
        protected virtual void KillFocus() => killFocus();

        private string lastCommitText = string.Empty;

        private void killFocus()
        {
            var manager = GetContainingInputManager();
            if (manager?.FocusedDrawable == this)
                manager.ChangeFocus(null);
        }

        /// <summary>
        /// Commits current text on this <see cref="TextBox"/> and releases focus if <see cref="ReleaseFocusOnCommit"/> is set.
        /// </summary>
        protected virtual void Commit()
        {
            FinalizeImeComposition(false);

            if (ReleaseFocusOnCommit && HasFocus)
            {
                killFocus();
                if (CommitOnFocusLost)
                    // the commit will happen as a result of the focus loss.
                    return;
            }

            bool isNew = text != lastCommitText;
            lastCommitText = text;

            OnTextCommitted(isNew);
            OnCommit?.Invoke(this, isNew);
        }

        protected override void OnKeyUp(KeyUpEvent e)
        {
            Scheduler.AddOnce(revertBlockingStateIfRequired);
            base.OnKeyUp(e);
        }

        protected override void OnDrag(DragEvent e)
        {
            if (ReadOnly)
                return;

            FinalizeImeComposition(true);

            var lastSelectionBounds = getTextSelectionBounds();

            if (doubleClickWord != null)
            {
                //select words at a time
                if (getCharacterClosestTo(e.MousePosition) > doubleClickWord[1])
                {
                    selectionStart = doubleClickWord[0];
                    selectionEnd = findSeparatorIndex(text, getCharacterClosestTo(e.MousePosition) - 1, 1);
                    selectionEnd = selectionEnd >= 0 ? selectionEnd : text.Length;
                }
                else if (getCharacterClosestTo(e.MousePosition) < doubleClickWord[0])
                {
                    selectionStart = doubleClickWord[1];
                    selectionEnd = findSeparatorIndex(text, getCharacterClosestTo(e.MousePosition), -1);
                    selectionEnd = selectionEnd >= 0 ? selectionEnd + 1 : 0;
                }
                else
                {
                    //in the middle
                    selectionStart = doubleClickWord[0];
                    selectionEnd = doubleClickWord[1];
                }
            }
            else
            {
                if (text.Length == 0) return;

                selectionEnd = getCharacterClosestTo(e.MousePosition);
                if (selectionLength > 0)
                    GetContainingInputManager().ChangeFocus(this);
            }

            cursorAndLayout.Invalidate();

            onTextSelectionChanged(doubleClickWord != null ? TextSelectionType.Word : TextSelectionType.Character, lastSelectionBounds);
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (HasFocus) return true;

            Vector2 posDiff = e.MouseDownPosition - e.MousePosition;

            return Math.Abs(posDiff.X) > Math.Abs(posDiff.Y);
        }

        protected override bool OnDoubleClick(DoubleClickEvent e)
        {
            FinalizeImeComposition(true);

            var lastSelectionBounds = getTextSelectionBounds();

            if (text.Length == 0) return true;

            if (AllowClipboardExport)
            {
                int hover = Math.Min(text.Length - 1, getCharacterClosestTo(e.MousePosition));

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

            onTextSelectionChanged(TextSelectionType.Word, lastSelectionBounds);

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

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (ReadOnly)
                return true;

            FinalizeImeComposition(true);

            var lastSelectionBounds = getTextSelectionBounds();

            selectionStart = selectionEnd = getCharacterClosestTo(e.MousePosition);

            cursorAndLayout.Invalidate();

            onTextDeselected(lastSelectionBounds);

            return false;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            doubleClickWord = null;
        }

        protected override void OnFocusLost(FocusLostEvent e)
        {
            // let's say that a focus loss is not a user event as focus is commonly indirectly lost.
            FinalizeImeComposition(false);

            unbindInput(e.NextFocused is TextBox);

            updateCaretVisibility();

            if (CommitOnFocusLost)
                Commit();
        }

        public override bool AcceptsFocus => true;

        protected override bool OnClick(ClickEvent e)
        {
            if (!ReadOnly && textInputBound)
                textInput.EnsureActivated(AllowIme);

            return !ReadOnly;
        }

        protected override void OnFocus(FocusEvent e)
        {
            bindInput(e.PreviouslyFocused is TextBox);

            updateCaretVisibility();
        }

        #endregion

        #region Native TextBox handling (platform-specific)

        /// <summary>
        /// Whether <see cref="textInput"/> has been activated and bound to.
        /// </summary>
        private bool textInputBound;

        private void bindInput(bool previousFocusWasTextBox)
        {
            if (textInputBound)
            {
                textInput.EnsureActivated(AllowIme);
                return;
            }

            // TextBox has special handling of text input activation when focus is changed directly from one TextBox to another.
            // We don't deactivate and activate, but instead keep text input active during the focus handoff, so that virtual keyboards on phones don't flicker.

            if (previousFocusWasTextBox)
                textInput.EnsureActivated(AllowIme);
            else
                textInput.Activate(AllowIme);

            textInput.OnTextInput += handleTextInput;
            textInput.OnImeComposition += handleImeComposition;
            textInput.OnImeResult += handleImeResult;

            textInputBound = true;
        }

        private void unbindInput(bool nextFocusIsTextBox)
        {
            if (!textInputBound)
                return;

            textInputBound = false;

            // see the comment above, in `bindInput(bool)`.
            if (!nextFocusIsTextBox)
                textInput.Deactivate();

            textInput.OnTextInput -= handleTextInput;
            textInput.OnImeComposition -= handleImeComposition;
            textInput.OnImeResult -= handleImeResult;

            // in case keys are held and we lose focus, we should no longer block key events
            textInputBlocking = false;
        }

        private void handleTextInput(string text) => textInputScheduler.Add(t =>
        {
            textInputBlocking = true;

            InsertString(t);
            OnUserTextAdded(t);

            // clear the flag in the next frame if no buttons are pressed/held.
            // needed in case a text event happens without an associated button press (and release).
            // this could be the case for software keyboards, for instance.
            Scheduler.AddOnce(revertBlockingStateIfRequired);
        }, text);

        /// <summary>
        /// Reverts the <see cref="textInputBlocking"/> flag to <c>false</c> if no keys are pressed.
        /// </summary>
        private void revertBlockingStateIfRequired() =>
            textInputBlocking &= GetContainingInputManager().CurrentState.Keyboard.Keys.HasAnyButtonPressed;

        private void handleImeComposition(string composition, int selectionStart, int selectionLength)
        {
            imeCompositionScheduler.Add(() => onImeComposition(composition, selectionStart, selectionLength, true));
        }

        private void handleImeResult(string result)
        {
            imeCompositionScheduler.Add(() =>
            {
                onImeComposition(result, result.Length, 0, false);
                onImeResult(true, true);
            });
        }

        /// <summary>
        /// Returns how many characters of the two strings match from the beginning, and from the end.
        /// </summary>
        /// <remarks>
        /// Characters matched from the beginning will not match from the end.
        /// </remarks>
        private void matchBeginningEnd(string a, string b, out int matchBeginning, out int matchEnd)
        {
            int minLength = Math.Min(a.Length, b.Length);

            matchBeginning = 0;

            for (int i = 0; i < minLength; i++)
            {
                if (a[i] == b[i])
                    matchBeginning = i + 1;
                else
                    break;
            }

            matchEnd = 0;

            // check how many match (of the ones we didn't match), starting from the end
            for (int i = 1; i <= minLength - matchBeginning; i++)
            {
                if (a[^i] == b[^i])
                    matchEnd = i;
                else
                    break;
            }
        }

        /// <summary>
        /// Sanitizes the given composition, ensuring it fits within <see cref="LengthLimit"/> and respects <see cref="CanAddCharacter"/>.
        /// </summary>
        /// <returns><c>true</c> if the composition was sanitized in some way.</returns>
        private bool sanitizeComposition(ref string composition, ref int selectionStart, ref int selectionLength)
        {
            bool sanitized = false;

            // remove characters that can't be added.

            var builder = new StringBuilder(composition);

            for (int index = 0; index < builder.Length; index++)
            {
                if (!canAddCharacter(builder[index]))
                {
                    builder.Remove(index, 1);
                    sanitized = true;

                    if (index < selectionStart)
                    {
                        selectionStart--;
                    }
                    else if (index < selectionStart + selectionLength)
                    {
                        selectionLength--;
                    }

                    // move index back so we don't skip over the next character.
                    index--;
                }
            }

            if (sanitized)
                composition = builder.ToString();

            // trim composition if goes beyond the LengthLimit.

            int lengthWithoutComposition = text.Length - imeCompositionLength;

            if (lengthWithoutComposition + composition.Length > LengthLimit)
            {
                composition = composition.Substring(0, (int)LengthLimit - lengthWithoutComposition);
                sanitized = true;
            }

            // keep selection within bounds.
            // the selection could be out of bounds if it was trimmed by the above,
            // or if the platform-native composition event was ill-formed.

            if (selectionStart > composition.Length)
            {
                selectionStart = composition.Length;
                sanitized = true;
            }

            if (selectionStart + selectionLength > composition.Length)
            {
                selectionLength = composition.Length - selectionStart;
                sanitized = true;
            }

            return sanitized;
        }

        /// <summary>
        /// Contains all the <see cref="Drawable"/>s from the <see cref="TextFlow"/>
        /// that are part of the current IME composition.
        /// </summary>
        private readonly List<Drawable> imeCompositionDrawables = new List<Drawable>();

        /// <summary>
        /// Length of the current IME composition.
        /// </summary>
        /// <remarks>A length of <c>0</c> means that IME composition isn't active.</remarks>
        private int imeCompositionLength => imeCompositionDrawables.Count;

        /// <summary>
        /// Index of the first character in the current composition.
        /// </summary>
        private int imeCompositionStart;

        /// <remarks>
        /// This checks which parts of the old and new compositions match,
        /// and only updates the non-matching part in the current composition text.
        /// </remarks>
        private void onImeComposition(string newComposition, int newSelectionStart, int newSelectionLength, bool userEvent)
        {
            if (Current.Disabled)
            {
                // don't raise error if composition text is empty, as the empty event could be generated indirectly,
                // and not by explicit user interaction. eg. if IME is reset, input language is changed, etc.
                if (userEvent && !string.IsNullOrEmpty(newComposition))
                {
                    NotifyInputError();

                    // importantly, we want to reset the IME so it doesn't falsely report that IME composition is active.
                    textInput.ResetIme();
                }

                return;
            }

            // used for tracking the selection to report for `OnImeComposition()`
            int oldStart = selectionStart;
            int oldEnd = selectionEnd;

            if (imeCompositionLength == 0)
            {
                // this is the start of a new composition, as we currently have no composition text.

                imeCompositionStart = selectionLeft;

                if (string.IsNullOrEmpty(newComposition))
                {
                    // we might get an empty composition when the IME is first activated,
                    // the IME mode has changed (eg. plaintext -> kana),
                    // or when the keyboard layout and language have changed to a one supported by an IME.
                    // we can use this opportunity to update the IME window so it appears in
                    // the correct place once the user starts compositing.
                    updateImeWindowPosition();

                    // early return as SDL might sometimes send empty text editing events.
                    // we don't want the currently selected text to be removed in that case
                    // (we only want it removed once the user has entered _some_ text).
                    // the composition text hasn't changed anyway, so there is no need to go
                    // through the rest of the method.
                    return;
                }

                if (selectionLength > 0)
                    removeSelection();
            }

            bool beganChange = beginTextChange();

            if (sanitizeComposition(ref newComposition, ref newSelectionStart, ref newSelectionLength))
            {
                NotifyInputError();
            }

            string oldComposition = text.Substring(imeCompositionStart, imeCompositionLength);

            matchBeginningEnd(oldComposition, newComposition, out int matchBeginning, out int matchEnd);

            // how many characters have been removed, starting from `matchBeginning`
            int removeCount = oldComposition.Length - matchEnd - matchBeginning;

            // remove the characters that don't match
            if (removeCount > 0)
            {
                selectionStart = imeCompositionStart + matchBeginning;
                selectionEnd = selectionStart + removeCount;
                removeSelection();

                imeCompositionDrawables.RemoveRange(matchBeginning, removeCount);
            }

            // how many characters have been added, starting from `matchBeginning`
            int addCount = newComposition.Length - matchEnd - matchBeginning;

            if (addCount > 0)
            {
                string addedText = newComposition.Substring(matchBeginning, addCount);

                // set up selection for `insertString`
                selectionStart = selectionEnd = imeCompositionStart + matchBeginning;

                int insertPosition = matchBeginning;
                insertString(addedText, d =>
                {
                    d.Alpha = 0.6f;
                    imeCompositionDrawables.Insert(insertPosition++, d);
                });
            }

            // update the selection to the one the IME requested.
            // this selection is only a hint to the user, and is not used in the compositing logic.
            selectionStart = imeCompositionStart + newSelectionStart;
            selectionEnd = selectionStart + newSelectionLength;

            if (userEvent) OnImeComposition(newComposition, removeCount, addCount, oldStart != selectionStart || oldEnd != selectionEnd);

            endTextChange(beganChange);
            cursorAndLayout.Invalidate();
        }

        private void onImeResult(bool userEvent, bool successful)
        {
            if (Current.Disabled)
            {
                if (userEvent) NotifyInputError();
                // importantly, we don't return here so that we can finalize the composition
                // if we were called because Current was disabled.
            }

            // we only succeeded if there is pending data in the textbox
            if (imeCompositionDrawables.Count > 0)
            {
                foreach (var d in imeCompositionDrawables)
                {
                    d.FadeTo(1, 200, Easing.Out);
                }

                // move the cursor to end of finalized composition.
                selectionStart = selectionEnd = imeCompositionStart + imeCompositionLength;

                if (userEvent) OnImeResult(text.Substring(imeCompositionStart, imeCompositionLength), successful);
            }

            imeCompositionDrawables.Clear();

            cursorAndLayout.Invalidate();
        }

        /// <summary>
        /// Updates the location of the platform-native IME composition window
        /// to the current composition string / current selection.
        /// </summary>
        private void updateImeWindowPosition()
        {
            if (!cursorAndLayout.IsValid || !textInputBound)
                return;

            int startIndex, endIndex;

            if (imeCompositionLength > 0)
            {
                startIndex = imeCompositionStart;
                endIndex = imeCompositionStart + imeCompositionLength;
            }
            else
            {
                startIndex = selectionLeft;
                endIndex = selectionRight;
            }

            float start = getPositionAt(startIndex) - textContainerPosX + LeftRightPadding;
            float end = getPositionAt(endIndex) - textContainerPosX + LeftRightPadding;

            if (LeftRightPadding <= DrawWidth - LeftRightPadding)
            {
                start = Math.Clamp(start, LeftRightPadding, DrawWidth - LeftRightPadding);
                end = Math.Clamp(end, LeftRightPadding, DrawWidth - LeftRightPadding);
            }
            else
            {
                // DrawWidth is probably zero/invalid, sane fallback instead of throwing in Math.Clamp
                start = 0;
                end = 0;
            }

            var compositionTextRectangle = new RectangleF
            {
                X = start,
                Y = 0,
                Width = end - start,
                Height = DrawHeight,
            };

            var quad = ToScreenSpace(compositionTextRectangle);
            textInput.SetImeRectangle(quad.AABBFloat);
        }

        #endregion

        public enum TextSelectionType
        {
            /// <summary>
            /// A character was added or removed from the selection.
            /// </summary>
            Character,

            /// <summary>
            /// A word was added or removed from the selection.
            /// </summary>
            Word,

            /// <summary>
            /// All of the text was selected (i.e. via <see cref="PlatformAction.SelectAll"/>).
            /// </summary>
            All
        }
    }
}
