// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Graphics.Containers
{
    internal static class CachedModelCompositeExtensions
    {
        public static IReadOnlyDependencyContainer CreateDependencies<TModel>(this ICachedModelComposite<TModel> composite, IReadOnlyDependencyContainer parent)
            where TModel : new()
            => DependencyActivator.MergeDependencies(composite.BoundModel, parent, new CacheInfo(parent: typeof(TModel)));

        public static void UpdateShadowModel<TModel>(this ICachedModelComposite<TModel> composite, TModel lastModel, TModel newModel)
            where TModel : new()
        {
            var newShadow = newModel == null ? new TModel() : TypeExtensions.Clone(newModel);

            foreach (var field in typeof(TModel).GetFields(CachedAttribute.ACTIVATOR_FLAGS).Where(f => f.GetCustomAttributes<CachedAttribute>().Any()))
                rebind(field);

            foreach (var property in typeof(TModel).GetProperties(CachedAttribute.ACTIVATOR_FLAGS).Where(f => f.GetCustomAttributes<CachedAttribute>().Any()))
                rebind(property);

            composite.BoundModel = newShadow;

            void rebind(MemberInfo member)
            {
                object shadowValue = null;
                object lastModelValue = null;
                object newModelValue = null;

                switch (member)
                {
                    case PropertyInfo pi:
                        shadowValue = pi.GetValue(composite.BoundModel);
                        lastModelValue = lastModel == null ? null : pi.GetValue(lastModel);
                        newModelValue = newModel == null ? null : pi.GetValue(newModel);

                        if (shadowValue is IBindable)
                            pi.SetValue(newShadow, shadowValue);

                        break;
                    case FieldInfo fi:
                        shadowValue = fi.GetValue(composite.BoundModel);
                        lastModelValue = lastModel == null ? null : fi.GetValue(lastModel);
                        newModelValue = newModel == null ? null : fi.GetValue(newModel);

                        if (shadowValue is IBindable)
                            fi.SetValue(newShadow, shadowValue);

                        break;
                }

                if (shadowValue is IBindable shadowBindable)
                {
                    // Unbind from the last model
                    if (lastModelValue is IBindable lastModelBindable)
                        shadowBindable.UnbindFrom(lastModelBindable);

                    // Bind to the new model
                    if (newModelValue is IBindable newModelBindable)
                        shadowBindable.BindTo(newModelBindable);
                }
            }
        }
    }
}