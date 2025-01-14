// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using CallKit;
using Foundation;

namespace osu.Framework.iOS
{
    internal class IOSCallObserver : NSObject, ICXCallObserverDelegate
    {
        public event Action? OnCall;
        public event Action? OnCallEnded;

        private readonly CXCallController callController;

        public IOSCallObserver()
        {
            callController = new CXCallController();
            callController.CallObserver.SetDelegate(this, null);

            if (callController.CallObserver.Calls.Any(c => !c.HasEnded))
                OnCall?.Invoke();
        }

        public void CallChanged(CXCallObserver callObserver, CXCall call)
        {
            if (!call.HasEnded)
                OnCall?.Invoke();
            else
                OnCallEnded?.Invoke();
        }

        protected override void Dispose(bool disposing)
        {
            callController.Dispose();
            base.Dispose(disposing);
        }
    }
}
