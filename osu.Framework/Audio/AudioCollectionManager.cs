// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

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
            if (Items.Contains(item)) return;

            item.AddAdjustmentDependency(this);
            Items.Add(item);
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];

                item?.Update();

                //todo: this is wrong (completed items may want to stay in an AudioCollectionManager ie. AudioTracks)
                if ((item as IHasCompletedState)?.HasCompleted ?? false)
                {
                    item.Dispose();
                    Items.RemoveAt(i--);
                }
            }
        }
    }
}
