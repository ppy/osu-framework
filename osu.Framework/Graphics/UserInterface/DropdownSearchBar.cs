// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract partial class DropdownSearchBar : VisibilityContainer
    {
        public TextBox TextBox { get; private set; } = null!;

        [Resolved]
        private GameHost host { get; set; } = null!;

        protected internal override IEnumerable<Drawable> AdditionalFocusTargets
        {
            get
            {
                // On mobile platforms, let's not make the keyboard popup unless the dropdown is intentionally searchable.
                // todo: however, this causes such dropdowns to not be searchable at all when using hardware keyboard on mobile.
                if (host.OnScreenKeyboardOverlapsGameWindow && !AlwaysDisplayOnFocus)
                    return Array.Empty<Drawable>();

                return new Drawable[] { TextBox };
            }
        }

        public Bindable<string> SearchTerm { get; } = new Bindable<string>();

        private bool alwaysDisplayOnFocus;

        public bool AlwaysDisplayOnFocus
        {
            get => alwaysDisplayOnFocus;
            set
            {
                alwaysDisplayOnFocus = value;

                if (IsLoaded)
                    updateVisibility();
            }
        }

        // Base implementation of these properties in VisibilityContainer include the condition of State == Visible.
        // However, we want to keep propagating input in order for the textbox to receive key/platform action events.
        public override bool PropagateNonPositionalInputSubTree => true;
        public override bool PropagatePositionalInputSubTree => true;

        [BackgroundDependencyLoader]
        private void load()
        {
            AlwaysPresent = true;
            RelativeSizeAxes = Axes.Both;

            InternalChild = TextBox = CreateTextBox().With(t =>
            {
                t.ManageFocus = false;
                t.RelativeSizeAxes = Axes.Both;
                t.Size = new Vector2(1f);
                t.Current = SearchTerm;
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            SearchTerm.BindValueChanged(v => updateVisibility());
            updateVisibility();
        }

        protected override void OnFocus(FocusEvent e)
        {
            base.OnFocus(e);
            updateVisibility();
        }

        protected override void OnFocusLost(FocusLostEvent e)
        {
            base.OnFocusLost(e);
            SearchTerm.Value = string.Empty;
            updateVisibility();
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape && !string.IsNullOrEmpty(SearchTerm.Value))
            {
                SearchTerm.Value = string.Empty;
                return true;
            }

            return false;
        }

        private void updateVisibility() => State.Value = HasFocus && (AlwaysDisplayOnFocus || !string.IsNullOrEmpty(SearchTerm.Value))
            ? Visibility.Visible
            : Visibility.Hidden;

        protected abstract TextBox CreateTextBox();
    }
}
