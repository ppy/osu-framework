// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Layout.FlowContainerTests
{
    public class TestFlowContainer : FillFlowContainer
    {
        public Action LayoutValidated;

        public Cached ChildrenSizeDependencies => this.Get<Cached>("childrenSizeDependencies");
        public Cached Layout => this.Get<Cached>("layout");

        protected override void ValidateLayout()
        {
            base.ValidateLayout();
            LayoutValidated?.Invoke();
        }
    }
}
