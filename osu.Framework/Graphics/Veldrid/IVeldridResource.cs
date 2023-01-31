// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    public interface IVeldridResource
    {
        ResourceSet GetResourceSet(ResourceLayout layout);
    }
}
