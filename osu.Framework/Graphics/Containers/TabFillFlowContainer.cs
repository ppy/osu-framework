// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.UserInterface;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    public class TabFillFlowContainer<T> : FillFlowContainer<T> where T : TabItem
    {
        public Action<T, bool> TabVisibilityChanged;

        protected override IEnumerable<Vector2> ComputeLayoutPositions()
        {
            foreach (var child in Children)
                child.Y = 0;

            var result = base.ComputeLayoutPositions().ToArray();
            int i = 0;
            foreach (var child in FlowingChildren)
            {
                updateChildIfNeeded(child, result[i].Y == 0);
                ++i;
            }
            return result;
        }

        private Dictionary<T, bool> tabVisibility = new Dictionary<T, bool>();

        private void updateChildIfNeeded(T child, bool isVisible)
        {
            if (!tabVisibility.ContainsKey(child) || tabVisibility[child] != isVisible)
            {
                TabVisibilityChanged?.Invoke(child, isVisible);
                tabVisibility[child] = isVisible;
            }
        }
    }
}
