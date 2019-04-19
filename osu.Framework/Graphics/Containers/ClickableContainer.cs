// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.Containers
{
    public class ClickableContainer : Container
    {
        private readonly object clickedLock = new object();
        private Action _clicked;

        public event Action Clicked
        {
            add
            {
                lock (clickedLock)
                {
                    _clicked += value;
                    Enabled.Value = _clicked != null;
                }
            }
            remove
            {
                lock (clickedLock)
                {
                    _clicked -= value;
                    Enabled.Value = _clicked != null;
                }
            }
        }

        public readonly BindableBool Enabled = new BindableBool();

        protected override bool OnClick(ClickEvent e)
        {
            _clicked?.Invoke();
            return true;
        }
    }
}
