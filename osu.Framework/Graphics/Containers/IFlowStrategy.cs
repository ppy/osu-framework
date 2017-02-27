using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Represents a flow strategy that can be used to customize the layout of a <see cref="FlowContainer"/>.
    /// </summary>
    public interface IFlowStrategy
    {
        /// <summary>
        /// This event is fired whenever the layout of the flow strategy gets invalidated due to changes to the flow strategy.
        /// </summary>
        event Action OnInvalidateLayout;

        /// <summary>
        /// Updates the layout for the elements in a <see cref="FlowContainer"/>. Returns a vector for every given element size.
        /// </summary>
        /// <param name="maximumSize">The maximum size of the <see cref="FlowContainer"/> whose layout should be updated..</param>
        /// <param name="elementSizes">The sizes of the elements in the <see cref="FlowContainer"/> whose layout should be updated.</param>
        /// <returns>A list of positions containing one position for every element in the <see cref="FlowContainer"/>.</returns>
        IEnumerable<Vector2> UpdateLayout<T>(FlowContainer<T> container, IReadOnlyCollection<Vector2> elementSizes) where T : Drawable;
    }
}
