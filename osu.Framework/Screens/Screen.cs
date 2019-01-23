// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Screens
{
    public class Screen : CompositeDrawable, IScreen
    {
        public bool ValidForResume { get; set; } = true;

        public bool ValidForPush { get; set; } = true;

        public virtual void OnEntering(IScreen last)
        {
        }

        public virtual bool OnExiting(IScreen next) => false;

        public virtual void OnResuming(IScreen last)
        {
        }

        public virtual void OnSuspending(IScreen next)
        {
        }

        public class ScreenNotCurrentException : InvalidOperationException
        {
            public ScreenNotCurrentException(string action)
                : base($"Cannot perform {action} on a non-current screen.")
            {
            }
        }

        public class ScreenHasChildException : InvalidOperationException
        {
            public ScreenHasChildException(string action, string description)
                : base($"Cannot perform {action} when a child is already present. {description}")
            {
            }
        }

        public class ScreenAlreadyEnteredException : InvalidOperationException
        {
            public ScreenAlreadyEnteredException()
                : base("Cannot push a screen in an entered state.")
            {
            }
        }

        public class ScreenWillBeRemovedOnPushException : InvalidOperationException
        {
            public ScreenWillBeRemovedOnPushException(Type type)
                : base($"The pushed ({type.ReadableName()}) has {nameof(RemoveWhenNotAlive)} = true and will be removed when a child screen is pushed. "
                       + $"Screens must set {nameof(RemoveWhenNotAlive)} to false.")
            {
            }
        }
    }
}
