// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Views;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK.Input;

namespace osu.Framework.Android.Input
{
    public class AndroidTouchHandler : InputHandler
    {
        private readonly AndroidGameView view;

        public AndroidTouchHandler(AndroidGameView view)
        {
            this.view = view;
            view.Touch += handleTouches;
            view.Hover += handleHover;
        }

        private void handleTouches(object sender, View.TouchEventArgs e)
        {
            handlePointerEvent(e.Event);

            switch (e.Event.Action & MotionEventActions.Mask)
            {
                case MotionEventActions.Down:
                case MotionEventActions.Move:
                    PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, true));
                    break;

                case MotionEventActions.Up:
                    PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, false));
                    break;
            }
        }

        private void handleHover(object sender, View.HoverEventArgs e)
        {
            handlePointerEvent(e.Event);

            PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Right, e.Event.ButtonState == MotionEventButtonState.StylusPrimary));
        }

        private void handlePointerEvent(MotionEvent e)
        {
            PendingInputs.Enqueue(new MousePositionAbsoluteInput
            {
                Position = new osuTK.Vector2(e.GetX() * view.ScaleX, e.GetY() * view.ScaleY)
            });
        }

        protected override void Dispose(bool disposing)
        {
            view.Touch -= handleTouches;
            view.Hover -= handleHover;
            base.Dispose(disposing);
        }

        public override bool IsActive => true;

        public override int Priority => 0;

        public override bool Initialize(GameHost host) => true;
    }
}
