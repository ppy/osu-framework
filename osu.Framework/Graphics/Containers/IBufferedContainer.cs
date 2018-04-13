// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    public interface IBufferedContainer : IContainer
    {
        Vector2 BlurSigma { get; set; }
    }
}
