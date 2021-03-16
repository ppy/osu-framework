// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using osu.Framework.Bindables;

namespace osu.Framework.Input.Handlers.Tablet
{
    /// <summary>
    /// An interface to access OpenTabletDriverHandler.
    /// Can be removed when we no longer require dual targeting against netstandard5.0.
    /// </summary>
    public interface ITabletHandler
    {
        BindableSize AreaOffset { get; }

        BindableSize AreaSize { get; }

        IBindable<Size> TabletSize { get; }

        string DeviceName { get; }

        BindableBool Enabled { get; }
    }
}
