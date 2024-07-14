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
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using OpenMetaverse;
using MEGAbolt.NetworkComm;
using MEGAbolt.Controls;
using System.Threading;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BugSplatDotNetStandard;


namespace MEGAbolt
{
    public partial class frmObjects : Form
    {
        private MEGAboltInstance instance;
        private GridClient client;
        private MEGAboltNetcom netcom;
        
        public AgentFlags Flags = AgentFlags.None;
        public AgentState State = AgentState.None;
        //private int duplicateCount;
        private bool sloading;
        private float range = 20;
        private float newrange = 20;

        private SafeDictionary<uint, ObjectsListItem> listItems = new SafeDictionary<uint, ObjectsListItem>();
        private SafeDictionary<uint, ObjectsListItem> ItemsProps = new SafeDictionary<uint, ObjectsListItem>();
        private SafeDictionary<uint, ObjectsListItem> childItems = new SafeDictionary<uint, ObjectsListItem>();
        private List<uint> objs = new List<uint>();
        //private SafeDictionary<UUID, string> avatars = new SafeDictionary<UUID, string>();

        private Popup toolTip;
        private Popup toolTip1;
        private CustomToolTip customToolTip;
        private bool eventsremoved = false;
        //private System.Timers.Timer sittimer;
        private bool txtDescChanged = false;
        private bool txtNameChanged = false;


        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                if (!String.IsNullOrEmpty(Generated.BugsplatDatabase))
                {
                    BugSplat crashReporter = new BugSplat(Generated.BugsplatDatabase, "MEGAbolt",
                        Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                    {
                        User = Generated.BugsplatUser,
                        ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                    };
                    crashReporter.Post(e.Exception);
                }
            }
        }

        public frmObjects(MEGAboltInstance instance)
        {
            InitializeComponent();

            Disposed += Objects_Disposed;
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            client = this.instance.Client;
            netcom = this.instance.Netcom;

            range = instance.Config.CurrentConfig.ObjectRange;
            //newrange = range;
            //numericUpDown1.Maximum = instance.Config.CurrentConfig.RadarRange;
            numericUpDown1.Value = Convert.ToDecimal(range);

            string msg1 = "Click for online help on how to use the Object Manager";
            toolTip1 = new Popup(customToolTip = new CustomToolTip(instance, msg1));
            toolTip1.AutoClose = false;
            toolTip1.FocusOnOpen = false;
            toolTip1.ShowingAnimation = toolTip1.HidingAnimation = PopupAnimations.Blend;

            client.Network.Disconnected += Network_OnDisconnected;
            client.Avatars.UUIDNameReply += Avatars_OnAvatarNames;
            netcom.ClientLoggedOut += netcom_ClientLoggedOut;
            netcom.ClientDisconnected += netcom_ClientDisconnected;
            client.Self.AvatarSitResponse += Self_AvatarSitResponse;
            client.Network.SimChanged += SIM_OnSimChanged;
            //client.Self.TeleportProgress += new EventHandler<TeleportEventArgs>(Self_TeleportProgress);
        }

        // separate thread
        private void Avatars_OnAvatarNames(object sender, UUIDNameReplyEventArgs names)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                OwnerReceived(sender, names);
            }));
        }

        //runs on the GUI thread
        private void OwnerReceived(object sender, UUIDNameReplyEventArgs names)
        {
            int iDx = lbxPrims.SelectedIndex;

            if (iDx == -1) return; 

            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];
            Primitive sPr = new Primitive();
            sPr = item.Prim;

            foreach (KeyValuePair<UUID, string> av in names.Names)
            {
                if (av.Key == sPr.Properties.OwnerID)
                {
                    labelOwnerName.Text = av.Value;
                    pictureBox1.Enabled = true;
                    pictureBox1.Cursor = Cursors.Hand;
                }

                if (av.Key == sPr.Properties.CreatorID)
                {
                    txtCreator.Text = av.Value;
                    pictureBox2.Enabled = true;
                    pictureBox2.Cursor = Cursors.Hand;
                    label21.Text = sPr.Properties.CreatorID.ToString(); 
                }

                if (!instance.avnames.ContainsKey(av.Key))
                {
                    instance.avnames.Add(av.Key, av.Value);
                }
            }
        }

        private void AddObjectEvents()
        {
            client.Objects.ObjectUpdate += Objects_OnNewPrim;
            client.Objects.KillObject += Objects_OnObjectKilled;
            eventsremoved = false;
        }

        private void RemoveNetcomEvents()
        {
            client.Objects.ObjectUpdate -= Objects_OnNewPrim;
            client.Objects.KillObject -= Objects_OnObjectKilled;
            client.Avatars.UUIDNameReply -= Avatars_OnAvatarNames;
            client.Network.Disconnected -= Network_OnDisconnected;
            netcom.ClientLoggedOut -= netcom_ClientLoggedOut;
            netcom.ClientDisconnected -= netcom_ClientDisconnected;
            client.Network.SimChanged -= SIM_OnSimChanged;
            //client.Self.TeleportProgress -= new EventHandler<TeleportEventArgs>(Self_TeleportProgress);
        }

        private void RemoveObjectEvents()
        {
            client.Objects.ObjectUpdate -= Objects_OnNewPrim;
            client.Objects.KillObject -= Objects_OnObjectKilled;
            eventsremoved = true;
        }

        private void netcom_ClientLoggedOut(object sender, EventArgs e)
        {
            try
            {
                RemoveNetcomEvents();
                Dispose();
            }
            catch
            {
                // do nothing
            }
        }

        private void netcom_ClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            try
            {
                RemoveNetcomEvents();
                Dispose();
            }
            catch
            {
                // do nothing
            }
        }

        private void SIM_OnSimChanged(object sender, SimChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    SIM_OnSimChanged(sender, e);
                }));
            }

            if (!IsHandleCreated) return;

            BeginInvoke(new MethodInvoker(() =>
            {
                ClearLists();
                ResetFields();
                lbxPrims.Items.Clear();

                //AddAllObjects();

                ////ResetObjects();
                //lbxPrims.Items.Clear();
                //lbxChildren.Items.Clear();
                //lbxTask.Items.Clear();
                //DisplayObjects();
                //button3.Visible = button7.Visible = false;
            }));
        }

        //private void Self_TeleportProgress(object sender, TeleportEventArgs e)
        //{
        //    if (e.Status == TeleportStatus.Finished)
        //    {
        //        ClearLists();
        //        ResetFields();
        //        lbxPrims.Items.Clear();
        //    }
        //}

        private void ClearLists()
        {
            listItems.Clear();
            ItemsProps.Clear();
            childItems.Clear();
            objs.Clear();
            //avatars.Clear();
        }

        private void Network_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            ClearLists();  
        }

        private void lbxPrims_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0) return;

            ObjectsListItem itemToDraw = (ObjectsListItem)lbxPrims.Items[e.Index];

            Brush textBrush = null;
            Brush dBrush = null;
            Font boldFont = new Font(e.Font, FontStyle.Bold);
            Font regularFont = new Font(e.Font, FontStyle.Regular);
            Font italicFont = new Font("Arial",7, FontStyle.Italic);

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                //textBrush = new SolidBrush(Color.FromKnownColor(KnownColor.HighlightText));
                textBrush = new SolidBrush(Color.White);
                dBrush = new SolidBrush(Color.Yellow);

                //e = new DrawItemEventArgs(e.Graphics,
                //                  e.Font,
                //                  e.Bounds,
                //                  e.Index,
                //                  e.State ^ DrawItemState.Selected,
                //                  e.ForeColor,
                //                  Color.DimGray);//Choose the color
                //e.DrawBackground();
            }
            else
            {
                textBrush = new SolidBrush(Color.Black);
                dBrush = new SolidBrush(Color.RoyalBlue);
            }

            string name = string.Empty;
            string description = string.Empty;
            string distance = string.Empty;  

            try
            {
                if (itemToDraw.Prim.Properties == null)
                //if (itemToDraw.Prim == null)
                {
                    name = "...";
                    description = "...";
                }
                else
                {
                    Vector3 location = new Vector3(Vector3.Zero); 
                    location = instance.SIMsittingPos();
                    Vector3 pos = new Vector3(Vector3.Zero); 
                    pos = itemToDraw.Prim.Position;
                    double dist = Math.Round(Vector3.Distance(location, pos), MidpointRounding.ToEven);

                    distance = " [" + dist.ToString(CultureInfo.CurrentCulture) + "m]";

                    name = itemToDraw.Prim.Properties.Name;
                    description = itemToDraw.Prim.Properties.Description;
                }
            }
            catch (Exception ex)
            {
                name = "...";
                description = "...";
                Logger.Log(ex.Message, Helpers.LogLevel.Debug, ex);     
            }

            SizeF nameSize = e.Graphics.MeasureString(name, boldFont);
            float nameX = e.Bounds.Left + 4;
            float nameY = e.Bounds.Top + 2;

            SizeF distanceSize = e.Graphics.MeasureString(distance, boldFont);

            e.Graphics.DrawString(name, boldFont, textBrush, nameX, nameY);
            //e.Graphics.DrawString(description, regularFont, textBrush, nameX + nameSize.Width + 5, nameY);
            e.Graphics.DrawString(distance, regularFont, dBrush, new PointF(nameX, nameY + nameSize.Height));
            //e.Graphics.DrawString(description, italicFont, textBrush, nameX + distanceSize.Width + 5, nameY);
            e.Graphics.DrawString(description, italicFont, textBrush, new PointF(nameX + distanceSize.Width + 5, nameY + nameSize.Height + 2));

            e.Graphics.DrawLine(new Pen(Color.FromArgb(200, 200, 200)), new Point(e.Bounds.Left, e.Bounds.Bottom - 1), new Point(e.Bounds.Right, e.Bounds.Bottom - 1));
            e.DrawFocusRectangle();

            boldFont.Dispose();
            regularFont.Dispose();
            italicFont.Dispose(); 
            textBrush.Dispose();
            dBrush.Dispose(); 
            boldFont = null;
            regularFont = null;
            textBrush = null;

            lbxPrims.ItemHeight = 35;
        }

        //Separate thread
        private void Objects_OnNewPrim(object sender, PrimEventArgs e)
        {
            //if (!this.IsHandleCreated) return;

            if (e.Simulator.Handle != client.Network.CurrentSim.Handle || e.Prim.Position == Vector3.Zero || e.Prim is Avatar) return;

            try
            {
                if (e.Prim.ParentID != 0)
                {
                    lock (childItems)
                    {
                        ObjectsListItem citem = new ObjectsListItem(e.Prim, client, lbxChildren);

                        if (!childItems.ContainsKey(e.Prim.LocalID))
                        {
                            try
                            {
                                childItems.Add(e.Prim.LocalID, citem);
                            }
                            catch
                            {
                                ;
                            }
                        }
                    }
                }
                else
                {
                    BeginInvoke(new MethodInvoker(() =>
                    {
                        lock (listItems)
                        {
                            ObjectsListItem item = new ObjectsListItem(e.Prim, client, lbxPrims);

                            Vector3 location = new Vector3(Vector3.Zero);
                            location = instance.SIMsittingPos();
                            Vector3 pos = new Vector3(Vector3.Zero);
                            pos = item.Prim.Position;

                            float dist = Vector3.Distance(location, pos);

                            if (dist < range)
                            {
                                try
                                {
                                    if (!listItems.ContainsKey(e.Prim.LocalID))
                                    {
                                        listItems.Add(e.Prim.LocalID, item);

                                        item.PropertiesReceived += iitem_PropertiesReceived;
                                        item.RequestProperties();
                                    }
                                    //else
                                    //{
                                    //    listItems.Remove(e.Prim.LocalID);
                                    //    listItems.Add(e.Prim.LocalID, item);

                                    //    lock (lbxPrims.Items)
                                    //    {
                                    //        lbxPrims.BeginUpdate();

                                    //        if (lbxPrims.Items.Contains(item))
                                    //        {
                                    //            lbxPrims.Items.Remove(item);
                                    //        }

                                    //        lbxPrims.Items.Add(item);
                                    //        lbxPrims.EndUpdate();
                                    //    }

                                    //    lbxPrims.SortList();
                                    //}
                                }
                                catch
                                {
                                    ;
                                }

                                //BeginInvoke(new MethodInvoker(delegate()
                                //{
                                //    pB1.Maximum += 1;
                                //}));
                            }
                        }
                    }));
                }
            }
            catch
            {
                ;
            }
        }

        //Separate thread
        private void Objects_OnObjectKilled(object sender, KillObjectEventArgs e)
        {
            //if (!this.IsHandleCreated) return;

            if (e.Simulator.Handle != client.Network.CurrentSim.Handle) return;

            ObjectsListItem item;

            uint objectID = e.ObjectLocalID;

            try
            {
                if (listItems.ContainsKey(objectID))
                {
                    item = listItems[objectID];
                }
                else
                {
                    return;
                }

                if (item.Prim.ParentID != 0)
                {
                    lock (childItems)
                    {
                        if (!childItems.ContainsKey(objectID)) return;

                        try
                        {
                            BeginInvoke(new MethodInvoker(() =>
                            {
                                item = childItems[objectID];

                                if (lbxChildren.Items.Contains(item))
                                {
                                    lbxChildren.Items.Remove(item);
                                }

                                try
                                {
                                    childItems.Remove(objectID);
                                }
                                catch
                                {
                                    ;
                                }
                            }));
                        }
                        catch
                        {
                            ;
                        }
                    }
                }
                else
                {
                    lock (listItems)
                    {
                        if (!listItems.ContainsKey(objectID)) return;

                        try
                        {
                            BeginInvoke(new MethodInvoker(() =>
                            {
                                item = listItems[objectID];

                                if (item != null)
                                {
                                    if (lbxPrims.Items.Contains(item))
                                    {
                                        lock (lbxPrims.Items)
                                        {
                                            lbxPrims.Items.Remove(item);
                                        }
                                    }

                                    if (pB1.Maximum > 0) pB1.Maximum -= 1;

                                    try
                                    {
                                        listItems.Remove(objectID);

                                        lock (ItemsProps)
                                        {
                                            ItemsProps.Remove(objectID);
                                        }
                                    }
                                    catch
                                    {
                                        ;
                                    }

                                    tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
                                }
                            }));
                        }
                        catch
                        {
                            ;
                        }
                    }
                }
            }
            catch
            {
                // passed key wasn't available
                return;
            }
        }

        private void item_PropertiesReceived(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    item_PropertiesReceived(sender, e);

                }));
                return;
            }

            ObjectsListItem item = (ObjectsListItem)sender;

            BeginInvoke(new MethodInvoker(() =>
            {
                Vector3 location = new Vector3(Vector3.Zero);
                location = instance.SIMsittingPos();
                Vector3 pos = new Vector3(Vector3.Zero);
                pos = item.Prim.Position;

                if (Vector3.Distance(location, pos) < range)
                {
                    lock (lbxPrims.Items)
                    {
                        lbxPrims.BeginUpdate();
                        lbxPrims.Items.Remove(item);
                        lbxPrims.Items.Add(item);
                        lbxPrims.EndUpdate();
                    }

                    if (!ItemsProps.ContainsKey(item.Prim.LocalID))
                    {
                        lock (ItemsProps)
                        {
                            ItemsProps.Add(item.Prim.LocalID, item);
                        }
                    }

                    tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
                }

                //if (pB1.Value + 1 <= pB1.Maximum)
                //    pB1.Value += 1;

                //if (pB1.Value >= pB1.Maximum)
                //{
                //    pB1.Value = 0;
                //    pB1.Visible = false;

                //    //lblStatus.Visible = true;
                //    //lbxPrims.SortList();
                //    //lblStatus.Visible = false;
                //}

                ////pB1.Visible = false;

                //lbxPrims.SortList();
            }));

            item.PropertiesReceived -= item_PropertiesReceived;
        }

        private void iitem_PropertiesReceived(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    iitem_PropertiesReceived(sender, e);

                }));
                return;
            }

            ObjectsListItem item = (ObjectsListItem)sender;

            BeginInvoke(new MethodInvoker(() =>
            {
                Vector3 location = new Vector3(Vector3.Zero);
                location = instance.SIMsittingPos();
                Vector3 pos = new Vector3(Vector3.Zero);
                pos = item.Prim.Position;

                if (Vector3.Distance(location, pos) < range)
                {
                    lock (lbxPrims.Items)
                    {
                        lbxPrims.BeginUpdate();
                        lbxPrims.Items.Remove(item);
                        lbxPrims.Items.Add(item);
                        lbxPrims.EndUpdate();
                    }

                    if (!ItemsProps.ContainsKey(item.Prim.LocalID))
                    {
                        lock (ItemsProps)
                        {
                            ItemsProps.Add(item.Prim.LocalID, item);
                        }
                    }

                    tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
                }

                //if (pB1.Value + 1 <= pB1.Maximum)
                //    pB1.Value += 1;

                //if (pB1.Value >= pB1.Maximum)
                //{
                //    pB1.Value = 0;
                //    pB1.Visible = false;
                //    groupBox2.Enabled = true;
                //    groupBox3.Enabled = true;

                //    //lblStatus.Visible = true;
                //    pBar3.Visible = true;
                //    lbxPrims.SortList();
                //    pBar3.Visible = false;
                //    //lblStatus.Visible = false;
                //}

                //pB1.Value = 0;
                pB1.Visible = false;
                groupBox2.Enabled = true;
                groupBox3.Enabled = true;

                //lblStatus.Visible = true;
                pBar3.Visible = true;
                lbxPrims.SortList();
                pBar3.Visible = false;
            }));

            //lbxPrims.SortList();
            item.PropertiesReceived -= iitem_PropertiesReceived;
        }

        private void AddAllObjects()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(AddAllObjects));
            }

            //pB1.Maximum = 0;

            if (eventsremoved) AddObjectEvents();

            Cursor.Current = Cursors.WaitCursor;

            pB1.Visible = true;
            //bool inmem = false;

            lbxPrims.location = instance.SIMsittingPos();
            lbxPrims.SortByName = false;
            //pBar3.Visible = true;
            //lbxPrims.SortList();
            //pBar3.Visible = false;

            //int ocnt = 1;

            try
            {
                lock (listItems)
                {
                    Vector3 location = new Vector3(Vector3.Zero); 
                    location = instance.SIMsittingPos();

                client.Network.CurrentSim.ObjectsPrimitives.ForEach(
                prim =>
                {
                    Vector3 pos = new Vector3(Vector3.Zero);
                    pos = prim.Position;



                    float dist = Vector3.Distance(location, pos);

                    //// Work around for the Magnum problem
                    //if (ocnt < 4)
                    //{
                    //    //instance.State.SetPointing(true, prim.ID);
                    //    //instance.State.SetPointing(false, prim.ID);

                    //    Vector3 target = new Vector3(Vector3.Zero);
                    //    target = prim.Position; // the object to look at

                    //    client.Self.Movement.TurnToward(target);

                    //    ocnt += 1;
                    //}

                    if (((int)dist < (int)range) && (prim.Position != Vector3.Zero))
                    {
                        ObjectsListItem item = new ObjectsListItem(prim, client, lbxPrims);

                        if (prim.ParentID == 0) //root prims only
                        {
                            if (!listItems.ContainsKey(prim.LocalID))
                            {
                                lock (listItems)
                                {
                                    listItems.Add(prim.LocalID, item);

                                    item.PropertiesReceived += iitem_PropertiesReceived;
                                    item.RequestProperties();
                                    //inmem = true;
                                }

                                //pB1.Maximum += 1;
                            }
                            else
                            {
                                lock (listItems)
                                {
                                    listItems.Remove(prim.LocalID);
                                    listItems.Add(prim.LocalID, item);

                                    lock (lbxPrims.Items)
                                    {
                                        lbxPrims.BeginUpdate();
                                        lbxPrims.Items.Remove(item);
                                        lbxPrims.Items.Add(item);
                                        lbxPrims.EndUpdate();
                                    }
                                }
                            }
                        }
                        else
                        {
                            ObjectsListItem citem = new ObjectsListItem(prim, client, lbxChildren);

                            if (!childItems.ContainsKey(prim.LocalID))
                            {
                                lock (childItems)
                                {
                                    childItems.Add(prim.LocalID, citem);
                                }
                            }
                            else
                            {
                                lock (childItems)
                                {
                                    childItems.Remove(prim.LocalID);
                                    childItems.Add(prim.LocalID, citem);
                                }
                            }
                        }
                    }
                });
                }



                //if (!inmem)
                //{
                    //pB1.Value = 0;
                    //pB1.Visible = false;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = true;
                    //lbxPrims.SortList();
                //}

                lblStatus.Visible = false;
                lbxPrims.Visible = true;
                lbxChildren.Visible = true;
                txtSearch.Enabled = true;

                //lbxPrims.SortList();

                pB1.Visible = false;

                //tlbStatus.Text = listItems.Count.ToString() + " objects";
                tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
            }
            catch (Exception ex)
            {
                //string exp = exc.Message;
                instance.CrashReporter?.Post(ex);
            }

            Cursor.Current = Cursors.Default;
        }

        //private void DisplayObjects()
        //{
        //    if (eventsremoved) AddObjectEvents();
 
        //    lbxPrims.Items.Clear();

        //    try
        //    {
        //        Vector3 location = instance.SIMsittingPos();
        //        pBar3.Visible = true;
        //        pB1.Visible = true;
        //        pB1.Value = 0;
        //        pB1.Maximum = ItemsProps.Count;

        //        lock (ItemsProps)
        //        {
        //            //Vector3 location = instance.SIMsittingPos();

        //            foreach (KeyValuePair<uint, ObjectsListItem> entry in ItemsProps)
        //            {
        //                ObjectsListItem item = entry.Value;

        //                if (item.Prim.ParentID == 0) //root prims only
        //                {
        //                    Vector3 pos = item.Prim.Position;

        //                    if (Vector3.Distance(location,pos) < range)
        //                    {
        //                        lock (lbxPrims.Items)
        //                        {
        //                            lbxPrims.BeginUpdate();
        //                            lbxPrims.Items.Add(item);
        //                            lbxPrims.EndUpdate();
        //                        }
        //                    }
        //                }

        //                if (pB1.Value < ItemsProps.Count) pB1.Value += 1;
        //            }
        //        }

        //        pB1.Visible = false;
        //        pBar3.Visible = false;

        //        //lblStatus.Visible = true;
        //        //lbxPrims.SortList();
        //        //lblStatus.Visible = false;

        //        //lbxPrims.Visible = true;
        //        //lbxChildren.Visible = true;
        //        txtSearch.Enabled = true;

        //        //tlbStatus.Text = listItems.Count.ToString() + " objects";
        //        tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
        //    }
        //    catch (Exception ex)
        //    {
        //        //string exp = exc.Message;
        //        reporter.Show(ex);
        //    }
        //}

        private void SearchFor(string text)
        {
            RemoveObjectEvents();

            lbxPrims.Items.Clear();
            pB1.Visible = true;

            string query = text.ToLower(CultureInfo.CurrentCulture);
            //bool inmem = false;

            List<Primitive> results =
                client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                prim =>
                {
                    try
                    {
                        //evil comparison of death!
                        return (prim.ParentID == 0 && prim.Properties != null) &&
                               (prim.Properties.Name.ToLower(CultureInfo.CurrentCulture).Contains(query) ||
                                prim.Properties.Description.ToLower(CultureInfo.CurrentCulture).Contains(query) ||
                                prim.Properties.OwnerID.ToString().ToLower(CultureInfo.CurrentCulture).Contains(query) ||
                                prim.Text.ToLower(CultureInfo.CurrentCulture).Contains(query) ||
                                prim.ID.ToString().ToLower(CultureInfo.CurrentCulture).Contains(query) ||
                                prim.Properties.CreatorID.ToString().ToLower(CultureInfo.CurrentCulture).Contains(query));
                    }
                    catch
                    {
                        return false;
                    }
                });

            pB1.Maximum = results.Count;

            lock (listItems)
            {
                foreach (Primitive prim in results)
                {
                    try
                    {
                        ObjectsListItem item = new ObjectsListItem(prim, client, lbxPrims);

                        if (!listItems.ContainsKey(prim.LocalID))
                        {
                            listItems.Add(prim.LocalID, item);

                            item.PropertiesReceived += item_PropertiesReceived;
                            item.RequestProperties();
                            //inmem = true;
                        }
                        else
                        {
                            if (pB1.Value < results.Count) pB1.Value += 1;

                            lock (lbxPrims.Items)
                            {
                                lbxPrims.BeginUpdate();
                                lbxPrims.Items.Remove(item);
                                lbxPrims.Items.Add(item);
                                lbxPrims.EndUpdate();
                            }
                        }
                    }
                    catch
                    {
                        ;
                    }
                }
            }

            //if (!inmem)
            //{
                pB1.Value = 0;
                pB1.Visible = false;
                lbxPrims.SortList();
            //}

            //tlbStatus.Text = listItems.Count.ToString() + " objects";
            tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
        }

        private void DisplayForSale()
        {
            lbxPrims.Items.Clear();

            try
            {
                Vector3 location = new Vector3(Vector3.Zero); 
                location = instance.SIMsittingPos();
                pB1.Maximum = ItemsProps.Count;
                pB1.Value = 0;
                pB1.Visible = true; 

                pBar3.Visible = true;

                lock (ItemsProps)
                {
                    foreach (KeyValuePair<uint, ObjectsListItem> entry in ItemsProps)
                    {
                        ObjectsListItem item = entry.Value;
                        Vector3 pos = new Vector3(Vector3.Zero); 
                        pos = item.Prim.Position;

                        if ((item.Prim.ParentID == 0) && (item.Prim.Properties.SaleType != 0) && (pos != Vector3.Zero) && (Vector3.Distance(location,pos) < range))
                        {
                            if (item.Prim.ParentID == 0) //root prims only
                            {
                                lock (lbxPrims.Items)
                                {
                                    lbxPrims.BeginUpdate();
                                    lbxPrims.Items.Add(item);
                                    lbxPrims.EndUpdate();
                                }
                            }
                        }

                        if (pB1.Value < ItemsProps.Count) pB1.Value += 1;
                    }
                }

                //lblStatus.Visible = true;
                lbxPrims.SortList();
                //lblStatus.Visible = false;
                pB1.Visible = false;
                pBar3.Visible = false;

                //lblStatus.Visible = false;
                //lbxPrims.Visible = true;
                //lbxChildren.Visible = true;
                txtSearch.Enabled = true;

                //tlbStatus.Text = listItems.Count.ToString() + " objects";
                tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
            }
            catch (Exception ex)
            {
                //string exp = exc.Message;
                instance.CrashReporter?.Post(ex);

            }
        }

        private void DisplayScriptedObjects()
        {
            lbxPrims.Items.Clear();

            try
            {
                Vector3 location = new Vector3(Vector3.Zero); 
                location = instance.SIMsittingPos();
                pB1.Maximum = ItemsProps.Count;
                pBar3.Visible = true;

                lock (ItemsProps)
                {
                    foreach (KeyValuePair<uint, ObjectsListItem> entry in ItemsProps)
                    {
                        ObjectsListItem item = entry.Value;
                        Vector3 pos = new Vector3(Vector3.Zero); 
                        pos = item.Prim.Position;

                        if ((item.Prim.ParentID == 0) && ((item.Prim.Flags & PrimFlags.Scripted) == PrimFlags.Scripted) && (pos != Vector3.Zero) && (Vector3.Distance(location, pos) < range))
                        {
                            if (item.Prim.ParentID == 0) //root prims only
                            {
                                lock (lbxPrims.Items)
                                {
                                    lbxPrims.BeginUpdate();
                                    lbxPrims.Items.Add(item);
                                    lbxPrims.EndUpdate();
                                }
                            }
                        }

                        if (pB1.Value < ItemsProps.Count) pB1.Value += 1;
                    }
                }

                //lblStatus.Visible = true;
                lbxPrims.SortList();
                //lblStatus.Visible = false;
                pB1.Visible = false;
                pBar3.Visible = false;

                //lblStatus.Visible = false;
                //lbxPrims.Visible = true;
                //lbxChildren.Visible = true;
                txtSearch.Enabled = true;

                //tlbStatus.Text = listItems.Count.ToString() + " objects";
                tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
            }
            catch (Exception ex)
            {
                //string exp = exc.Message;
                instance.CrashReporter?.Post(ex);
            }
        }

        private void DisplayMyObjects()
        {
            lbxPrims.Items.Clear();

            try
            {
                Vector3 location = new Vector3(Vector3.Zero); 
                location = instance.SIMsittingPos();
                pB1.Maximum = ItemsProps.Count;
                pBar3.Visible = true;

                lock (ItemsProps)
                {
                    foreach (KeyValuePair<uint, ObjectsListItem> entry in ItemsProps)
                    {
                        ObjectsListItem item = entry.Value;
                        Vector3 pos = new Vector3(Vector3.Zero); 
                        pos = item.Prim.Position;

                        if ((item.Prim.ParentID == 0) && item.Prim.Properties.OwnerID.ToString().ToLower(CultureInfo.CurrentCulture).Contains(client.Self.AgentID.ToString()) && (pos != Vector3.Zero) && (Vector3.Distance(location, pos) < range))
                        {
                            if (item.Prim.ParentID == 0) //root prims only
                            {
                                lock (lbxPrims.Items)
                                {
                                    lbxPrims.BeginUpdate();
                                    lbxPrims.Items.Add(item);
                                    lbxPrims.EndUpdate();
                                }
                            }
                        }

                        if (pB1.Value < ItemsProps.Count) pB1.Value += 1;
                    }
                }

                pB1.Visible = false;
                pBar3.Visible = false;
                lbxPrims.SortList();

                txtSearch.Enabled = true;

                tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
            }
            catch (Exception ex)
            {
                //string exp = exc.Message;
                instance.CrashReporter?.Post(ex);

            }
        }

        private void DisplayOthersObjects()
        {
            lbxPrims.Items.Clear();

            try
            {
                Vector3 location = new Vector3(Vector3.Zero); 
                location = instance.SIMsittingPos();
                pB1.Maximum = ItemsProps.Count;
                pBar3.Visible = true;

                lock (ItemsProps)
                {
                    foreach (KeyValuePair<uint, ObjectsListItem> entry in ItemsProps)
                    {
                        ObjectsListItem item = entry.Value;
                        Vector3 pos = new Vector3(Vector3.Zero); 
                        pos = item.Prim.Position;

                        if ((item.Prim.ParentID == 0) && !item.Prim.Properties.OwnerID.ToString().ToLower(CultureInfo.CurrentCulture).Contains(client.Self.AgentID.ToString()) && (pos != Vector3.Zero) && (Vector3.Distance(location, pos) < range))
                        {
                            if (item.Prim.ParentID == 0) //root prims only
                            {
                                lock (lbxPrims.Items)
                                {
                                    lbxPrims.BeginUpdate();
                                    lbxPrims.Items.Add(item);
                                    lbxPrims.EndUpdate();
                                }
                            }
                        }

                        if (pB1.Value < ItemsProps.Count) pB1.Value += 1;
                    }
                }

                pB1.Visible = false;
                pBar3.Visible = false;
                lbxPrims.SortList();

                txtSearch.Enabled = true;

                tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
            }
            catch (Exception ex)
            {
                //string exp = exc.Message;
                instance.CrashReporter?.Post(ex);

            }
        }

        private void DisplayEmptyObjects()
        {
            lbxPrims.Items.Clear();

            try
            {
                Vector3 location = new Vector3(Vector3.Zero); 
                location = instance.SIMsittingPos();
                pB1.Maximum = ItemsProps.Count;
                pBar3.Visible = true;

                lock (ItemsProps)
                {
                    foreach (KeyValuePair<uint, ObjectsListItem> entry in ItemsProps)
                    {
                        ObjectsListItem item = entry.Value;
                        Vector3 pos = new Vector3(Vector3.Zero); 
                        pos = item.Prim.Position;

                        if ((item.Prim.ParentID == 0) && ((item.Prim.Flags & PrimFlags.InventoryEmpty) == PrimFlags.InventoryEmpty) && (pos != Vector3.Zero) && (Vector3.Distance(location, pos) < range))
                        {
                            if (item.Prim.ParentID == 0) //root prims only
                            {
                                lock (lbxPrims.Items)
                                {
                                    lbxPrims.BeginUpdate();
                                    lbxPrims.Items.Add(item);
                                    lbxPrims.EndUpdate();
                                }
                            }
                        }

                        if (pB1.Value < ItemsProps.Count) pB1.Value += 1;
                    }
                }

                pB1.Visible = false;
                pBar3.Visible = false;
                lbxPrims.SortList();

                txtSearch.Enabled = true;

                tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
            }
            catch (Exception ex)
            {
                //string exp = exc.Message;
                instance.CrashReporter?.Post(ex);
            }
        }

        private void DisplayCreatedByMeObjects()
        {
            lbxPrims.Items.Clear();

            try
            {
                Vector3 location = new Vector3(Vector3.Zero); 
                location = instance.SIMsittingPos();
                pB1.Maximum = ItemsProps.Count;
                pBar3.Visible = true;

                lock (ItemsProps)
                {
                    foreach (KeyValuePair<uint, ObjectsListItem> entry in ItemsProps)
                    {
                        ObjectsListItem item = entry.Value;
                        Vector3 pos = new Vector3(Vector3.Zero); 
                        pos = item.Prim.Position;

                        if ((item.Prim.ParentID == 0) && item.Prim.Properties.CreatorID.ToString().Contains(client.Self.AgentID.ToString()) && (pos != Vector3.Zero) && (Vector3.Distance(location, pos) < range))
                        {
                            if (item.Prim.ParentID == 0) //root prims only
                            {
                                lock (lbxPrims.Items)
                                {
                                    lbxPrims.BeginUpdate();
                                    lbxPrims.Items.Add(item);
                                    lbxPrims.EndUpdate();
                                }
                            }
                        }

                        if (pB1.Value < ItemsProps.Count) pB1.Value += 1;
                    }
                }

                pB1.Visible = false;
                pBar3.Visible = false;
                lbxPrims.SortList();

                txtSearch.Enabled = true;

                tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
            }
            catch (Exception ex)
            {
                //string exp = exc.Message;
                instance.CrashReporter?.Post(ex);
            }
        }

        private void DisplayFullModObjects()
        {
            lbxPrims.Items.Clear();

            try
            {
                Vector3 location = new Vector3(Vector3.Zero); 
                location = instance.SIMsittingPos();
                pB1.Maximum = ItemsProps.Count;
                pBar3.Visible = true;

                lock (ItemsProps)
                {
                    foreach (KeyValuePair<uint, ObjectsListItem> entry in ItemsProps)
                    {
                        ObjectsListItem item = entry.Value;
                        Vector3 pos = new Vector3(Vector3.Zero); 
                        pos = item.Prim.Position;

                        if ((item.Prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(location, pos) < range))
                        {
                            if (item.Prim.ParentID == 0) //root prims only
                            {
                                PermissionMask sPerm = item.Prim.Properties.Permissions.NextOwnerMask;
                                //PermissionMask sOPerm = item.Prim.Properties.Permissions.OwnerMask;
                                string sEp = sPerm.ToString();
                                //string sOEp = sOPerm.ToString();

                                if (sEp.ToLower(CultureInfo.CurrentCulture).Contains("modify") && sEp.ToLower(CultureInfo.CurrentCulture).Contains("copy") & sEp.ToLower(CultureInfo.CurrentCulture).Contains("transfer"))
                                {
                                    lock (lbxPrims.Items)
                                    {
                                        lbxPrims.BeginUpdate();
                                        lbxPrims.Items.Add(item);
                                        lbxPrims.EndUpdate();
                                    }
                                }
                            }
                        }

                        if (pB1.Value < ItemsProps.Count) pB1.Value += 1;
                    }
                }

                pB1.Visible = false;
                pBar3.Visible = false;
                lbxPrims.SortList();

                txtSearch.Enabled = true;

                tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
            }
            catch (Exception ex)
            {
                //string exp = exc.Message;
                instance.CrashReporter?.Post(ex);
            }
        }

        private void DisplayCFullModObjects()
        {
            lbxPrims.Items.Clear();

            try
            {
                Vector3 location = new Vector3(Vector3.Zero); 
                location = instance.SIMsittingPos();
                pB1.Maximum = ItemsProps.Count;
                pBar3.Visible = true;

                lock (ItemsProps)
                {
                    foreach (KeyValuePair<uint, ObjectsListItem> entry in ItemsProps)
                    {
                        ObjectsListItem item = entry.Value;
                        Vector3 pos = new Vector3(Vector3.Zero); 
                        pos = item.Prim.Position;

                        if ((item.Prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(location, pos) < range))
                        {
                            if (item.Prim.ParentID == 0) //root prims only
                            {
                                PermissionMask sPerm = item.Prim.Properties.Permissions.OwnerMask;
                                //PermissionMask sOPerm = item.Prim.Properties.Permissions.OwnerMask;
                                string sEp = sPerm.ToString();
                                //string sOEp = sOPerm.ToString();

                                if (sEp.ToLower(CultureInfo.CurrentCulture).Contains("modify") && sEp.ToLower(CultureInfo.CurrentCulture).Contains("copy") & sEp.ToLower(CultureInfo.CurrentCulture).Contains("transfer"))
                                {
                                    lock (lbxPrims.Items)
                                    {
                                        lbxPrims.BeginUpdate();
                                        lbxPrims.Items.Add(item);
                                        lbxPrims.EndUpdate();
                                    }
                                }
                            }
                        }

                        if (pB1.Value < ItemsProps.Count) pB1.Value += 1;
                    }
                }

                pB1.Visible = false;
                pBar3.Visible = false;
                lbxPrims.SortList();

                txtSearch.Enabled = true;

                tlbDisplay.Text = lbxPrims.Items.Count.ToString(CultureInfo.CurrentCulture) + " objects";
            }
            catch (Exception ex)
            {
                //string exp = exc.Message;
                instance.CrashReporter?.Post(ex);
            }
        }

        //private void ResetObjects()
        //{
        //    lbxPrims.Items.Clear();
        //    lbxChildren.Items.Clear();
        //    lbxTask.Items.Clear();
        //    listItems.Clear();
        //    childItems.Clear();
        //    DisplayObjects();
        //    button3.Visible = button7.Visible = false;
        //}

        private void frmObjects_Load(object sender, EventArgs e)
        {
            CenterToParent();
            
            Text = "Object Manager [" + client.Self.FirstName + " " + client.Self.LastName + "]";

            //numericUpDown1.Maximum = instance.Config.CurrentConfig.RadarRange;

            AddObjectEvents();
            
            if (instance.Config.CurrentConfig.SortByDistance)
            {
                radioButton2.Checked = true;
            }
            else
            {
                radioButton1.Checked = true;
            }

            //AddAllObjects();
            cboDisplay.SelectedIndex = 0;
        }

        private void lbxPrims_SelectedIndexChanged(object sender, EventArgs e)
        {
            sloading = true;

            DisplaySelected();
        }

        private void DisplaySelected()
        {
            try
            {
                lbxTask.Items.Clear();

                button6.Enabled = groupBox1.Enabled = gbxInworld.Enabled = (lbxPrims.SelectedItem != null);

                int iDx = lbxPrims.SelectedIndex;

                if (iDx < 0)
                {
                    //btnTP.Enabled = false;
                    return;
                }

                ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

                Primitive sPr = new Primitive();
                sPr = item.Prim;

                if (sPr.Properties == null)
                {
                    //client.Objects.SelectObject(client.Network.CurrentSim, sPr.LocalID);
                    client.Objects.SelectObject(client.Network.CurrentSim, sPr.LocalID, true);
                    //client.Objects.RequestObject(client.Network.CurrentSim, sPr.LocalID);
                    return;
                }

                //btnTP.Enabled = true;

                labelOwnerId.Text = sPr.Properties.OwnerID.ToString();
                lblUUID.Text = sPr.Properties.ObjectID.ToString();

                if (instance.State.SitPrim != UUID.Zero)
                {
                    btnSitOn.Text = sPr.ID == instance.State.SitPrim ? "&Stand" : "&Sit";
                }
                else
                {
                    btnSitOn.Text = "&Sit";
                }

                // Get the owner name
                UUID lookup = sPr.Properties.OwnerID;
                if (!instance.avnames.ContainsKey(lookup))
                {
                    client.Avatars.RequestAvatarName(lookup);
                    pictureBox1.Cursor = Cursors.Default;
                }
                else
                {
                    labelOwnerName.Text = instance.avnames[lookup];
                    pictureBox1.Enabled = true;
                    pictureBox1.Cursor = Cursors.Hand;
                }

                txtCreator.Text = "??? (click on selected object)";

                lookup = sPr.Properties.CreatorID;

                if (lookup != UUID.Zero)
                {
                    if (!instance.avnames.ContainsKey(lookup))
                    {
                        client.Avatars.RequestAvatarName(lookup);
                        pictureBox2.Cursor = Cursors.Default;
                    }
                    else
                    {
                        txtCreator.Text = instance.avnames[lookup];
                        pictureBox2.Enabled = true;
                        pictureBox2.Cursor = Cursors.Hand;
                    }

                    label21.Text = sPr.Properties.CreatorID.ToString();
                }
                else
                {
                    pictureBox2.Enabled = false;
                    pictureBox2.Cursor = Cursors.Default;
                }


                btnReturn.Enabled = btnTake.Enabled = true;

                PermissionMask sPerm = sPr.Properties.Permissions.NextOwnerMask;
                PermissionMask sOPerm = sPr.Properties.Permissions.OwnerMask;

                string sEp = sPerm.ToString();
                string sOEp = sOPerm.ToString();

                if (sPr.Properties.SaleType != 0)
                {
                    labelSalePrice.Text = "L$" + sPr.Properties.SalePrice.ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    labelSalePrice.Text = "Not for sale";
                }

                label11.Text = sPr.Text;
                txtHover.Text = sPr.Properties.Name;
                labelDesc.Text = sPr.Properties.Description;

                Vector3 primpos = new Vector3(Vector3.Zero); 
                primpos = sPr.Position;
                //// Calculate the distance here in metres
                //int pX = (int)primpos.X;
                //int pY = (int)primpos.Y;
                //int pZ = (int)primpos.Z;

                //int sX = (int)client.Self.SimPosition.X;
                //int sY = (int)client.Self.SimPosition.Y;
                //int sZ = (int)client.Self.SimPosition.Z;

                int vZ = (int)primpos.Z - (int)instance.SIMsittingPos().Z;

                //int vX = sX - pX;
                //int vY = sY - pY;

                //int pX2 = vX * vX;
                //int pY2 = vY * vY;
                //int h2 = pX2 + pY2;

                //int hyp1 = (int)Math.Sqrt(h2);
                //int hyp = instance.Distance3D(sX, sY, sZ, pX, pY, pZ);

                double dist = Math.Round(Vector3.Distance(instance.SIMsittingPos(), primpos), MidpointRounding.ToEven);

                labelDistance.Text = " " + dist.ToString(CultureInfo.CurrentCulture) + "m - [ Elev.:" + vZ.ToString(CultureInfo.CurrentCulture) + "m]";

                labelCost.Text = "L$" + sPr.Properties.OwnershipCost.ToString(CultureInfo.CurrentCulture);
                //label3.Text = sPr.Properties.SaleType.ToString(); 

                // Owner perms
                chkModify.Checked = sOEp.Contains("Modify");
                chkCopy.Checked = sOEp.Contains("Copy");
                chkResell.Checked = sOEp.Contains("Transfer");

                // Next Owner perms
                chkNextOwnerModify.Checked = sEp.Contains("Modify");
                chkNextOwnerCopy.Checked = sEp.Contains("Copy");
                chkNextOwnerResell.Checked = sEp.Contains("Transfer");

                //if (btnTP.Enabled)
                //    btnTP.Enabled = false; 
                
                lkLocation.Text = "";

                //sPr.Flags = LLObject.ObjectFlags.Scripted;
                //client.Objects.RequestObject("", sPr.LocalID);
                //client.Objects.SelectObject();

                pBar1.Visible = true;
                pBar1.Refresh();

                label22.Text = "Local ID: " + sPr.LocalID.ToString(CultureInfo.CurrentCulture);  

                // Populate child items here
                lbxChildren.BeginUpdate();
                lbxChildren.Items.Clear();

                button3.Visible = button7.Visible = false;

                List<Primitive> results = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                    prim => (prim.ParentID == sPr.LocalID));

                if (results is { Count: > 0 })
                {
                    foreach (var prim in results)
                    {
                        ObjectsListItem citem = new ObjectsListItem(prim, client, lbxChildren);

                        if (!childItems.ContainsKey(prim.LocalID))
                        {
                            childItems.Add(prim.LocalID, citem);
                        }
                    }
                }

                foreach (KeyValuePair<uint, ObjectsListItem> kvp in childItems)
                {
                    if (sPr.LocalID == kvp.Value.Prim.ParentID)
                    {
                        //ObjectsListItem citem = new ObjectsListItem(kvp.Value.Prim, client, lbxChildren);
                        //sPr.
                        //citem.PropertiesReceived += new EventHandler(citem_PropertiesReceived);
                        //citem.RequestProperties();
                        lbxChildren.Items.Add(kvp.Value);
                    }
                }

                lbxChildren.EndUpdate();
                pBar1.Visible = false;

                SetPerm(sPr);

                //if (sPr.Properties.OwnerID != client.Self.AgentID)
                //{
                //    //checkBox1.Enabled = checkBox2.Enabled = checkBox3.Enabled = label11.Enabled = label15.Enabled = textBox2.Enabled = false;
                //    label11.Enabled = label15.Enabled = textBox2.Enabled = false;
                //}
                //else
                //{
                //    //checkBox1.Enabled = checkBox2.Enabled = checkBox3.Enabled = label11.Enabled = label15.Enabled = textBox2.Enabled = true;
                //    label11.Enabled = label15.Enabled = textBox2.Enabled = true;
                //}

                sloading = false;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, Helpers.LogLevel.Error);
            }

            string msg1 = label11.Text;
            toolTip = new Popup(customToolTip = new CustomToolTip(instance, msg1));
            toolTip.AutoClose = false;
            toolTip.FocusOnOpen = false;
            toolTip.ShowingAnimation = toolTip.HidingAnimation = PopupAnimations.Blend;

            lbxPrims.SelectedItem = lbxPrims.SelectedItem;
            lbxPrims.Select();
        }

        private void SetPerm(Primitive sPr)
        {
            if (sPr.Properties.OwnerID != client.Self.AgentID)
            {
                chkNextOwnerModify.Enabled = chkNextOwnerCopy.Enabled = chkNextOwnerResell.Enabled = false;
                //label11.Enabled = label15.Enabled = textBox2.Enabled = false;
                label11.ReadOnly = labelDesc.ReadOnly = txtHover.ReadOnly = true;
            }
            else
            {
                chkNextOwnerModify.Enabled = chkNextOwnerCopy.Enabled = chkNextOwnerResell.Enabled = true;
                //label11.Enabled = label15.Enabled = textBox2.Enabled = true;
                label11.ReadOnly = labelDesc.ReadOnly = txtHover.ReadOnly = false;
            }

            //// Set permission checboxes  
            //if ((sPr.Properties.Permissions.OwnerMask & PermissionMask.Modify) == PermissionMask.Modify)
            //{
            //    checkBox1.Enabled = true;
            //}
            //else
            //{
            //    checkBox1.Enabled = false;
            //}

            //if ((sPr.Properties.Permissions.OwnerMask & PermissionMask.Copy) == PermissionMask.Copy)
            //{
            //    checkBox2.Enabled = true;
            //}
            //else
            //{
            //    checkBox2.Enabled = false;
            //}

            //if ((sPr.Properties.Permissions.OwnerMask & PermissionMask.Transfer) == PermissionMask.Transfer)
            //{
            //    checkBox3.Enabled = true;
            //}
            //else
            //{
            //    checkBox3.Enabled = false;
            //}

            //if ((sPr.Properties.Permissions.OwnerMask & PermissionMask.Modify) != PermissionMask.Modify)
            //{
            //    checkBox1.Enabled = checkBox2.Enabled = checkBox3.Enabled = false;
            //}
        }

        private void SetPerms(Primitive oPrm)
        {
            Dictionary<UUID, Primitive> Objects = new Dictionary<UUID, Primitive>();
            PermissionMask Perms = PermissionMask.None;
            List<Primitive> childPrims;
            List<uint> localIDs = new List<uint>();
            UUID rootID = oPrm.ID;

            Primitive rootPrim = new Primitive();
            rootPrim = client.Network.CurrentSim.ObjectsPrimitives.Find(
             prim => prim.ID == oPrm.ID);

            if (chkNextOwnerModify.Checked)
            {
                Perms |= PermissionMask.Modify;
            }

            if (chkNextOwnerCopy.Checked)
            {
                Perms |= PermissionMask.Copy;
            }

            if (chkNextOwnerResell.Checked)
            {
                Perms |= PermissionMask.Transfer;
            }


            rootPrim = client.Network.CurrentSim.ObjectsPrimitives.Find(prim => prim.ID == rootID);

            if (rootPrim == null)
                return;
            else
                Logger.DebugLog("Found requested prim " + rootPrim.ID, client);

            if (rootPrim.ParentID != 0)
            {
                // This is not actually a root prim, find the root
                if (!client.Network.CurrentSim.ObjectsPrimitives.TryGetValue(rootPrim.ParentID, out rootPrim))
                    return;
            }

            // Save the description
            //client.Objects.SetDescription(client.Network.CurrentSim, rootPrim.LocalID , label15.Text);
            //client.Objects.SetName(client.Network.CurrentSim, rootPrim.LocalID, textBox2.Text );         

            // Find all of the child objects linked to this root
            childPrims = client.Network.CurrentSim.ObjectsPrimitives.FindAll(prim => prim.ParentID == rootPrim.LocalID);

            // Build a dictionary of primitives for referencing later
            Objects[rootPrim.ID] = rootPrim;
            foreach (var p in childPrims)
                Objects[p.ID] = p;

            // Build a list of all the localIDs to set permissions for
            localIDs.Add(rootPrim.LocalID);
            localIDs.AddRange(childPrims.Select(p => p.LocalID));

            client.Objects.SetPermissions(client.Network.CurrentSim, localIDs, PermissionWho.NextOwner,
                PermissionMask.Modify, (Perms & PermissionMask.Modify) == PermissionMask.Modify);

            client.Objects.SetPermissions(client.Network.CurrentSim, localIDs, PermissionWho.NextOwner,
                PermissionMask.Copy, (Perms & PermissionMask.Copy) == PermissionMask.Copy);

            client.Objects.SetPermissions(client.Network.CurrentSim, localIDs, PermissionWho.NextOwner,
                PermissionMask.Transfer, (Perms & PermissionMask.Transfer) == PermissionMask.Transfer);


            //// Check each prim for task inventory and set permissions on the task inventory
            //int taskItems = 0;
            //foreach (Primitive prim in Objects.Values)
            //{
            //    if ((prim.Flags & PrimFlags.InventoryEmpty) == 0)
            //    {
            //        List<InventoryBase> items = client.Inventory.GetTaskInventory(prim.ID, prim.LocalID, 1000 * 30);

            //        if (items != null)
            //        {
            //            for (int i = 0; i < items.Count; i++)
            //            {
            //                if (!(items[i] is InventoryFolder))
            //                {
            //                    InventoryItem itemf = (InventoryItem)items[i];
            //                    itemf.Permissions.NextOwnerMask = Perms;

            //                    client.Inventory.UpdateTaskInventory(prim.LocalID, itemf);
            //                    ++taskItems;
            //                }
            //            }
            //        }
            //    }
            //}
        }

        private void btnSitOn_Click(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            if (btnSitOn.Text == "&Sit")
            {
                instance.State.SetSitting(true, item.Prim.ID);

                //// start the timer
                //sittimer = new System.Timers.Timer();
                //sittimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                //// Set the Interval to 10 seconds.
                //sittimer.Interval = 5000;
                //sittimer.Enabled = true;
                //sittimer.Start();  
            }
            else if (btnSitOn.Text == "&Stand")
            {
                instance.State.SetSitting(false, item.Prim.ID);
                btnSitOn.Text = "&Sit";
            }
        }

        void Self_AvatarSitResponse(object sender, AvatarSitResponseEventArgs e)
        {
            instance.State.SitPrim = e.ObjectID;
            instance.State.IsSitting = true;

            BeginInvoke(new MethodInvoker(() =>
            {
                btnSitOn.Text = "&Stand";
            }));
        }

        //private void OnTimedEvent(object sender, ElapsedEventArgs e)
        //{
        //    //PrimEvent.WaitOne(4000, false);

        //    if (client.Self.SittingOn == 0)
        //    {
        //        instance.State.SetSitting(false, instance.State.SitPrim);
        //        btnSitOn.Text = "&Sit";
        //    }

        //    sittimer.Stop();
        //    sittimer.Enabled = false; 
        //}

        private void btnTouch_Click(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            if ((item.Prim.Flags & PrimFlags.Touch) != 0)
            {
                Vector3 pos = new Vector3(Vector3.Zero); 
                pos = item.Prim.Position;

                uint regionX, regionY;
                Utils.LongToUInts(client.Network.CurrentSim.Handle, out regionX, out regionY);
                Vector3d objpos;

                objpos.X = pos.X + (double)regionX;
                objpos.Y = pos.Y + (double)regionY;
                objpos.Z = pos.Z;   // -2f;

                instance.State.SetPointingTouch(true, item.Prim.ID, objpos, pos);
                instance.State.LookAtObject(true, item.Prim.ID);

                client.Self.Touch(item.Prim.LocalID);
                Thread.Sleep(800);

                instance.State.SetPointingTouch(false, item.Prim.ID, objpos, pos);
                instance.State.LookAtObject(false, item.Prim.ID);
            }
        }

        private void frmObjects_FormClosing(object sender, FormClosingEventArgs e)
        {
            ClearLists();
            lbxPrims.Items.Clear();  

            //RemoveObjectEvents();
            RemoveNetcomEvents();
            
            client.Avatars.UUIDNameReply -= Avatars_OnAvatarNames;
            client.Self.AvatarSitResponse -= Self_AvatarSitResponse;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnLocation_Click(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            //"http://slurl.com/GridClient/" + 
            string sPos = client.Network.CurrentSim.Name + "/" + item.Prim.Position.X + "/" + item.Prim.Position.Y + "/" + item.Prim.Position.Z;
            lkLocation.Text = sPos;
            //btnTP.Enabled = true;
        }

        private void btnTP_Click(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            (new frmTeleport(instance, client.Network.CurrentSim.Name, item.Prim.Position.X, item.Prim.Position.Y, item.Prim.Position.Z, false)).Show();
        }

        private void btnTurnTo_Click(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            Vector3 target = new Vector3(Vector3.Zero); 
            target = item.Prim.Position; // the object to look at
            
            client.Self.Movement.TurnToward(target);
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }

        //private int GetDistance(Vector3 primpos)
        //{
        //    // Calculate the distance here in metres
        //    int pX = (int)primpos.X;
        //    int pY = (int)primpos.Y;
        //    int pZ = (int)primpos.Z;

        //    int sX = (int)client.Self.SimPosition.X;
        //    int sY = (int)client.Self.SimPosition.Y;
        //    int sZ = (int)client.Self.SimPosition.Z;

        //    int vX = sX - pX;
        //    int vY = sY - pY;

        //    int pX2 = vX * vX;
        //    int pY2 = vY * vY;
        //    int h2 = pX2 + pY2;

        //    int vZ = pZ - sZ;

        //    int hyp = (int)Math.Sqrt(h2);

        //    return hyp;
        //}

        private void btnPay_Click_1(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            Primitive sPr = new Primitive();
            sPr = item.Prim;
            SaleType styp = sPr.Properties.SaleType;

            int sprice = sPr.Properties.SalePrice;
            

            //if (sprice != 0)
            if (styp != SaleType.Not)
            {
                (new frmPay(instance, item.Prim.ID, labelOwnerName.Text, sprice, sPr)).Show(this);
            }
            else
            {
                (new frmPay(instance, item.Prim.ID, labelOwnerName.Text, item.Prim.Properties.Name, sPr)).Show(this);
            }
        }

        private void btnPointAt_Click_1(object sender, EventArgs e)
        {
            try
            {
                int iDx = lbxPrims.SelectedIndex;
                ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

                uint regionX, regionY;
                Utils.LongToUInts(client.Network.CurrentSim.Handle, out regionX, out regionY);
                Vector3 pos = new Vector3(Vector3.Zero);
                pos = item.Prim.Position;

                Vector3d objpos;

                objpos.X = pos.X + (double)regionX;
                objpos.Y = pos.Y + (double)regionY;
                objpos.Z = pos.Z;   // -2f;

                if (btnPointAt.Text == "Po&int At")
                {
                    client.Self.AnimationStart(Animations.TURNLEFT, false);
                    client.Self.Movement.TurnToward(item.Prim.Position);
                    client.Self.Movement.FinishAnim = true;
                    Thread.Sleep(200);
                    client.Self.AnimationStop(Animations.TURNLEFT, false);

                    instance.State.SetPointing(true, item.Prim.ID, objpos, pos);
                    instance.State.LookAtObject(true, item.Prim.ID);
                    btnPointAt.Text = "Unpo&int";
                }
                else if (btnPointAt.Text == "Unpo&int")
                {
                    instance.State.SetPointing(false, item.Prim.ID, objpos, pos);
                    instance.State.LookAtObject(false, item.Prim.ID);
                    btnPointAt.Text = "Po&int At";
                }
            }
            catch (Exception ex)
            {
                instance.CrashReporter?.Post(ex);
            }
        }

        private void lbxChildren_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0) return;

            ObjectsListItem itemToDraw = (ObjectsListItem)lbxChildren.Items[e.Index];

            Brush textBrush = null;
            Font boldFont = new Font(e.Font, FontStyle.Bold);
            Font regularFont = new Font(e.Font, FontStyle.Regular);

            textBrush = (e.State & DrawItemState.Selected) == DrawItemState.Selected 
                ? new SolidBrush(Color.FromKnownColor(KnownColor.HighlightText)) 
                : new SolidBrush(Color.FromKnownColor(KnownColor.ControlText));

            string name;
            string description;

            ////GM: protected from null properties
            //if (itemToDraw.Prim.Properties == null)
            //{
            //    name = description = "Null";
            //}
            if (itemToDraw.Prim.Properties == null)
            {
                if ((itemToDraw.Prim.Flags & PrimFlags.Scripted) != 0)
                {
                    name = (itemToDraw.Prim.Flags & PrimFlags.Touch) != 0 
                        ? "Child Object (scripted/touch)" : "Child Object (scripted)";
                }
                else
                {
                    name = "Child Object";
                }

                description = itemToDraw.Prim.ID.ToString();
            }
            else
            {
                name = itemToDraw.Prim.Properties.Name;
                description = itemToDraw.Prim.Properties.Description;
            }

            SizeF nameSize = e.Graphics.MeasureString(name, boldFont);
            float nameX = e.Bounds.Left + 4;
            float nameY = e.Bounds.Top + 2;

            e.Graphics.DrawString(name, boldFont, textBrush, nameX, nameY);
            e.Graphics.DrawString(description, regularFont, textBrush, nameX + nameSize.Width + 8, nameY);

            e.DrawFocusRectangle();

            boldFont.Dispose();
            regularFont.Dispose();
            textBrush.Dispose();
            boldFont = null;
            regularFont = null;
            textBrush = null;
        }

        private void lbxChildren_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnTask.Enabled = button7.Visible = button3.Visible = (lbxChildren.SelectedItem != null);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int iDx = lbxChildren.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxChildren.Items[iDx];

            client.Self.Touch(item.Prim.LocalID);
        }

        private void btnWalkTo_Click(object sender, EventArgs e)
        {
            if (btnWalkTo.Text == "Walk to")
            {
                int iDx = lbxPrims.SelectedIndex;
                ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

                Primitive prim = new Primitive();
                prim = item.Prim;
                Vector3 pos = new Vector3(Vector3.Zero); 
                pos = prim.Position;
                ulong regionHandle = client.Network.CurrentSim.Handle;

                int followRegionX = (int)(regionHandle >> 32);
                int followRegionY = (int)(regionHandle & 0xFFFFFFFF);
                ulong x = (ulong)(pos.X + followRegionX);
                ulong y = (ulong)(pos.Y + followRegionY);

                //string sPos = client.Network.CurrentSim.Name + "/" + item.Prim.Position.X + "/" + item.Prim.Position.Y + "/" + item.Prim.Position.Z;

                btnWalkTo.Text = "Stop";
                client.Self.AutoPilotCancel();
                client.Self.AutoPilot(x, y, pos.Z);
            }
            else
            {
                client.Self.AutoPilotCancel();
                btnWalkTo.Text = "Walk to";
            }
        }

        private void btnTake_Click(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            Primitive sPr = new Primitive();
            sPr = item.Prim;

            if (sPr.Properties.OwnerID == client.Self.AgentID)
            {
                Vector3 pos = new Vector3(Vector3.Zero); 
                pos = sPr.Position;

                UUID pointID = UUID.Random();
                UUID beamID = UUID.Random();

                client.Self.PointAtEffect(client.Self.AgentID, item.Prim.ID, Vector3d.Zero, PointAtType.Select, pointID);
                client.Self.BeamEffect(client.Self.AgentID, item.Prim.ID, Vector3d.Zero, new Color4(0, 0, 255, 0), 250.0f, beamID);

                client.Self.Movement.TurnToward(pos);

                client.Inventory.RequestDeRezToInventory(item.Prim.LocalID);

                client.Self.PointAtEffect(client.Self.AgentID, UUID.Zero, Vector3d.Zero, PointAtType.None, pointID);
                client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, Vector3d.Zero, new Color4(0, 0, 255, 0), 0, beamID);
                instance.MediaManager.PlayUISound(UISounds.ObjectDelete);
            }
        }

        private void chkNextOwnerModify_CheckedChanged(object sender, EventArgs e)
        {
            if (sloading) return;

            int iDx = lbxPrims.SelectedIndex;

            if (iDx < 0)
                return;

            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            Primitive sPr = new Primitive();
            sPr = item.Prim;

            SetPerms(sPr);
        }

        private void chkNextOwnerCopy_CheckedChanged(object sender, EventArgs e)
        {
            if (sloading) return;

            int iDx = lbxPrims.SelectedIndex;

            if (iDx < 0)
                return;

            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            Primitive sPr = new Primitive();
            sPr = item.Prim;

            SetPerms(sPr);
        }

        private void chkNextOwnerResell_CheckedChanged(object sender, EventArgs e)
        {
            if (sloading) return;

            int iDx = lbxPrims.SelectedIndex;

            if (iDx < 0)
                return;

            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            Primitive sPr = new Primitive();
            sPr = item.Prim;

            SetPerms(sPr);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            newrange = (float)numericUpDown1.Value;
            //instance.Config.CurrentConfig.ObjectRange = (int)newrange; 
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            string query = txtSearch.Text.Trim();

            lbxChildren.Items.Clear();
            lbxTask.Items.Clear();
            button3.Visible = button7.Visible = false;

            btnClear.Enabled = query.Length != 0;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            //txtSearch.Clear();
            //txtSearch.Select();

            if (txtSearch.Text.Length == 0) return;

            lbxPrims.Items.Clear();
            lbxChildren.Items.Clear();
            lbxTask.Items.Clear();

            button3.Visible = button7.Visible = false;

            string query = txtSearch.Text.Trim();

            SearchFor(query);
        }

        private void cboDisplay_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetFields();

            if (range != newrange)
            {
                range = newrange;
                //cboDisplay.SelectedIndex = 0;
                lbxPrims.Items.Clear();
                AddAllObjects();
            }
            else
            {
                if (cboDisplay.SelectedIndex == 0)
                {
                    //DisplayObjects();
                    //lbxPrims.Items.Clear();
                    //AddAllObjects();
                    lbxPrims.Items.Clear();
                    AddAllObjects();
                }
                else if (cboDisplay.SelectedIndex == 1)
                {
                    DisplayForSale();
                }
                else if (cboDisplay.SelectedIndex == 2)
                {
                    DisplayScriptedObjects();
                }
                else if (cboDisplay.SelectedIndex == 3)
                {
                    DisplayMyObjects();
                }
                else if (cboDisplay.SelectedIndex == 4)
                {
                    DisplayOthersObjects();
                }
                else if (cboDisplay.SelectedIndex == 5)
                {
                    DisplayEmptyObjects();
                }
                else if (cboDisplay.SelectedIndex == 6)
                {
                    DisplayCreatedByMeObjects();
                }
                else if (cboDisplay.SelectedIndex == 7)
                {
                    DisplayFullModObjects();
                }
                else if (cboDisplay.SelectedIndex == 8)
                {
                    DisplayCFullModObjects();
                }

                lbxPrims.SortList();
            }
        }

        private void ResetFields()
        {
            lbxChildren.Items.Clear();
            lbxTask.Items.Clear();
            lbxPrims.Enabled = true;

            //btnClear.PerformClick();
            txtSearch.Text = string.Empty;   

            label11.Text = string.Empty;
            labelDesc.Text = string.Empty;
            labelDistance.Text = string.Empty;
            labelOwnerName.Text = string.Empty;
            labelOwnerId.Text = string.Empty;
            lblUUID.Text = string.Empty;
            labelSalePrice.Text = string.Empty;
            labelCost.Text = string.Empty;
            chkNextOwnerModify.Checked = false;
            chkNextOwnerCopy.Checked = false;
            chkNextOwnerResell.Checked = false;
            chkResell.Checked = false;
            chkCopy.Checked = false;
            chkModify.Checked = false;
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            ////ResetFields();

            //if (range != newrange)
            //{
            //    //range = newrange;
            //    if (cboDisplay.SelectedIndex == 0)
            //    {
            //        range = newrange;
            //        lbxPrims.Items.Clear();
            //        AddAllObjects();
            //    }
            //    else
            //    {
            //        cboDisplay.SelectedIndex = 0;
            //    }
            //}
            ////else
            ////{
            ////    if (cboDisplay.SelectedIndex == -1)
            ////    {
            ////        MessageBox.Show("Select a 'Display' option from above first.", "Object Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ////        return;
            ////    }

            ////    if (cboDisplay.SelectedIndex == 0)
            ////    {
            ////        DisplayObjects();
            ////    }
            ////    else if (cboDisplay.SelectedIndex == 1)
            ////    {
            ////        DisplayForSale();
            ////    }
            ////    else if (cboDisplay.SelectedIndex == 2)
            ////    {
            ////        DisplayScriptedObjects();
            ////    }

            ////    lbxPrims.SortList();
            ////}

            gbxInworld.Enabled = false;

            txtSearch.Text = string.Empty;  

            range = newrange = (float)numericUpDown1.Value;

            if (cboDisplay.SelectedIndex != 0)
            {
                cboDisplay.SelectedIndex = 0;
            }
            else
            {
                lbxPrims.Items.Clear();
                AddAllObjects();
            }

            lbxPrims.SortList();
        }

        private void GetTaskInventory(UUID objID, uint localID)
        {
            lbxTask.Items.Clear();

            List<InventoryBase> items = client.Inventory.GetTaskInventory(objID, localID, 1000 * 30);

            if (items != null)
            {
                foreach (var i in items)
                {
                    if (i is InventoryFolder)
                    {
                        //ListViewItem fitem = lbxTask.Items.Add(items[i].Name, "- " + items[i].Name + " folder", string.Empty);
                        //fitem.Tag = fitem;
                    }
                    else
                    {
                        InventoryItem sitem = (InventoryItem)i;
                        string perms = string.Empty;

                        if (sitem.OwnerID == client.Self.AgentID)
                        {
                            if ((sitem.Permissions.OwnerMask & PermissionMask.Modify) != PermissionMask.Modify)
                            {
                                perms = "(no modify)";
                            }

                            if ((sitem.Permissions.OwnerMask & PermissionMask.Copy) != PermissionMask.Copy)
                            {
                                perms += " (no copy)";
                            }

                            if ((sitem.Permissions.OwnerMask & PermissionMask.Transfer) != PermissionMask.Transfer)
                            {
                                perms += " (no transfer)";
                            }
                        }
                        else
                        {
                            if ((sitem.Permissions.EveryoneMask & PermissionMask.Modify) != PermissionMask.Modify)
                            {
                                perms += "(no modify)";
                            }

                            if ((sitem.Permissions.EveryoneMask & PermissionMask.Copy) != PermissionMask.Copy)
                            {
                                perms += " (no copy)";
                            }

                            if ((sitem.Permissions.EveryoneMask & PermissionMask.Transfer) != PermissionMask.Transfer)
                            {
                                perms += " (no transfer)";
                            }
                        }

                        ListViewItem litem = lbxTask.Items.Add(sitem.Name, $"   - [{sitem.AssetType}] {sitem.Name} {perms}", string.Empty);
                        litem.Tag = sitem;
                    }
                }
            }

            pBar2.Visible = false;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void lbxTask_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label11_MouseHover(object sender, EventArgs e)
        {
            if (label11.Text.Length > 0)
            {
                toolTip.Show(label11);
            }
        }

        private void label11_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Close();
        }

        private void lbxPrims_Leave(object sender, EventArgs e)
        {
            gbxInworld.Enabled = (lbxPrims.SelectedItem != null);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;

            if (iDx < 0)
                return;

            pBar2.Visible = true;
            pBar2.Refresh();

            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];
            Primitive sPr = new Primitive();
            sPr = item.Prim;

            GetTaskInventory(sPr.ID, sPr.LocalID);
        }

        private void btnTask_Click(object sender, EventArgs e)
        {
            int iDx = lbxChildren.SelectedIndex;

            if (iDx < 0)
                return;

            pBar2.Visible = true;
            pBar2.Refresh();

            ObjectsListItem item = (ObjectsListItem)lbxChildren.Items[iDx];
            Primitive sPr = new Primitive();
            sPr = item.Prim;

            GetTaskInventory(sPr.ID, sPr.LocalID);
        }

        private void btnMute_Click(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            if (instance.IsObjectMuted(item.Prim.ID, item.Prim.Properties.Name))
            {
                MessageBox.Show($"{item.Prim.Properties.Name} is already in your mute list.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            //DataRow dr = instance.MuteList.NewRow();
            //dr["uuid"] = item.Prim.ID;
            //dr["mute_name"] = item.Prim.Properties.Name;
            //instance.MuteList.Rows.Add(dr);

            instance.Client.Self.UpdateMuteListEntry(MuteType.Object, item.Prim.ID, item.Prim.Properties.Name);

            MessageBox.Show($"{item.Prim.Properties.Name} is now muted.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            int iDx = lbxChildren.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxChildren.Items[iDx];

            switch (button7.Text)
            {
                case "Sit On":
                    instance.State.SetSitting(true, item.Prim.ID);
                    button7.Text = "Stand Up";
                    break;
                case "Stand Up":
                    instance.State.SetSitting(false, item.Prim.ID);
                    button7.Text = "Sit On";
                    break;
            }
        }

        private void label11_Leave(object sender, EventArgs e)
        {
            //if (sloading) return;

            //int iDx = lbxPrims.SelectedIndex;

            //if (iDx < 0)
            //    return;

            //ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            //Primitive sPr = new Primitive();
            //sPr = item.Prim;

            //SetPerms(sPr);
        }

        private void labelDesc_Leave(object sender, EventArgs e)
        {
            if (sloading) return;

            if (txtDescChanged)
            {
                int iDx = lbxPrims.SelectedIndex;

                if (iDx < 0)
                    return;

                ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

                Primitive sPr = new Primitive();
                sPr = item.Prim;

                txtDescChanged = false;

                //SetPerms(sPr);

                client.Objects.SetDescription(client.Network.CurrentSim, sPr.LocalID, labelDesc.Text);
            }
        }

        private void pBar2_Click(object sender, EventArgs e)
        {

        }

        private void lbxTask_DoubleClick(object sender, EventArgs e)
        {
            if (lbxTask.SelectedItems.Count == 1)
            {
                int iDx = lbxPrims.SelectedIndex;

                if (iDx < 0) return;

                ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];
                Primitive sPr = new Primitive();
                sPr = item.Prim;

                if (sPr.Properties.OwnerID != client.Self.AgentID)
                {
                    return;
                }

                InventoryItem llitem = lbxTask.SelectedItems[0].Tag as InventoryItem;
                ListViewItem selitem = lbxTask.SelectedItems[0];

                if (llitem.InventoryType == InventoryType.LSL)
                {
                    // TPV precaution just in case SL messes up and allows it
                    if ((llitem.Permissions.OwnerMask & PermissionMask.Modify) != PermissionMask.Modify)
                    {
                        return;
                    }

                    InventoryLSL sobj = (InventoryLSL)selitem.Tag;

                    if (sobj == null) return;

                    if (sobj is InventoryLSL)
                    {
                        // open the Script Manager
                        (new frmScriptEditor(instance, sobj, sPr)).Show();
                        return;
                    }
                }
                else if (llitem.InventoryType == InventoryType.Notecard)
                {
                    // TPV precaution just in case SL messes up and allows it
                    if ((llitem.Permissions.OwnerMask & PermissionMask.Modify) != PermissionMask.Modify)
                    {
                        return;
                    }

                    InventoryNotecard nobj = (InventoryNotecard)selitem.Tag;
                    if (nobj == null) return;

                    if (nobj is InventoryNotecard)
                    {
                        (new frmNotecardEditor(instance, nobj, sPr)).Show();
                    }
                }
            }
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode node = e.Data.GetData(typeof(TreeNode)) as TreeNode;

            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                int iDx = lbxPrims.SelectedIndex;

                if (iDx < 0)
                {
                    MessageBox.Show("You must first select an object before you can drop an item.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);     
                    return;
                }

                ObjectsListItem tiobject;

                int iDx2 = lbxChildren.SelectedIndex;

                if (iDx2 < 0)
                {

                    tiobject = (ObjectsListItem)lbxPrims.Items[iDx];
                }
                else
                {
                    tiobject = (ObjectsListItem)lbxChildren.Items[iDx2];
                }

                Primitive sPr = new Primitive();
                sPr = tiobject.Prim;

                InventoryItem item = node.Tag as InventoryItem;

                if ((item.Permissions.OwnerMask & PermissionMask.Copy) != PermissionMask.Copy)
                {
                    DialogResult res = MessageBox.Show("This is a 'no copy' item and you will lose ownership if you continue.", "Warning", MessageBoxButtons.OKCancel);

                    if (res == DialogResult.Cancel) return;
                }

                if (item.InventoryType == InventoryType.LSL)
                {
                    client.Inventory.CopyScriptToTask(sPr.LocalID, item, true);
                }
                else
                {
                    client.Inventory.UpdateTaskInventory(sPr.LocalID, item);
                }

                //client.Inventory.TaskInventoryReply += new EventHandler<TaskInventoryReplyEventArgs>(Inventory_TaskInventoryReply); 

                button4.PerformClick();  
                button6.PerformClick();  
            }
        }

        private void Item_CopiedCallback(InventoryBase item)
        {
            //string citem = item.Name;
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(TreeNode))
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void textBox1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(TreeNode)) 
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void lbxTask_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;

            DeleteTaskItem();
        }

        private void DeleteTaskItem()
        {
            int iDx = lbxPrims.SelectedIndex;

            if (iDx < 0) return;

            ObjectsListItem item;

            int iDx2 = lbxChildren.SelectedIndex;

            if (iDx2 < 0)
            {
                item = (ObjectsListItem)lbxPrims.Items[iDx];
            }
            else
            {
                item = (ObjectsListItem)lbxChildren.Items[iDx2];
            }

            Primitive sPr = new Primitive();
            sPr = item.Prim;

            InventoryItem llitem = lbxTask.SelectedItems[0].Tag as InventoryItem;
            //ListViewItem selitem = lbxTask.SelectedItems[0];

            client.Inventory.RemoveTaskInventory(sPr.LocalID, llitem.UUID, client.Network.CurrentSim);

            button4.PerformClick();
            button6.PerformClick();  
        }

        private void CopyTaskItem()
        {
            int iDx = lbxPrims.SelectedIndex;

            if (iDx < 0) return;

            ObjectsListItem item;

            int iDx2 = lbxChildren.SelectedIndex;

            if (iDx2 < 0)
            {
                item = (ObjectsListItem)lbxPrims.Items[iDx];
            }
            else
            {
                item = (ObjectsListItem)lbxChildren.Items[iDx2];
            }

            Primitive sPr = new Primitive();
            sPr = item.Prim;

            InventoryItem llitem = lbxTask.SelectedItems[0].Tag as InventoryItem;
            InventoryItem llitemname = ((ListViewItem)lbxPrims.SelectedItems[0]).Tag as InventoryItem;
            
            //ListViewItem selitem = lbxTask.SelectedItems[0];


            InventoryFolder ifolders = client.Inventory.Store.RootFolder;
            List<InventoryBase> foundfolders = client.Inventory.Store.GetContents(ifolders);
            
            bool folderfound = false;
            UUID newfolder = UUID.Zero;

            foreach (var o in foundfolders.Where(
                         o => o.Name.ToLower(CultureInfo.CurrentCulture) 
                              == llitemname.Name.ToLower(CultureInfo.CurrentCulture)).OfType<InventoryFolder>())
            {
                folderfound = true;
                newfolder = o.UUID;
                break;
            }

            if (!folderfound)
            {
                newfolder = client.Inventory.CreateFolder(client.Inventory.Store.RootFolder.UUID, llitemname.Name);
            }

            client.Inventory.MoveTaskInventory(sPr.LocalID, llitem.UUID, newfolder, client.Network.CurrentSim);

            button4.PerformClick();
            button6.PerformClick();

            MessageBox.Show($"Item has been copied to {llitemname.Name} folder");
        }

        private void lbxTask_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                mnuTask.Show(lbxTask, new Point(e.X, e.Y));     
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            DeleteTaskItem();
        }

        private void mnuTask_Opening(object sender, CancelEventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                lbxPrims.SortByName = true;
                lbxPrims.SortList();
                instance.Config.CurrentConfig.SortByDistance = false;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                lbxPrims.location = instance.SIMsittingPos();
                lbxPrims.SortByName = false;
                lbxPrims.SortList();
                instance.Config.CurrentConfig.SortByDistance = true;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            UUID aID = (UUID)labelOwnerId.Text;

            (new frmProfile(instance, labelOwnerName.Text, aID)).Show();
        }

        private void btnReturn_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("Are you sure you want to return the selected object?", "MEGAbolt", MessageBoxButtons.YesNo);

            if (res == DialogResult.Yes)
            {
                int iDx = lbxPrims.SelectedIndex;
                ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

                Primitive sPr = new Primitive();
                sPr = item.Prim;

                //if (sPr.Properties.OwnerID != client.Self.AgentID) return;

                instance.MediaManager.PlayUISound(UISounds.ObjectDelete);
                client.Inventory.RequestDeRezToInventory(sPr.LocalID, DeRezDestination.ReturnToOwner, UUID.Zero, UUID.Random());
            }
        }

        private void btnView3D_Click(object sender, EventArgs e)
        {
            int iDx = lbxPrims.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];
            
            (new MEGA3D(instance, item)).Show();
        }

        private void picAutoSit_Click(object sender, EventArgs e)
        {
            //*FIXME: System.Diagnostics.Process.Start(@"http://www.metabolt.net/metawiki/ObjectManager.ashx");
        }

        private void picAutoSit_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show(picAutoSit);
        }

        private void picAutoSit_MouseLeave(object sender, EventArgs e)
        {
            toolTip1.Close();  
        }

        void Objects_Disposed(object sender, EventArgs e)
        {
            //GC.Collect();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            UUID aID = (UUID)label21.Text;

            (new frmProfile(instance, txtCreator.Text, aID)).Show();
        }

        private void txtSearch_Click(object sender, EventArgs e)
        {
            txtSearch.SelectionStart = 0;
            txtSearch.SelectionLength = txtSearch.Text.Length;
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            CopyTaskItem();
        }

        private void label11_TextChanged(object sender, EventArgs e)
        {
            //if (sloading) return;

            //int iDx = lbxPrims.SelectedIndex;

            //if (iDx < 0)
            //    return;

            //ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            //Primitive sPr = new Primitive();
            //sPr = item.Prim;

            //SetPerms(sPr);
        }

        private void labelDesc_TextChanged(object sender, EventArgs e)
        {
            if (sloading) return;

            txtDescChanged = true;
        }

        private void txtHover_TextChanged(object sender, EventArgs e)
        {
            if (sloading) return;

            txtNameChanged = true;
        }

        private void txtHover_Leave(object sender, EventArgs e)
        {
            if (sloading) return;

            if (txtNameChanged)
            {
                int iDx = lbxPrims.SelectedIndex;

                if (iDx < 0)
                    return;

                ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

                Primitive sPr = new Primitive();
                sPr = item.Prim;

                txtNameChanged = false;

                //SetPerms(sPr);

                client.Objects.SetName(client.Network.CurrentSim, sPr.LocalID, txtHover.Text);     
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Avatar sav = new Avatar();
            sav = client.Network.CurrentSim.ObjectsAvatars.Find(av => av.ID == client.Self.AgentID);

            (new WornAttachments(instance, sav)).Show(this);
        }

        private void lkLocation_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //LinkLabel.Link lk = e.Link;

            int iDx = lbxPrims.SelectedIndex;
            ObjectsListItem item = (ObjectsListItem)lbxPrims.Items[iDx];

            (new frmTeleport(instance, client.Network.CurrentSim.Name, item.Prim.Position.X, item.Prim.Position.Y, item.Prim.Position.Z, false)).Show();
        }
    }
}