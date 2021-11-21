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

namespace MEGAbolt
{
    public partial class ImageViewer : Form
    {
        //private GridClient client;
        private Image img;

        public ImageViewer(MEGAboltInstance instance, Image img)
        {
            InitializeComponent();

            //this.instance = instance;
            //client = this.instance.Client;

            this.img = img;
        }

        private void ImageViewer_Load(object sender, EventArgs e)
        {
            pbView.Image = img; 
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pbView.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            pbView.Refresh();  
        }

        private void button2_Click(object sender, EventArgs e)
        {
            pbView.Image.RotateFlip(RotateFlipType.Rotate180FlipNone);
            pbView.Refresh();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            pbView.Image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            pbView.Refresh();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            pbView.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
            pbView.Refresh(); 
              
        }
    }
}
