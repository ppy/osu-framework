// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.UserInterface
{
    public class RearrangableTextList : RearrangeableListContainer<RearrangeableTextLabel>
    {
        public void AddItem(string text)
        {
            base.AddItem(new RearrangeableTextLabel(text));
        }
    }
}
