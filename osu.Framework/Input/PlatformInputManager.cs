// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Input
{
    public class PlatformInputManager : KeyBindingInputManager<PlatformAction>
    {
        public override IEnumerable<KeyBinding> DefaultKeyBindings => Host.PlatformKeyBindings;
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
        LineEnd
    }

    public enum PlatformActionMethod
    {
        Move,
        Select,
        Delete
    }
}
