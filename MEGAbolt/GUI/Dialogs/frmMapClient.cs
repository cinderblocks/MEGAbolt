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
using MEGAbolt.NetworkComm;
using MEGAbolt.Controls;
using OpenMetaverse.Assets;
using System.Threading;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;
using CSJ2K;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

/* Some of this code has been borrowed from the libsecondlife GUI */

namespace MEGAbolt
{
    public partial class frmMapClient : Form
    {
        private MEGAboltInstance instance;
        private GridClient client; // = new GridClient();
        private MEGAboltNetcom netcom;
        private UUID _MapImageID;
        private Image _MapLayer;
        private Image _LandLayer;
        private int px = 128;
        private int py = 128;
        private Simulator sim;

        private Popup toolTip;
        private CustomToolTip customToolTip;
        private bool showing = false;
        private UUID avuuid = UUID.Zero;
        private string avname = string.Empty;
        private ManualResetEvent TPEvent = new ManualResetEvent(false);
        private int clickedx = 0;
        private int clickedy = 0;
        private GridRegion selregion;
        private Image selectedmap;
        private bool mloaded = false;
        private Image orgmap;

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

        public frmMapClient(MEGAboltInstance instance)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            netcom = this.instance.Netcom;

            client = this.instance.Client;
            sim = client.Network.CurrentSim;

            client.Grid.CoarseLocationUpdate += Grid_OnCoarseLocationUpdate;
            client.Network.SimChanged += Network_OnCurrentSimChanged;

            client.Grid.GridRegion += Grid_OnGridRegion;
            netcom.Teleporting += netcom_Teleporting;
            netcom.TeleportStatusChanged += netcom_TeleportStatusChanged;

            string msg1 = "Yellow dot with red border = your avatar \nGreen dots = avs at your altitude\nRed squares = avs 20m+ below you\nBlue squares = avs 20m+ above you\n\n Click on map area to get TP position.";
            toolTip = new Popup(customToolTip = new CustomToolTip(instance, msg1));
            toolTip.AutoClose = false;
            toolTip.FocusOnOpen = false;
            toolTip.ShowingAnimation = toolTip.HidingAnimation = PopupAnimations.Blend;

            //List<AvLocation> avlocations = new List<AvLocation>();

            world.Cursor = Cursors.Cross;
            pictureBox2.Cursor = Cursors.Cross;
        }

        private void Assets_OnImageReceived(TextureRequestState image, AssetTexture texture)
        {
            if (texture.AssetID != _MapImageID) { return; }

            using (var bitmap = J2kImage.FromBytes(texture.AssetData).As<SKBitmap>())
            {
                _MapLayer = bitmap.ToBitmap();
            }
                
            BeginInvoke((MethodInvoker)delegate { UpdateMiniMap(sim); });
        }

        private void Network_OnCurrentSimChanged(object sender, SimChangedEventArgs e)
        {
            //GetMap();

            BeginInvoke(new MethodInvoker(() =>
            {
                if (chkForSale.Checked)
                {
                    chkForSale.Checked = false;
                }
            }));

            //_LandLayer = null;
            //_MapLayer = null;
            //client.Grid.RequestMapRegion(client.Network.CurrentSim.Name, GridLayerType.Objects);

            BeginInvoke((MethodInvoker)GetMap);
        }

        private void GetMap()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(GetMap));

                //BeginInvoke((MethodInvoker)delegate { GetMap(); });
                return;
            }

            GridRegion region;
            //List<Simulator> connectedsims = client.Network.Simulators;

            if (_MapLayer == null || sim != client.Network.CurrentSim)
            {
                sim = client.Network.CurrentSim;
                TabCont.TabPages[0].Text = client.Network.CurrentSim.Name;

                if (!chkForSale.Checked)
                {
                    if (client.Grid.GetGridRegion(client.Network.CurrentSim.Name, GridLayerType.Objects, out region))
                    {
                        _MapImageID = region.MapImageID;
                        client.Assets.RequestImage(_MapImageID, ImageType.Baked, Assets_OnImageReceived);
                    }
                }
                else
                {
                    if (client.Grid.GetGridRegion(client.Network.CurrentSim.Name, GridLayerType.LandForSale, out region))
                    {
                        _MapImageID = region.MapImageID;
                        client.Assets.RequestImage(_MapImageID, ImageType.Baked, Assets_OnImageReceived);
                    }
                }
            }
            else
            {
                //UpdateMiniMap(sim);
                BeginInvoke(new OnUpdateMiniMap(UpdateMiniMap), new object[] { sim });
            }
        }

        private delegate void OnUpdateMiniMap(Simulator ssim);
        private void UpdateMiniMap(Simulator ssim)
        {
            if (InvokeRequired) BeginInvoke((MethodInvoker)delegate { UpdateMiniMap(ssim); });
            else
            {
                sim = ssim;

                if (sim != client.Network.CurrentSim) return;

                //Bitmap nbmp = new Bitmap(256, 256);

                Bitmap bmp = _MapLayer == null ? new Bitmap(256, 256) : (Bitmap)_MapLayer.Clone();

                Graphics g = Graphics.FromImage(bmp);

                //nbmp.Dispose(); 

                if (_MapLayer == null)
                {
                    g.Clear(BackColor);
                    g.FillRectangle(Brushes.White, 0f, 0f, 256f, 256f);
                    lblDownloading.Visible = true;
                }
                else
                {
                    lblDownloading.Visible = false;
                }

                if (_LandLayer != null)
                {
                    //nbmp = new Bitmap(256, 256);

                    bmp = _LandLayer == null ? new Bitmap(256, 256) : (Bitmap)_LandLayer.Clone();
                    //g = Graphics.FromImage((Bitmap)_LandLayer.Clone());

                    g = Graphics.FromImage(bmp);

                    //nbmp.Dispose(); 

                    //ColorMatrix cm = new ColorMatrix();
                    //cm.Matrix00 = cm.Matrix11 = cm.Matrix22 = cm.Matrix44 = 1f;
                    //cm.Matrix33 = 1.0f;

                    //ImageAttributes ia = new ImageAttributes();
                    //ia.SetColorMatrix(cm);

                    if (_MapLayer != null)
                    {
                        g.DrawImage(_MapLayer, new Rectangle(0, 0, _MapLayer.Width, _MapLayer.Height), 0, 0, _MapLayer.Width, _MapLayer.Height, GraphicsUnit.Pixel);   //, ia);
                    }
                }

                // Draw compass points
                StringFormat strFormat = new StringFormat();
                strFormat.Alignment = StringAlignment.Center;

                g.DrawString("N", new Font("Arial", 12), Brushes.Black, new RectangleF(0, 0, bmp.Width, bmp.Height), strFormat);
                g.DrawString("N", new Font("Arial", 9, FontStyle.Bold), Brushes.White, new RectangleF(0, 2, bmp.Width, bmp.Height), strFormat);

                strFormat.LineAlignment = StringAlignment.Center;
                strFormat.Alignment = StringAlignment.Near;

                g.DrawString("W", new Font("Arial", 12), Brushes.Black, new RectangleF(0, 0, bmp.Width, bmp.Height), strFormat);
                g.DrawString("W", new Font("Arial", 9, FontStyle.Bold), Brushes.White, new RectangleF(2, 0, bmp.Width, bmp.Height), strFormat);

                strFormat.LineAlignment = StringAlignment.Center;
                strFormat.Alignment = StringAlignment.Far;

                g.DrawString("E", new Font("Arial", 12), Brushes.Black, new RectangleF(0, 0, bmp.Width, bmp.Height), strFormat);
                g.DrawString("E", new Font("Arial", 9, FontStyle.Bold), Brushes.White, new RectangleF(-2, 0, bmp.Width, bmp.Height), strFormat);

                strFormat.LineAlignment = StringAlignment.Far;
                strFormat.Alignment = StringAlignment.Center;

                g.DrawString("S", new Font("Arial", 12), Brushes.Black, new RectangleF(0, 0, bmp.Width, bmp.Height), strFormat);
                g.DrawString("S", new Font("Arial", 9, FontStyle.Bold), Brushes.White, new RectangleF(0, 0, bmp.Width, bmp.Height), strFormat);

                // V0.9.8.0 changes for OpenSIM compatibility
                Vector3 myPos = new Vector3();

                // Rollback change from 9.2.1
                //if (!sim.AvatarPositions.ContainsKey(client.Self.AgentID))
                //{
                //    myPos = instance.SIMsittingPos();
                //}
                //else
                //{
                //    myPos = sim.AvatarPositions[client.Self.AgentID];
                //}

                myPos = instance.SIMsittingPos();

                // Draw self position
                int rg = instance.Config.CurrentConfig.RadarRange;

                if (chkRange.Checked)
                {
                    rg *= 2;

                    Rectangle myrect = new Rectangle(((int)Math.Round(myPos.X, 0)) - (rg / 2), (255 - ((int)Math.Round(myPos.Y, 0))) - (rg / 2 - 4), rg + 2, rg + 2);
                    SolidBrush semiTransBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 255));
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.GammaCorrected;
                    g.FillEllipse(semiTransBrush, myrect);

                    myrect = new Rectangle(((int)Math.Round(myPos.X, 0)) - (rg / 4), (255 - ((int)Math.Round(myPos.Y, 0))) - (rg / 4 - 4), rg / 2 + 2, rg / 2 + 2);
                    //semiTransBrush = new SolidBrush(Color.FromArgb(128, 0, 245, 225));
                    g.DrawEllipse(new Pen(Color.Blue, 1), myrect);

                    myrect = new Rectangle((int)Math.Round(myPos.X, 0) - 2, 255 - ((int)Math.Round(myPos.Y, 0) - 2), 7, 7);
                    g.FillEllipse(new SolidBrush(Color.Yellow), myrect);
                    g.DrawEllipse(new Pen(Brushes.Red, 3), myrect);
                }
                else
                {
                    Rectangle myrect = new Rectangle((int)Math.Round(myPos.X, 0) - 2, 255 - ((int)Math.Round(myPos.Y, 0) - 2), 7, 7);
                    g.FillEllipse(new SolidBrush(Color.Yellow), myrect);
                    g.DrawEllipse(new Pen(Brushes.Red, 3), myrect);
                }

                if (clickedx != 0 && clickedy != 0)
                {
                    Point mouse = new Point(clickedx, clickedy);

                    MEGAboltInstance.AvLocation CurrentLoc = null;

                    try
                    {
                        CurrentLoc = instance.avlocations.Find(gck => gck.Rectangle.Contains(mouse) == true);
                    }
                    catch { ; }

                    if (CurrentLoc == null)
                    {
                        Rectangle selectedrect = new Rectangle(clickedx - 2, clickedy - 2, 10, 10);
                        g.DrawEllipse(new Pen(Brushes.Red, 2), selectedrect);
                    }
                }

                if (chkResident.Checked)
                {
                    int i = 0;
                    Rectangle rect = new Rectangle();


                    if (myPos.Z < 0.1f)
                    {
                        myPos.Z = 1020f;   // Convert.ToSingle(client.Self.GlobalPosition.Z);    //1024f;
                    }

                    client.Network.CurrentSim.AvatarPositions.ForEach(
                        pos =>
                        {
                            int x = (int)pos.Value.X - 2;
                            int y = 255 - (int)pos.Value.Y - 2;

                            rect = new Rectangle(x, y, 7, 7);

                            Vector3 oavPos = new Vector3(0, 0, 0)
                            {
                                X = pos.Value.X,
                                Y = pos.Value.Y,
                                Z = pos.Value.Z
                            };

                            if (oavPos.Z < 0.1f)
                            {
                                oavPos.Z = 1020f;
                            }

                            if (pos.Key != client.Self.AgentID)
                            {
                                if (myPos.Z - oavPos.Z > 20)
                                {
                                    g.FillRectangle(Brushes.DarkRed, rect);
                                    g.DrawRectangle(new Pen(Brushes.Red, 1), rect);
                                }
                                else if (myPos.Z - oavPos.Z > -11 && myPos.Z - oavPos.Z < 11)
                                {
                                    g.FillEllipse(Brushes.LightGreen, rect);
                                    g.DrawEllipse(new Pen(Brushes.Green, 1), rect);
                                }
                                else
                                {
                                    g.FillRectangle(Brushes.MediumBlue, rect);
                                    g.DrawRectangle(new Pen(Brushes.Red, 1), rect);
                                }
                            }

                            i++;
                        }
                    );
                }

                g.DrawImage(bmp, 0, 0);                

                world.Image = bmp;

                strFormat.Dispose(); 
                g.Dispose();

                string strInfo = string.Format(CultureInfo.CurrentCulture, "Total Avatars: {0}", client.Network.CurrentSim.AvatarPositions.Count);
                lblSimData.Text = strInfo;

                strInfo = string.Format(CultureInfo.CurrentCulture, "{0}/{1}/{2}/{3}", client.Network.CurrentSim.Name,
                                                                            Math.Round(myPos.X, 0),
                                                                            Math.Round(myPos.Y, 0),
                                                                            Math.Round(myPos.Z, 0));
                lblSlurl.Text = "http://slurl.com/secondlife/" + strInfo;
            }
        }

        private void Grid_OnCoarseLocationUpdate(object sender, CoarseLocationUpdateEventArgs e)
        {
            try
            {
                //UpdateMiniMap(sim);
                BeginInvoke((MethodInvoker)delegate { UpdateMiniMap(e.Simulator); });
            }
            catch { ; }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmMapClient_Load(object sender, EventArgs e)
        {
            CenterToParent();

            GetMap();

            Vector3 apos = new Vector3();
            apos = instance.SIMsittingPos();
            float aZ = apos.Z;

            //printMap();
            nuX.Value = 128;
            nuY.Value = 128;
            nuZ.Value = (decimal)aZ;

            chkForSale.Checked = true;
            chkForSale.Checked = false;

            chkRange.Checked = !instance.Config.CurrentConfig.DisableRadarImageMiniMap;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (instance.State.IsSitting)
            {
                client.Self.Stand();
                instance.State.SetStanding();
                TPEvent.WaitOne(2000, false);
            }

            clickedx = 0;
            clickedy = 0;

            btnClearMarker.Enabled = false;

            try
            {
                //netcom.Teleport(client.Network.CurrentSim.Name, new Vector3((float)nuX.Value, (float)nuY.Value, (float)nuZ.Value));
                client.Self.Teleport(client.Network.CurrentSim.Name, new Vector3((float)nuX.Value, (float)nuY.Value, (float)nuZ.Value));
            }
            catch
            {
                MessageBox.Show("An error occured while Teleporting. \n Please re-try later.", "MEGAbolt");
                return;
            }
        }

        private void world_MouseUp(object sender, MouseEventArgs e)
        {
            px = NormaliseSize(e.X);   // Convert.ToInt32(Math.Round(e.X * ssize));
            py = NormaliseSize(255 - e.Y);   // Convert.ToInt32(Math.Round(e.Y * ssize));

            nuX.Value = px;
            nuY.Value = py;
            nuZ.Value = 10;

            clickedx = px;   // NormaliseSize(e.X);
            clickedy = py;   // NormaliseSize(e.Y); 

            //PlotSelected(e.X, e.Y);

            Point mouse = new Point(clickedx, clickedy);

            MEGAboltInstance.AvLocation CurrentLoc = null;

            btnClearMarker.Enabled = true; 

            try
            {
                CurrentLoc = instance.avlocations.Find(g => g.Rectangle.Contains(mouse) == true);
            }
            catch { ; }

            if (CurrentLoc != null)
            {
                (new frmProfile(instance, avname, avuuid)).Show();
            }
            else
            {
                PlotSelected(e.X, e.Y);
            }
        }

        private int NormaliseSize(int number)
        {
            //decimal ssize = (decimal)256 / (decimal)tabPage1.Width;
            decimal ssize = 256 / (decimal)world.Width;

            int pos = Convert.ToInt32(Math.Round(number * ssize));

            return pos;
        }

        private void PlotSelected(int x, int y)
        {
            if (world.Image == null) return;

            try
            {
                //UpdateMiniMap(sim);
                BeginInvoke(new OnUpdateMiniMap(UpdateMiniMap), new object[] { sim });

                Bitmap map = (Bitmap)world.Image;
                Graphics g = Graphics.FromImage(map);

                Rectangle selectedrect = new Rectangle(x - 2, y - 2, 10, 10);
                g.DrawEllipse(new Pen(Brushes.Red, 2), selectedrect);
                world.Image = map;

                g.Dispose();
            }
            catch
            {
                // do nothing for now
                return;
            }
        }

        private void frmMapClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            client.Grid.CoarseLocationUpdate -= Grid_OnCoarseLocationUpdate;
            client.Network.SimChanged -= Network_OnCurrentSimChanged;

            client.Grid.GridRegion -= Grid_OnGridRegion;
            netcom.Teleporting -= netcom_Teleporting;
            netcom.TeleportStatusChanged -= netcom_TeleportStatusChanged;

            _LandLayer = _MapLayer;
            _MapLayer = null;
            client.Grid.RequestMapRegion(client.Network.CurrentSim.Name, GridLayerType.Objects);
        }

        private void frmMapClient_Enter(object sender, EventArgs e)
        {
            
        }

        private void pictureBox1_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show(pictureMap);
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Close();
        }

        private void nuZ_ValueChanged(object sender, EventArgs e)
        {

        }

        private void nuY_ValueChanged(object sender, EventArgs e)
        {
            if (world.Image == null) return;

            clickedx = (int)nuX.Value;
            clickedy = (int)nuY.Value;
            PlotSelected(clickedx, clickedy);

            btnClearMarker.Enabled = true; 
        }

        private void world_MouseMove(object sender, MouseEventArgs e)
        {
            int posX = NormaliseSize(e.X);   // Convert.ToInt32(Math.Round(e.X * ssize));
            int posY = NormaliseSize(e.Y);   // Convert.ToInt32(Math.Round(e.Y * ssize));

            Point mouse = new Point(posX, posY);

            MEGAboltInstance.AvLocation CurrentLoc = null;

            try
            {
                CurrentLoc = instance.avlocations.Find(g => g.Rectangle.Contains(mouse) == true);
            }
            catch { ; }

            if (CurrentLoc != null)
            {
                if (!showing)
                {
                    UUID akey = (UUID)CurrentLoc.LocationName;
                    string apstn = "\nCoords.: " + Math.Round(CurrentLoc.Position.X).ToString(CultureInfo.CurrentCulture) + "/" + Math.Round(CurrentLoc.Position.Y).ToString(CultureInfo.CurrentCulture) + "/" + Math.Round(CurrentLoc.Position.Z).ToString(CultureInfo.CurrentCulture);

                    world.Cursor = Cursors.Hand;
                    string anme = string.Empty;

                    lock (instance.avnames)
                    {
                        if (instance.avnames.ContainsKey(akey))
                        {
                            avname = instance.avnames[akey];

                            if (instance.avtags.ContainsKey(akey))
                            {
                                anme = "\nTag: " + instance.avtags[akey];
                            }

                            toolTip1.SetToolTip(world, avname + anme + apstn);
                            avuuid = akey;
                        }
                        else
                        {
                            toolTip1.SetToolTip(world, CurrentLoc.LocationName + apstn);
                        }
                    }

                    //world.Cursor = Cursors.Hand;

                    showing = true;
                }
            }
            else
            {
                world.Cursor = Cursors.Cross;
                toolTip1.RemoveAll();
                showing = false;
            }
        }

        private void world_Click(object sender, EventArgs e)
        {

        }

        private void btnClearMarker_Click(object sender, EventArgs e)
        {
            clickedx = 0;
            clickedy = 0;

            btnClearMarker.Enabled = false; 
        }

        private void chkForSale_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkForSale.Checked)
            {
                _LandLayer = null;
                _MapLayer = null;
                client.Grid.RequestMapRegion(client.Network.CurrentSim.Name, GridLayerType.LandForSale);
            }
            else
            {
                _LandLayer = _MapLayer;
                _MapLayer = null;
                client.Grid.RequestMapRegion(client.Network.CurrentSim.Name, GridLayerType.Objects);
            }

            GetMap(); 
        }

        private void TabCont_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TabCont.SelectedIndex == 0)
            {
                Width = 300;
                button2.Visible = false;
                return;
            }

            if (TabCont.SelectedIndex == 1)
            {
                Width = 592;
                txtSearchFor.Focus(); 
                return;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            StartRegionSearch();
            mloaded = true;
        }

        private void StartRegionSearch()
        {
            lbxRegionSearch.Items.Clear();

            client.Grid.RequestMapRegion(txtSearchFor.Text.Trim(), GridLayerType.Objects);
        }

        //Separate thread
        private void Grid_OnGridRegion(object sender, GridRegionEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Grid_OnGridRegion(sender, e)));
                return;
            }

            BeginInvoke(new MethodInvoker(() =>
            {
                RegionSearchResult(e.Region);
            }));
        }

        //UI thread
        private void RegionSearchResult(GridRegion region)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => RegionSearchResult(region)));
                return;
            }

            if (!mloaded)
            {
                return;
            }

            if (TabCont.SelectedIndex == 0) return;

            RegionSearchResultItem item = new RegionSearchResultItem(instance, region, lbxRegionSearch);
            int index = lbxRegionSearch.Items.Add(item);
            item.ListIndex = index;
            selregion = item.Region;
        }

        private void netcom_TeleportStatusChanged(object sender, TeleportEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => netcom_TeleportStatusChanged(sender, e)));
                return;
            }

            try
            {
                switch (e.Status)
                {
                    case TeleportStatus.Start:
                        RefreshControls();
                        pnlTeleporting.Visible = true;
                        lblTeleportStatus.Visible = true;
                        break;
                    case TeleportStatus.Progress:
                        lblTeleportStatus.Text = e.Message;
                        break;

                    case TeleportStatus.Failed:
                        RefreshControls();
                        pnlTeleporting.Visible = false;
                        MessageBox.Show(e.Message, "Teleport", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;

                    case TeleportStatus.Finished:
                        RefreshControls();
                        pnlTeleporting.Visible = false;
                        //lblTeleportStatus.Visible = false;
                        Close();
                        break;
                }
            }
            catch { ; }
        }

        private void RefreshControls()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    RefreshControls();
                    return;
                }));
            }

            try
            {
                if (netcom.IsTeleporting)
                {
                    pnlTeleportOptions.Enabled = false;
                    btnTeleport.Enabled = false;
                    pnlTeleporting.Visible = true;
                }
                else
                {
                    pnlTeleportOptions.Enabled = true;
                    btnTeleport.Enabled = true;
                    pnlTeleporting.Visible = false;
                }
            }
            catch
            {
                ;
            }
        }

        private void netcom_Teleporting(object sender, TeleportingEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => netcom_Teleporting(sender, e)));
                return;
            }

            try
            {
                RefreshControls();
            }
            catch
            {
                ;
            }
        }

        private void txtRegion_TextChanged(object sender, EventArgs e)
        {
            btnTeleport.Enabled = (txtRegion.Text.Trim().Length > 0);
        }

        private void btnTeleport_Click(object sender, EventArgs e)
        {
            if (instance.State.IsSitting)
            {
                client.Self.Stand();
                instance.State.SetStanding();
            }

            pnlTeleporting.Visible = true;

            if (selregion.RegionHandle == 0 && !string.IsNullOrEmpty(txtRegion.Text))
            {
                //RefreshControls();
                netcom.Teleport(txtRegion.Text.Trim(), new Vector3((float)nudX1.Value, (float)nudY1.Value, (float)nudZ1.Value));
            }
            else
            {
                client.Self.RequestTeleport(selregion.RegionHandle, new Vector3((float)nudX1.Value, (float)nudY1.Value, (float)nudZ1.Value));
            }
        }

        private void txtSearchFor_TextChanged(object sender, EventArgs e)
        {
            btnFind.Enabled = (txtSearchFor.Text.Trim().Length > 0);
        }

        private void lbxRegionSearch_DoubleClick(object sender, EventArgs e)
        {
            //if (lbxRegionSearch.SelectedItem == null) return;
            //RegionSearchResultItem item = (RegionSearchResultItem)lbxRegionSearch.SelectedItem;
            
            //selregion = item.Region;
            //txtRegion.Text = item.Region.Name;
            //nudX.Value = 128;
            //nudY.Value = 128;
            //nudZ.Value = 0;

            //pictureBox2.Image = item.MapImage; 
        }

        private void txtSearchFor_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            if (!btnFind.Enabled) return;
            e.SuppressKeyPress = true;

            StartRegionSearch();
        }

        private void lbxRegionSearch_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0) return;

            RegionSearchResultItem itemToDraw = (RegionSearchResultItem)lbxRegionSearch.Items[e.Index];
            Brush textBrush = null;

            textBrush = (e.State & DrawItemState.Selected) == DrawItemState.Selected 
                ? new SolidBrush(Color.FromKnownColor(KnownColor.HighlightText)) 
                : new SolidBrush(Color.FromKnownColor(KnownColor.ControlText));

            Font newFont = new Font(e.Font, FontStyle.Bold);
            SizeF stringSize = e.Graphics.MeasureString(itemToDraw.Region.Name, newFont);

            float iconSize = 64;   // trkIconSize.Value;
            float leftTextMargin = e.Bounds.Left + iconSize + 6.0f;
            float topTextMargin = e.Bounds.Top + 4.0f;

            if (itemToDraw.IsImageDownloaded)
            {
                if (itemToDraw.MapImage != null)
                {
                    e.Graphics.DrawImage(itemToDraw.MapImage, new RectangleF(e.Bounds.Left + 4.0f, e.Bounds.Top + 4.0f, iconSize, iconSize));
                }
            }
            else
            {
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(200, 200, 200)), e.Bounds.Left + 4.0f, e.Bounds.Top + 4.0f, iconSize, iconSize);

                if (!itemToDraw.IsImageDownloading)
                    itemToDraw.RequestMapImage(125000.0f);
            }


            e.Graphics.DrawString(itemToDraw.Region.Name, newFont, textBrush, new PointF(leftTextMargin, topTextMargin));

            if (itemToDraw.GotAgentCount)
            {
                string peeps = " person";

                if (itemToDraw.Region.Agents != 1)
                {
                    peeps = " people";
                }

                string s = Convert.ToString(itemToDraw.Region.Agents, CultureInfo.CurrentCulture);

                e.Graphics.DrawString(s + peeps, e.Font, textBrush, new PointF(leftTextMargin + stringSize.Width + 6.0f, topTextMargin));
            }
            else
            {
                if (!itemToDraw.GettingAgentCount)
                {
                    itemToDraw.RequestAgentLocations();
                }
            }

            switch (itemToDraw.Region.Access)
            {
                case SimAccess.PG:
                    e.Graphics.DrawString("PG", e.Font, textBrush, new PointF(leftTextMargin, topTextMargin + stringSize.Height));
                    break;

                case SimAccess.Mature:
                    e.Graphics.DrawString("Mature", e.Font, textBrush, new PointF(leftTextMargin, topTextMargin + stringSize.Height));
                    break;

                case SimAccess.Adult:
                    e.Graphics.DrawString("Adult", e.Font, textBrush, new PointF(leftTextMargin, topTextMargin + stringSize.Height));
                    break;

                case SimAccess.Down:
                    e.Graphics.DrawString("Offline", e.Font, new SolidBrush(Color.Red), new PointF(leftTextMargin, topTextMargin + stringSize.Height));
                    break;
            }

            e.Graphics.DrawLine(new Pen(Color.FromArgb(200, 200, 200)), new Point(e.Bounds.Left, e.Bounds.Bottom - 1), new Point(e.Bounds.Right, e.Bounds.Bottom - 1));
            e.DrawFocusRectangle();

            textBrush.Dispose();
            newFont.Dispose();
            textBrush = null;
            newFont = null;

            lbxRegionSearch.ItemHeight = 73;   // trkIconSize.Value + 10;
        }

        private void txtSearchFor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void lbxRegionSearch_Click(object sender, EventArgs e)
        {
            try
            {
                if (lbxRegionSearch.SelectedItem == null)
                {
                    button4.Enabled = false;
                    return;
                }

                RegionSearchResultItem item = (RegionSearchResultItem)lbxRegionSearch.SelectedItem;

                button4.Enabled = true; 

                selregion = item.Region;
                txtRegion.Text = item.Region.Name;
                nudX1.Value = 128;
                nudY1.Value = 128;
                nudZ1.Value = 0;

                orgmap = item.MapImage;
                selectedmap = item.MapImage;
                //pictureBox2.Image = selectedmap;

                Bitmap bmp = new Bitmap(selectedmap, 256, 256);

                Graphics g = Graphics.FromImage(bmp);

                Rectangle rect = new Rectangle();

                foreach (MapItem itm in item.AgentLocations)
                {
                    // Draw avatar location icons
                    int x = (int)itm.LocalX + 7;
                    int y = 255 - (int)itm.LocalY - 16;

                    rect = new Rectangle(x, y, 7, 7);

                    g.FillEllipse(Brushes.LightGreen, rect);
                    g.DrawEllipse(new Pen(Brushes.Green, 1), rect);
                }

                g.DrawImage(bmp, 0, 0);
                pictureBox2.Image = bmp;

                g.Dispose();
            }
            catch { ; }
        }

        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            px = e.X;
            py = 255 - e.Y;

            nudX1.Value = px;
            nudY1.Value = py;
            nudZ1.Value = 10;

            //Bitmap map = (Bitmap)selectedmap;   // pictureBox2.Image;
            //Graphics g = Graphics.FromImage(map);

            Bitmap map = new Bitmap(selectedmap, 256, 256);
            Graphics g = Graphics.FromImage(map);

            Rectangle selectedrect = new Rectangle(e.X - 2, e.Y - 2, 10, 10);
            g.DrawEllipse(new Pen(Brushes.Red, 2), selectedrect);
            pictureBox2.Image = map;

            g.Dispose();

            button2.Visible = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            nudX1.Value = 128;
            nudY1.Value = 128;
            nudZ1.Value = 0;

            pictureBox2.Image = orgmap;
            button2.Enabled = false;
        }

        private void nuX_Scroll(object sender, ScrollEventArgs e)
        {
            if (world.Image == null) return;

            clickedx = (int)nuX.Value;
            clickedy = (int)nuY.Value;
            PlotSelected(clickedx, clickedy);
            btnClearMarker.Enabled = true; 
        }

        private void nuX_ValueChanged(object sender, EventArgs e)
        {
            if (world.Image == null) return;

            clickedx = (int)nuX.Value;
            clickedy = (int)nuY.Value;
            PlotSelected(clickedx, clickedy);
            btnClearMarker.Enabled = true; 
        }

        private void nuY_MouseUp(object sender, MouseEventArgs e)
        {
            if (world.Image == null) return;

            clickedx = (int)nuX.Value;
            clickedy = (int)nuY.Value;
            PlotSelected(clickedx, clickedy);
            btnClearMarker.Enabled = true; 
        }

        private void btnBrowser_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(lblSlurl.Text))
            {
                Utilities.OpenBrowser(lblSlurl.Text);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSearchFor.Text))
            {
                RegionSearchResultItem item = (RegionSearchResultItem)lbxRegionSearch.SelectedItem;
                string surl = "http://slurl.com/secondlife/" + item.Region.Name.Trim() + "/" + nudX1.Value.ToString(CultureInfo.CurrentCulture) + "/" + nudY1.Value.ToString(CultureInfo.CurrentCulture) + "/" + nudZ1.Value.ToString(CultureInfo.CurrentCulture);
                Utilities.OpenBrowser(@surl);
            }
        }

        private void frmMapClient_SizeChanged(object sender, EventArgs e)
        {
            world.Height = world.Width; 
        }
    }
}
