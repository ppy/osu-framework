﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.States;
using osu.Framework.Logging;
using OpenTK;

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
        private readonly KeyCombinationMatchingMode matchingMode;

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="simultaneousMode">Specify how to deal with multiple matches of <see cref="KeyCombination"/>s and <see cref="T"/>s.</param>
        /// <param name="matchingMode">Specify how to deal with exact <see cref="KeyCombination"/> matches.</param>
        protected KeyBindingContainer(SimultaneousBindingMode simultaneousMode = SimultaneousBindingMode.None, KeyCombinationMatchingMode matchingMode = KeyCombinationMatchingMode.Any)
        {
            RelativeSizeAxes = Axes.Both;

            this.simultaneousMode = simultaneousMode;
            this.matchingMode = matchingMode;
        }

        private readonly List<KeyBinding> pressedBindings = new List<KeyBinding>();

        private readonly List<T> pressedActions = new List<T>();

        /// <summary>
        /// All actions in a currently pressed state.
        /// </summary>
        public IEnumerable<T> PressedActions => pressedActions;

        /// <summary>
        /// The input queue to be used for processing key bindings. Based on the non-positional <see cref="InputManager.InputQueue"/>.
        /// Can be overridden to change priorities.
        /// </summary>
        protected virtual IEnumerable<Drawable> KeyBindingInputQueue => childrenInputQueue;

        private List<Drawable> childrenInputQueue
        {
            get
            {
                var queue = new List<Drawable>();
                BuildKeyboardInputQueue(queue, false);
                queue.Reverse();

                return queue;
            }
        }

        /// <summary>
        /// Override to enable or disable sending of repeated actions (disabled by default).
        /// Each repeated action will have its own pressed/released event pair.
        /// </summary>
        protected virtual bool SendRepeats => false;

        /// <summary>
        /// Whether this <see cref="KeyBindingContainer"/> should attempt to handle input before any of its children.
        /// </summary>
        protected virtual bool Prioritised => false;

        protected override bool OnScroll(InputState state)
        {
            var scrollDelta = state.Mouse.ScrollDelta;
            var isPrecise = state.Mouse.HasPreciseScroll;
            var key = KeyCombination.FromScrollDelta(scrollDelta);
            if (key == InputKey.None) return false;
            return handleNewPressed(state, key, false, scrollDelta, isPrecise) | handleNewReleased(state, key);
        }

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (!base.BuildKeyboardInputQueue(queue, allowBlocking))
                return false;

            if (Prioritised)
            {
                queue.Remove(this);
                queue.Add(this);
            }

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

        private bool handleNewPressed(InputState state, InputKey newKey, bool repeat, Vector2? scrollDelta = null, bool isPrecise = false)
        {
            float scrollAmount = 0;
            if (newKey == InputKey.MouseWheelUp)
                scrollAmount = scrollDelta?.Y ?? 0;
            else if (newKey == InputKey.MouseWheelDown)
                scrollAmount = -(scrollDelta?.Y ?? 0);
            var pressedCombination = KeyCombination.FromInputState(state, scrollDelta);

            bool handled = false;
            var bindings = (repeat ? KeyBindings : KeyBindings?.Except(pressedBindings)) ?? Enumerable.Empty<KeyBinding>();
            var newlyPressed = bindings.Where(m =>
                m.KeyCombination.Keys.Contains(newKey) // only handle bindings matching current key (not required for correct logic)
                && m.KeyCombination.IsPressed(pressedCombination, matchingMode));

            if (KeyCombination.IsModifierKey(newKey))
                // if the current key pressed was a modifier, only handle modifier-only bindings.
                newlyPressed = newlyPressed.Where(b => b.KeyCombination.Keys.All(KeyCombination.IsModifierKey));

            // we want to always handle bindings with more keys before bindings with less.
            newlyPressed = newlyPressed.OrderByDescending(b => b.KeyCombination.Keys.Count()).ToList();

            if (!repeat)
                pressedBindings.AddRange(newlyPressed);

            // exact matching may result in no pressed (new or old) bindings, in which case we want to trigger releases for existing actions
            if (simultaneousMode == SimultaneousBindingMode.None && (matchingMode == KeyCombinationMatchingMode.Exact || matchingMode == KeyCombinationMatchingMode.Modifiers))
            {
                // only want to release pressed actions if no existing bindings would still remain pressed
                if (pressedBindings.Count > 0 && !pressedBindings.Any(m => m.KeyCombination.IsPressed(pressedCombination, matchingMode)))
                    releasePressedActions();
            }

            foreach (var newBinding in newlyPressed)
            {
                handled |= PropagatePressed(KeyBindingInputQueue, newBinding.GetAction<T>(), scrollAmount, isPrecise);

                // we only want to handle the first valid binding (the one with the most keys) in non-simultaneous mode.
                if (simultaneousMode == SimultaneousBindingMode.None && handled)
                    break;
            }

            return handled;
        }

        protected virtual bool PropagatePressed(IEnumerable<Drawable> drawables, T pressed, float scrollAmount = 0, bool isPrecise = false)
        {
            IDrawable handled = null;

            // we handled a new binding and there is an existing one. if we don't want concurrency, let's propagate a released event.
            if (simultaneousMode == SimultaneousBindingMode.None)
                releasePressedActions();

            // only handle if we are a new non-pressed action (or a concurrency mode that supports multiple simultaneous triggers).
            if (simultaneousMode == SimultaneousBindingMode.All || !pressedActions.Contains(pressed))
            {
                pressedActions.Add(pressed);
                if (scrollAmount != 0)
                    handled = drawables.OfType<IScrollBindingHandler<T>>().FirstOrDefault(d => d.OnScroll(pressed, scrollAmount, isPrecise));
                if (handled == null)
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
            var newlyReleased = pressedBindings.Where(b => !b.KeyCombination.IsPressed(pressedCombination, KeyCombinationMatchingMode.Any)).ToList();

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
