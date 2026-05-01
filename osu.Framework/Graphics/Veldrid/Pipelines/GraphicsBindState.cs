// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Framework.Graphics.Veldrid.Pipelines
{
    internal sealed class GraphicsBindState
    {
        private object? pipeline;
        private readonly Dictionary<uint, ResourceSetBinding> resourceSets = new Dictionary<uint, ResourceSetBinding>();

        public void Reset()
        {
            pipeline = null;
            resourceSets.Clear();
        }

        public bool ShouldBindPipeline(object nextPipeline)
        {
            if (ReferenceEquals(pipeline, nextPipeline))
                return false;

            pipeline = nextPipeline;
            resourceSets.Clear();
            return true;
        }

        public bool ShouldBindResourceSet(uint set, object resourceSet, uint? dynamicOffset = null)
        {
            if (resourceSets.TryGetValue(set, out var current) && current.Matches(resourceSet, dynamicOffset))
                return false;

            resourceSets[set] = new ResourceSetBinding(resourceSet, dynamicOffset);
            return true;
        }

        private readonly struct ResourceSetBinding
        {
            private readonly object resourceSet;
            private readonly uint? dynamicOffset;

            public ResourceSetBinding(object resourceSet, uint? dynamicOffset)
            {
                this.resourceSet = resourceSet;
                this.dynamicOffset = dynamicOffset;
            }

            public bool Matches(object otherResourceSet, uint? otherDynamicOffset)
                => ReferenceEquals(resourceSet, otherResourceSet) && dynamicOffset == otherDynamicOffset;
        }
    }
}
