// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK.Input;

#nullable enable

namespace osu.Framework.Graphics.Cursor
{
    public class PopoverContainer : CursorEffectContainer<PopoverContainer, IHasPopover>
    {
        private readonly Container content;
        private readonly Container popoverContainer;

        private IHasPopover? target;
        private Popover? currentPopover;

        protected override Container<Drawable> Content => content;

        public PopoverContainer()
        {
            InternalChildren = new[]
            {
                content = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                },
                popoverContainer = new Container
                {
                    AutoSizeAxes = Axes.Both
                },
            };
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    target = FindTargets().FirstOrDefault();
                    break;
            }

            return false;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            base.OnMouseUp(e);

            if (target == null)
                return;

            currentPopover?.Hide();

            var newPopover = target.GetPopover();
            if (newPopover == null)
                return;

            popoverContainer.Add(currentPopover = newPopover);
            currentPopover.Show();
            currentPopover.State.BindValueChanged(_ => cleanUpPopover(currentPopover));
        }

        private void cleanUpPopover(Popover popover)
        {
            if (popover.State.Value == Visibility.Hidden)
                popover.Expire();
        }
    }
}
