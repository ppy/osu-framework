// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Platform;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A <see cref="SnapTargetContainer"/> that is automatically cached and provides a bindable <see cref="SafeAreaPadding"/> representing
    /// the desired safe area margins. Should be used in conjunction with child <see cref="SafeAreaContainer"/>s.
    /// The root of the scenegraph contains an instance of this container, with <see cref="SafeAreaPadding"/> automatically bound
    /// to the host <see cref="GameWindow"/>'s <see cref="GameWindow.SafeAreaPadding"/>.
    /// Developers may set a custom bindable for testing various safe area insets.
    /// </summary>
    [Cached(typeof(SafeAreaTargetContainer))]
    public class SafeAreaTargetContainer : SnapTargetContainer
    {
        private readonly BindableSafeArea safeAreaPadding = new BindableSafeArea();
        private BindableSafeArea boundSafeAreaPadding;

        /// <summary>
        /// Setting this property will bind a new <see cref="BindableSafeArea"/> and unbind any previously bound bindables.
        /// Automatically bound to <see cref="GameWindow.SafeAreaPadding"/> if not assigned before injecting dependencies.
        /// </summary>
        internal BindableSafeArea SafeAreaPadding
        {
            get => safeAreaPadding;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (boundSafeAreaPadding != null)
                    safeAreaPadding.UnbindFrom(boundSafeAreaPadding);

                safeAreaPadding.BindTo(boundSafeAreaPadding = value);
            }
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            if (boundSafeAreaPadding == null && host.Window != null)
                SafeAreaPadding = host.Window.SafeAreaPadding;
        }
    }
}
