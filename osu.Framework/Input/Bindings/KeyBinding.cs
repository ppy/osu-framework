// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Input.Bindings
{
    public class KeyBinding : IKeyBinding
    {
        public KeyCombination KeyCombination { get; set; }

        public object Action { get; set; }

        /// <summary>
        /// Construct a new instance.
        /// </summary>
        /// <param name="keys">The combination of keys which will trigger this binding.</param>
        /// <param name="action">The resultant action which is triggered by this binding. Usually an enum type.</param>
        public KeyBinding(KeyCombination keys, object action)
        {
            KeyCombination = keys;

            Action = action;
        }

        /// <summary>
        /// Construct a new instance.
        /// </summary>
        /// <param name="key">The key which will trigger this binding.</param>
        /// <param name="action">The resultant action which is triggered by this binding. Usually an enum type.</param>
        public KeyBinding(InputKey key, object action)
            : this((KeyCombination)key, action)
        {
        }

        /// <summary>
        /// Constructor for derived classes that may require serialisation.
        /// </summary>
        public KeyBinding()
        {
        }

        public override string ToString() => $"{KeyCombination}=>{Action}";
    }
}
