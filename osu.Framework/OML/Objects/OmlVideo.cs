// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Network;
using osu.Framework.OML.Attributes;

namespace osu.Framework.OML.Objects
{
    [OmlObject("video")]
    public class OmlVideo : OmlObject
    {
        private VideoSprite videoSprite;

        [UsedImplicitly]
        public Uri Src { get; set; } // This is called source because it can be a URL or File! calling it "FileName" would be invalid.

        private bool loop;

        [UsedImplicitly]
        public bool Loop // Fixes NullReferenceException
        {
            get => videoSprite?.Loop ?? loop;
            set
            {
                if (videoSprite != null)
                    videoSprite.Loop = value;
                else
                    loop = value;
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var webStream = new WebRequest(Src.OriginalString);
            webStream.Perform();

            videoSprite = new VideoSprite(webStream.ResponseStream) { Loop = loop };

            Child = videoSprite;
        }
    }
}
