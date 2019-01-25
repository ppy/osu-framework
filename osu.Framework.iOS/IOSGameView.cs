// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Foundation;
using ObjCRuntime;
using UIKit;
using System.Threading.Tasks;
using osu.Framework.Graphics.OpenGL;
using OpenGLES;
using CoreAnimation;
using osuTK.Graphics.ES30;

namespace osu.Framework.iOS
{
    [Register("iOSGameView")]
    public class IOSGameView : osuTK.iOS.iOSGameView
    {
        public event Action<NSSet> HandleTouches;

        public DummyTextField KeyboardTextField { get; private set; }

        [Export("layerClass")]
        public static Class LayerClass() => GetLayerClass();

        [Export("initWithFrame:")]
        public IOSGameView(System.Drawing.RectangleF frame) : base(frame)
        {
            Scale = (float)UIScreen.MainScreen.Scale;
            ContentScaleFactor = UIScreen.MainScreen.Scale;
            LayerColorFormat = EAGLColorFormat.RGBA8;
            ContextRenderingApi = EAGLRenderingAPI.OpenGLES3;
            LayerRetainsBacking = false;

            AddSubview(KeyboardTextField = new DummyTextField());
        }

        protected override void ConfigureLayer(CAEAGLLayer eaglLayer)
        {
            eaglLayer.Opaque = true;
            ExclusiveTouch = true;
            MultipleTouchEnabled = true;
            UserInteractionEnabled = true;
        }

        public float Scale { get; private set; }

        public override void TouchesBegan(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches);
        public override void TouchesCancelled(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches);
        public override void TouchesEnded(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches);
        public override void TouchesMoved(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches);

        protected override void CreateFrameBuffer()
        {
            base.CreateFrameBuffer();
            GLWrapper.DefaultFrameBuffer = Framebuffer;
        }

        private bool needsResizeFrameBuffer;
        public void RequestResizeFrameBuffer() => needsResizeFrameBuffer = true;

        public override void SwapBuffers()
        {
            base.SwapBuffers();

            // ResizeFrameBuffer needs to run on the main thread, but triggered in such a way that it blocks our draw thread until done
            if (needsResizeFrameBuffer)
            {
                needsResizeFrameBuffer = false;
                GL.Finish();
                InvokeOnMainThread(ResizeFrameBuffer);
            }
        }

        protected override bool ShouldCallOnRender => false;

        public class DummyTextField : UITextField
        {
            public event Action<NSRange, string> HandleShouldChangeCharacters;
            public event Action HandleShouldReturn;
            public event Action<UIKeyCommand> HandleKeyCommand;

            public const int CURSOR_POSITION = 5;

            private int responderSemaphore;

            public DummyTextField()
            {
                AutocapitalizationType = UITextAutocapitalizationType.None;
                AutocorrectionType = UITextAutocorrectionType.No;
                KeyboardType = UIKeyboardType.Default;
                KeyboardAppearance = UIKeyboardAppearance.Default;

                resetText();

                ShouldChangeCharacters = (textField, range, replacementString) =>
                {
                    resetText();
                    HandleShouldChangeCharacters?.Invoke(range, replacementString);
                    return false;
                };

                ShouldReturn = textField =>
                {
                    resetText();
                    HandleShouldReturn?.Invoke();
                    return false;
                };
            }

            public override UIKeyCommand[] KeyCommands => new[]
            {
                UIKeyCommand.Create(UIKeyCommand.LeftArrow, 0, new Selector("keyPressed:")),
                UIKeyCommand.Create(UIKeyCommand.RightArrow, 0, new Selector("keyPressed:")),
                UIKeyCommand.Create(UIKeyCommand.UpArrow, 0, new Selector("keyPressed:")),
                UIKeyCommand.Create(UIKeyCommand.DownArrow, 0, new Selector("keyPressed:"))
            };

            [Export("keyPressed:")]
            private void keyPressed(UIKeyCommand cmd) => HandleKeyCommand?.Invoke(cmd);

            private void resetText()
            {
                // we put in some dummy text and move the cursor to the middle so that backspace (and potentially delete or cursor keys) will be detected
                Text = "dummytext";
                var newPosition = GetPosition(BeginningOfDocument, CURSOR_POSITION);
                SelectedTextRange = GetTextRange(newPosition, newPosition);
            }

            public void UpdateFirstResponder(bool become)
            {
                if (become)
                {
                    responderSemaphore = Math.Max(responderSemaphore + 1, 1);
                    InvokeOnMainThread(() => BecomeFirstResponder());
                }
                else
                {
                    responderSemaphore = Math.Max(responderSemaphore - 1, 0);
                    Task.Delay(200).ContinueWith(task =>
                    {
                        if (responderSemaphore <= 0)
                            InvokeOnMainThread(() => ResignFirstResponder());
                    });
                }
            }
        }
    }
}
