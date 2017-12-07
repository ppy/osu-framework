// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
        protected List<T> Items = new List<T>();

        public void AddItem(T item)
        {
            RegisterItem(item);
            AddItemToList(item);
        }

        public void AddItemToList(T item)
        {
            PendingActions.Enqueue(delegate
            {
                if (Items.Contains(item)) return;
                Items.Add(item);
            });
        }

        public void RegisterItem(T item)
        {
            PendingActions.Enqueue(() => item.AddAdjustmentDependency(this));
        }

        public void UnregisterItem(T item)
        {
            PendingActions.Enqueue(() => item.RemoveAdjustmentDependency(this));
        }

        internal override void OnStateChanged()
        {
            base.OnStateChanged();
            foreach (var item in Items)
                item.OnStateChanged();
        }

        public virtual void UpdateDevice(int deviceIndex)
        {
            foreach (var item in Items.OfType<IBassAudio>())
                item.UpdateDevice(deviceIndex);
        }

        protected override void UpdateState()
        {
            base.UpdateState();

            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];

                if (!item.IsAlive)
                {
                    Items.RemoveAt(i--);
                    continue;
                }

                item.Update();
            }
        }
    }
}
