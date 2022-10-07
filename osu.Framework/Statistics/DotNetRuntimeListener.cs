// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using osu.Framework.Extensions.TypeExtensions;
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
        private readonly GlobalStatistic<int> statAllocated = GlobalStatistics.Get<int>(arraypool_statistics_grouping, "Allocated");
        private readonly GlobalStatistic<int> statRented = GlobalStatistics.Get<int>(arraypool_statistics_grouping, "Rented");
        private readonly GlobalStatistic<int> statReturned = GlobalStatistics.Get<int>(arraypool_statistics_grouping, "Returned");

        protected override void OnEventWritten(EventWrittenEventArgs data)
        {
            switch (data.EventSource.Name)
            {
                case source_arraypool:

                    switch ((ArrayPoolEventType)data.EventId)
                    {
                        case ArrayPoolEventType.BufferAllocated:
                            statAllocated.Value++;
                            break;

                        case ArrayPoolEventType.BufferRented:
                            statRented.Value++;
                            statInflight.Value++;
                            break;

                        case ArrayPoolEventType.BufferReturned:
                            statReturned.Value++;

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

                        case GCEventType.FinalizeObject when data.Payload != null:
                            if (data.Payload[0] == null)
                                break;

                            var type = getTypeFromHandle((IntPtr)data.Payload[0]);
                            if (type == null)
                                break;

                            Logger.Log($"Finalizing object: {type.ReadableName()}", LoggingTarget.Performance);

                            break;
                    }

                    break;
            }
        }

        /// <summary>
        /// Retrieves a <see cref="Type"/> from a CLR type id.
        /// </summary>
        /// <remarks>
        /// Attrib: https://stackoverflow.com/questions/26972066/type-from-intptr-handle/54469241#54469241
        /// </remarks>
        // ReSharper disable once RedundantUnsafeContext
        // ReSharper disable once UnusedParameter.Local
        private static unsafe Type getTypeFromHandle(IntPtr handle)
        {
#if NET6_0_OR_GREATER
            // This is super unsafe code which is dependent upon internal CLR structures.
            TypedReferenceAccess tr = new TypedReferenceAccess { Type = handle };
            return __reftype(*(TypedReference*)&tr);
#else
            return null;
#endif
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Matches the internal layout of <see cref="TypedReference"/>.
        /// See: https://source.dot.net/#System.Private.CoreLib/src/System/TypedReference.cs
        /// </summary>
        private struct TypedReferenceAccess
        {
            [JetBrains.Annotations.UsedImplicitly]
            public IntPtr Value;

            [JetBrains.Annotations.UsedImplicitly]
            public IntPtr Type;
        }
#endif

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
            GCAllocationTick_V2 = 10,
            FinalizeObject = 29
        }
    }
}
