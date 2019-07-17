// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.Tracing;

namespace osu.Framework.Statistics
{
    internal sealed class DotNetRuntimeListener : EventListener
    {
        private const int gc_keyword = 0x0000001;

        // Called whenever an EventSource is created.
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // Watch for the .NET runtime EventSource and enable all of its events.
            if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime"))
            {
                EnableEvents(
                    eventSource,
                    EventLevel.Verbose,
                    (EventKeywords)(gc_keyword)
                );
            }
        }

        // Called whenever an event is written.
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            switch (eventData.EventId)
            {
                case 1: // GCStart_V1
                    GlobalStatistics.Get<int>("GC", $"Collections Gen{eventData.Payload[1]}").Value++;
                    break;

                case 4: // GCHeapStats_V1
                    for (int i = 0; i <= 6; i += 2)
                        GlobalStatistics.Get<ulong>("GC", $"Size Gen{i / 2}").Value = (ulong)eventData.Payload[i];

                    GlobalStatistics.Get<ulong>("GC", "Finalization queue length").Value = (ulong)eventData.Payload[9];
                    GlobalStatistics.Get<uint>("GC", "Pinned objects").Value = (uint)eventData.Payload[10];
                    break;
            }
        }
    }
}
