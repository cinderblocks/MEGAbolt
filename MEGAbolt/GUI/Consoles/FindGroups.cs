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


namespace MEGAbolt
{
    public partial class FindGroups : UserControl
    {
        private readonly MEGAboltInstance instance;
        private readonly GridClient client;

        public event EventHandler SelectedIndexChanged;
        private readonly NumericStringComparer lvwColumnSorter;

        public FindGroups(MEGAboltInstance instance, UUID queryID)
        {
            InitializeComponent();

            LLUUIDs = new SafeDictionary<string, UUID>();
            QueryID = queryID;

            this.instance = instance;
            client = this.instance.Client;
            AddClientEvents();

            lvwColumnSorter = new NumericStringComparer();
            lvwFindGroups.ListViewItemSorter = lvwColumnSorter;
        }

        private void AddClientEvents()
        {
            client.Directory.DirGroupsReply += Directory_OnDirGroupsReply;   
        }

        private void Directory_OnDirGroupsReply(object sender, DirGroupsReplyEventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate
            {
                GroupsReply(e.QueryID, e.MatchedGroups);
            });
        }

        private void GroupsReply(UUID qqueryID, List<DirectoryManager.GroupSearchData> matchedGroups)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    GroupsReply(qqueryID, matchedGroups);
                }));

                return;
            }

            if (qqueryID != QueryID) return;

            lvwFindGroups.BeginUpdate();

            foreach (DirectoryManager.GroupSearchData group in matchedGroups)
            {
                if (!LLUUIDs.ContainsKey(group.GroupName))
                {
                    LLUUIDs.Add(group.GroupName, group.GroupID);
                }

                ListViewItem item = lvwFindGroups.Items.Add(group.GroupName);
                item.Tag = group.GroupID;
                item.SubItems.Add("Total " + group.Members.ToString(CultureInfo.CurrentCulture) + " members");
            }

            lvwFindGroups.Sort();
            lvwFindGroups.EndUpdate();
            pGroups.Visible = false;
        }

        public void ClearResults()
        {
            LLUUIDs.Clear();
            lvwFindGroups.Items.Clear();
        }

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
        }

        private void lvwFindGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            OnSelectedIndexChanged(e);
        }

        public SafeDictionary<string, UUID> LLUUIDs { get; }

        public UUID QueryID { get; set; }

        public int SelectedIndex
        {
            get
            {
                if (lvwFindGroups.SelectedItems.Count == 0) return -1;

                return lvwFindGroups.SelectedIndices[0];
            }
        }

        public string SelectedName => lvwFindGroups.SelectedItems.Count == 0 
            ? string.Empty : lvwFindGroups.SelectedItems[0].Text;

        public UUID SelectedGroupUUID
        {
            get
            {
                if (lvwFindGroups.SelectedItems.Count == 0) return UUID.Zero;

                return (UUID)lvwFindGroups.SelectedItems[0].Tag;
            }
        }

        private void pGroups_Click(object sender, EventArgs e)
        {

        }

        private void lvwFindGroups_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                lvwColumnSorter.Order = lvwColumnSorter.Order == SortOrder.Ascending 
                    ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            lvwFindGroups.Sort(); 
        }
    }
}
