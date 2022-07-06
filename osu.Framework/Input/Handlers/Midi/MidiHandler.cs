// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Commons.Music.Midi;
using osu.Framework.Extensions;
using osu.Framework.Input.StateChanges;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Threading;

namespace osu.Framework.Input.Handlers.Midi
{
    public class MidiHandler : InputHandler
    {
        public override string Description => "MIDI";
        public override bool IsActive => inGoodState;

        private bool inGoodState = true;

        private ScheduledDelegate scheduledRefreshDevices;

        private readonly Dictionary<string, IMidiInput> openedDevices = new Dictionary<string, IMidiInput>();

        /// <summary>
        /// The last event for each midi device. This is required for Running Status (repeat messages sent without
        /// event type).
        /// </summary>
        private readonly Dictionary<string, byte> runningStatus = new Dictionary<string, byte>();

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            Enabled.BindValueChanged(e =>
            {
                if (e.NewValue)
                {
                    inGoodState = true;
                    host.InputThread.Scheduler.Add(scheduledRefreshDevices = new ScheduledDelegate(() => refreshDevices(), 0, 500));
                }
                else
                {
                    scheduledRefreshDevices?.Cancel();

                    lock (openedDevices)
                    {
                        foreach (var device in openedDevices.Values)
                            closeDevice(device);

                        openedDevices.Clear();
                    }
                }
            }, true);

            return refreshDevices();
        }

        private bool refreshDevices()
        {
            try
            {
                var inputs = MidiAccessManager.Default.Inputs.ToList();

                lock (openedDevices)
                {
                    // check removed devices
                    foreach (string key in openedDevices.Keys.ToArray())
                    {
                        var device = openedDevices[key];

                        if (inputs.All(i => i.Id != key))
                        {
                            closeDevice(device);
                            openedDevices.Remove(key);

                            Logger.Log($"Disconnected MIDI device: {device.Details.Name}");
                        }
                    }

                    // check added devices
                    foreach (IMidiPortDetails input in inputs)
                    {
                        if (openedDevices.All(x => x.Key != input.Id))
                        {
                            var newInput = MidiAccessManager.Default.OpenInputAsync(input.Id).GetResultSafely();
                            newInput.MessageReceived += onMidiMessageReceived;
                            openedDevices[input.Id] = newInput;

                            Logger.Log($"Connected MIDI device: {newInput.Details.Name}");
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                string message = RuntimeInfo.OS == RuntimeInfo.Platform.Linux
                    ? "Is libasound2-dev installed?"
                    : "There may be another application already using MIDI.";

                Logger.Log($"MIDI devices could not be enumerated. {message} ({e.Message})");

                // stop attempting to refresh devices until next startup.
                inGoodState = false;
                scheduledRefreshDevices?.Cancel();
                return false;
            }
        }

        private void closeDevice(IMidiInput device)
        {
            device.MessageReceived -= onMidiMessageReceived;

            // some devices may take some time to close, so this should be fire-and-forget.
            // the internal implementations look to have their own (eventual) timeout logic.
            Task.Factory.StartNew(() => device.CloseAsync(), TaskCreationOptions.LongRunning);
        }

        private void onMidiMessageReceived(object sender, MidiReceivedEventArgs e)
        {
            Debug.Assert(sender is IMidiInput);
            string senderId = ((IMidiInput)sender).Details.Id;

            try
            {
                for (int i = e.Start; i < e.Length;)
                {
                    readEvent(e.Data, senderId, ref i, out byte eventType, out byte key, out byte velocity);
                    dispatchEvent(eventType, key, velocity);
                }
            }
            catch (Exception exception)
            {
                string dataString = string.Join("-", e.Data.Select(b => b.ToString("X2")));
                Logger.Error(exception, $"An exception occurred while reading MIDI data from sender {senderId}: {dataString}");
            }
        }

        /// <remarks>
        /// This function is not intended to provide complete correctness of MIDI parsing.
        /// For now the goal is to correctly parse "note start" and "note end" events and correctly delimit all events.
        /// </remarks>
        private void readEvent(byte[] data, string senderId, ref int i, out byte eventType, out byte key, out byte velocity)
        {
            byte statusType = data[i++];

            // continuation messages:
            // need running status to be interpreted correctly
            if (statusType <= 0x7F)
            {
                if (!runningStatus.ContainsKey(senderId))
                    throw new InvalidDataException($"Received running status of sender {senderId}, but no event type was stored");

                eventType = runningStatus[senderId];
                key = statusType;
                velocity = data[i++];
                return;
            }

            // real-time messages:
            // 0 additional data bytes always, do not reset running status
            if (statusType >= 0xF8)
            {
                eventType = statusType;
                key = velocity = 0;
                return;
            }

            // system common messages:
            // variable number of additional data bytes, reset running status
            if (statusType >= 0xF0)
            {
                eventType = statusType;

                // system exclusive message
                // vendor-specific, terminated by 0xF7
                // ignoring their whole contents for now since we can't do anything with them anyway
                if (statusType == 0xF0)
                {
                    while (data[i - 1] != 0xF7)
                        i++;

                    key = velocity = 0;
                }
                // other common system messages
                // fixed size given by MidiEvent.FixedDataSize
                else
                {
                    key = MidiEvent.FixedDataSize(statusType) >= 1 ? data[i++] : (byte)0;
                    velocity = MidiEvent.FixedDataSize(statusType) == 2 ? data[i++] : (byte)0;
                }

                runningStatus.Remove(senderId);
                return;
            }

            // channel messages
            // fixed size (varying per event type), set running status
            eventType = statusType;
            key = MidiEvent.FixedDataSize(statusType) >= 1 ? data[i++] : (byte)0;
            velocity = MidiEvent.FixedDataSize(statusType) == 2 ? data[i++] : (byte)0;

            runningStatus[senderId] = eventType;
        }

        private void dispatchEvent(byte eventType, byte key, byte velocity)
        {
            // Low nibble only contains channel data in note on/off messages
            // Ignore to receive messages from all channels
            switch (eventType & 0xF0)
            {
                case MidiEvent.NoteOn:
                    if (velocity != 0)
                        noteOn();
                    else
                        noteOff();
                    break;

                case MidiEvent.NoteOff:
                    noteOff();
                    break;
            }

            void noteOn()
            {
                PendingInputs.Enqueue(new MidiKeyInput((MidiKey)key, velocity, true));
                FrameStatistics.Increment(StatisticsCounterType.MidiEvents);
            }

            void noteOff()
            {
                PendingInputs.Enqueue(new MidiKeyInput((MidiKey)key, 0, false));
                FrameStatistics.Increment(StatisticsCounterType.MidiEvents);
            }
        }
    }
}
