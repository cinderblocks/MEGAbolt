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
using System.Windows.Forms;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using System.Globalization;
using OpenJpegDotNet.IO;

namespace MEGAbolt
{
    public partial class UploadImage : Form
    {
        private MEGAboltInstance instance;
        private GridClient client;
        private Bitmap img;
        //private string ext;
        private string file;
        private byte[] ImgUp;

        public UploadImage(MEGAboltInstance instance, Bitmap img, string file, string ext)
        {
            InitializeComponent();

            this.instance = instance;
            client = this.instance.Client;

            this.img = img;
            //this.ext = ext;
            this.file = file;
        }

        private void ImageViewer_Load(object sender, EventArgs e)
        {
            textBox1.Text = System.IO.Path.GetFileNameWithoutExtension(file);

            label3.Text = "Loading image " + file;
            byte[] jpeg2k = LoadImage(file);

            if (jpeg2k == null)
            {
                label3.Text = "Failed to compress image"; 
                return;
            }

            label3.Text = "Ready";
            pbView.Image = img;
        }

        private byte[] LoadImage(string fileName)
        {
            string lowfilename = fileName.ToLower(CultureInfo.CurrentCulture);
            Bitmap bitmap = null;

            try
            {
                if (lowfilename.EndsWith(".jp2", StringComparison.CurrentCultureIgnoreCase) || lowfilename.EndsWith(".j2c", StringComparison.CurrentCultureIgnoreCase))
                {

                    // Upload JPEG2000 images untouched
                    ImgUp = System.IO.File.ReadAllBytes(fileName);

                    using var reader = new Reader(ImgUp);
                    reader.ReadHeader();
                    reader.DecodeToBitmap();
                }
                else
                {
                    if (lowfilename.EndsWith(".tga", StringComparison.CurrentCultureIgnoreCase))
                        bitmap = LoadTGAClass.LoadTGA(fileName);
                    else
                        bitmap = (Bitmap)Image.FromFile(fileName);

                    int oldwidth = bitmap.Width;
                    int oldheight = bitmap.Height;

                    if (!IsPowerOfTwo((uint)oldwidth) || !IsPowerOfTwo((uint)oldheight))
                    {
                        Bitmap resized = new Bitmap(256, 256, bitmap.PixelFormat);
                        Graphics graphics = Graphics.FromImage(resized);

                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.InterpolationMode =
                           System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(bitmap, 0, 0, 256, 256);

                        bitmap.Dispose();
                        bitmap = resized;

                        oldwidth = 256;
                        oldheight = 256;
                    }

                    // Handle resizing to prevent excessively large images
                    if (oldwidth > 1024 || oldheight > 1024)
                    {
                        int newwidth = (oldwidth > 1024) ? 1024 : oldwidth;
                        int newheight = (oldheight > 1024) ? 1024 : oldheight;

                        Bitmap resized = new Bitmap(newwidth, newheight, bitmap.PixelFormat);
                        Graphics graphics = Graphics.FromImage(resized);

                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(bitmap, 0, 0, newwidth, newheight);

                        bitmap.Dispose();
                        bitmap = resized;
                    }

                    using var writer = new Writer(bitmap);
                    ImgUp = writer.Encode();
                }
            }
            catch (Exception ex)
            {
                label3.Text = ex + " SL Image Upload ";
                return null;
            }

            return ImgUp;
        }

        private static bool IsPowerOfTwo(uint n)
        {
            return (n & (n - 1)) == 0 && n != 0;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            img.Dispose(); 
            Close();
        }        

        private void button1_Click(object sender, EventArgs e)
        {
            textBox2.Text += " uploaded by MEGAbolt " + DateTime.Now.ToLongDateString(); 

            label3.Text = "Uploading image...";
            UploadImg(textBox1.Text, textBox2.Text);    
        }

        private void UploadImg(string fname, string desc)
        {
            if (ImgUp != null)
            {
                //string name = System.IO.Path.GetFileNameWithoutExtension(file);
                UUID folder = client.Inventory.FindFolderForType(AssetType.Texture);

                client.Inventory.RequestCreateItemFromAsset(ImgUp, fname, desc, AssetType.Texture, InventoryType.Texture,
                folder, Img_Upload);
            }
        }

        private void Img_Upload(bool success, string status, UUID itemID, UUID assetID)
        {
            if (InvokeRequired)
            {
                if (IsHandleCreated)
                {
                    BeginInvoke(new MethodInvoker(() => Img_Upload(success, status, itemID, assetID)));
                }

                return;
            }

            if (success)
            {
                label3.Text = "Image uploaded successfully";
            }
            else
            {
                label3.Text = "Upload failed";
            }

            img.Dispose();
        }
    }
}
