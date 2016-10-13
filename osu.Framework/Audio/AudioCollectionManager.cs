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
        List<T> ActiveItems = new List<T>();

        protected void AddItem(T item)
        {
            item.AddAdjustmentDependency(this);
            ActiveItems.Add(item);
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < ActiveItems.Count; i++)
            {
                var item = ActiveItems[i];

                item.Update();

                if ((item as IHasCompletedState)?.HasCompleted ?? false)
                {
                    item.Dispose();
                    ActiveItems.RemoveAt(i--);
                }
            }
        }
    }
}
