/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2008-2014, www.metabolt.net (METAbolt)
 * Copyright(c) 2021, Sjofn, LLC
 * All rights reserved.
 *  
 * Radegast is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.If not, see<https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Collections;
using System.Globalization;

namespace METAbolt
{
    #region AutoCompleteListSorter
    public class AutoCompleteStringListSorter : IComparer<string>
    {
        private CaseInsensitiveComparer ItemComparer;

        public AutoCompleteStringListSorter()
        {
            SortingOrder = SortOrder.None;
            ItemComparer = new CaseInsensitiveComparer(CultureInfo.CurrentCulture);
        }

        #region IComparer Member

        public int Compare(string x, string y)
        {
            int compareResult = 0;

            String ItemX, ItemY;

            ItemX = x;
            ItemY = y;

            try
            {
                if (ItemX.StartsWith("_", StringComparison.CurrentCultureIgnoreCase) && !ItemY.StartsWith("_", StringComparison.CurrentCultureIgnoreCase))
                {
                    compareResult = 1;
                }
                else if (!ItemX.StartsWith("_", StringComparison.CurrentCultureIgnoreCase) && ItemY.StartsWith("_", StringComparison.CurrentCultureIgnoreCase))
                {
                    compareResult = -1;
                }
                else
                {
                    compareResult = ItemComparer.Compare(ItemX, ItemY);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "AutoCompleteListSorter");
            }

            if (SortingOrder == SortOrder.Ascending)
            {
                return compareResult;
            }
            else if (SortingOrder == SortOrder.Descending)
            {
                return (-compareResult);
            }
            else
            {
                return 0;
            }
        }
        #endregion

        public SortOrder SortingOrder { set; get; }
    }
    #endregion

    #region SWoDLAutoCompleteItem IComparer Member
    //public class AutoCompleteItemListSorter : IComparer<SWoDLAutoCompleteItem>
    //{
    //    private SortOrder OrderOfSort;
    //    private CaseInsensitiveComparer ItemComparer;

    //    public AutoCompleteItemListSorter()
    //    {

    //        OrderOfSort = SortOrder.None;

    //        ItemComparer = new CaseInsensitiveComparer();
    //    }

    //    #region IComparer Member

    //    public int Compare(SWoDLAutoCompleteItem x, SWoDLAutoCompleteItem y)
    //    {
    //        int compareResult = 0;

    //        SWoDLAutoCompleteItem ItemX, ItemY;

    //        ItemX = x;
    //        ItemY = y;

    //        try
    //        {
    //            if (ItemX.Name.StartsWith("_") && !ItemY.Name.StartsWith("_"))
    //            {
    //                compareResult = 1;
    //            }
    //            else if (!ItemX.Name.StartsWith("_") && ItemY.Name.StartsWith("_"))
    //            {
    //                compareResult = -1;
    //            }
    //            else
    //            {
    //                compareResult = ItemComparer.Compare(ItemX.Name, ItemY.Name);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            MessageBox.Show(ex.ToString(), "AutoCompleteListSorter");
    //        }

    //        if (OrderOfSort == SortOrder.Ascending)
    //        {
    //            return compareResult;
    //        }
    //        else if (OrderOfSort == SortOrder.Descending)
    //        {
    //            return (-compareResult);
    //        }
    //        else
    //        {
    //            return 0;
    //        }
    //    }
    //    #endregion

    //    public SortOrder SortingOrder
    //    {
    //        set
    //        {
    //            OrderOfSort = value;
    //        }

    //        get
    //        {
    //            return OrderOfSort;
    //        }
    //    }
    //}
    #endregion
}
