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

using System.Collections;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Security;

namespace METAbolt
{
    [SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeDateMethods
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);
    } 

    /// <summary>
    /// This class is an implementation of the 'IComparer' interface.
    /// This will only work on Windows platforms.
    /// </summary>
    public class NumericStringComparerDateGroups : IComparer
    {
        /// <summary>
        /// Specifies the column to be sorted
        /// </summary>
        private int ColumnToSort;
        /// <summary>
        /// Specifies the order in which to sort (i.e. 'Ascending').
        /// </summary>
        private SortOrder OrderOfSort;
        /// <summary>
        /// Case insensitive comparer object
        /// </summary>
        //private CaseInsensitiveComparer ObjectCompare;

        /// <summary>
        /// Class constructor.  Initializes various elements
        /// </summary>
        public NumericStringComparerDateGroups()
        {
            // Initialize the column to '0'
            ColumnToSort = 0;

            // Initialize the sort order to 'none'
            OrderOfSort = SortOrder.Ascending;

            // Initialize the CaseInsensitiveComparer object
            //ObjectCompare = new CaseInsensitiveComparer();
        }

        /// <summary>
        /// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
        /// </summary>
        /// <param name="x">First object to be compared</param>
        /// <param name="y">Second object to be compared</param>
        /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        public int Compare(object x, object y)
        {
            int compareResult;
            ListViewItem listviewX, listviewY;

            // Cast the objects to be compared to ListViewItem objects
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;

            // Get the text to be compared
            string a = listviewX.SubItems[ColumnToSort].Text;
            string b = listviewY.SubItems[ColumnToSort].Text;

            // Sanity check
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            string[] d1;
            string[] d2;

            if (a.Contains("/"))
            {
                d1 = a.Split('/');
                a = d1[2] + d1[1] + d1[0];
            }

            if (b.Contains("/"))
            {
                d2 = b.Split('/');
                b = d2[2] + d2[1] + d2[0];
            }

            //a = a.Replace("/", "").Trim() + "abc";
            //b = b.Replace("/", "").Trim() + "abc";

            try
            {
                // Compare the two items
                compareResult = SafeNativeDateMethods.StrCmpLogicalW(a, b);

                // Calculate correct return value based on object comparison
                if (OrderOfSort == SortOrder.Ascending)
                {
                    // Ascending sort is selected, return normal result of compare operation
                    return compareResult;
                }
                else if (OrderOfSort == SortOrder.Descending)
                {
                    // Descending sort is selected, return negative result of compare operation
                    return (-compareResult);
                }
                else
                {
                    // Return '0' to indicate they are equal
                    return 0;
                }
            }
            catch { return 0; }
        }

        //public bool IsDate(string strDate)
        //{
        //    //string strDate = obj.ToString();

        //    try
        //    {
        //        DateTime dt;
        //        return DateTime.TryParse(strDate, out dt);
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        /// <summary>
        /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
        /// </summary>
        public int SortColumn
        {
            set => ColumnToSort = value;
            get => ColumnToSort;
        }

        /// <summary>
        /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
        /// </summary>
        public SortOrder Order
        {
            set => OrderOfSort = value;
            get => OrderOfSort;
        }
    }
}
