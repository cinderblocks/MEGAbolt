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
    public class AttachmentsListItem
    {
        private GridClient client;
        private ListBox listBox;

        public AttachmentsListItem(Primitive prim, GridClient client, ListBox listBox)
        {
            Prim = prim;
            this.client = client;
            this.listBox = listBox;

            client.Objects.ObjectProperties += Objects_OnObjectProperties;
        }

        public void RequestProperties()
        {
            try
            {
                if (Prim.Properties == null)
                {
                    GettingProperties = true;
                    //client.Objects.SelectObject(client.Network.CurrentSim, prim.LocalID);
                    client.Objects.SelectObject(client.Network.CurrentSim, Prim.LocalID, true);
                    //client.Objects.RequestObject(client.Network.CurrentSim, prim.LocalID);
                }
                else
                {
                    GotProperties = true;
                    OnPropertiesReceived(EventArgs.Empty);
                }
            }
            catch
            {
                ;
            }
        }

        private void Objects_OnObjectProperties(object sender, ObjectPropertiesEventArgs e)
        {
            if (e.Properties.ObjectID != Prim.ID) return;

            try
            {
                GettingProperties = false;
                GotProperties = true;
                Prim.Properties = e.Properties;

                listBox.BeginInvoke(
                    new OnPropReceivedRaise(OnPropertiesReceived),
                    new object[] { EventArgs.Empty });
            }
            catch
            {
                ;
            }
        }

        public override string ToString()
        {
            try
            {
                if (Prim.Properties == null) return "???";
                return (string.IsNullOrEmpty(Prim.Properties.Name) ? "..." : Prim.Properties.Name);
            }
            catch
            {
                return "***";
            }
        }

        public event EventHandler PropertiesReceived;
        private delegate void OnPropReceivedRaise(EventArgs e);
        protected virtual void OnPropertiesReceived(EventArgs e)
        {
            if (PropertiesReceived != null) PropertiesReceived(this, e);
        }

        public Primitive Prim { get; } = new Primitive();

        public bool GotProperties { get; private set; } = false;

        public bool GettingProperties { get; private set; } = false;
    }
}
