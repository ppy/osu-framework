extern alias IOS;

using System;
using System.Diagnostics;
using IOS::System.Drawing;

using IOS::Foundation;
using IOS::GLKit;
using IOS::OpenGLES;
using IOS::ObjCRuntime;
using IOS::CoreAnimation;
using IOS::CoreGraphics;
using IOS::UIKit;
using OpenTK;
using OpenTK.Graphics.ES20;
using OpenTK.Platform.iPhoneOS;

namespace osu.Framework.Platform.iOS
{
    [Register("iOSPlatformGameView")]
    public class iOSPlatformGameView : iPhoneOSGameView
    {
        public event Action<NSSet> HandleTouches;

        [Export("layerClass")]
        static Class LayerClass()
        {
            return iPhoneOSGameView.GetLayerClass();
        }

        protected override void ConfigureLayer(CAEAGLLayer eaglLayer)
        {
            eaglLayer.Opaque = true;
            ExclusiveTouch = true;
            MultipleTouchEnabled = true;
            UserInteractionEnabled = true;
        }

        [Export("initWithFrame:")]
        public iOSPlatformGameView(IOS::System.Drawing.RectangleF frame) : base(frame)
        {
            LayerRetainsBacking = false;
            LayerColorFormat = EAGLColorFormat.RGBA8;
            ContextRenderingApi = EAGLRenderingAPI.OpenGLES3;

            Scale = (float)UIScreen.MainScreen.Scale;
            ContentScaleFactor = UIScreen.MainScreen.Scale;

        }

        public float Scale { get; private set; }

        public override void TouchesBegan(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches);
        public override void TouchesCancelled(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches);
        public override void TouchesEnded(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches);
        public override void TouchesMoved(NSSet touches, UIEvent evt) => HandleTouches?.Invoke(touches);
    }
}
