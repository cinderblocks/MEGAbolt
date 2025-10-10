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

using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace MEGAbolt
{
    public partial class FindLand : UserControl
    {
        private readonly MEGAboltInstance instance;
        private readonly GridClient client;
        private float fX;
        private float fY;
        private float fZ;
        //private string sSIM;

        private readonly SafeDictionary<string, DirectoryManager.DirectoryParcel> findLandResults;
        //private DirectoryManager.DirectoryParcel EmptyPlace;

        public event EventHandler SelectedIndexChanged;
        private readonly NumericStringComparer lvwColumnSorter;

        public FindLand(MEGAboltInstance instance, UUID queryID)
        {
            InitializeComponent();

            findLandResults = new SafeDictionary<string, DirectoryManager.DirectoryParcel>();
            QueryID = queryID;

            this.instance = instance;
            client = this.instance.Client;

            AddClientEvents();

            lvwColumnSorter = new NumericStringComparer();
            lvwFindLand.ListViewItemSorter = lvwColumnSorter;
        }

        private void AddClientEvents()
        {
            client.Directory.DirLandReply += Directory_OnLandReply;
        }

        //Separate thread
        private void Directory_OnLandReply(object sender, DirLandReplyEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    Directory_OnLandReply(sender, e);
                }));

                return;
            }

            BeginInvoke(new MethodInvoker(() =>
            {
                LandReply(e.DirParcels);
            }));
        }

        private void LandReply(List<DirectoryManager.DirectoryParcel> matchedPlaces)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => LandReply(matchedPlaces)));
                return;
            }

            //if (queryID != this.queryID) return;

            lvwFindLand.BeginUpdate();

            var icnt = 0;

            foreach (var places in matchedPlaces)
            {
                try
                {
                    var fullName = places.Name;
                    var fx = findLandResults.ContainsKey(fullName);

                    if (!fx)
                    {
                        findLandResults.Add(fullName, places);
                    }
                    else
                    {
                        fullName += " (" + icnt.ToString(CultureInfo.CurrentCulture) + ")";
                        findLandResults.Add(fullName, places);
                    }

                    //ListViewItem item = lvwFindLand.Items.Add(fullName);
                    //item.SubItems.Add(places.ActualArea.ToString());
                    //item.SubItems.Add(places.SalePrice.ToString());

                    //double pricesqm = (Convert.ToDouble(places.SalePrice) / Convert.ToDouble(places.ActualArea));
                    //item.SubItems.Add(pricesqm.ToString("N2"));
                }
                catch
                {
                    ;
                }

                icnt += 1;
            }

            var items = from k in findLandResults.Keys
                        orderby (Convert.ToDouble(findLandResults[k].SalePrice) / Convert.ToDouble(findLandResults[k].ActualArea)) ascending
                        select k;

            foreach (var k in items)
            {
                var item = lvwFindLand.Items.Add(k);
                item.SubItems.Add(findLandResults[k].ActualArea.ToString(CultureInfo.CurrentCulture));
                item.SubItems.Add(findLandResults[k].SalePrice.ToString(CultureInfo.CurrentCulture));

                var pricesqm = (Convert.ToDouble(findLandResults[k].SalePrice) / Convert.ToDouble(findLandResults[k].ActualArea));
                item.SubItems.Add(pricesqm.ToString("N3", CultureInfo.CurrentCulture));
            }

            //lvwFindLand.Sort();
            lvwFindLand.EndUpdate();
            pLand.Visible = false;
        }

        // UI thread
        public void DisplayPlace(ParcelInfo place)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => DisplayPlace(place)));
                return;
            }

            if (place.Name == null)
                return;

            var sForSale = "";

            if (place.SalePrice > 0)
            {
                sForSale = "For Sale for L$" + place.SalePrice.ToString(CultureInfo.CurrentCulture);
            }

            txtName.Text = place.Name;

            txtDescription.Text = place.Description;
            txtInformation.Text = $"Traffic: {place.Dwell} Area: {place.ActualArea.ToString(CultureInfo.CurrentCulture)} sq. m. {sForSale}";
            chkMature.Checked = place.Mature;

            // Convert Global pos to local
            var locX = place.GlobalX; ;
            var locY = place.GlobalY;
            Helpers.GlobalPosToRegionHandle(locX, locY, out var locX1, out var locY1);

            fX = locX1;
            fY = locY1;
            fZ = place.GlobalZ;
            //sSIM = place.SimName;  

            txtLocation.Text = place.SimName.ToString(CultureInfo.CurrentCulture) + " " + fX.ToString(CultureInfo.CurrentCulture) + ", " + fY.ToString(CultureInfo.CurrentCulture) + ", " + fZ.ToString(CultureInfo.CurrentCulture);
        }

        public void ClearResults()
        {
            findLandResults.Clear();
            lvwFindLand.Items.Clear();
            button1.Enabled = false;
        }

        private void lvwFindLand_SelectedIndexChanged(object sender, EventArgs e)
        {
            OnSelectedIndexChanged(e);

            button1.Enabled = true;
        }

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UUID QueryID { get; set; }

        public int SelectedIndex
        {
            get
            {
                if (lvwFindLand.SelectedItems.Count == 0) return -1;

                return lvwFindLand.SelectedIndices[0];
            }
        }

        public DirectoryManager.DirectoryParcel SelectedName
        {
            get
            {
                var pcl = new DirectoryManager.DirectoryParcel
                {
                    ID = UUID.Zero,
                    Name = string.Empty
                };

                if (lvwFindLand.SelectedItems.Count == 0) return pcl;

                var name = lvwFindLand.SelectedItems[0].Text;
                return findLandResults[name];
            }
        }

        private void lvwFindLand_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
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

            // Perform the sort with these new sort options.
            lvwFindLand.Sort();
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (instance.State.IsSitting)
            {
                client.Self.Stand();
                instance.State.SetStanding();
            }

            var posn = new Vector3
            {
                X = fX,
                Y = fY,
                Z = fZ
            };

            var sLoc = txtLocation.Text;

            var deli = " ".ToCharArray();
            var iDets = sLoc.Split(deli);

            (new frmTeleport(instance, iDets[0], fX, fY, fZ, false)).Show();
        }
    }
}
