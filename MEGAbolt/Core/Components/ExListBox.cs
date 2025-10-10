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

using OpenMetaverse;
using System;
using System.Windows.Forms;


namespace MEGAbolt.Core.Components
{
    public class ExListBox : ListBox
    {
        public ExListBox() : base()
        {
        }

        public bool SortByName;
        public Vector3 location = new Vector3();

        protected override void Sort()
        {
            if (Items.Count > 1)
            {
                bool swapped;
                do
                {
                    var counter = Items.Count - 1;
                    swapped = false;

                    while (counter > 0)
                    {
                        var item1 = Items[counter].ToString();
                        var item2 = Items[counter - 1].ToString();

                        if (SortByName)
                        {
                            if (string.Compare(item1, item2, StringComparison.Ordinal) == -1)
                            {
                                (Items[counter], Items[counter - 1]) = (Items[counter - 1], Items[counter]);
                                swapped = true;
                            }
                        }
                        else
                        {
                            var itemp1 = (ObjectsListItem)Items[counter];
                            var pos1 = new Vector3();
                            pos1 = itemp1.Prim.Position;
                            var dist1 = Math.Round(Vector3.Distance(location, pos1), MidpointRounding.ToEven);

                            var itemp2 = (ObjectsListItem)Items[counter - 1];
                            var pos2 = new Vector3();
                            pos2 = itemp2.Prim.Position;
                            var dist2 = Math.Round(Vector3.Distance(location, pos2), MidpointRounding.ToEven);

                            if (dist1 < dist2)
                            {
                                (Items[counter], Items[counter - 1]) = (Items[counter - 1], Items[counter]);
                                swapped = true;
                            }
                        }

                        counter -= 1;
                    }
                }
                while ((swapped == true));
            }
        }

        public void SortList()
        {
            Sort();
        }
    }
}
