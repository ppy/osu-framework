﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// Maps input actions to custom action data of type <typeparamref name="T"/>. Use in conjunction with <see cref="Drawable"/>s implementing <see cref="IKeyBindingHandler{T}"/>.
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
        /// <param name="simultaneousMode">Specify how to deal with multiple matches of <see cref="KeyCombination"/>s and <typeparamref name="T"/>s.</param>
        /// <param name="matchingMode">Specify how to deal with exact <see cref="KeyCombination"/> matches.</param>
        protected KeyBindingContainer(SimultaneousBindingMode simultaneousMode = SimultaneousBindingMode.None, KeyCombinationMatchingMode matchingMode = KeyCombinationMatchingMode.Any)
        {
            RelativeSizeAxes = Axes.Both;

            this.simultaneousMode = simultaneousMode;
            this.matchingMode = matchingMode;
        }

        private readonly List<IKeyBinding> pressedBindings = new List<IKeyBinding>();

        private readonly List<T> pressedActions = new List<T>();

        /// <summary>
        /// All actions in a currently pressed state.
        /// </summary>
        public IEnumerable<T> PressedActions => pressedActions;

        private readonly Dictionary<IKeyBinding, List<Drawable>> keyBindingQueues = new Dictionary<IKeyBinding, List<Drawable>>();
        private readonly InputQueue queue = new InputQueue();

        /// <summary>
        /// The input queue to be used for processing key bindings. Based on the non-positional <see cref="InputManager.NonPositionalInputQueue"/>.
        /// Can be overridden to change priorities.
        /// </summary>
        protected virtual IEnumerable<Drawable> KeyBindingInputQueue
        {
            get
            {
                queue.Clear();
                BuildNonPositionalInputQueue(queue, false);
                queue.Reverse();

                return queue.Regular;
            }
        }

        protected override void Update()
        {
            base.Update();

            // aggressively clear to avoid holding references.
            queue.Clear();
        }

        /// <summary>
        /// Override to enable or disable sending of repeated actions (disabled by default).
        /// Each repeated action will have its own pressed/released event pair.
        /// </summary>
        protected virtual bool SendRepeats => false;

        /// <summary>
        /// All input keys which are currently pressed and have reached this <see cref="KeyBindingContainer"/>.
        /// </summary>
        private readonly HashSet<InputKey> pressedInputKeys = new HashSet<InputKey>();

        protected override bool Handle(UIEvent e)
        {
            switch (e)
            {
                case MouseDownEvent mouseDown:
                    return handleNewPressed(KeyCombination.FromMouseButton(mouseDown.Button), false);

                case MouseUpEvent mouseUp:
                    handleNewReleased(KeyCombination.FromMouseButton(mouseUp.Button));
                    return false;

                case KeyDownEvent keyDown:
                    if (keyDown.Repeat && !SendRepeats)
                        return pressedBindings.Count > 0;

                    if (handleNewPressed(KeyCombination.FromKey(keyDown.Key), keyDown.Repeat))
                        return true;

                    return false;

                case KeyUpEvent keyUp:
                    handleNewReleased(KeyCombination.FromKey(keyUp.Key));
                    return false;

                case JoystickPressEvent joystickPress:
                    return handleNewPressed(KeyCombination.FromJoystickButton(joystickPress.Button), false);

                case JoystickReleaseEvent joystickRelease:
                    handleNewReleased(KeyCombination.FromJoystickButton(joystickRelease.Button));
                    return false;

                case MidiDownEvent midiDown:
                    return handleNewPressed(KeyCombination.FromMidiKey(midiDown.Key), false);

                case MidiUpEvent midiUp:
                    handleNewReleased(KeyCombination.FromMidiKey(midiUp.Key));
                    return false;

                case TabletPenButtonPressEvent tabletPenButtonPress:
                    return handleNewPressed(KeyCombination.FromTabletPenButton(tabletPenButtonPress.Button), false);

                case TabletPenButtonReleaseEvent tabletPenButtonRelease:
                    handleNewReleased(KeyCombination.FromTabletPenButton(tabletPenButtonRelease.Button));
                    return false;

                case TabletAuxiliaryButtonPressEvent tabletAuxiliaryButtonPress:
                    return handleNewPressed(KeyCombination.FromTabletAuxiliaryButton(tabletAuxiliaryButtonPress.Button), false);

                case TabletAuxiliaryButtonReleaseEvent tabletAuxiliaryButtonRelease:
                    handleNewReleased(KeyCombination.FromTabletAuxiliaryButton(tabletAuxiliaryButtonRelease.Button));
                    return false;

                case ScrollEvent scroll:
                {
                    var keys = KeyCombination.FromScrollDelta(scroll.ScrollDelta);
                    bool handled = false;

                    foreach (var key in keys)
                    {
                        handled |= handleNewPressed(key, false, scroll.ScrollDelta, scroll.IsPrecise);
                        handleNewReleased(key);
                    }

                    return handled;
                }
            }

            return false;
        }

        private bool handleNewPressed(InputKey newKey, bool repeat, Vector2? scrollDelta = null, bool isPrecise = false)
        {
            pressedInputKeys.Add(newKey);

            var scrollAmount = getScrollAmount(newKey, scrollDelta);
            var pressedCombination = new KeyCombination(pressedInputKeys);

            bool handled = false;
            var bindings = (repeat ? KeyBindings : KeyBindings?.Except(pressedBindings)) ?? Enumerable.Empty<IKeyBinding>();
            var newlyPressed = bindings.Where(m =>
                m.KeyCombination.IsPressed(pressedCombination, matchingMode));

            if (KeyCombination.IsModifierKey(newKey))
            {
                // if the current key pressed was a modifier, only handle modifier-only bindings.
                // lambda expression is used so that the delegate is cached (see: https://github.com/dotnet/roslyn/issues/5835)
                newlyPressed = newlyPressed.Where(b => b.KeyCombination.Keys.All(key => KeyCombination.IsModifierKey(key)));
            }

            // we want to always handle bindings with more keys before bindings with less.
            newlyPressed = newlyPressed.OrderByDescending(b => b.KeyCombination.Keys.Length).ToList();

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
                // we handled a new binding and there is an existing one. if we don't want concurrency, let's propagate a released event.
                if (simultaneousMode == SimultaneousBindingMode.None)
                    releasePressedActions();

                List<Drawable> inputQueue = getInputQueue(newBinding, true);
                Drawable handledBy = PropagatePressed(inputQueue, newBinding.GetAction<T>(), scrollAmount, isPrecise);

                if (handledBy != null)
                {
                    // only drawables up to the one that handled the press should handle the release, so remove all subsequent drawables from the queue (for future use).
                    var count = inputQueue.IndexOf(handledBy) + 1;
                    inputQueue.RemoveRange(count, inputQueue.Count - count);

                    handled = true;
                }

                // we only want to handle the first valid binding (the one with the most keys) in non-simultaneous mode.
                if (simultaneousMode == SimultaneousBindingMode.None && handled)
                    break;
            }

            return handled;
        }

        private static float getScrollAmount(InputKey newKey, Vector2? scrollDelta)
        {
            switch (newKey)
            {
                case InputKey.MouseWheelUp:
                    return scrollDelta?.Y ?? 0;

                case InputKey.MouseWheelDown:
                    return -(scrollDelta?.Y ?? 0);

                case InputKey.MouseWheelRight:
                    return scrollDelta?.X ?? 0;

                case InputKey.MouseWheelLeft:
                    return -(scrollDelta?.X ?? 0);

                default:
                    return 0;
            }
        }

        protected virtual Drawable PropagatePressed(IEnumerable<Drawable> drawables, T pressed, float scrollAmount = 0, bool isPrecise = false)
        {
            Drawable handled = null;

            // only handle if we are a new non-pressed action (or a concurrency mode that supports multiple simultaneous triggers).
            if (simultaneousMode == SimultaneousBindingMode.All || !pressedActions.Contains(pressed))
            {
                pressedActions.Add(pressed);
                if (scrollAmount != 0)
                    handled = (Drawable)drawables.OfType<IScrollBindingHandler<T>>().FirstOrDefault(d => d.OnScroll(pressed, scrollAmount, isPrecise));
                handled ??= (Drawable)drawables.OfType<IKeyBindingHandler<T>>().FirstOrDefault(d => d.OnPressed(pressed));
            }

            if (handled != null)
                Logger.Log($"Pressed ({pressed}) handled by {handled}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handled;
        }

        /// <summary>
        /// Releases all pressed actions.
        /// Note that the relevant key bindings remain in a pressed state by the user and are not released by this method.
        /// </summary>
        private void releasePressedActions()
        {
            foreach (var action in pressedActions)
            {
                foreach (var kvp in keyBindingQueues.Where(k => EqualityComparer<T>.Default.Equals(k.Key.GetAction<T>(), action)))
                    kvp.Value.OfType<IKeyBindingHandler<T>>().ForEach(d => d.OnReleased(action));
            }

            pressedActions.Clear();
        }

        private void handleNewReleased(InputKey releasedKey)
        {
            pressedInputKeys.Remove(releasedKey);

            if (pressedBindings.Count == 0)
                return;

            // we don't want to consider exact matching here as we are dealing with bindings, not actions.
            var pressedCombination = new KeyCombination(pressedInputKeys);

            var newlyReleased = pressedInputKeys.Count == 0
                ? pressedBindings.ToList()
                : pressedBindings.Where(b => !b.KeyCombination.IsPressed(pressedCombination, KeyCombinationMatchingMode.Any)).ToList();

            foreach (var binding in newlyReleased)
            {
                pressedBindings.Remove(binding);
                PropagateReleased(getInputQueue(binding), binding.GetAction<T>());
                keyBindingQueues[binding].Clear();
            }
        }

        protected virtual void PropagateReleased(IEnumerable<Drawable> drawables, T released)
        {
            // we either want multiple release events due to the simultaneous mode, or we only want one when we
            // - were pressed (as an action)
            // - are the last pressed binding with this action
            if (simultaneousMode == SimultaneousBindingMode.All || pressedActions.Contains(released) && pressedBindings.All(b => !EqualityComparer<T>.Default.Equals(b.GetAction<T>(), released)))
            {
                foreach (var d in drawables.OfType<IKeyBindingHandler<T>>())
                    d.OnReleased(released);
                pressedActions.Remove(released);
            }
        }

        public void TriggerReleased(T released) => PropagateReleased(KeyBindingInputQueue, released);

        public void TriggerPressed(T pressed)
        {
            if (simultaneousMode == SimultaneousBindingMode.None)
                releasePressedActions();

            PropagatePressed(KeyBindingInputQueue, pressed);
        }

        private List<Drawable> getInputQueue(IKeyBinding binding, bool rebuildIfEmpty = false)
        {
            if (!keyBindingQueues.ContainsKey(binding))
                keyBindingQueues.Add(binding, new List<Drawable>());

            var currentQueue = keyBindingQueues[binding];

            if (rebuildIfEmpty && !currentQueue.Any())
                currentQueue.AddRange(KeyBindingInputQueue);

            return currentQueue;
        }
    }

    /// <summary>
    /// Maps input actions to custom action data.
    /// </summary>
    public abstract class KeyBindingContainer : Container
    {
        // This is only specified here (rather than in PlatformContainer, where it is consumed) to workaround
        // a critical iOS / Xamarin bug, where consumer applications may crash during startup at an unmanaged level.
        // It should eventually be removed when the issue is identified and fixed upstream.
        // See https://github.com/ppy/osu-framework/pull/4263 for discussion.
        [Resolved]
        protected GameHost Host { get; private set; }

        protected IEnumerable<IKeyBinding> KeyBindings;

        public abstract IEnumerable<IKeyBinding> DefaultKeyBindings { get; }

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
