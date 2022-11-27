// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Framework.Logging;
using osuTK;

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// Maps input actions to custom action data of type <typeparamref name="T"/>. Use in conjunction with <see cref="Drawable"/>s implementing <see cref="IKeyBindingHandler{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the custom action.</typeparam>
    public abstract partial class KeyBindingContainer<T> : KeyBindingContainer
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
        private readonly List<Drawable> queue = new List<Drawable>();
        private List<Drawable> keyRepeatInputQueue;

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

                return queue;
            }
        }

        protected override void Update()
        {
            base.Update();

            // aggressively clear to avoid holding references.
            queue.Clear();
        }

        /// <summary>
        /// Whether this <see cref="KeyBindingContainer"/> should attempt to handle input before any of its children.
        /// </summary>
        protected virtual bool Prioritised => false;

        internal override bool BuildNonPositionalInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (!base.BuildNonPositionalInputQueue(queue, allowBlocking))
                return false;

            if (Prioritised)
            {
                queue.Remove(this);
                queue.Add(this);
            }

            return true;
        }

        /// <summary>
        /// All input keys which are currently pressed and have reached this <see cref="KeyBindingContainer"/>.
        /// </summary>
        private readonly HashSet<InputKey> pressedInputKeys = new HashSet<InputKey>();

        protected override bool Handle(UIEvent e)
        {
            var state = e.CurrentState;

            switch (e)
            {
                case MouseDownEvent mouseDown:
                    return handleNewPressed(state, KeyCombination.FromMouseButton(mouseDown.Button));

                case MouseUpEvent mouseUp:
                    handleNewReleased(state, KeyCombination.FromMouseButton(mouseUp.Button));
                    return false;

                case KeyDownEvent keyDown:
                    if (keyDown.Repeat)
                        return handleRepeat(state);
                    else
                        return handleNewPressed(state, KeyCombination.FromKey(keyDown.Key));

                case KeyUpEvent keyUp:
                    handleNewReleased(state, KeyCombination.FromKey(keyUp.Key));
                    return false;

                case JoystickPressEvent joystickPress:
                    return handleNewPressed(state, KeyCombination.FromJoystickButton(joystickPress.Button));

                case JoystickReleaseEvent joystickRelease:
                    handleNewReleased(state, KeyCombination.FromJoystickButton(joystickRelease.Button));
                    return false;

                case MidiDownEvent midiDown:
                    return handleNewPressed(state, KeyCombination.FromMidiKey(midiDown.Key));

                case MidiUpEvent midiUp:
                    handleNewReleased(state, KeyCombination.FromMidiKey(midiUp.Key));
                    return false;

                case TabletPenButtonPressEvent tabletPenButtonPress:
                    return handleNewPressed(state, KeyCombination.FromTabletPenButton(tabletPenButtonPress.Button));

                case TabletPenButtonReleaseEvent tabletPenButtonRelease:
                    handleNewReleased(state, KeyCombination.FromTabletPenButton(tabletPenButtonRelease.Button));
                    return false;

                case TabletAuxiliaryButtonPressEvent tabletAuxiliaryButtonPress:
                    return handleNewPressed(state, KeyCombination.FromTabletAuxiliaryButton(tabletAuxiliaryButtonPress.Button));

                case TabletAuxiliaryButtonReleaseEvent tabletAuxiliaryButtonRelease:
                    handleNewReleased(state, KeyCombination.FromTabletAuxiliaryButton(tabletAuxiliaryButtonRelease.Button));
                    return false;

                case ScrollEvent scroll:
                {
                    var keys = KeyCombination.FromScrollDelta(scroll.ScrollDelta);
                    bool handled = false;

                    foreach (var key in keys)
                    {
                        handled |= handleNewPressed(state, key, scroll.ScrollDelta, scroll.IsPrecise);
                        handleNewReleased(state, key);
                    }

                    return handled;
                }
            }

            return false;
        }

        private bool handleRepeat(InputState state)
        {
            if (!HandleRepeats)
                return false;

            if (pressedActions.Count == 0)
                return false;

            // A simplistic approach to key repeat (that mostly matches OS level implementations) is that the last binding - or action - to
            // trigger is the one and only action to repeat.
            T action = pressedActions.Last();

            var pressEvent = new KeyBindingPressEvent<T>(state, action, true);

            // Only drawables that can still handle input should handle the repeat
            var drawables = keyRepeatInputQueue.Intersect(KeyBindingInputQueue).Where(t => t.IsAlive && t.IsPresent);

            return drawables.FirstOrDefault(d => triggerKeyBindingEvent(d, pressEvent)) != null;
        }

        private bool handleNewPressed(InputState state, InputKey newKey, Vector2? scrollDelta = null, bool isPrecise = false)
        {
            pressedInputKeys.Add(newKey);

            float scrollAmount = getScrollAmount(newKey, scrollDelta);
            var pressedCombination = new KeyCombination(pressedInputKeys);

            bool handled = false;
            var bindings = KeyBindings?.Except(pressedBindings) ?? Enumerable.Empty<IKeyBinding>();
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

            pressedBindings.AddRange(newlyPressed);

            // exact matching may result in no pressed (new or old) bindings, in which case we want to trigger releases for existing actions
            if (simultaneousMode == SimultaneousBindingMode.None && (matchingMode == KeyCombinationMatchingMode.Exact || matchingMode == KeyCombinationMatchingMode.Modifiers))
            {
                // only want to release pressed actions if no existing bindings would still remain pressed
                if (pressedBindings.Count > 0 && !pressedBindings.Any(m => m.KeyCombination.IsPressed(pressedCombination, matchingMode)))
                    releasePressedActions(state);
            }

            foreach (var newBinding in newlyPressed)
            {
                // we handled a new binding and there is an existing one. if we don't want concurrency, let's propagate a released event.
                if (simultaneousMode == SimultaneousBindingMode.None)
                {
                    releasePressedActions(state);
                }

                List<Drawable> inputQueue = getInputQueue(newBinding, true);
                Drawable handledBy = PropagatePressed(inputQueue, state, newBinding.GetAction<T>(), scrollAmount, isPrecise);

                if (handledBy != null)
                {
                    // only drawables up to the one that handled the press should handle the release, so remove all subsequent drawables from the queue (for future use).
                    int count = inputQueue.IndexOf(handledBy) + 1;
                    inputQueue.RemoveRange(count, inputQueue.Count - count);

                    handled = true;
                }

                keyRepeatInputQueue = inputQueue;

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

        protected virtual Drawable PropagatePressed(IEnumerable<Drawable> drawables, InputState state, T pressed, float scrollAmount = 0, bool isPrecise = false, bool repeat = false)
        {
            Drawable handled = null;

            // only handle if we are a new non-pressed action (or a concurrency mode that supports multiple simultaneous triggers).
            if (simultaneousMode == SimultaneousBindingMode.All || !pressedActions.Contains(pressed))
            {
                pressedActions.Add(pressed);

                if (scrollAmount != 0)
                {
                    var scrollEvent = new KeyBindingScrollEvent<T>(state, pressed, scrollAmount, isPrecise);
                    handled = drawables.FirstOrDefault(d => triggerKeyBindingEvent(d, scrollEvent));
                }

                if (handled == null)
                {
                    var pressEvent = new KeyBindingPressEvent<T>(state, pressed, repeat);
                    handled = drawables.FirstOrDefault(d => triggerKeyBindingEvent(d, pressEvent));
                }
            }

            if (handled != null)
                Logger.Log($"Pressed ({pressed}) handled by {handled}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handled;
        }

        /// <summary>
        /// Releases all pressed actions.
        /// Note that the relevant key bindings remain in a pressed state by the user and are not released by this method.
        /// </summary>
        private void releasePressedActions(InputState state)
        {
            foreach (var action in pressedActions)
            {
                var releaseEvent = new KeyBindingReleaseEvent<T>(state, action);

                foreach (var kvp in keyBindingQueues.Where(k => EqualityComparer<T>.Default.Equals(k.Key.GetAction<T>(), action)))
                    kvp.Value.ForEach(d => triggerKeyBindingEvent(d, releaseEvent));
            }

            pressedActions.Clear();
        }

        private void handleNewReleased(InputState state, InputKey releasedKey)
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

                PropagateReleased(getInputQueue(binding), state, binding.GetAction<T>());
                keyBindingQueues[binding].Clear();
            }
        }

        protected virtual void PropagateReleased(IEnumerable<Drawable> drawables, InputState state, T released)
        {
            // we either want multiple release events due to the simultaneous mode, or we only want one when we
            // - were pressed (as an action)
            // - are the last pressed binding with this action
            if (simultaneousMode == SimultaneousBindingMode.All || (pressedActions.Contains(released) && pressedBindings.All(b => !EqualityComparer<T>.Default.Equals(b.GetAction<T>(), released))))
            {
                var releaseEvent = new KeyBindingReleaseEvent<T>(state, released);

                foreach (var d in drawables.OfType<IKeyBindingHandler<T>>())
                    triggerKeyBindingEvent(d, releaseEvent);

                pressedActions.Remove(released);
            }
        }

        public void TriggerReleased(T released) => PropagateReleased(KeyBindingInputQueue, GetContainingInputManager()?.CurrentState ?? new InputState(), released);

        public void TriggerPressed(T pressed)
        {
            var state = GetContainingInputManager()?.CurrentState ?? new InputState();

            if (simultaneousMode == SimultaneousBindingMode.None)
                releasePressedActions(state);

            PropagatePressed(KeyBindingInputQueue, state, pressed);
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

        private bool triggerKeyBindingEvent(IDrawable drawable, KeyBindingEvent<T> e)
        {
            e.Target = (Drawable)drawable;

            switch (e)
            {
                case KeyBindingPressEvent<T> press:
                    return (drawable as IKeyBindingHandler<T>)?.OnPressed(press) ?? false;

                case KeyBindingReleaseEvent<T> release:
                    (drawable as IKeyBindingHandler<T>)?.OnReleased(release);
                    return false;

                case KeyBindingScrollEvent<T> scroll:
                    return (drawable as IScrollBindingHandler<T>)?.OnScroll(scroll) ?? false;

                default:
                    throw new ArgumentException($"Invalid event type: {e.GetType()}", nameof(e));
            }
        }
    }

    /// <summary>
    /// Maps input actions to custom action data.
    /// </summary>
    public abstract partial class KeyBindingContainer : Container
    {
        protected IEnumerable<IKeyBinding> KeyBindings;

        public abstract IEnumerable<IKeyBinding> DefaultKeyBindings { get; }

        /// <summary>
        /// Whether key repeat events should be sent. Defaults to <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Key repeats will invoke actions at the same rates as <see cref="Drawable.OnKeyDown"/> events.
        /// Disabling this is recommended if you either don't require key repeats (quite often applicable to gameplay input),
        /// or want a custom repeat rate.
        /// </remarks>
        protected virtual bool HandleRepeats => true;

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
        /// The last binding to be released will trigger an <see cref="IKeyBindingHandler{T}.OnReleased"/>.
        /// </summary>
        Unique,

        /// <summary>
        /// Unique actions are allowed to be pressed at the same time, as well as multiple times from different bindings. There may therefore be
        /// more than one action in an pressed state at once, as well as multiple consecutive <see cref="IKeyBindingHandler{T}.OnPressed"/> events
        /// for a single action (followed by an eventual balancing number of <see cref="IKeyBindingHandler{T}.OnReleased"/> events).
        /// </summary>
        All,
    }
}
