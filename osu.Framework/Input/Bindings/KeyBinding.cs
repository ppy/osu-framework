// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// A binding of a <see cref="Bindings.KeyCombination"/> to an action.
    /// </summary>
    public class KeyBinding
    {
        /// <summary>
        /// The combination of keys which will trigger this binding.
        /// </summary>
        public KeyCombination KeyCombination;

        /// <summary>
        /// The resultant action which is triggered by this binding.
        /// </summary>
        public object Action;

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

        /// <summary>
        /// Get the action associated with this binding, cast to the required enum type.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <returns>A cast <see cref="T"/> representation of <see cref="Action"/>.</returns>
        public virtual T GetAction<T>() => (T)Action;

        public override string ToString() => $"{KeyCombination}=>{Action}";
    }
}
