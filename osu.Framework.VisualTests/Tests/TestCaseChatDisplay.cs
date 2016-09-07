//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Threading;
using OpenTK;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseChatDisplay : TestCase
    {
        private ScheduledDelegate messageRequest;

        internal override string Name => @"Chat";
        internal override string Description => @"Testing API polling";

        List<Channel> channels = new List<Channel>();
        private FlowContainer flow;

        internal override void Reset()
        {
            base.Reset();

            flow = new FlowContainer(FlowDirection.VerticalOnly)
            {
                LayoutDuration = 100,
                LayoutEasing = EasingTypes.Out,
                Padding = new Vector2(1, 1)
            };


            lastMessageId = null;

            if (Game.API.State != APIAccess.APIState.Online)
                Game.API.OnStateChange += delegate { initializeChannels(); };
            else
                initializeChannels();


            ScrollContainer scrolling = new ScrollContainer()
            {
                SizeMode = InheritMode.Inherit,
                Size = new Vector2(1, 0.5f)
            };

            scrolling.Add(flow);
            Add(scrolling);
        }

        private void initializeChannels()
        {
            if (Game.API.State != APIAccess.APIState.Online)
                return;

            messageRequest?.Cancel();

            ListChannelsRequest req = new ListChannelsRequest();
            req.Success += delegate (List<Channel> channels)
            {
                NotificationManager.ShowMessage($@"Received details for {channels.Count} channels!");
                this.channels = channels;

                messageRequest = Game.Scheduler.AddDelayed(requestNewMessages, 1000, true);
            };
            Game.API.Queue(req);
        }

        long? lastMessageId = null;

        private void requestNewMessages()
        {
            messageRequest.Wait();

            Channel osu = channels.Find(c => c.Name == "#osu");

            GetMessagesRequest gm = new GetMessagesRequest(new List<Channel> { osu }, lastMessageId);
            gm.Success += delegate (List<Message> messages)
            {
                foreach (Message m in messages)
                {
                    m.LineWidth = this.Size.X; //this is kinda ugly.
                    m.Drawable.Depth = m.Id;
                    m.Drawable.FadeInFromZero(800);

                    flow.Add(m.Drawable);

                    if (osu.Messages.Count > 50)
                    {
                        osu.Messages[0].Drawable.Expire();
                        osu.Messages.RemoveAt(0);
                    }

                    osu.Messages.Add(m);
                }

                lastMessageId = messages.LastOrDefault()?.Id ?? lastMessageId;

                Debug.Write("success!");
                messageRequest.Continue();
            };
            gm.Failure += delegate
            {
                Debug.Write("failure!");
                messageRequest.Continue();
            };

            Game.API.Queue(gm);
        }
    }
}
