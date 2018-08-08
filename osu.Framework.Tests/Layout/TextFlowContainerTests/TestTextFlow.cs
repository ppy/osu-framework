// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Layout.TextFlowContainerTests
{
    public class TestTextFlow : TextFlowContainer
    {
        public Cached FlowLayout => this.Get<Cached>("layout", typeof(FillFlowContainer));
        public Cached TextLayout => this.Get<Cached>("layout");
    }
}
