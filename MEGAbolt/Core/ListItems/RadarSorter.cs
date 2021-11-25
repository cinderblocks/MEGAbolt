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
using System.Globalization;

namespace MEGAbolt
{
    public class RadarSorter : IComparer
    {
        //private int col;

        public RadarSorter()
        {
            
        }
        
        public int Compare(object x, object y)
        {
            ListViewItem item1 = (ListViewItem)x;
            ListViewItem item2 = (ListViewItem)y;

            int distx = int.Parse(ExtractNumbers(item1.Text), CultureInfo.CurrentCulture);
            int disty = int.Parse(ExtractNumbers(item2.Text), CultureInfo.CurrentCulture);

            if (distx > disty)
            {
                return 1;
            }
            else if (distx < disty)
            {
                return -1;
            }
            else            
            {
                return 1;
            }
        }

        private static string ExtractNumbers(string expr)
        { 
            string number = System.Text.RegularExpressions.Regex.Match(expr, @"\[(?<r>\d+)m").Groups[1].Value;

            if (string.IsNullOrEmpty(number)) number = "0";

            return number;
        }
    }
}