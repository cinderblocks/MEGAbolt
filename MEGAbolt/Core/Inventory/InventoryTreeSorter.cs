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
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using OpenMetaverse;

namespace MEGAbolt
{
    public class InventoryTreeSorter : IComparer
    {
        private SafeDictionary<string, ITreeSortMethod> sortMethods = new SafeDictionary<string, ITreeSortMethod>();
        private string currentMethodName;
        private ITreeSortMethod currentMethod;

        public InventoryTreeSorter()
        {
            RegisterSortMethods();
            
            //because the Values property is gay and doesn't have an indexer
            foreach (ITreeSortMethod method in sortMethods.Values)
            {
                currentMethodName = method.Name;
                break;
            }
        }

        private void RegisterSortMethods()
        {
            AddSortMethod(new DateTreeSort());
            AddSortMethod(new NameTreeSort());
        }

        private void AddSortMethod(ITreeSortMethod sort)
        {
            if (!sortMethods.ContainsKey(sort.Name))
            {
            sortMethods.Add(sort.Name, sort);
            }
        }

        public List<ITreeSortMethod> GetSortMethods()
        {
            if (sortMethods.Values.Count == 0) return null;

            List<ITreeSortMethod> methods = new List<ITreeSortMethod>();

            foreach (ITreeSortMethod method in sortMethods.Values)
                methods.Add(method);

            return methods;
        }

        public string CurrentSortName
        {
            get => currentMethodName;
            set
            {
                if (!sortMethods.ContainsKey(value))
                    throw new Exception("The specified sort method does not exist.");

                currentMethodName = value;
                currentMethod = sortMethods[currentMethodName];
            }
        }

        #region IComparer Members

        public int Compare(object x, object y)
        {
            TreeNode nodeX = (TreeNode)x;
            TreeNode nodeY = (TreeNode)y;

            try
            {
                InventoryBase ibX = (InventoryBase)nodeX.Tag;
                InventoryBase ibY = (InventoryBase)nodeY.Tag;

                if (currentMethod == null)
                {
                    currentMethod = sortMethods[currentMethodName];
                }

                try
                {
                    return currentMethod.CompareNodes(ibX, ibY, nodeX, nodeY);
                }
                catch (Exception ex)
                {
                    Logger.Log("Inventory error", Helpers.LogLevel.Error, ex);
                    return 0;
                }
            }
            catch 
            {
                return 0;
            }
        }

        #endregion
    }
}
