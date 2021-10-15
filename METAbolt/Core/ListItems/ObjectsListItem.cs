//  Copyright (c) 2008 - 2014, www.metabolt.net (METAbolt)
//  Copyright (c) 2006-2008, Paul Clement (a.k.a. Delta)
//  All rights reserved.

//  Redistribution and use in source and binary forms, with or without modification, 
//  are permitted provided that the following conditions are met:

//  * Redistributions of source code must retain the above copyright notice, 
//    this list of conditions and the following disclaimer. 
//  * Redistributions in binary form must reproduce the above copyright notice, 
//    this list of conditions and the following disclaimer in the documentation 
//    and/or other materials provided with the distribution. 

//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND 
//  ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED 
//  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
//  IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, 
//  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT 
//  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR 
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
//  WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
//  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
//  POSSIBILITY OF SUCH DAMAGE.


using System;
using System.Windows.Forms;
using OpenMetaverse;

namespace METAbolt
{
    public class ObjectsListItem
    {
        private GridClient client;
        private ListBox listBox;

        public ObjectsListItem(Primitive prim, GridClient client, ListBox listBox)
        {
            this.Prim = prim;
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
            if (PropertiesReceived != null) PropertiesReceived(this, e);
        }

        public Primitive Prim { get; } = new Primitive();

        public bool GotProperties { get; private set; } = false;

        public bool GettingProperties { get; private set; } = false;
    }
}
