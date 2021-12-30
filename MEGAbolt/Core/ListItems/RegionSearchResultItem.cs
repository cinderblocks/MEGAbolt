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
using System.Windows.Forms;
using OpenMetaverse;
using OpenMetaverse.Assets;

namespace MEGAbolt
{
    public class RegionSearchResultItem
    {
        private MEGAboltInstance instance;
        //private SLNetCom netcom;
        private GridClient client;

        private GridRegion region;

        private ListBox listBox;

        private BackgroundWorker agentCountWorker;

        public RegionSearchResultItem(MEGAboltInstance instance, GridRegion region, ListBox listBox)
        {
            this.instance = instance;
            //netcom = this.instance.Netcom;
            client = this.instance.Client;
            this.region = region;
            this.listBox = listBox;

            agentCountWorker = new BackgroundWorker();
            agentCountWorker.DoWork += agentCountWorker_DoWork;
            agentCountWorker.RunWorkerCompleted += agentCountWorker_RunWorkerCompleted;

            AddClientEvents();
        }

        private void agentCountWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<MapItem> items =
                client.Grid.MapItems(
                    region.RegionHandle,
                    OpenMetaverse.GridItemType.AgentLocations,
                    GridLayerType.Terrain, 500);

            if (items != null)
            {
                e.Result = (byte)items.Count;
                AgentLocations = items; 
            }
        }

        private void agentCountWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            GettingAgentCount = false;
            GotAgentCount = true;

            if (e.Result != null)
            {
                region.Agents = (byte)e.Result;
            }

            RefreshListBox();
        }

        private void AddClientEvents()
        {
            //client.Assets.OnImageReceived += new AssetManager.ImageReceivedCallback(Assets_OnImageReceived);
        }

        //Separate thread
        private void Assets_OnImageReceived(TextureRequestState image, AssetTexture texture)
        {
            if (texture.AssetID != region.MapImageID) return;
            if (texture.AssetData == null) return;

            MapImage = ImageHelper.Decode(texture.AssetData);
            if (MapImage == null) return;

            instance.ImageCache.AddImage(texture.AssetID, MapImage);

            IsImageDownloading = false;
            IsImageDownloaded = true;
            listBox.BeginInvoke(new MethodInvoker(RefreshListBox));
            listBox.BeginInvoke(new OnMapImageRaise(OnMapImageDownloaded), new object[] { EventArgs.Empty });
        }

        //UI thread
        private void RefreshListBox()
        {
            listBox.Refresh();
        }

        public void RequestMapImage(float priority)
        {
            if (region.MapImageID == UUID.Zero)
            {
                IsImageDownloaded = true;
                OnMapImageDownloaded(EventArgs.Empty);
                return;
            }

            if (instance.ImageCache.ContainsImage(region.MapImageID))
            {
                MapImage = instance.ImageCache.GetImage(region.MapImageID);
                IsImageDownloaded = true;
                OnMapImageDownloaded(EventArgs.Empty);
                RefreshListBox();
            }
            else
            {
                client.Assets.RequestImage(region.MapImageID, ImageType.Normal, Assets_OnImageReceived); //, priority, 0);
                IsImageDownloading = true;
            }
        }

        public void RequestAgentLocations()
        {
            GettingAgentCount = true;
            agentCountWorker.RunWorkerAsync();
        }

        public override string ToString()
        {
            return region.Name;
        }

        public event EventHandler MapImageDownloaded;
        private delegate void OnMapImageRaise(EventArgs e);
        protected virtual void OnMapImageDownloaded(EventArgs e)
        {
            MapImageDownloaded?.Invoke(this, e);
        }

        public GridRegion Region => region;

        public System.Drawing.Image MapImage { get; private set; }

        public bool IsImageDownloaded { get; private set; } = false;

        public bool IsImageDownloading { get; private set; } = false;

        public bool GettingAgentCount { get; private set; } = false;

        public bool GotAgentCount { get; private set; } = false;

        public int ListIndex { get; set; }

        public List<MapItem> AgentLocations { get; private set; } = new List<MapItem>();
    }
}
