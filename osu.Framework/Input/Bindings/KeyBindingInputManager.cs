// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// Maps input actions to custom action data of type <see cref="T"/>. Use in conjunction with <see cref="Drawable"/>s implementing <see cref="IKeyBindingHandler{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the custom action.</typeparam>
    public abstract class KeyBindingInputManager<T> : KeyBindingInputManager
        where T : struct
    {
        private readonly SimultaneousBindingMode simultaneousMode;

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="simultaneousMode">Specify how to deal with multiple matches of <see cref="KeyCombination"/>s and <see cref="T"/>s.</param>
        protected KeyBindingInputManager(SimultaneousBindingMode simultaneousMode = SimultaneousBindingMode.None)
        {
            this.simultaneousMode = simultaneousMode;
        }

        private readonly List<KeyBinding> pressedBindings = new List<KeyBinding>();

        private readonly List<T> pressedActions = new List<T>();

        /// <summary>
        /// All actions in a currently pressed state.
        /// </summary>
        public IEnumerable<T> PressedActions => pressedActions;

        private bool isModifier(InputKey k) => k < InputKey.F1;

        /// <summary>
        /// The input queue to be used for processing key bindings. Based on the non-positional <see cref="InputManager.InputQueue"/>.
        /// Can be overridden to change priorities.
        /// </summary>
        protected virtual IEnumerable<Drawable> KeyBindingInputQueue => InputQueue;

        protected override bool PropagateMouseDown(IEnumerable<Drawable> drawables, InputState state, MouseDownEventArgs args) =>
            handleNewDown(state, KeyCombination.FromMouseButton(args.Button)) || base.PropagateMouseDown(drawables, state, args);

        protected override bool PropagateMouseUp(IEnumerable<Drawable> drawables, InputState state, MouseUpEventArgs args) =>
            handleNewUp(state, KeyCombination.FromMouseButton(args.Button)) || base.PropagateMouseUp(drawables, state, args);

        protected override bool PropagateKeyDown(IEnumerable<Drawable> drawables, InputState state, KeyDownEventArgs args)
        {
            if (args.Repeat)
            {
                if (pressedBindings.Count > 0)
                    return true;

                return base.PropagateKeyDown(drawables, state, args);
            }

            return handleNewDown(state, KeyCombination.FromKey(args.Key)) || base.PropagateKeyDown(drawables, state, args);
        }

        protected override bool PropagateKeyUp(IEnumerable<Drawable> drawables, InputState state, KeyUpEventArgs args) =>
            handleNewUp(state, KeyCombination.FromKey(args.Key)) || base.PropagateKeyUp(drawables, state, args);

        private bool handleNewDown(InputState state, InputKey newKey)
        {
            var pressedCombination = KeyCombination.FromInputState(state);

            bool handled = false;

            var newlyPressed = KeyBindings.Except(pressedBindings).Where(m =>
                m.KeyCombination.Keys.Contains(newKey) // only handle bindings matching current key (not required for correct logic)
                && m.KeyCombination.IsPressed(pressedCombination));

            if (isModifier(newKey))
                // if the current key pressed was a modifier, only handle modifier-only bindings.
                newlyPressed = newlyPressed.Where(b => b.KeyCombination.Keys.All(isModifier));

            // we want to always handle bindings with more keys before bindings with less.
            newlyPressed = newlyPressed.OrderByDescending(b => b.KeyCombination.Keys.Count()).ToList();

            pressedBindings.AddRange(newlyPressed);

            foreach (var newBinding in newlyPressed)
            {
                // we handled a new binding and there is an existing one. if we don't want concurrency, let's propagate a released event.
                if (simultaneousMode == SimultaneousBindingMode.None)
                {
                    // we want to release any existing pressed actions.
                    foreach (var action in pressedActions)
                        KeyBindingInputQueue.OfType<IKeyBindingHandler<T>>().ForEach(d => d.OnReleased(action));
                    pressedActions.Clear();
                }

                // only handle if we are a new non-pressed action (or a concurrency mode that supports multiple simultaneous triggers).
                if (simultaneousMode == SimultaneousBindingMode.All || !pressedActions.Contains(newBinding.GetAction<T>()))
                {
                    pressedActions.Add(newBinding.GetAction<T>());
                    handled |= KeyBindingInputQueue.OfType<IKeyBindingHandler<T>>().Any(d => d.OnPressed(newBinding.GetAction<T>()));
                }

                // we only want to handle the first valid binding (the one with the most keys) in non-simultaneous mode.
                if (simultaneousMode == SimultaneousBindingMode.None && handled)
                    break;
            }

            return handled;
        }

        // ReSharper disable once UnusedParameter.Local
        private bool handleNewUp(InputState state, InputKey releasedKey)
        {
            var pressedCombination = KeyCombination.FromInputState(state);

            bool handled = false;

            var newlyReleased = pressedBindings.Where(b => !b.KeyCombination.IsPressed(pressedCombination)).ToList();

            Trace.Assert(newlyReleased.All(b => b.KeyCombination.Keys.Contains(releasedKey)));

            foreach (var binding in newlyReleased)
            {
                pressedBindings.Remove(binding);

                var action = binding.GetAction<T>();

                // we either want multiple release events due to the simultaneous mode, or we only want one when we
                // - were pressed (as an action)
                // - are the last pressed binding with this action
                if (simultaneousMode == SimultaneousBindingMode.All || pressedActions.Contains(action) && pressedBindings.All(b => !b.Action.Equals(binding.Action)))
                {
                    handled |= KeyBindingInputQueue.OfType<IKeyBindingHandler<T>>().Any(d => d.OnReleased(binding.GetAction<T>()));
                    pressedActions.Remove(action);
                }
            }

            return handled;
        }
    }

    /// <summary>
    /// Maps input actions to custom action data.
    /// </summary>
    public abstract class KeyBindingInputManager : PassThroughInputManager
    {
        protected IEnumerable<KeyBinding> KeyBindings;

        public abstract IEnumerable<KeyBinding> DefaultKeyBindings { get; }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            ReloadMappings();
        }

        protected virtual void ReloadMappings()
        {
            KeyBindings = DefaultKeyBindings;
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
        /// If one action has multiple bindings, only the first will trigger an <see cref="IKeyBindingHandler{T}.OnPressed"/>.
        /// The last binding to be released will trigger an <see cref="IKeyBindingHandler{T}.OnReleased(T)"/>.
        /// </summary>
        Unique,
        /// <summary>
        /// Unique actions are allowed to be pressed at the same time, as well as multiple times from different bindings. There may therefore be
        /// more than one action in an pressed state at once, as well as multiple consecutive <see cref="IKeyBindingHandler{T}.OnPressed"/> events
        /// for a single action (followed by an eventual balancing number of <see cref="IKeyBindingHandler{T}.OnReleased(T)"/> events).
        /// </summary>
        All,
    }
}
