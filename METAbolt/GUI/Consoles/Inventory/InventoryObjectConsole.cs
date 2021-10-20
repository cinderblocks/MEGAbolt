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

namespace METAbolt
{
    public partial class InventoryObjectConsole : UserControl
    {
        private METAboltInstance instance;
        //private SLNetCom netcom;
        private GridClient client;
        private InventoryItem item;

        public InventoryObjectConsole(METAboltInstance instance, InventoryItem item)
        {
            InitializeComponent();

            this.instance = instance;
            //netcom = this.instance.Netcom;
            client = this.instance.Client;
            this.item = item; 
        }

        private void btnRezObject_Click(object sender, EventArgs e)
        {
            //Vector3 forward = new Vector3(1, 0, 0);
            ////Vector3 offset = Vector3.Norm(target - myPos);

            try
            {
                Vector3 rezpos = new Vector3(1, 0, 0);
                rezpos = (instance.SIMsittingPos() + rezpos);

                client.Inventory.RequestRezFromInventory(
                    client.Network.CurrentSim, Quaternion.Identity, rezpos, (InventoryObject)item);
            }
            catch
            {
                //string err = ex.Message;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Primitive targetPrim = client.Network.CurrentSim.ObjectsPrimitives.Find(
                    delegate(Primitive prim)
                    {
                        return prim.ID == item.UUID;
                    }
                );

            if (targetPrim != null)
            {
                client.Self.Touch(targetPrim.LocalID);
            }
        }
    }
}
