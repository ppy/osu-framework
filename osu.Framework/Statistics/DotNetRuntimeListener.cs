// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using osu.Framework.Logging;

namespace osu.Framework.Statistics
{
    // https://medium.com/criteo-labs/c-in-process-clr-event-listeners-with-net-core-2-2-ef4075c14e87
    internal sealed class DotNetRuntimeListener : EventListener
    {
        private const int gc_keyword = 0x0000001;

        private const string gc_statistics_grouping = "GC";

        private const string arraypool_statistics_grouping = "ArrayPool";

        private const string source_runtime = "Microsoft-Windows-DotNETRuntime";
        private const string source_arraypool = "System.Buffers.ArrayPoolEventSource";

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            switch (eventSource.Name)
            {
                case source_runtime:
                    EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)gc_keyword);
                    break;

                case source_arraypool:
                    EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
                    break;
            }
        }

        private readonly GlobalStatistic<int> statInflight = GlobalStatistics.Get<int>(arraypool_statistics_grouping, "Inflight");

        protected override void OnEventWritten(EventWrittenEventArgs data)
        {
            switch (data.EventSource.Name)
            {
                case source_arraypool:

                    switch ((ArrayPoolEventType)data.EventId)
                    {
                        case ArrayPoolEventType.BufferAllocated:
                            GlobalStatistics.Get<int>(arraypool_statistics_grouping, "Allocated").Value++;
                            break;

                        case ArrayPoolEventType.BufferRented:
                            GlobalStatistics.Get<int>(arraypool_statistics_grouping, "Rented").Value++;
                            statInflight.Value++;
                            break;

                        case ArrayPoolEventType.BufferReturned:
                            GlobalStatistics.Get<int>(arraypool_statistics_grouping, "Returned").Value++;

                            // the listener may have been started while buffers were already rented.
                            if (statInflight.Value > 0)
                                statInflight.Value--;
                            break;
                    }

                    break;

                case source_runtime:
                    switch ((GCEventType)data.EventId)
                    {
                        case GCEventType.GCStart_V1 when data.Payload != null:
                            // https://docs.microsoft.com/en-us/dotnet/framework/performance/garbage-collection-etw-events#gcstart_v1_event
                            GlobalStatistics.Get<int>(gc_statistics_grouping, $"Collections Gen{data.Payload[1]}").Value++;
                            break;

                        case GCEventType.GCHeapStats_V1 when data.Payload != null:
                            // https://docs.microsoft.com/en-us/dotnet/framework/performance/garbage-collection-etw-events#gcheapstats_v1_event
                            for (int i = 0; i <= 6; i += 2)
                                addStatistic<ulong>($"Size Gen{i / 2}", data.Payload[i]);

                            addStatistic<ulong>("Finalization queue length", data.Payload[9]);
                            addStatistic<uint>("Pinned objects", data.Payload[10]);
                            break;

                        case GCEventType.GCAllocationTick_V2 when data.Payload != null:
                            string name = (string)data.Payload[5];
                            if (string.IsNullOrEmpty(name))
                                break;

                            var allocType = Type.GetType(name, false, false);
                            if (allocType == null)
                                break;

                            var finalizeMethod = allocType.GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance);
                            Debug.Assert(finalizeMethod != null); // All objects have this.

                            if (finalizeMethod.DeclaringType != typeof(object))
                                Logger.Log($"Allocated finalizable object: {name}", LoggingTarget.Performance);

                            break;
                    }

                    break;
            }
        }

        private void addStatistic<T>(string name, object data)
            => GlobalStatistics.Get<T>(gc_statistics_grouping, name).Value = (T)data;

        private enum ArrayPoolEventType
        {
            BufferRented = 1,
            BufferAllocated,
            BufferReturned
        }

        private enum GCEventType
        {
            GCStart_V1 = 1,
            GCHeapStats_V1 = 4,
            GCAllocationTick_V2 = 10
        }
    }
}
