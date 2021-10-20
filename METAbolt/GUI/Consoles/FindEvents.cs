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
using System.Windows.Forms;
using OpenMetaverse;
using System.Globalization;

namespace METAbolt
{
    public partial class FindEvents : UserControl
    {
        private METAboltInstance instance;
        //private SLNetCom netcom;
        private GridClient client;
        private float fX;
        private float fY;
        private float fZ;

        public event EventHandler SelectedIndexChanged;
        private NumericStringComparer lvwColumnSorter;

        public FindEvents(METAboltInstance instance, UUID queryID)
        {
            InitializeComponent();

            LLUUIDs = new SafeDictionary<string, uint>();
            QueryID = queryID;

            this.instance = instance;
            //netcom = this.instance.Netcom;
            client = this.instance.Client;
            AddClientEvents();

            lvwColumnSorter = new NumericStringComparer();
            lvwFindEvents.ListViewItemSorter = lvwColumnSorter;
        }

        private void AddClientEvents()
        {
            client.Directory.DirEventsReply += Directory_OnEventsReply;
            client.Directory.EventInfoReply += eventsconsole_OnEventInfo;
            
        }

        //Separate thread
        private void Directory_OnEventsReply(object sender, DirEventsReplyEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate
            {
                EventsReply(e.QueryID, e.MatchedEvents);
            });
        }

        // Separate thread
        private void eventsconsole_OnEventInfo(object sender, EventInfoReplyEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate
            {
                EventInf(e.MatchedEvent);
            });
        }

        // UI thread
        private void EventInf(DirectoryManager.EventInfo matchedEvent)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    EventInf(matchedEvent);
                }));

                return;
            }

            textBox7.Text = matchedEvent.Creator.ToString();
            textBox2.Text = matchedEvent.Name.ToString();
            textBox3.Text = DirectoryManager.EventCategories.All.ToString();   // matchedEvent.Category.ToString();

            if (matchedEvent.Duration > 59)
            {
                uint dur = matchedEvent.Duration/60;
                textBox5.Text = dur.ToString(CultureInfo.CurrentCulture) + " hours"; 
            }
            else
            {
                textBox5.Text = matchedEvent.Duration.ToString(CultureInfo.CurrentCulture) + " minutes";
            }
            textBox6.Text = matchedEvent.Date.ToString();

            // Get region handle
            //ulong regionhand =Helpers.UIntsToLong((uint)(matchedEvent.GlobalPos.X - (matchedEvent.GlobalPos.X % 256)), (uint)(matchedEvent.GlobalPos.Y - (matchedEvent.GlobalPos.Y % 256)));
            
            // Convert Global pos to local
            float locX = (float)matchedEvent.GlobalPos.X; ;
            float locY = (float)matchedEvent.GlobalPos.Y;
            float locX1;
            float locY1;
            Helpers.GlobalPosToRegionHandle(locX, locY, out locX1, out locY1); 

            fX = locX1;
            fY = locY1;
            fZ = (float)matchedEvent.GlobalPos.Z;

            textBox8.Text = matchedEvent.SimName.ToString(CultureInfo.CurrentCulture) + "/" + fX.ToString(CultureInfo.CurrentCulture) + "/" + fY.ToString(CultureInfo.CurrentCulture) + "/" + fZ.ToString(CultureInfo.CurrentCulture);

            if (matchedEvent.Cover == 0)
            {
                textBox9.Text = "none";
            }
            else
            {
                textBox9.Text = "L$ " + matchedEvent.Cover.ToString(CultureInfo.CurrentCulture);
            }

            textBox1.Text = matchedEvent.Desc.ToString();
        }

        //UI thread
        private void EventsReply(UUID qqueryID, List<DirectoryManager.EventsSearchData> matchedEvents)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    EventsReply(qqueryID, matchedEvents);
                }));

                return;
            }

            if (qqueryID != QueryID) return;

            lvwFindEvents.BeginUpdate();

            int icnt = 0;

            foreach (DirectoryManager.EventsSearchData  events in matchedEvents)
            {
                try
                {
                    string fullName = events.Name;
                    bool fx = false;

                    if (LLUUIDs.ContainsKey(fullName))
                    {
                        fx = true;
                    }

                    if (!fx)
                    {
                        LLUUIDs.Add(fullName, events.ID);
                    }
                    else
                    {
                        fullName += " (" + icnt.ToString(CultureInfo.CurrentCulture) + ")";
                        LLUUIDs.Add(fullName, events.ID);
                    }

                    ListViewItem item = lvwFindEvents.Items.Add(fullName);
                    item.SubItems.Add(events.Date);   // + "-" + events.Time);
                }
                catch
                {
                    ; 
                }

                icnt += 1;
            }
 
            lvwFindEvents.Sort();
            lvwFindEvents.EndUpdate();
            pEvents.Visible = false; 
        }

        public void ClearResults()
        {
            LLUUIDs.Clear();
            lvwFindEvents.Items.Clear();
            button1.Enabled = false;
            button2.Enabled = false;
        }

        private void lvwFindEvents_SelectedIndexChanged(object sender, EventArgs e)
        {
            OnSelectedIndexChanged(e);

            button1.Enabled = true;
            button2.Enabled = true;  
        }

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            if (SelectedIndexChanged != null) SelectedIndexChanged(this, e);
        }

        public SafeDictionary<string, uint> LLUUIDs { get; }

        public UUID QueryID { get; set; }

        public int SelectedIndex
        {
            get
            {
                if (lvwFindEvents.SelectedItems == null) return -1;
                if (lvwFindEvents.SelectedItems.Count == 0) return -1;

                return lvwFindEvents.SelectedIndices[0];
            }
        }

        public string SelectedName
        {
            get
            {
                if (lvwFindEvents.SelectedItems == null) return string.Empty;
                if (lvwFindEvents.SelectedItems.Count == 0) return string.Empty;

                return lvwFindEvents.SelectedItems[0].Text;
            }
        }

        public string SelectedTime
        {
            get
            {
                if (lvwFindEvents.SelectedItems == null) return "";
                if (lvwFindEvents.SelectedItems.Count == 0) return "";

                string sTime = lvwFindEvents.SelectedItems[0].SubItems[0].Text;

                return sTime;
            }
        }

        public uint SelectedEventUUID
        {
            get
            {
                if (lvwFindEvents.SelectedItems == null) return 0;
                if (lvwFindEvents.SelectedItems.Count == 0) return 0;

                string name = lvwFindEvents.SelectedItems[0].Text;
                return LLUUIDs[name];
            }
        }

        private void FindEvents_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"http://secondlife.com/events/");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string sLoc = textBox8.Text;

            char[] deli = "/".ToCharArray();
            string[] iDets = sLoc.Split(deli);

            //netcom.Teleport(iDets[0],
            //client.Self.Teleport(   

            (new frmTeleport(instance, iDets[0].ToString(), fX, fY, fZ, false)).Show();
        }

        private void lvwFindEvents_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            lvwFindEvents.Sort();
        }    
    }
}