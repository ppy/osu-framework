using System;

namespace osu.Framework.Events
{
    public class OnSearchEventArgs : EventArgs
    {
        public string SearchRequest { get; set; }

        public OnSearchEventArgs(string searchRequest)
        {
            SearchRequest = searchRequest;
        }
    }
}
