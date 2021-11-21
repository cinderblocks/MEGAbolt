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

using System.Windows.Forms;
using OpenMetaverse;

namespace MEGAbolt
{
    public class DateTreeSort : ITreeSortMethod
    {
        #region ITreeSortMethod Members

        public int CompareNodes(InventoryBase x, InventoryBase y, TreeNode nodeX, TreeNode nodeY)
        {
            int returnVal = 0;

            if (x is InventoryItem itemX && y is InventoryItem itemY)
            {
                returnVal = -itemX.CreationDate.CompareTo(itemY.CreationDate);
            }
            else if (x is InventoryFolder && y is InventoryFolder)
                returnVal = nodeX.Text.CompareTo(nodeY.Text);
            else if (x is InventoryFolder && y is InventoryItem)
                returnVal = 1;
            else if (x is InventoryItem && y is InventoryFolder)
                returnVal = -1;

            return returnVal;
        }

        public string Name { get; } = "By Date";

        public string Description { get; } = "Sorts items in the inventory tree according to date, starting with the newest.";

        #endregion
    }
}
