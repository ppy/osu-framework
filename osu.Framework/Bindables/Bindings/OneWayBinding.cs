// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Bindables.Bindings
{
    /// <summary>
    /// A <see cref="Binding{T}"/> implementation where changes are propagated only from <see cref="Binding{T}.Source"/> to <see cref="Binding{T}.Target"/>
    /// </summary>
    /// <typeparam name="T">The type of our stored <see cref="Bindable{T}.Value"/></typeparam>
    public class OneWayBinding<T> : Binding<T>
    {
        /// <summary>
        /// Creates a new <see cref="OneWayBinding{T}"/> instance.
        /// </summary>
        /// <param name="source">A binding source</param>
        /// <param name="target">A binding target</param>
        public OneWayBinding(Bindable<T> source, Bindable<T> target)
            : base(source, target)
        {
        }

        /// <summary>
        /// Propagates <see cref="Bindable{T}.Value"/> changes from <see cref="Binding{T}.Source"/> to <see cref="Binding{T}.Target"/>
        /// </summary>
        /// <param name="previousValue">Previous <see cref="Bindable{T}.Value"/></param>
        /// <param name="value">New <see cref="Bindable{T}.Value"/></param>
        /// <param name="bypassChecks">Whether the checks will be skipped</param>
        /// <param name="source"><see cref="Bindable{T}"/> which propagates the change to others</param>
        public override void PropagateValueChange(T previousValue, T value, bool bypassChecks, Bindable<T> source)
        {
            if (Source.TryGetTarget(out var bindingSource) && bindingSource == source && Target.TryGetTarget(out var bindingTarget))
                bindingTarget.SetValue(previousValue, value, bypassChecks);
        }

        /// <summary>
        /// Propagates <see cref="Bindable{T}.Default"/> changes from <see cref="Binding{T}.Source"/> to <see cref="Binding{T}.Target"/>
        /// </summary>
        /// <param name="previousValue">Previous <see cref="Bindable{T}.Value"/></param>
        /// <param name="value">New <see cref="Bindable{T}.Value"/></param>
        /// <param name="bypassChecks">Whether the checks will be skipped</param>
        /// <param name="source"><see cref="Bindable{T}"/> which propagates the change to others</param>
        public override void PropagateDefaultChange(T previousValue, T value, bool bypassChecks, Bindable<T> source)
        {
            if (Source.TryGetTarget(out var bindingSource) && bindingSource == source && Target.TryGetTarget(out var bindingTarget))
                bindingTarget.SetDefaultValue(previousValue, value, bypassChecks);
        }

        /// <summary>
        /// Propagates <see cref="Bindable{T}.Disabled"/> changes from <see cref="Binding{T}.Source"/> to <see cref="Binding{T}.Target"/>
        /// </summary>
        /// <param name="source">New <see cref="Bindable{T}.Value"/></param>
        /// <param name="bypassChecks">Whether the checks will be skipped</param>
        public override void PropagateDisabledChange(Bindable<T> source, bool bypassChecks)
        {
            if (Source.TryGetTarget(out var bindingSource) && bindingSource == source && Target.TryGetTarget(out var bindingTarget))
                bindingTarget.SetDisabled(source.Disabled, bypassChecks);
        }
    }
}
