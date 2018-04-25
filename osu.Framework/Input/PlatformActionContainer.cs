// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Input.Bindings;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    /// <summary>
    /// Provides actions that are expected to have different key bindings per platform.
    /// The framework will always contain one top-level instance of this class, but extra instances
    /// can be created to handle events that should trigger specifically on a focused drawable.
    /// Will send repeat events by default.
    /// </summary>
    public class PlatformActionContainer : KeyBindingContainer<PlatformAction>, IHandleGlobalInput
    {
        private GameHost host;

        public PlatformActionContainer()
            : base(SimultaneousBindingMode.NoneExact)
        {
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            this.host = host;
        }

        public override IEnumerable<KeyBinding> DefaultKeyBindings => host.PlatformKeyBindings;

        protected override bool Prioritised => true;

        protected override bool SendRepeats => true;
    }

    public struct PlatformAction
    {
        public PlatformActionType ActionType;
        public PlatformActionMethod? ActionMethod;

        public PlatformAction(PlatformActionType actionType, PlatformActionMethod? actionMethod = null)
        {
            ActionType = actionType;
            ActionMethod = actionMethod;
        }
    }

    public enum PlatformActionType
    {
        Cut,
        Copy,
        Paste,
        SelectAll,
        CharPrevious,
        CharNext,
        WordPrevious,
        WordNext,
        LineStart,
        LineEnd,
        DocumentPrevious,
        DocumentNext
    }

    public enum PlatformActionMethod
    {
        Move,
        Select,
        Delete
    }
}
