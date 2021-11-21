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
//using MEGAbolt.NetworkComm;

namespace MEGAbolt
{
    public partial class FindPeopleConsole : UserControl
    {
        private MEGAboltInstance instance;
        //private SLNetCom netcom;
        private GridClient client;

        public event EventHandler SelectedIndexChanged;
        private NumericStringComparer lvwColumnSorter;

        public FindPeopleConsole(MEGAboltInstance instance, UUID queryID)
        {
            InitializeComponent();

            LLUUIDs = new SafeDictionary<string, UUID>();
            QueryID = queryID;

            this.instance = instance;
            //netcom = this.instance.Netcom;
            client = this.instance.Client;
            AddClientEvents();

            lvwColumnSorter = new NumericStringComparer();
            lvwFindPeople.ListViewItemSorter = lvwColumnSorter;
        }

        private void AddClientEvents()
        {
            client.Directory.DirPeopleReply += Directory_OnDirPeopleReply;
        }

        //Separate thread
        private void Directory_OnDirPeopleReply(object sender, DirPeopleReplyEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate {
                PeopleReply(e.QueryID, e.MatchedPeople); 
            });
        }

        //UI thread
        private void PeopleReply(UUID qqueryID, List<DirectoryManager.AgentSearchData> matchedPeople)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    PeopleReply(qqueryID, matchedPeople);
                }));

                return;
            }

            if (qqueryID != QueryID) return;

            lvwFindPeople.BeginUpdate();

            foreach (DirectoryManager.AgentSearchData person in matchedPeople)
            {
                string fullName = person.FirstName + " " + person.LastName;

                if (!LLUUIDs.ContainsKey(fullName))
                {
                    LLUUIDs.Add(fullName, person.AgentID);
                }

                ListViewItem item = lvwFindPeople.Items.Add(fullName);
                item.SubItems.Add(person.Online ? "Yes" : "No");
            }

            lvwFindPeople.Sort();
            lvwFindPeople.EndUpdate();
            pPeople.Visible = false;  
        }

        public void ClearResults()
        {
            LLUUIDs.Clear();
            lvwFindPeople.Items.Clear();
        }

        private void lvwFindPeople_SelectedIndexChanged(object sender, EventArgs e)
        {
            OnSelectedIndexChanged(e);
        }

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            if (SelectedIndexChanged != null) SelectedIndexChanged(this, e);
        }

        public SafeDictionary<string, UUID> LLUUIDs { get; }

        public UUID QueryID { get; set; }

        public int SelectedIndex
        {
            get
            {
                if (lvwFindPeople.SelectedItems == null) return -1;
                if (lvwFindPeople.SelectedItems.Count == 0) return -1;

                return lvwFindPeople.SelectedIndices[0];
            }
        }

        public string SelectedName
        {
            get
            {
                if (lvwFindPeople.SelectedItems == null) return string.Empty;
                if (lvwFindPeople.SelectedItems.Count == 0) return string.Empty;

                return lvwFindPeople.SelectedItems[0].Text;
            }
        }

        public bool SelectedOnlineStatus
        {
            get
            {
                if (lvwFindPeople.SelectedItems == null) return false;
                if (lvwFindPeople.SelectedItems.Count == 0) return false;

                string yesNo = lvwFindPeople.SelectedItems[0].SubItems[0].Text;

                if (yesNo == "Yes")
                    return true;
                else if (yesNo == "No")
                    return false;
                else
                    return false;
            }
        }

        public UUID SelectedAgentUUID
        {
            get
            {
                if (lvwFindPeople.SelectedItems == null) return UUID.Zero;
                if (lvwFindPeople.SelectedItems.Count == 0) return UUID.Zero;

                string name = lvwFindPeople.SelectedItems[0].Text;
                return LLUUIDs[name];
            }
        }

        private void pPeople_Click(object sender, EventArgs e)
        {

        }

        private void lvwFindPeople_ColumnClick(object sender, ColumnClickEventArgs e)
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

            // Perform the sort with these new sort options.
            lvwFindPeople.Sort();
        }
    }
}
