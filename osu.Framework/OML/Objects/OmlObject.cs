using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.OML.Objects
{
    public class OmlObject : OmlObject<Drawable>
    {
    }

    public class OmlObject<T> : Container<T>
        where T : Drawable
    {
        public virtual Bindable<string> BindableValue { get; set; } // a Value is always a string and can't be something other than a string.

        protected OmlObject()
        {
            // This clearly be done better. well, lets keep this for now. TODO: Fix
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            FillMode = FillMode.Stretch;
        }
    }
}
