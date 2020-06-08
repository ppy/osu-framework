// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Framework.Bindables.Bindings
{
    /// <summary>
    /// A <see cref="Binding{T}"/> implementation where changes are propagated bidirectional
    /// </summary>
    /// <typeparam name="T">The type of our stored <see cref="Bindable{T}.Value"/></typeparam>
    public class TwoWayBinding<T> : Binding<T>
    {
        /// <summary>
        /// Creates a new <see cref="TwoWayBinding{T}"/> instance.
        /// </summary>
        /// <param name="source">A binding source</param>
        /// <param name="target">A binding target</param>
        public TwoWayBinding(Bindable<T> source, Bindable<T> target)
            : base(source, target)
        {
        }

        /// <summary>
        /// Propagates <see cref="Bindable{T}.Value"/> changes bidirectionally
        /// </summary>
        /// <param name="previousValue"></param>
        /// <param name="value"></param>
        /// <param name="bypassChecks"></param>
        /// <param name="source"></param>
        public override void PropagateValueChange(T previousValue, T value, bool bypassChecks, Bindable<T> source)
        {
            if (Source.TryGetTarget(out var bindingSource) && Target.TryGetTarget(out var bindingTarget))
            {
                if (!ReferenceEquals(bindingSource, source) && !EqualityComparer<T>.Default.Equals(bindingSource.Value, value))
                    bindingSource.SetValue(previousValue, value, bypassChecks);
                else if (!ReferenceEquals(bindingTarget, source) && !EqualityComparer<T>.Default.Equals(bindingTarget.Value, value))
                    bindingTarget.SetValue(previousValue, value, bypassChecks);
            }
        }

        /// <summary>
        /// Propagates <see cref="Bindable{T}.Default"/> changes bidirectionally
        /// </summary>
        /// <param name="previousValue"></param>
        /// <param name="value"></param>
        /// <param name="bypassChecks"></param>
        /// <param name="source"></param>
        public override void PropagateDefaultChange(T previousValue, T value, bool bypassChecks, Bindable<T> source)
        {
            if (Source.TryGetTarget(out var bindingSource) && Target.TryGetTarget(out var bindingTarget))
            {
                if (!ReferenceEquals(bindingSource, source) && !EqualityComparer<T>.Default.Equals(bindingSource.Default, value))
                    bindingSource.SetDefaultValue(previousValue, value, bypassChecks);
                else
                {
                    if (!ReferenceEquals(bindingTarget, source) && !EqualityComparer<T>.Default.Equals(bindingTarget.Default, value))
                        bindingTarget.SetDefaultValue(previousValue, value, bypassChecks);
                }
            }
        }

        /// <summary>
        /// Propagates <see cref="Bindable{T}.Disabled"/> changes bidirectionally
        /// </summary>
        /// <param name="source"></param>
        /// <param name="bypassChecks"></param>
        public override void PropagateDisabledChange(Bindable<T> source, bool bypassChecks)
        {
            if (Source.TryGetTarget(out var bindingSource) && Target.TryGetTarget(out var bindingTarget))
            {
                if (!ReferenceEquals(bindingSource, source) && bindingSource.Disabled != source.Disabled)
                    bindingSource.SetDisabled(source.Disabled, bypassChecks);
                else
                {
                    if (!ReferenceEquals(bindingTarget, source) && bindingTarget.Disabled != source.Disabled)
                        bindingTarget.SetDisabled(source.Disabled, bypassChecks);
                }
            }
        }
    }
}
