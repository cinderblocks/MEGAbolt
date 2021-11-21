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
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
//using MEGAbolt.NetworkComm;
using OpenMetaverse;
using OpenMetaverse.Assets;

namespace MEGAbolt
{
    public partial class InventoryImageConsole : UserControl
    {
        private MEGAboltInstance instance;
        //private SLNetCom netcom;
        private GridClient client;
        private InventoryItem item;

        public InventoryImageConsole(MEGAboltInstance instance, InventoryItem item)
        {
            InitializeComponent();

            this.instance = instance;
            //netcom = this.instance.Netcom;
            client = this.instance.Client;
            this.item = item;
            
            if (instance.ImageCache.ContainsImage(item.AssetUUID))
                SetFinalImage(instance.ImageCache.GetImage(item.AssetUUID));
            else
            {
                Disposed += InventoryImageConsole_Disposed;
                //client.Assets.OnImageRecieveProgress += new AssetManager.ImageReceiveProgressCallback(Assets_OnImageReceived);
            }
        }

        private void InventoryImageConsole_Disposed(object sender, EventArgs e)
        {
            //client.Assets.OnImageRecieveProgress -= new AssetManager.ImageReceiveProgressCallback(Assets_OnImageReceived);
        }

        //comes in on separate thread
        private void Assets_OnImageReceived(TextureRequestState image, AssetTexture texture)
        {
            if (texture.AssetID != item.AssetUUID) return;

            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Assets_OnImageReceived(image, texture)));
                return;
            }

            BeginInvoke(new OnSetStatusText(SetStatusText), "Image downloaded. Decoding...");

            Image sImage = null;

            using (OpenJpegDotNet.IO.Reader reader = new(texture.AssetData))
            {
                reader.ReadHeader();
                sImage = reader.DecodeToBitmap();
            }

            if (sImage == null)
            {
                BeginInvoke(new OnSetStatusText(SetStatusText), "D'oh! Error decoding image.");
                BeginInvoke(new MethodInvoker(DoErrorState));
                return;
            }

            instance.ImageCache.AddImage(texture.AssetID, sImage);
            BeginInvoke(new OnSetFinalImage(SetFinalImage), sImage);
        }

        //called on GUI thread
        private delegate void OnSetFinalImage(Image finalImage);
        private void SetFinalImage(Image finalImage)
        {
            pbxImage.Image = finalImage;

            pnlOptions.Visible = true;
            pnlStatus.Visible = false;
            
            // TPV change to allow only the creator to save the image. 31 Mar 2010

            //if ((item.Permissions.OwnerMask & PermissionMask.Copy) == PermissionMask.Copy)
            //{
            //    if ((item.Permissions.OwnerMask & PermissionMask.Modify) == PermissionMask.Modify)
            //    {
            //        btnSave.Click += delegate(object sender, EventArgs e)
            //        {
            //            if (sfdImage.ShowDialog() == DialogResult.OK)
            //            {
            //                switch (sfdImage.FilterIndex)
            //                {
            //                    case 1: //BMP
            //                        pbxImage.Image.Save(sfdImage.FileName, ImageFormat.Bmp);
            //                        break;

            //                    case 2: //JPG
            //                        pbxImage.Image.Save(sfdImage.FileName, ImageFormat.Jpeg);
            //                        break;

            //                    case 3: //PNG
            //                        pbxImage.Image.Save(sfdImage.FileName, ImageFormat.Png);
            //                        break;

            //                    default:
            //                        pbxImage.Image.Save(sfdImage.FileName, ImageFormat.Bmp);
            //                        break;
            //                }
            //            }
            //        };

            //        btnSave.Enabled = true;
            //    }
            //    else
            //    {
            //        btnSave.Enabled = false;
            //    }
            //}

            if (item.CreatorID == client.Self.AgentID)
            {
                btnSave.Click += delegate(object sender, EventArgs e)
                {
                    if (sfdImage.ShowDialog() == DialogResult.OK)
                    {
                        switch (sfdImage.FilterIndex)
                        {
                            case 1: //BMP
                                pbxImage.Image.Save(sfdImage.FileName, ImageFormat.Bmp);
                                break;

                            case 2: //JPG
                                pbxImage.Image.Save(sfdImage.FileName, ImageFormat.Jpeg);
                                break;

                            case 3: //PNG
                                pbxImage.Image.Save(sfdImage.FileName, ImageFormat.Png);
                                break;

                            default:
                                pbxImage.Image.Save(sfdImage.FileName, ImageFormat.Bmp);
                                break;
                        }
                    }
                };

                btnSave.Enabled = true;
            }
            else
            {
                btnSave.Enabled = false;
            }
        }

        //called on GUI thread
        private delegate void OnSetStatusText(string text);
        private void SetStatusText(string text)
        {
            lblStatus.Text = text;
        }

        private void DoErrorState()
        {
            lblStatus.Visible = true;
            lblStatus.ForeColor = Color.Red;
            proActivity.Visible = false;

            pnlStatus.Visible = true;
            pnlOptions.Visible = false;
        }

        private void pbxImage_Click(object sender, EventArgs e)
        {

        }

        private void InventoryImageConsole_Load(object sender, EventArgs e)
        {
            if (instance.ImageCache.ContainsImage(item.AssetUUID))
            {
                SetFinalImage(instance.ImageCache.GetImage(item.AssetUUID));
            }
            else
            {
                client.Assets.RequestImage(item.AssetUUID, ImageType.Normal, Assets_OnImageReceived);
            }
        }

        private void pnlStatus_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnView_Click(object sender, EventArgs e)
        {
            (new ImageViewer(instance, pbxImage.Image)).Show();
        }
    }
}
