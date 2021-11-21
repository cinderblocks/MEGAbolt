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
using System.Data;
using System.Windows.Forms;
using OpenMetaverse;
//using MEGAbolt.NetworkComm;
using System.Linq;
using System.Globalization;

namespace MEGAbolt
{
    public partial class FindLand : UserControl
    {
        private MEGAboltInstance instance;
        //private SLNetCom netcom;
        private GridClient client;
        private float fX;
        private float fY;
        private float fZ;
        //private string sSIM;

        private SafeDictionary<string, DirectoryManager.DirectoryParcel> findLandResults;
        //private DirectoryManager.DirectoryParcel EmptyPlace;

        public event EventHandler SelectedIndexChanged;
        private NumericStringComparer lvwColumnSorter;

        public FindLand(MEGAboltInstance instance, UUID queryID)
        {
            InitializeComponent();

            findLandResults = new SafeDictionary<string, DirectoryManager.DirectoryParcel>();
            QueryID = queryID;

            this.instance = instance;
            //netcom = this.instance.Netcom;
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
                BeginInvoke(new MethodInvoker(delegate()
                {
                    Directory_OnLandReply(sender, e);
                }));

                return;
            }

            BeginInvoke(new MethodInvoker(delegate()
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

            int icnt = 0;

            foreach (DirectoryManager.DirectoryParcel places in matchedPlaces)
            {
                try
                {
                    string fullName = places.Name;
                    bool fx = false;

                    if (findLandResults.ContainsKey(fullName))
                    {
                        //DirectoryManager.DirectoryParcel pcl = findLandResults[fullName];
                        fx = true; 
                    }

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

            foreach (string k in items)
            {
                ListViewItem item = lvwFindLand.Items.Add(k);
                item.SubItems.Add(findLandResults[k].ActualArea.ToString(CultureInfo.CurrentCulture));
                item.SubItems.Add(findLandResults[k].SalePrice.ToString(CultureInfo.CurrentCulture));

                double pricesqm = (Convert.ToDouble(findLandResults[k].SalePrice) / Convert.ToDouble(findLandResults[k].ActualArea));
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

            string sForSale = "";

            if (place.SalePrice > 0)
            {
                sForSale = "For Sale for L$" + place.SalePrice.ToString(CultureInfo.CurrentCulture);   
            }

            txtName.Text = place.Name;

            txtDescription.Text = place.Description;
            txtInformation.Text = "Traffic: " + place.Dwell + " Area: " + place.ActualArea.ToString(CultureInfo.CurrentCulture) + " sq. m. " + sForSale;
            chkMature.Checked = place.Mature;   

            // Convert Global pos to local
            float locX = (float)place.GlobalX; ;
            float locY = (float)place.GlobalY;
            float locX1;
            float locY1;
            Helpers.GlobalPosToRegionHandle(locX, locY, out locX1, out locY1);

            fX = locX1;
            fY = locY1;
            fZ = (float)place.GlobalZ;
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
            if (SelectedIndexChanged != null) SelectedIndexChanged(this, e);
        }


        public UUID QueryID { get; set; }

        public int SelectedIndex
        {
            get
            {
                if (lvwFindLand.SelectedItems == null) return -1;
                if (lvwFindLand.SelectedItems.Count == 0) return -1;

                return lvwFindLand.SelectedIndices[0];
            }
        }

        public DirectoryManager.DirectoryParcel SelectedName
        {
            get
            {
                DirectoryManager.DirectoryParcel pcl = new DirectoryManager.DirectoryParcel
                {
                    ID = UUID.Zero,
                    Name = string.Empty
                };

                if (lvwFindLand.SelectedItems == null) return pcl;
                if (lvwFindLand.SelectedItems.Count == 0) return pcl;

                string name = lvwFindLand.SelectedItems[0].Text;
                return findLandResults[name];
            }
        }

        private void lvwFindLand_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
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

            Vector3 posn = new Vector3
            {
                X = fX,
                Y = fY,
                Z = fZ
            };

            string sLoc = txtLocation.Text;

            char[] deli = " ".ToCharArray();
            string[] iDets = sLoc.Split(deli);

            (new frmTeleport(instance, iDets[0].ToString(), fX, fY, fZ, false)).Show();   
        }
    }
}
