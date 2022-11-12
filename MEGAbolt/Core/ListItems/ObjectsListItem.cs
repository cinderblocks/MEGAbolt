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
    public class ObjectsListItem
    {
        private GridClient client;
        private ListBox listBox;

        public ObjectsListItem(Primitive prim, GridClient client, ListBox listBox)
        {
            Prim = prim;
            this.client = client;
            this.listBox = listBox;
        }

        public void RequestProperties()
        {
            try
            {
                    //if (string.IsNullOrEmpty(prim.Properties.Name))
                    //if (prim.Properties == null) // Rollback ).9.2.1
                        if (Prim.Properties == null)   // || string.IsNullOrEmpty(prim.Properties.Name)) //GM changed it to BOTH!
                        {
                            GettingProperties = true;
                            client.Objects.ObjectPropertiesFamily += Objects_OnObjectPropertiesFamily;
                            client.Objects.RequestObjectPropertiesFamily(client.Network.CurrentSim, Prim.ID);
                        }
                        else
                        {
                            GotProperties = true;
                            OnPropertiesReceived(EventArgs.Empty);
                        }

                        //gettingProperties = true;
                        //client.Objects.ObjectPropertiesFamily += new EventHandler<ObjectPropertiesFamilyEventArgs>(Objects_OnObjectPropertiesFamily);
                        //client.Objects.RequestObjectPropertiesFamily(client.Network.CurrentSim, prim.ID);
            }
            catch
            {
                ;
            }
        }

        public void RequestObjectProperties()
        {
            try
            {
                //if (string.IsNullOrEmpty(prim.Properties.Name))
                //if (prim.Properties == null) // Rollback ).9.2.1
                if (Prim.Properties == null)   // || string.IsNullOrEmpty(prim.Properties.Name)) //GM changed it to BOTH!
                {
                    GettingProperties = true;
                    client.Objects.ObjectProperties += Objects_ObjectProperties;
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

        private void Objects_ObjectProperties(object sender, ObjectPropertiesEventArgs e)
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

            //client.Objects.ObjectProperties -= new EventHandler<ObjectPropertiesEventArgs>(Objects_ObjectProperties);
        }

        private void Objects_OnObjectPropertiesFamily(object sender, ObjectPropertiesFamilyEventArgs e)
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

            //client.Objects.ObjectPropertiesFamily -= new EventHandler<ObjectPropertiesFamilyEventArgs>(Objects_OnObjectPropertiesFamily);
        }

        public override string ToString()
        {
            try
            {
                //return (string.IsNullOrEmpty(prim.Properties.Name) ? "..." : prim.Properties.Name);
                //return (prim.Properties == null ? "..." : prim.Properties.Name); // Rollback ).9.2.1
                //GM changed to BOTH!
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
            PropertiesReceived?.Invoke(this, e);
        }

        public Primitive Prim { get; } = new Primitive();

        public bool GotProperties { get; private set; } = false;

        public bool GettingProperties { get; private set; } = false;
    }
}
