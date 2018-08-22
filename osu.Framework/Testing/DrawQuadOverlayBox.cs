// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using JetBrains.Annotations;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using OpenTK.Graphics;

namespace osu.Framework.Testing
{
    public class DrawQuadOverlayBox : Box
    {
        public Drawable Target;

        public DrawQuadOverlayBox([NotNull] Drawable target)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            if (Colour == Color4.Transparent)
                Colour = Color4.White.Opacity(.5f);
        }

        public override Quad ScreenSpaceDrawQuad => Target.ScreenSpaceDrawQuad;

        protected override void Update()
        {
            base.Update();
            Invalidate(Invalidation.DrawNode);
        }
    }
}
