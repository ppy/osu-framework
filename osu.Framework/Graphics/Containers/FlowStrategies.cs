// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Helper class containing factory properties and methods for convenient use of <see cref="IFlowStrategy"/>.
    /// </summary>
    public static class FlowStrategies
    {
        /// <summary>
        /// The default flow strategy used by <see cref="FlowContainer"/>. This is a <see cref="FillFlowStrategy"/> filling from left-to-right and top-to-bottom with no spacing.
        /// </summary>
        public static IFlowStrategy CreateDefault() => new FillFlowStrategy();

        /// <summary>
        /// Returns a <see cref="FillFlowStrategy"/> filling from left to right and top to bottom with the given spacing.
        /// </summary>
        /// <param name="spacing">The spacing the <see cref="FillFlowStrategy"/> should use, or null if the <see cref="FillFlowStrategy"/> should have its default spacing.</param>
        /// <returns>A <see cref="FillFlowStrategy"/> filling from left to right and top to bottom with the given spacing.</returns>
        public static IFlowStrategy CreateFillFlow(Vector2? spacing = null)
            => new FillFlowStrategy { Spacing = spacing ?? Vector2.Zero };

        /// <summary>
        /// Returns a <see cref="FillFlowStrategy"/> filling from top to bottom with the given spacing.
        /// </summary>
        /// <param name="spacing">The spacing the <see cref="FillFlowStrategy"/> should use, or null if the <see cref="FillFlowStrategy"/> should have its default spacing.</param>
        /// <returns>A <see cref="FillFlowStrategy"/> filling from top to bottom with the given spacing.</returns>
        public static IFlowStrategy CreateVerticalFlow(Vector2? spacing = null)
            => new FillFlowStrategy() { HorizontalFlow = HorizontalDirection.None, Spacing = spacing ?? Vector2.Zero };

        /// <summary>
        /// Returns a <see cref="FillFlowStrategy"/> filling from left to right with the given spacing.
        /// </summary>
        /// <param name="spacing">The spacing the <see cref="FillFlowStrategy"/> should use, or null if the <see cref="FillFlowStrategy"/> should have its default spacing.</param>
        /// <returns>A <see cref="FillFlowStrategy"/> filling from left to right with the given spacing.</returns>
        public static IFlowStrategy CreateHorizontalFlow(Vector2? spacing = null)
            => new FillFlowStrategy() { VerticalFlow = VerticalDirection.None, Spacing = spacing ?? Vector2.Zero };
    }
}
