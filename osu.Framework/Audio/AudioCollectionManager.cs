// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Audio
{
    /// <summary>
    /// A collection of audio components which need central property control.
    /// </summary>
    public class AudioCollectionManager<T> : AdjustableAudioComponent
        where T : AdjustableAudioComponent
    {
        protected List<WeakReference<T>> Items = new List<WeakReference<T>>();

        public void AddItem(T item)
        {
            var weakRef = new WeakReference<T>(item);
            PendingActions.Enqueue(delegate
            {
                if (Items.Contains(weakRef)) return;

                item.AddAdjustmentDependency(this);
                Items.Add(weakRef);
            });
        }

        public virtual void UpdateDevice(int deviceIndex)
        {
            foreach (var weakRef in Items)
            {
                T item;
                if (weakRef.TryGetTarget(out item) && item is IBassAudio)
                    ((IBassAudio)item).UpdateDevice(deviceIndex);
            }
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < Items.Count; i++)
            {
                var weakRef = Items[i];
                T item;
                if (!weakRef.TryGetTarget(out item))
                {
                    Items.RemoveAt(i--);
                    continue;
                }
                //todo: this is wrong (completed items may want to stay in an AudioCollectionManager ie. AudioTracks)
                else if (item.HasCompleted)
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
