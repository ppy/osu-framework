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
        private readonly Action onCall;
        private readonly Action onCallEnded;

        private readonly CXCallController callController;

        public IOSCallObserver(Action onCall, Action onCallEnded)
        {
            this.onCall = onCall;
            this.onCallEnded = onCallEnded;

            callController = new CXCallController();
            callController.CallObserver.SetDelegate(this, null);

            if (callController.CallObserver.Calls.Any(c => !c.HasEnded))
                onCall();
        }

        public void CallChanged(CXCallObserver callObserver, CXCall call)
        {
            if (!call.HasEnded)
                onCall();
            else
                onCallEnded();
        }

        protected override void Dispose(bool disposing)
        {
            callController.Dispose();
            base.Dispose(disposing);
        }
    }
}
