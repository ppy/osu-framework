// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;

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

        public override bool IsPresent => base.IsPresent && Y == 0;

        public override bool IsRemovable => false;

        /// <summary>
        /// When true, this tab can be switched to using PlatformAction.DocumentPrevious and PlatformAction.DocumentNext. Otherwise, it will be skipped.
        /// </summary>
        public virtual bool IsSwitchable => true;

        public readonly T Value;

        protected TabItem(T value)
        {
            Value = value;

            Active.ValueChanged += active_ValueChanged;
        }

        private void active_ValueChanged(bool newValue)
        {
            if (newValue)
                OnActivated();
            else
                OnDeactivated();
        }

        private bool pinned;

        public bool Pinned
        {
            get { return pinned; }
            set
            {
                if (pinned == value) return;

                pinned = value;
                PinnedChanged?.Invoke(this);
            }
        }

        protected abstract void OnActivated();
        protected abstract void OnDeactivated();

        public readonly BindableBool Active = new BindableBool();

        protected override bool OnClick(InputState state)
        {
            base.OnClick(state);
            ActivationRequested?.Invoke(this);
            return true;
        }

        public override string ToString() => $"{base.ToString()} value: {Value}";
    }
}
