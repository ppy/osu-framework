// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// A binding of a <see cref="KeyCombination"/> to an action.
    /// </summary>
    public class KeyBinding
    {
        public KeyCombination Keys;

        public int Action;

        public KeyBinding()
        {
        }

        public KeyBinding(KeyCombination keys, object action)
        {
            Keys = keys;
            Action = (int)action;
        }

        public virtual T GetAction<T>() => (T)(object)Action;

        public override string ToString() => $"{Keys}=>{Action}";
    }
}