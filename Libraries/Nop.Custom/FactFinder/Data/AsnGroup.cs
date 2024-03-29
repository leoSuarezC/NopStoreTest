﻿using System.Collections.Generic;

namespace Omikron.FactFinder.Data
{
    public class AsnGroup : List<AsnFilterItem>
    {
        public string Name { get; private set; }
        public int DetailedLinkCount { get; private set; }
        public string Unit { get; private set; }
        public AsnGroupStyle Style { get; private set; }
        public AsnGroupSelectionType SelectionType { get; private set; }

        public AsnGroup(
            IEnumerable<AsnFilterItem> filters,
            string name = "",
            int detailedLinkCount = 0,
            string unit = "",
            AsnGroupStyle style = AsnGroupStyle.Default,
            AsnGroupSelectionType selectionType =AsnGroupSelectionType.singleHideUnselected
        )
            : base(filters)    
        {
            Name = name;
            DetailedLinkCount = detailedLinkCount;
            Unit = unit;
            Style = style;
            SelectionType = selectionType;
        }

        public bool HasPreviewImages()
        {
            foreach (var item in this)
            {
                if (item.HasPreviewImage())
                    return true;
            }
            return false;
        }

        public bool HasSelectedItems()
        {
            foreach (var item in this)
            {
                if (item.Selected)
                    return true;
            }
            return false;
        }
    }
}
