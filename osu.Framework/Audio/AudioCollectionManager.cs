// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace osu.Framework.Audio
{
    /// <summary>
    /// A collection of audio components which need central property control.
    /// </summary>
    public class AudioCollectionManager<T> : AdjustableAudioComponent
        where T : AdjustableAudioComponent
    {
        protected List<T> Items = new List<T>();

        public void AddItem(T item)
        {
            PendingActions.Enqueue(delegate
            {
                if (Items.Contains(item)) return;

                item.AddAdjustmentDependency(this);
                Items.Add(item);
            });
        }

        internal override void OnStateChanged(object sender, EventArgs e)
        {
            base.OnStateChanged(sender, e);
            foreach (var item in Items)
                item.OnStateChanged(sender, e);
        }

        public virtual void UpdateDevice(int deviceIndex)
        {
            foreach (var item in Items.OfType<IBassAudio>())
                item.UpdateDevice(deviceIndex);
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];

                //todo: this is wrong (completed items may want to stay in an AudioCollectionManager ie. AudioTracks)
                if (item.HasCompleted)
                {
                    Items.RemoveAt(i--);
                    item.Dispose();
                    continue;
                }

                item.Update();
            }
        }
    }
}
