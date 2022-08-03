// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CoreAnimation;
using Foundation;
using ObjCRuntime;
using OpenGLES;
using osu.Framework.Graphics.OpenGL;
using osuTK.Graphics.ES30;
using osuTK.iOS;
using UIKit;

namespace osu.Framework.iOS
{
    [Register("iOSGameView")]
    public class IOSGameView : iOSGameView
    {
        public event Action<NSSet<UIPress>, UIPressesEvent> HandlePresses;
        public event Action<NSSet, UIEvent> HandleTouches;

        public HiddenTextField KeyboardTextField { get; }

        public int DefaultFrameBuffer;

        [Export("layerClass")]
        public static Class LayerClass() => GetLayerClass();

        [Export("initWithFrame:")]
        public IOSGameView(RectangleF frame)
            : base(frame)
        {
            Scale = (float)UIScreen.MainScreen.Scale;
            ContentScaleFactor = UIScreen.MainScreen.Scale;
            LayerColorFormat = EAGLColorFormat.RGBA8;
            ContextRenderingApi = EAGLRenderingAPI.OpenGLES3;
            LayerRetainsBacking = false;

            AddSubview(KeyboardTextField = new HiddenTextField());
        }

        protected override void ConfigureLayer(CAEAGLLayer eaglLayer)
        {
            eaglLayer.Opaque = true;
            ExclusiveTouch = true;
            MultipleTouchEnabled = true;
            UserInteractionEnabled = true;
        }

        public float Scale { get; }

        // SafeAreaInsets is cached to prevent access outside the main thread
        private UIEdgeInsets safeArea = UIEdgeInsets.Zero;

        internal UIEdgeInsets SafeArea
        {
            get => safeArea;
            set
            {
                if (value.Equals(safeArea))
                    return;

                safeArea = value;
                OnResize(EventArgs.Empty);
            }
        }

        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            base.PressesBegan(presses, evt);

            // On early iOS versions, hardware keyboard events are handled from GSEvents in GameUIApplication instead.
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
                HandlePresses?.Invoke(presses, evt);
        }

        public override void PressesCancelled(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            base.PressesCancelled(presses, evt);

            // On early iOS versions, hardware keyboard events are handled from GSEvents in GameUIApplication instead.
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
                HandlePresses?.Invoke(presses, evt);
        }

        public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            base.PressesEnded(presses, evt);

            // On early iOS versions, hardware keyboard events are handled from GSEvents in GameUIApplication instead.
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
                HandlePresses?.Invoke(presses, evt);
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches, evt);
        public override void TouchesCancelled(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches, evt);
        public override void TouchesEnded(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches, evt);
        public override void TouchesMoved(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches, evt);

        protected override void CreateFrameBuffer()
        {
            base.CreateFrameBuffer();
            DefaultFrameBuffer = Framebuffer;
        }

        private bool needsResizeFrameBuffer;
        public void RequestResizeFrameBuffer() => needsResizeFrameBuffer = true;

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            SafeArea = SafeAreaInsets;
        }

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

        public class HiddenTextField : UITextField
        {
            public event Action<NSRange, string> HandleShouldChangeCharacters;
            public event Action HandleShouldReturn;

            /// <summary>
            /// Placeholder text that the <see cref="HiddenTextField"/> will be populated with after every keystroke.
            /// </summary>
            private const string placeholder_text = "aaaaaa";

            /// <summary>
            /// The approximate midpoint of <see cref="placeholder_text"/> that the cursor will be reset to after every keystroke.
            /// </summary>
            public const int CURSOR_POSITION = 3;

            private int responderSemaphore;

            /// <summary>
            /// The list of actions which can't be supported with this text field.
            /// </summary>
            /// <remarks>
            /// Paste is supported since it doesn't rely on the text entered in this field, only raising an <see cref="UITextField.ShouldChangeCharacters"/> event containing the pasted text.
            /// </remarks>
            private readonly IEnumerable<Selector> blockedActions = new[]
            {
                new Selector("cut:"),
                new Selector("copy:"),
                new Selector("select:"),
                new Selector("selectAll:"),
            };

            public override UITextSmartDashesType SmartDashesType => UITextSmartDashesType.No;
            public override UITextSmartInsertDeleteType SmartInsertDeleteType => UITextSmartInsertDeleteType.No;
            public override UITextSmartQuotesType SmartQuotesType => UITextSmartQuotesType.No;

            public HiddenTextField()
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

            public override bool CanPerform(Selector action, NSObject withSender) => !blockedActions.Contains(action) && base.CanPerform(action, withSender);

            private void resetText()
            {
                // we put in some dummy text and move the cursor to the middle so that backspace (and potentially delete or cursor keys) will be detected
                Text = placeholder_text;
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
