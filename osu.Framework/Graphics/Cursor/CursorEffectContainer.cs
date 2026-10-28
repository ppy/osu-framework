// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Extensions.ListExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Lists;

namespace osu.Framework.Graphics.Cursor
{
    /// <summary>
    /// An abstract container type that handles positional input for many drawables of type <typeparamref name="TTarget"/>.
    /// </summary>
    /// <typeparam name="TSelf">The derived container type.</typeparam>
    /// <typeparam name="TTarget">The target drawable type.</typeparam>
    public abstract partial class CursorEffectContainer<TSelf, TTarget> : Container
        where TSelf : CursorEffectContainer<TSelf, TTarget>
        where TTarget : class, IDrawable
    {
        public override bool HandlePositionalInput => true;

        private InputManager inputManager;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            inputManager = GetContainingInputManager();
        }

        private readonly HashSet<IDrawable> childDrawables = new HashSet<IDrawable>();
        private readonly HashSet<IDrawable> nestedContainerChildDrawables = new HashSet<IDrawable>();
        private readonly List<IDrawable> newChildDrawables = new List<IDrawable>();
        private readonly List<TTarget> targetChildren = new List<TTarget>();

        private void findTargetChildren()
        {
            Debug.Assert(childDrawables.Count == 0, $"{nameof(childDrawables)} should be empty but has {childDrawables.Count} elements.");
            Debug.Assert(nestedContainerChildDrawables.Count == 0, $"{nameof(nestedContainerChildDrawables)} should be empty but has {nestedContainerChildDrawables.Count} elements.");
            Debug.Assert(newChildDrawables.Count == 0, $"{nameof(newChildDrawables)} should be empty but has {newChildDrawables.Count} elements.");
            Debug.Assert(targetChildren.Count == 0, $"{nameof(targetChildren)} should be empty but has {targetChildren.Count} elements.");

            // Skip all drawables in the hierarchy prior to (and including) ourself.
            var targetCandidates = inputManager.PositionalInputQueue;

            int selfIndex = targetCandidates.IndexOf(this);

            if (selfIndex < 0) return;

            childDrawables.Add(this);

            // keep track of all hovered drawables below this and nested effect containers
            // so we can decide which ones are valid candidates for receiving our effect and so
            // we know when we can abort our search.
            for (int i = selfIndex - 1; i >= 0; i--)
            {
                var candidate = targetCandidates[i] as TTarget;

                if (candidate == null)
                    continue;

                if (!candidate.IsHovered)
                    continue;

                // Children of drawables we are responsible for transitively also fall into our subtree,
                // and therefore we need to handle them. If they are not children of any drawables we handle,
                // it means that we iterated beyond our subtree and may terminate.
                IDrawable parent = candidate.Parent;

                // We keep track of all drawables we found while traversing the parent chain upwards.
                newChildDrawables.Clear();
                newChildDrawables.Add(candidate);

                // When we encounter a drawable we already encountered before, then there is no need
                // to keep going upward, since we already recorded it previously. At that point we know
                // the drawables we found are in fact children of ours.
                while (!childDrawables.Contains(parent))
                {
                    // If we reach to the root node (i.e. parent == null), then we found a drawable
                    // which is no longer a child of ours and we may terminate.
                    if (parent == null)
                        return;

                    newChildDrawables.Add(parent);
                    parent = parent.Parent;
                }

                // Assuming we did _not_ end up terminating, then all found drawables are children of ours
                // and need to be added.
                foreach (var newChild in newChildDrawables)
                    childDrawables.Add(newChild);

                // Keep track of child drawables whose effects are managed by a nested effect container.
                // Note, that nested effect containers themselves could implement TTarget and
                // are still our own responsibility to handle.
                for (int j = newChildDrawables.Count - 1; j >= 0; j--)
                {
                    var d = newChildDrawables[j];

                    if (d.Parent == this || (!(d.Parent is TSelf) && !nestedContainerChildDrawables.Contains(d.Parent)))
                        continue;

                    nestedContainerChildDrawables.Add(d);
                }

                // Ignore drawables whose effects are managed by a nested effect container.
                if (nestedContainerChildDrawables.Contains(candidate))
                    continue;

                // We found a valid candidate; keep track of it
                targetChildren.Add(candidate);
            }
        }

        private static readonly SlimReadOnlyListWrapper<TTarget> empty_list = new SlimReadOnlyListWrapper<TTarget>(new List<TTarget>(0));

        /// <summary>
        /// Returns currently hovered child drawables of type <typeparamref name="TTarget"/>.
        /// </summary>
        /// <returns>The list of candidate drawables in top-down order (see <see cref="InputManager.PositionalInputQueue"/>).</returns>
        /// <remarks>Only drawables where <see cref="Drawable.HandlePositionalInput"/> is true are considered. When multiple <typeparamref name="TSelf"/> are nested, only the closest parent container receives the candidate.</remarks>
        protected SlimReadOnlyListWrapper<TTarget> FindTargets()
        {
            findTargetChildren();

            // Clean up
            childDrawables.Clear();
            nestedContainerChildDrawables.Clear();
            newChildDrawables.Clear();

            if (targetChildren.Count == 0)
                return empty_list;

            List<TTarget> result = new List<TTarget>(targetChildren);
            result.Reverse();

            targetChildren.Clear();

            return result.AsSlimReadOnly();
        }
    }
}
