// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class TabItem : ClickableContainer
    {
    }

    public class TabItem<T> : TabItem
    {
        internal Action<TabItem<T>> SelectAction;

        internal Action<TabItem<T>> PinnedChanged;

        public override bool IsPresent => base.IsPresent && Y == 0;

        public T Value { get; set; }

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

        private bool active;
        public virtual bool Active
        {
            get { return active; }
            set
            {
                if (active == value) return;

                active = value;
                if (active)
                    SelectAction?.Invoke(this);
            }
        }

        protected override bool OnClick(InputState state)
        {
            base.OnClick(state);
            Active = true;
            return true;
        }
    }
}
