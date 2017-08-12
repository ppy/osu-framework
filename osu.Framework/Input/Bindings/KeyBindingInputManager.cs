// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using OpenTK.Input;

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// Maps input actions to custom action data of type <see cref="T"/>. Use in conjunction with <see cref="Drawable"/>s implementing <see cref="IHandleKeyBindings{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the custom action.</typeparam>
    public abstract class KeyBindingInputManager<T> : PassThroughInputManager
        where T : struct
    {
        private readonly SimultaneousBindingMode concurrencyMode;

        protected readonly List<KeyBinding> Mappings = new List<KeyBinding>();

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="concurrencyMode">Specify how to deal with multiple matches of <see cref="KeyCombination"/>s and <see cref="T"/>s.</param>
        protected KeyBindingInputManager(SimultaneousBindingMode concurrencyMode = SimultaneousBindingMode.None)
        {
            this.concurrencyMode = concurrencyMode;
        }

        protected abstract IEnumerable<KeyBinding> CreateDefaultMappings();

        protected override void LoadComplete()
        {
            base.LoadComplete();
            ReloadMappings();
        }

        protected virtual void ReloadMappings()
        {
            Mappings.Clear();
            Mappings.AddRange(CreateDefaultMappings());
        }

        private readonly List<KeyBinding> pressedBindings = new List<KeyBinding>();
        private readonly List<T> pressedActions = new List<T>();

        private bool isModifier(Key k) => k < Key.F1;

        protected override bool PropagateKeyDown(IEnumerable<Drawable> drawables, InputState state, KeyDownEventArgs args)
        {
            bool anyHandled = false;

            if (args.Repeat)
            {
                if (pressedBindings.Count > 0)
                    return true;

                return base.PropagateKeyDown(drawables, state, args);
            }

            var validBindings = Mappings.Except(pressedBindings).Where(m => m.Keys.Keys.Contains(args.Key) && m.Keys.CheckValid(state.Keyboard.Keys));

            if (isModifier(args.Key))
                // if the current key pressed was a modifier, only handle modifier-only bindings.
                validBindings = validBindings.Where(b => b.Keys.Keys.All(isModifier));

            // we want to always handle bindings with more keys before bindings with less.
            validBindings = validBindings.OrderByDescending(b => b.Keys.Keys.Count());

            foreach (var newBinding in validBindings.ToList())
            {
                // store both the pressed combination and the resulting action, just in case the assignments change while we are actuated.
                pressedBindings.Add(newBinding);

                // we handled a new binding and there is an existing one. if we don't want concurrency, let's propagate a released event.
                if (concurrencyMode == SimultaneousBindingMode.None)
                {
                    // we only want to handle the first valid binding (the one with the most keys) in non-simultaneous mode.
                    if (anyHandled)
                        continue;

                    // we also want to release any existing pressed actions.
                    foreach (var action in pressedActions)
                        drawables.OfType<IHandleKeyBindings<T>>().ForEach(d => d.OnReleased(action));
                    pressedActions.Clear();
                }

                // only handle if we are a new non-pressed action (or a concurrency mode that supports multiple simultaneous triggers).
                if (concurrencyMode == SimultaneousBindingMode.All || !pressedActions.Contains(newBinding.GetAction<T>()))
                {
                    pressedActions.Add(newBinding.GetAction<T>());
                    anyHandled |= drawables.OfType<IHandleKeyBindings<T>>().Any(d => d.OnPressed(newBinding.GetAction<T>()));
                }
            }

            return anyHandled || base.PropagateKeyDown(drawables, state, args);
        }

        protected override bool PropagateKeyUp(IEnumerable<Drawable> drawables, InputState state, KeyUpEventArgs args)
        {
            bool handled = false;

            foreach (var binding in pressedBindings.Where(b => !b.Keys.CheckValid(state.Keyboard.Keys)).ToList())
            {
                // clear the no-longer-valid combination/action.
                pressedBindings.Remove(binding);

                var thisAction = binding.GetAction<T>();
                var thisActionInt = (int)(object)thisAction;

                // we either want multiple release events due to concurrency mode, or we only want one when we
                // - were pressed (as an action)
                // - are the last pressed binding with this action
                if (concurrencyMode == SimultaneousBindingMode.All || pressedActions.Contains(thisAction) && pressedBindings.All(b => b.Action != thisActionInt))
                {
                    handled |= drawables.OfType<IHandleKeyBindings<T>>().Any(d => d.OnReleased(binding.GetAction<T>()));
                    pressedActions.Remove(thisAction);
                }
            }

            return handled || base.PropagateKeyUp(drawables, state, args);
        }
    }

    public enum SimultaneousBindingMode
    {
        /// <summary>
        /// One action can be in a pressed state at once. If a new matching binding is encountered, any existing binding is first released.
        /// </summary>
        None,
        /// <summary>
        /// Unique actions are allowed to be pressed at the same time. There may therefore be more than one action in an actuated state at once.
        /// If one action has multiple bindings, only the first will trigger an <see cref="IHandleKeyBindings{T}.OnPressed"/>.
        /// The last binding to be released will trigger an <see cref="IHandleKeyBindings{T}.OnReleased(T)"/>.
        /// </summary>
        Unique,
        /// <summary>
        /// Unique actions are allowed to be pressed at the same time, as well as multiple times from different bindings. There may therefore be
        /// more than one action in an pressed state at once, as well as multiple consecutive <see cref="IHandleKeyBindings{T}.OnPressed"/> events
        /// for a single action (followed by an eventual balancing number of <see cref="IHandleKeyBindings{T}.OnReleased(T)"/> events).
        /// </summary>
        All,
    }
}
