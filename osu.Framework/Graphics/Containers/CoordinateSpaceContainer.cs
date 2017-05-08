// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics.Transforms;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which can define a custom coordinate space for the positioning of its children.
    /// If all children are of a specific non-<see cref="Drawable"/> type, use the
    /// generic version <see cref="CoordinateSpaceContainer{T}"/>.
    /// </summary>
    public class CoordinateSpaceContainer : CoordinateSpaceContainer<Drawable>
    {
    }

    /// <summary>
    /// A container which can define a custom coordinate space for the positioning of its children.
    /// </summary>
    public class CoordinateSpaceContainer<T> : Container<T>
        where T : Drawable
    {
        private Vector2? coordinateSpace;
        /// <summary>
        /// The coordinate space to use for the absolute positioning of children within this container.
        /// </summary>
        public Vector2? CoordinateSpace
        {
            get { return coordinateSpace ?? DrawSize; }
            set
            {
                if (coordinateSpace == value)
                    return;
                coordinateSpace = value;

                Invalidate(Invalidation.Position, shallPropagate: true);
            }
        }

        public override Vector2 ChildPositionSpace => CoordinateSpace ?? DrawSize;

        /// <summary>
        /// Tweens the coordinate space.
        /// </summary>
        /// <param name="newCoordinateSpace">The coordinate space to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        public void TransformCoordinateSpaceTo(Vector2 newCoordinateSpace, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            TransformTo(() => ChildPositionSpace, newCoordinateSpace, duration, easing, new TransformCoordinateSpace());
        }

        private class TransformCoordinateSpace : TransformVector
        {
            public override void Apply(Drawable d)
            {
                base.Apply(d);

                var c = (CoordinateSpaceContainer<T>)d;
                c.CoordinateSpace = CurrentValue;
            }
        }
    }
}