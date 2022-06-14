// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class TabItem : ClickableContainer
    {
        /// <summary>
        /// If false, ths <see cref="TabItem{T}"/> cannot be removed from its <see cref="TabControl{T}"/>.
        /// </summary>
        public abstract bool IsRemovable { get; }
    }

    public abstract class TabItem<T> : TabItem
    {
        internal Action<TabItem<T>> ActivationRequested;
        internal Action<TabItem<T>> PinnedChanged;

        public readonly BindableBool Active = new BindableBool();

        public override bool IsPresent => base.IsPresent || Y == 0;

        public override bool IsRemovable => true;

        /// <summary>
        /// When true, this tab can be switched to using PlatformAction.DocumentPrevious and PlatformAction.DocumentNext. Otherwise, it will be skipped.
        /// </summary>
        public virtual bool IsSwitchable => true;

        public readonly T Value;

        protected TabItem(T value)
        {
            Value = value;

            Active.ValueChanged += active =>
            {
                if (active.NewValue)
                    OnActivated();
                else
                    OnDeactivated();
            };
        }

        private bool pinned;

        public bool Pinned
        {
            get => pinned;
            set
            {
                if (pinned == value) return;

                pinned = value;
                PinnedChanged?.Invoke(this);
            }
        }

        protected abstract void OnActivated();
        protected abstract void OnDeactivated();

        protected override bool OnClick(ClickEvent e)
        {
            base.OnClick(e);
            ActivationRequested?.Invoke(this);
            return true;
        }

        public override string ToString() => $"{base.ToString()} value: {Value}";
    }
}
