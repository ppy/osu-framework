// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Commons.Music.Midi;
using osu.Framework.Input.StateChanges;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace osu.Framework.Input.Handlers
{
    public class MidiInputHandler : InputHandler
    {
        public override bool IsActive => true;
        public override int Priority => 0;

        private ScheduledDelegate scheduledRefreshDevices;

        private readonly Dictionary<string, IMidiInput> openedDevices = new Dictionary<string, IMidiInput>();

        public override bool Initialize(GameHost host)
        {
            // Try to initialize. This can throw on Linux if asound cannot be found.
            try {
                var unused = MidiAccessManager.Default.Inputs.ToList();
            } catch (Exception e) {
                Logger.Error(e, RuntimeInfo.OS == RuntimeInfo.Platform.Linux
                    ? "Couldn't list input devices, is libasound2-dev installed?"
                    : "Couldn't list input devices.");
                return false;
            }

            Enabled.BindValueChanged(e =>
            {
                if (e.NewValue)
                {
                    host.InputThread.Scheduler.Add(scheduledRefreshDevices = new ScheduledDelegate(refreshDevices, 0, 500));
                }
                else {
                    scheduledRefreshDevices?.Cancel();

                    foreach (var value in openedDevices.Values) {
                        value.MessageReceived -= onMidiMessageReceived;
                    }

                    openedDevices.Clear();
                }
            }, true);

            return true;
        }

        private void refreshDevices()
        {
            var inputs = MidiAccessManager.Default.Inputs.ToList();

            // check removed devices
            foreach (string key in openedDevices.Keys.ToArray()) {
                var value = openedDevices[key];

                if (inputs.All(i => i.Id != key)) {
                    value.MessageReceived -= onMidiMessageReceived;
                    openedDevices.Remove(key);

                    Logger.Log($"Disconnected MIDI device: {value.Details.Name}");
                }
            }

            // check added devices
            foreach (IMidiPortDetails input in inputs) {
                if (openedDevices.All(x => x.Key != input.Id)) {
                    var newInput = MidiAccessManager.Default.OpenInputAsync(input.Id).Result;
                    newInput.MessageReceived += onMidiMessageReceived;
                    openedDevices[input.Id] = newInput;

                    Logger.Log($"Connected MIDI device: {newInput.Details.Name}");
                }
            }
        }

        private void onMidiMessageReceived(object sender, MidiReceivedEventArgs e)
        {
            var events = MidiEvent.Convert(e.Data, e.Start, e.Length);

            foreach (MidiEvent midiEvent in events) {
                var key = midiEvent.Msb;
                var velocity = midiEvent.Lsb;
                Logger.Log("Event " + midiEvent);

                switch (midiEvent.EventType) {
                    case MidiEvent.NoteOn:
                        Logger.Log($"NoteOn: {(MidiKey)key}/{velocity/64f:P}");
                        PendingInputs.Enqueue(new MidiKeyInput((MidiKey)midiEvent.Msb, true));
                        break;

                    case MidiEvent.NoteOff:
                        Logger.Log($"NoteOff: {(MidiKey)key}/{velocity/64f:P}");
                        PendingInputs.Enqueue(new MidiKeyInput((MidiKey)midiEvent.Msb, false));
                        break;
                }
            }
        }
    }
}
