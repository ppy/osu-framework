// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// Maps input actions to custom action data of type <see cref="T"/>. Use in conjunction with <see cref="Drawable"/>s implementing <see cref="IKeyBindingHandler{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the custom action.</typeparam>
    public abstract class KeyBindingContainer<T> : KeyBindingContainer
        where T : struct
    {
        private readonly SimultaneousBindingMode simultaneousMode;

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="simultaneousMode">Specify how to deal with multiple matches of <see cref="KeyCombination"/>s and <see cref="T"/>s.</param>
        protected KeyBindingContainer(SimultaneousBindingMode simultaneousMode = SimultaneousBindingMode.None)
        {
            RelativeSizeAxes = Axes.Both;

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
        protected virtual IEnumerable<Drawable> KeyBindingInputQueue => localQueue;

        private readonly List<Drawable> localQueue = new List<Drawable>();

        /// <summary>
        /// Override to enable or disable sending of repeated actions (disabled by default).
        /// Each repeated action will have its own pressed/released event pair.
        /// </summary>
        protected virtual bool SendRepeats => false;

        /// <summary>
        /// Whether this <see cref="KeyBindingContainer"/> should attempt to handle input before any of its children.
        /// </summary>
        protected virtual bool Prioritised => false;

        protected override bool OnWheel(InputState state)
        {
            InputKey key = state.Mouse.WheelDelta > 0 ? InputKey.MouseWheelUp : InputKey.MouseWheelDown;

            // we need to create a local cloned state to ensure the underlying code in handleNewReleased thinks we are in a sane state,
            // even though we are pressing and releasing an InputKey in a single frame.
            // the important part of this cloned state is the value of Wheel reset to zero.
            var clonedState = state.Clone();
            clonedState.Mouse = new MouseState { Buttons = clonedState.Mouse.Buttons };

            return handleNewPressed(state, key, false) | handleNewReleased(clonedState, key);
        }

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue)
        {
            localQueue.Clear();

            if (!base.BuildKeyboardInputQueue(localQueue))
                return false;

            if (Prioritised)
            {
                localQueue.Remove(this);
                localQueue.Add(this);
            }

            queue.AddRange(localQueue);

            localQueue.Reverse();
            return true;
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args) => handleNewPressed(state, KeyCombination.FromMouseButton(args.Button), false);

        protected override bool OnMouseUp(InputState state, MouseUpEventArgs args) => handleNewReleased(state, KeyCombination.FromMouseButton(args.Button));

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (args.Repeat && !SendRepeats)
            {
                if (pressedBindings.Count > 0)
                    return true;

                return false;
            }

            return handleNewPressed(state, KeyCombination.FromKey(args.Key), args.Repeat);
        }

        protected override bool OnKeyUp(InputState state, KeyUpEventArgs args) => handleNewReleased(state, KeyCombination.FromKey(args.Key));

        protected override bool OnJoystickPress(InputState state, JoystickEventArgs args) => handleNewPressed(state, KeyCombination.FromJoystickButton(args.Button), false);

        protected override bool OnJoystickRelease(InputState state, JoystickEventArgs args) => handleNewReleased(state, KeyCombination.FromJoystickButton(args.Button));

        private bool handleNewPressed(InputState state, InputKey newKey, bool repeat)
        {
            var pressedCombination = KeyCombination.FromInputState(state);

            bool handled = false;
            var bindings = repeat ? KeyBindings : KeyBindings.Except(pressedBindings);
            var newlyPressed = bindings.Where(m =>
                m.KeyCombination.Keys.Contains(newKey) // only handle bindings matching current key (not required for correct logic)
                && m.KeyCombination.IsPressed(pressedCombination, simultaneousMode == SimultaneousBindingMode.NoneExact));

            if (isModifier(newKey))
                // if the current key pressed was a modifier, only handle modifier-only bindings.
                newlyPressed = newlyPressed.Where(b => b.KeyCombination.Keys.All(isModifier));

            // we want to always handle bindings with more keys before bindings with less.
            newlyPressed = newlyPressed.OrderByDescending(b => b.KeyCombination.Keys.Count()).ToList();

            if (!repeat)
                pressedBindings.AddRange(newlyPressed);

            // exact matching may result in no pressed (new or old) bindings, in which case we want to trigger releases for existing actions
            if (simultaneousMode == SimultaneousBindingMode.NoneExact)
            {
                // only want to release pressed actions if no existing bindings would still remain pressed
                if (pressedBindings.Count > 0 && !pressedBindings.Any(m => m.KeyCombination.IsPressed(pressedCombination, simultaneousMode == SimultaneousBindingMode.NoneExact)))
                    releasePressedActions();
            }

            foreach (var newBinding in newlyPressed)
            {
                handled |= PropagatePressed(KeyBindingInputQueue, newBinding.GetAction<T>());

                // we only want to handle the first valid binding (the one with the most keys) in non-simultaneous mode.
                if ((simultaneousMode == SimultaneousBindingMode.None || simultaneousMode == SimultaneousBindingMode.NoneExact) && handled)
                    break;
            }

            return handled;
        }

        protected virtual bool PropagatePressed(IEnumerable<Drawable> drawables, T pressed)
        {
            IDrawable handled = null;

            // we handled a new binding and there is an existing one. if we don't want concurrency, let's propagate a released event.
            if (simultaneousMode == SimultaneousBindingMode.None || simultaneousMode == SimultaneousBindingMode.NoneExact)
                releasePressedActions();

            // only handle if we are a new non-pressed action (or a concurrency mode that supports multiple simultaneous triggers).
            if (simultaneousMode == SimultaneousBindingMode.All || !pressedActions.Contains(pressed))
            {
                pressedActions.Add(pressed);
                handled = drawables.OfType<IKeyBindingHandler<T>>().FirstOrDefault(d => d.OnPressed(pressed));
            }

            if (handled != null)
                Logger.Log($"Pressed ({pressed}) handled by {handled}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handled != null;
        }

        /// <summary>
        /// Releases all pressed actions.
        /// </summary>
        private void releasePressedActions()
        {
            foreach (var action in pressedActions)
                KeyBindingInputQueue.OfType<IKeyBindingHandler<T>>().ForEach(d => d.OnReleased(action));
            pressedActions.Clear();
        }

        private bool handleNewReleased(InputState state, InputKey releasedKey)
        {
            var pressedCombination = KeyCombination.FromInputState(state);

            bool handled = false;

            // we don't want to consider exact matching here as we are dealing with bindings, not actions.
            var newlyReleased = pressedBindings.Where(b => !b.KeyCombination.IsPressed(pressedCombination, false)).ToList();

            Trace.Assert(newlyReleased.All(b => b.KeyCombination.Keys.Contains(releasedKey)));

            foreach (var binding in newlyReleased)
            {
                pressedBindings.Remove(binding);

                var action = binding.GetAction<T>();

                handled |= PropagateReleased(KeyBindingInputQueue, action);
            }

            return handled;
        }

        protected virtual bool PropagateReleased(IEnumerable<Drawable> drawables, T released)
        {
            IDrawable handled = null;

            // we either want multiple release events due to the simultaneous mode, or we only want one when we
            // - were pressed (as an action)
            // - are the last pressed binding with this action
            if (simultaneousMode == SimultaneousBindingMode.All || pressedActions.Contains(released) && pressedBindings.All(b => !b.GetAction<T>().Equals(released)))
            {
                handled = drawables.OfType<IKeyBindingHandler<T>>().FirstOrDefault(d => d.OnReleased(released));
                pressedActions.Remove(released);
            }

            if (handled != null)
                Logger.Log($"Released ({released}) handled by {handled}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handled != null;
        }

        public void TriggerReleased(T released) => PropagateReleased(KeyBindingInputQueue, released);

        public void TriggerPressed(T pressed) => PropagatePressed(KeyBindingInputQueue, pressed);
    }

    /// <summary>
    /// Maps input actions to custom action data.
    /// </summary>
    public abstract class KeyBindingContainer : Container
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
        /// One action can be in a pressed state at once.
        /// If a new matching binding is encountered, any existing binding is first released.
        /// </summary>
        None,

        /// <summary>
        /// One action can be in a pressed state at once. Exact key combinations are required for actions to be triggered.
        /// If a new matching binding is encountered, any existing binding is first released.
        /// </summary>
        NoneExact,

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
