// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// Maps input actions to custom action data of type <see cref="T"/>. Use in conjunction with <see cref="Drawable"/>s implementing <see cref="IHandleKeyBindings{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the custom action.</typeparam>
    public abstract class KeyBindingInputManager<T> : PassThroughInputManager
        where T : struct
    {
        private readonly ConcurrentActionMode concurrencyMode;

        protected readonly List<KeyBinding> Mappings = new List<KeyBinding>();

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="concurrencyMode">Specify how to deal with multiple matches of <see cref="KeyCombination"/>s and <see cref="T"/>s.</param>
        protected KeyBindingInputManager(ConcurrentActionMode concurrencyMode = ConcurrentActionMode.None)
        {
            this.concurrencyMode = concurrencyMode;
        }

        protected abstract IDictionary<KeyCombination, T> CreateDefaultMappings();

        protected override void LoadComplete()
        {
            base.LoadComplete();
            ReloadMappings();

        }

        protected virtual void ReloadMappings()
        {
            Mappings.Clear();
            foreach (var kvp in CreateDefaultMappings())
                Mappings.Add(new KeyBinding(kvp.Key, kvp.Value));
        }

        private readonly List<KeyBinding> pressedBindings = new List<KeyBinding>();

        protected override bool PropagateKeyDown(IEnumerable<Drawable> drawables, InputState state, KeyDownEventArgs args)
        {
            bool handled = false;

            if (args.Repeat)
            {
                if (pressedBindings.Count > 0)
                    return true;

                return base.PropagateKeyDown(drawables, state, args);
            }

            KeyBinding newBinding;

            while ((newBinding = Mappings.Except(pressedBindings).LastOrDefault(m => m.Keys.CheckValid(state.Keyboard.Keys))) != null)
            {
                if (concurrencyMode == ConcurrentActionMode.All || pressedBindings.All(p => p.Action != newBinding.Action))
                {
                    handled = drawables.OfType<IHandleKeyBindings<T>>().Any(d => d.OnPressed(newBinding.GetAction<T>()));

                    //we handled a new binding and there is an existing one. if we don't want concurrency, let's propagate a released event.
                    if (handled && concurrencyMode == ConcurrentActionMode.None && pressedBindings.Count > 0)
                        handled |= drawables.OfType<IHandleKeyBindings<T>>().Any(d => d.OnReleased(pressedBindings.First().GetAction<T>()));
                }

                // store both the pressed combination and the resulting action, just in case the assignments change while we are actuated.
                pressedBindings.Add(newBinding);
            }

            return handled || base.PropagateKeyDown(drawables, state, args);
        }

        protected override bool PropagateKeyUp(IEnumerable<Drawable> drawables, InputState state, KeyUpEventArgs args)
        {
            bool handled = false;

            foreach (var binding in pressedBindings.ToList())
            {
                if (!binding.Keys.CheckValid(state.Keyboard.Keys))
                {
                    // clear the no-longer-valid combination/action.
                    pressedBindings.Remove(binding);

                    if (concurrencyMode == ConcurrentActionMode.All || pressedBindings.All(p => p.Action != binding.Action))
                    {
                        // set data as KeyUp if we're all done with this action.
                        handled = drawables.OfType<IHandleKeyBindings<T>>().Any(d => d.OnReleased(binding.GetAction<T>()));
                    }
                }
            }

            return handled || base.PropagateKeyUp(drawables, state, args);
        }
    }

    public enum ConcurrentActionMode
    {
        /// <summary>
        /// One action can be pressed at once. If a new matching binding is encountered, any existing binding is first released.
        /// </summary>
        None,
        /// <summary>
        /// Unique actions are allowed to be pressed at the same time. There may therefore be more than one action in an actuated state at once.
        /// If one action has multiple bindings, only the first will trigger an <see cref="IHandleKeyBindings{T}.OnPressed"/>.
        /// The last binding to be released will trigger an <see cref="IHandleKeyBindings{T}.OnReleased(T)"/>.
        /// </summary>
        UniqueActions,
        /// <summary>
        /// Unique actions are allowed to be pressed at the same time, as well as multiple times from different bindings. There may therefore be
        /// more than one action in an pressed state at once, as well as multiple consecutive <see cref="IHandleKeyBindings{T}.OnPressed"/> events
        /// for a single action (followed by an eventual balancing number of <see cref="IHandleKeyBindings{T}.OnReleased(T)"/> events).
        /// </summary>
        All,
    }
}
