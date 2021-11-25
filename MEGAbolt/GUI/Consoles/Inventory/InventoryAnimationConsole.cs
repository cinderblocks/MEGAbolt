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
//using System.Linq;
using System.Windows.Forms;
//using MEGAbolt.NetworkComm;
using OpenMetaverse;

namespace MEGAbolt
{
    public partial class InventoryAnimationConsole : UserControl
    {
        private MEGAboltInstance instance;
        //private SLNetCom netcom;
        private GridClient client;
        private InventoryItem item;

        public InventoryAnimationConsole(MEGAboltInstance instance, InventoryItem item)
        {
            InitializeComponent();

            this.instance = instance;
            //netcom = this.instance.Netcom;
            client = this.instance.Client;
            this.item = item;

            Disposed += InventoryAnimation_Disposed;
        }

        private void InventoryAnimation_Disposed(object sender, EventArgs e)
        {
            
        }

        private void AddClientEvents()
        {
            
        }

        private void btnAnimate_Click(object sender, EventArgs e)
        {
            UUID AnimationID = new UUID(item.AssetUUID.ToString());

            Dictionary<UUID, bool> bAnim = new Dictionary<UUID, bool>();
            bAnim.Add(AnimationID, true);
            client.Self.Animate(bAnim, true);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            UUID AnimationID = new UUID(item.AssetUUID.ToString());

            Dictionary<UUID, bool> bAnim = new Dictionary<UUID, bool>();
            bAnim.Add(AnimationID, false);

            client.Self.Animate(bAnim, true);      
        }

        private void InventoryAnimationConsole_Load(object sender, EventArgs e)
        {

        }
    }
}
