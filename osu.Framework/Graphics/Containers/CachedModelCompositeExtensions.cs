// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.Containers
{
    internal static class CachedModelCompositeExtensions
    {
        public static void UpdateShadowBindings<TModel>(this ICachedModelComposite<TModel> composite, TModel lastModel, TModel newModel)
            where TModel : new()
        {
            foreach (var field in typeof(TModel).GetFields(CachedAttribute.ACTIVATOR_FLAGS).Where(f => f.GetCustomAttributes<CachedAttribute>().Any()))
                rebind(field);

            foreach (var property in typeof(TModel).GetProperties(CachedAttribute.ACTIVATOR_FLAGS).Where(f => f.GetCustomAttributes<CachedAttribute>().Any()))
                rebind(property);

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
                        break;
                    case FieldInfo fi:
                        shadowValue = fi.GetValue(composite.BoundModel);
                        lastModelValue = lastModel == null ? null : fi.GetValue(lastModel);
                        newModelValue = newModel == null ? null : fi.GetValue(newModel);
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