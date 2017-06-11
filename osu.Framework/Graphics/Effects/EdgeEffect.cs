using osu.Framework.Graphics.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Effects
{
    /// <summary>
    /// 
    /// </summary>
    public class EdgeEffect : IEffect<Container>
    {
        public EdgeEffectParameters Parameters { get; set; }

        public float CornerRadius { get; set; }

        public Container ApplyTo(Drawable drawable)
        {
            return new Container
            {
                Masking = true,
                EdgeEffect = Parameters,
                CornerRadius = CornerRadius,
                Anchor = drawable.Anchor,
                Origin = drawable.Origin,
                RelativeSizeAxes = drawable.RelativeSizeAxes,
                AutoSizeAxes = Axes.Both & ~drawable.RelativeSizeAxes,
                Children = new[] { drawable }
            };
        }
    }
}
