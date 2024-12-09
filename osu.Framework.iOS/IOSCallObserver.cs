// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using CallKit;
using Foundation;

namespace osu.Framework.iOS
{
    internal class IOSCallObserver : NSObject, ICXCallObserverDelegate
    {
        private readonly Action incomingCall;
        private readonly Action endedCall;

        private readonly CXCallController callController;

        public IOSCallObserver(Action incomingCall, Action endedCall)
        {
            this.incomingCall = incomingCall;
            this.endedCall = endedCall;

            callController = new CXCallController();
            callController.CallObserver.SetDelegate(this, null);
        }

        public void CallChanged(CXCallObserver callObserver, CXCall call)
        {
            if (!call.HasEnded)
                incomingCall();
            else
                endedCall();
        }

        protected override void Dispose(bool disposing)
        {
            callController.Dispose();
            base.Dispose(disposing);
        }
    }
}
