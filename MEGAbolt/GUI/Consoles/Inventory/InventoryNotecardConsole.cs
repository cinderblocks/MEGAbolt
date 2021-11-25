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
using System.Windows.Forms;
using OpenMetaverse;

namespace MEGAbolt
{
    public partial class InventoryNotecardConsole : UserControl
    {
        private MEGAboltInstance instance;
        //private SLNetCom netcom;
        //private GridClient client;
        private InventoryItem item;

        public InventoryNotecardConsole(MEGAboltInstance instance, InventoryItem item)
        {
            InitializeComponent();

            this.instance = instance;
            //netcom = this.instance.Netcom;
            //client = this.instance.Client;
            this.item = item;
        }

        private void btnEditNotecard_Click(object sender, EventArgs e)
        {
            if ((item.Permissions.OwnerMask & PermissionMask.Copy) == PermissionMask.Copy)
            {
                if ((item.Permissions.OwnerMask & PermissionMask.Modify) == PermissionMask.Modify)
                {
                    (new frmNotecardEditor(instance, item, false)).Show();
                }
                else
                {
                    (new frmNotecardEditor(instance, item, true)).Show();
                }
            }
            else
            {
                MessageBox.Show("You do not have permissions to view this notecard", "MEGAbolt");  
            }
        }
    }
}
