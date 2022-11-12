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
using System.Drawing;
using System.Windows.Forms;
using OpenMetaverse;
using System.Threading;
using System.Globalization;
using System.Linq;


namespace MEGAbolt
{
    public partial class WornAttachments : Form
    {
        private MEGAboltInstance instance;
        private GridClient client;
        private Avatar av = null;
        private Dictionary<uint, AttachmentsListItem> listItems = new Dictionary<uint, AttachmentsListItem>();
        //private Dictionary<uint, AttachmentsListItem> groupItems = new Dictionary<uint, AttachmentsListItem>();

        public WornAttachments(MEGAboltInstance instance, Avatar av)
        {
            InitializeComponent();

            Disposed += WornAttachments_Disposed;

            this.instance = instance;
            client = this.instance.Client;
            this.av = av;

            client.Network.SimChanged += SIM_OnSimChanged;
            //client.Self.TeleportProgress += new EventHandler<TeleportEventArgs>(Self_TeleportProgress);
        }

        private void SIM_OnSimChanged(object sender, SimChangedEventArgs e)
        {
            if (!IsHandleCreated) return;

            lock (listItems)
            {
                listItems.Clear();
            }

            BeginInvoke(new MethodInvoker(() =>
            {
                pBar3.Visible = true;
                lbxPrims.Items.Clear();
                lbxPrimGroup.Items.Clear();

                ThreadPool.QueueUserWorkItem(delegate (object sync)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    Thread.Sleep(5000);
                    ReLoadItems();
                    //GetAttachments();
                    Cursor.Current = Cursors.Default;
                });
            }));
        }

        //private void Self_TeleportProgress(object sender, TeleportEventArgs e)
        //{
        //    if (!this.IsHandleCreated) return;

        //    switch (e.Status)
        //    {
        //        case TeleportStatus.Start:
        //        case TeleportStatus.Progress:
        //        case TeleportStatus.Failed:
        //        case TeleportStatus.Cancelled:
        //            return;

        //        case TeleportStatus.Finished:
        //            WorkPool.QueueUserWorkItem(delegate(object sync)
        //            {
        //                Cursor.Current = Cursors.WaitCursor;
        //                Thread.Sleep(6000);
        //                ReLoadItems();
        //                //GetAttachments();
        //                Cursor.Current = Cursors.Default;
        //            });

        //            return;
        //    }
        //}

        private void ReLoadItems()
        {
            try
            {
                Avatar sav = client.Network.CurrentSim.ObjectsAvatars.Find(fa => fa.ID == av.ID);

                if (sav != null)
                {
                    List<Primitive> prims = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                            prim =>
                            {
                                try
                                {
                                    return (prim.ParentID == sav.LocalID);
                                }
                                catch { return false; }
                            });

                    BeginInvoke(new MethodInvoker(() =>
                    {
                        lbxPrims.BeginUpdate();
                        lbxPrims.Items.Clear();

                        foreach (Primitive prim in prims)
                        {
                            try
                            {
                                AttachmentsListItem item = new AttachmentsListItem(prim, client, lbxPrims);

                                if (!listItems.ContainsKey(prim.LocalID))
                                {
                                    listItems.Add(prim.LocalID, item);

                                    item.PropertiesReceived += item_PropertiesReceived;
                                    item.RequestProperties();
                                }
                            }
                            catch
                            {
                                ;
                            }
                        }

                        lbxPrims.EndUpdate();
                        lbxPrims.Visible = true;

                        pBar3.Visible = false;
                    }));
                }
                else
                {
                    //this.Close();
                    Dispose();
                }
            }
            catch { ; }
        }

        private void WornAssets_Load(object sender, EventArgs e)
        {
            CenterToParent();

            GetAttachments();

            client.Objects.ObjectUpdate += Objects_OnNewPrim;
            client.Objects.KillObject += Objects_OnObjectKilled;
        }

        private void Objects_OnNewPrim(object sender, PrimEventArgs e)
        {
            //if (!this.IsHandleCreated) return;

            if (e.Simulator.Handle != client.Network.CurrentSim.Handle || e.Prim is Avatar) return;

            //if (e.Prim.ParentID != 0) return;

            if (!e.Prim.IsAttachment) return;

            if (e.Prim.ParentID != av.LocalID) return;

            lock (listItems)
            {
                listItems.Clear();
            }

            BeginInvoke(new MethodInvoker(() =>
            {
                pBar3.Visible = true;
                lbxPrims.Items.Clear();
                lbxPrimGroup.Items.Clear();

                ReLoadItems();
            }));
        }

        private void Objects_OnObjectKilled(object sender, KillObjectEventArgs e)
        {
            if (e.Simulator.Handle != client.Network.CurrentSim.Handle) return;

            //if (!e.Prim.IsAttachment) return;
            
            //if (e.Prim.ParentID != av.LocalID) return;

            if (listItems.ContainsKey(e.ObjectLocalID))
            {
                lock (listItems)
                {
                    listItems.Clear();
                }

                BeginInvoke(new MethodInvoker(() =>
                {
                    pBar3.Visible = true;
                    lbxPrims.Items.Clear();
                    lbxPrimGroup.Items.Clear();

                    ReLoadItems();
                }));
            }
        }

        private void GetAttachments()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(GetAttachments));

                return;
            }

            List<Primitive> prims = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                prim =>
                {
                    try
                    {
                        return (prim.ParentID == av.LocalID);
                    }
                    catch { return false; }
                });

            lbxPrims.BeginUpdate();
            lbxPrims.Items.Clear();

            foreach (Primitive prim in prims)
            {
                try
                {
                    AttachmentsListItem item = new AttachmentsListItem(prim, client, lbxPrims);

                    if (!listItems.ContainsKey(prim.LocalID))
                    {
                        listItems.Add(prim.LocalID, item);

                        item.PropertiesReceived += item_PropertiesReceived;
                        item.RequestProperties();
                    }
                }
                catch
                {
                    ;
                }
            }

            lbxPrims.EndUpdate();
            lbxPrims.Visible = true;
        }

        private void lbxPrims_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0) return;

            AttachmentsListItem itemToDraw = (AttachmentsListItem)lbxPrims.Items[e.Index];

            Brush textBrush = null;
            //float fsize = 12.0f;
            Font boldFont = new Font(e.Font, FontStyle.Bold);
            Font regularFont = new Font(e.Font, FontStyle.Regular);

            textBrush = (e.State & DrawItemState.Selected) == DrawItemState.Selected 
                ? new SolidBrush(Color.FromKnownColor(KnownColor.HighlightText)) 
                : new SolidBrush(Color.FromKnownColor(KnownColor.ControlText));

            string name = string.Empty;
            string wornat = string.Empty;
            string stas = string.Empty;

            try
            {
                if (itemToDraw.Prim.Properties == null)
                //if (itemToDraw.Prim == null)
                {
                    name = "...";
                    wornat = "...";
                }
                else
                {
                    name = itemToDraw.Prim.Properties.Name;
                    wornat = "worn on: " + itemToDraw.Prim.PrimData.AttachmentPoint;

                    stas = (itemToDraw.Prim.Flags & PrimFlags.Touch) == PrimFlags.Touch ? " (Touch)" : string.Empty;
                }
            }
            catch (Exception ex)
            {
                name = "...";
                wornat = "...";
                Logger.Log(ex.Message, Helpers.LogLevel.Debug, ex);
            }

            SizeF nameSize = e.Graphics.MeasureString(name, boldFont);
            float nameX = e.Bounds.Left + 4;
            float nameY = e.Bounds.Top + 2;

            e.Graphics.DrawString(name, boldFont, textBrush, nameX, nameY);
            e.Graphics.DrawString(wornat, regularFont, textBrush, nameX + nameSize.Width + 8, nameY);

            SizeF nameSize1 = e.Graphics.MeasureString(wornat, regularFont);

            e.Graphics.DrawString(stas, boldFont, textBrush, nameX + nameSize.Width + nameSize1.Width + 4, nameY);

            e.DrawFocusRectangle();

            boldFont.Dispose();
            regularFont.Dispose();
            textBrush.Dispose();
            boldFont = null;
            regularFont = null;
            textBrush = null;
        }

        private void item_PropertiesReceived(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                AttachmentsListItem item = (AttachmentsListItem)sender;

                if (listItems.ContainsKey(item.Prim.LocalID))
                {
                    lbxPrims.BeginUpdate();
                    lbxPrims.Items.Add(item);
                    lbxPrims.EndUpdate();

                    label1.Text = "Ttl: " + lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " attachments";
                }
            }));
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
            //this.Dispose();
        }

        private void btnTouch_Click(object sender, EventArgs e)
        {
            try
            {
                int iGx = lbxPrimGroup.SelectedIndex;

                if (iGx == -1)
                {
                    int iDx = lbxPrims.SelectedIndex;

                    if (iDx != -1)
                    {
                        AttachmentsListItem item = (AttachmentsListItem)lbxPrims.Items[iDx];

                        if (item == null) return;

                        client.Self.Touch(item.Prim.LocalID);
                        label4.Text = "Touched " + item.Prim.Properties.Name;
                    }
                    else
                    {
                        MessageBox.Show("You must select an attachment first", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                else
                {
                    AttachmentsListItem gitem = (AttachmentsListItem)lbxPrimGroup.Items[iGx];
                    client.Self.Touch(gitem.Prim.LocalID);
                    label4.Text = "Touched " + gitem.Prim.Properties.Name;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Worn Attachments: " + ex.Message, Helpers.LogLevel.Error);
            }
        }

        private void lbxPrimGroup_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0) return;

            AttachmentsListItem itemToDraw = (AttachmentsListItem)lbxPrimGroup.Items[e.Index];

            Brush textBrush = null;
            //float fsize = 12.0f;
            Font boldFont = new Font(e.Font, FontStyle.Bold);
            Font regularFont = new Font(e.Font, FontStyle.Regular);

            textBrush = (e.State & DrawItemState.Selected) == DrawItemState.Selected 
                ? new SolidBrush(Color.FromKnownColor(KnownColor.HighlightText)) 
                : new SolidBrush(Color.FromKnownColor(KnownColor.ControlText));

            string name = string.Empty;
            string wornat = string.Empty;

            try
            {
                if (itemToDraw.Prim.Properties == null)
                {
                    name = "...";
                    wornat = string.Empty;
                }
                else
                {
                    name = itemToDraw.Prim.Properties.Name;

                    wornat = (itemToDraw.Prim.Flags & PrimFlags.Touch) == PrimFlags.Touch ? "(Touch)" : string.Empty;
                }
            }
            catch
            {
                name = "...";
                wornat = "...";
                //Logger.Log(ex.Message, Helpers.LogLevel.Debug, ex);
            }

            SizeF nameSize = e.Graphics.MeasureString(name, boldFont);
            float nameX = e.Bounds.Left + 4;
            float nameY = e.Bounds.Top + 2;

            e.Graphics.DrawString(name, boldFont, textBrush, nameX, nameY);
            e.Graphics.DrawString(wornat, regularFont, textBrush, nameX + nameSize.Width + 8, nameY);

            e.DrawFocusRectangle();

            boldFont.Dispose();
            regularFont.Dispose();
            textBrush.Dispose();
            boldFont = null;
            regularFont = null;
            textBrush = null;
        }

        private void lbxPrims_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtUUID.Text = string.Empty;
            txtPoint.Text = string.Empty;  
   
            label4.Text = string.Empty;

            lbxPrimGroup.BeginUpdate();

            int iDx = lbxPrims.SelectedIndex;
            lbxPrimGroup.Items.Clear();
            label2.Text = string.Empty;

            if (iDx < 0)
            {
                btnTouch.Enabled = false;
                button2.Enabled = false;

                if (av.ID == client.Self.AgentID) button1.Enabled = false; 
                return;
            }
            else
            {
                button2.Enabled = true;
                if (av.ID == client.Self.AgentID) button1.Enabled = true; 
            }

            AttachmentsListItem item = (AttachmentsListItem)lbxPrims.Items[iDx];

            btnTouch.Enabled = (item.Prim.Flags & PrimFlags.Touch) == PrimFlags.Touch;

            txtUUID.Text = item.Prim.ID.ToString();
            txtPoint.Text = item.Prim.PrimData.AttachmentPoint.ToString(); 

            List<Primitive> group = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                prim => (prim.ParentID == item.Prim.LocalID));

            label5.Text = item.Prim.Text;

            foreach (Primitive gprim in group)
            {
                try
                {
                    AttachmentsListItem gitem = new AttachmentsListItem(gprim, client, lbxPrimGroup);

                    gitem.PropertiesReceived += gitem_PropertiesReceived;
                    gitem.RequestProperties();
                }
                catch
                {
                    ;
                }
            }

            lbxPrimGroup.EndUpdate();
        }

        private void gitem_PropertiesReceived(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                AttachmentsListItem item = (AttachmentsListItem)sender;

                lbxPrimGroup.Items.Add(item);

                label2.Text = "Ttl: " + lbxPrimGroup.Items.Count.ToString(CultureInfo.CurrentCulture) + " linked objects";
            }));
        }

        private void lbxPrimGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            label4.Text = string.Empty;

            int iDx = lbxPrimGroup.SelectedIndex;

            if (iDx < 0)
            {
                btnTouch.Enabled = false;
                button2.Enabled = false;
                return;
            }
            else
            {
                button2.Enabled = true;
            }

            AttachmentsListItem item = (AttachmentsListItem)lbxPrimGroup.Items[iDx];

            btnTouch.Enabled = (item.Prim.Flags & PrimFlags.Touch) == PrimFlags.Touch;

            label5.Text = item.Prim.Text;
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;
            AttachmentsListItem item = (AttachmentsListItem)lbxPrims.Items[iDx];

            if (item == null) return;
            (new MEGA3D(instance, item.Prim.LocalID, item.Prim)).Show();

            //using (META3D frm = new META3D(instance, item.Prim.LocalID, item.Prim))
            //{
            //    frm.Show(); 
            //}
        }

        private void WornAttachments_FormClosing(object sender, FormClosingEventArgs e)
        {
            //client.Network.SimChanged -= new EventHandler<SimChangedEventArgs>(SIM_OnSimChanged);
            //client.Self.TeleportProgress -= new EventHandler<TeleportEventArgs>(Self_TeleportProgress);

            listItems.Clear();
            lbxPrims.Items.Clear();
            lbxPrimGroup.Items.Clear();
        }

        void WornAttachments_Disposed(object sender, EventArgs e)
        {
            client.Network.SimChanged -= SIM_OnSimChanged;
            //client.Self.TeleportProgress -= new EventHandler<TeleportEventArgs>(Self_TeleportProgress);

            //lock (listItems)
            //{
            //    listItems.Clear();
            //}

            //GC.Collect();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;

            if (iDx != -1)
            {
                AttachmentsListItem item = (AttachmentsListItem)lbxPrims.Items[iDx];

                UUID itmid = GetItemID(item.Prim);

                if (itmid == UUID.Zero) return;

                InventoryItem attid = client.Inventory.Store[itmid] as InventoryItem;

                //client.Appearance.Detach(attid);

                //List<UUID> remclothing = new List<UUID>();
                //remclothing.Add(attid.UUID);

                List<InventoryBase> contents = client.Inventory.Store.GetContents(instance.CoF.UUID);

                foreach (var ritem in contents.Cast<InventoryItem>().Where(ritem => ritem.AssetUUID == attid.UUID))
                {
                    client.Inventory.RemoveItem(ritem.UUID);
                }
                
                client.Appearance.Detach(attid);

                lock (listItems)
                {
                    listItems.Clear();
                }

                pBar3.Visible = true;
                lbxPrims.Items.Clear();
                lbxPrimGroup.Items.Clear();

                ThreadPool.QueueUserWorkItem(delegate
                {
                    Cursor.Current = Cursors.WaitCursor;
                    Thread.Sleep(2000);
                    ReLoadItems();
                    //GetAttachments();
                    Cursor.Current = Cursors.Default;
                });
            }
        }

        private static UUID GetItemID(Primitive att)
        {
            if (att.NameValues == null) return UUID.Zero;

            for (int i = 0; i < att.NameValues.Length; i++)
            {
                if (att.NameValues[i].Name == "AttachItemID")
                {
                    return (UUID)att.NameValues[i].Value.ToString();
                }
            }
            return UUID.Zero;
        }

        private void WornAttachments_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            client.Objects.ObjectUpdate -= Objects_OnNewPrim;
            client.Objects.KillObject -= Objects_OnObjectKilled;
        }
    }
}
