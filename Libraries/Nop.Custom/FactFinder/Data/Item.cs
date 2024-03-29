﻿using System;

namespace Omikron.FactFinder.Data
{

    /// <summary>
    /// Represents any control item on the page. Associated with it are a label
    /// to display and a URL to follow, if it is clicked or selected.
    /// </summary>
    public class Item
    {
        public string Label { get; private set; }
        public virtual Uri Url { get; private set; }
        public virtual bool Selected { get; private set; }

        public Item(string label, Uri url, bool selected = false)
        {
            Label = label;
            Url = url;
            Selected = selected;
        }
    }
}
