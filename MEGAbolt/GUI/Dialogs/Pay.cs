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
using System.Windows.Forms;

namespace MEGAbolt
{
    public partial class frmPay : Form
    {
        private readonly MEGAboltInstance instance;
        private readonly GridClient client;
        private readonly UUID target = UUID.Zero;
        //private string name;
        private readonly Primitive Prim = new Primitive();
        private int buyprice = -1;
        private readonly int oprice = -1;

        public frmPay(MEGAboltInstance instance, UUID target, string name)
        {
            InitializeComponent();

            this.instance = instance;
            client = this.instance.Client;
            txtPerson.Text = name;
            txtPerson.Visible = true;
            label3.Visible = true;

            this.target = target;

            //LoadCallBacks();

            Text += "   " + "[ " + client.Self.Name + " ]";
        }

        public frmPay(MEGAboltInstance instance, UUID target, string name, string itemname)
        {
            InitializeComponent();

            this.instance = instance;
            client = this.instance.Client;
            //this.name = txtPerson.Text = name;
            textBox1.Text = itemname;

            this.target = target;

            LoadCallBacks();
        }

        public frmPay(MEGAboltInstance instance, UUID target, string name, int sprice)
        {
            InitializeComponent();

            this.instance = instance;
            client = this.instance.Client;

            this.target = target;
            nudAmount.Value = sprice;

            LoadCallBacks();
        }

        public frmPay(MEGAboltInstance instance, UUID target, string name, string itemname, Primitive prim)
        {
            InitializeComponent();

            this.instance = instance;
            client = this.instance.Client;
            //this.name = txtPerson.Text = name;
            textBox1.Text = itemname;

            Prim = prim;

            this.target = target;

            LoadCallBacks();
        }

        public frmPay(MEGAboltInstance instance, UUID target, string name, int sprice, Primitive prim)
        {
            InitializeComponent();

            this.instance = instance;
            client = this.instance.Client;

            this.target = target;
            //this.name = txtPerson.Text = name;
            textBox1.Text = prim.Properties.Name;
            btnPay.Text = "&Buy";

            nudAmount.Value = sprice;
            oprice = sprice;

            Prim = prim;

            LoadCallBacks();
        }

        private void LoadCallBacks()
        {
            client.Objects.PayPriceReply += PayPrice;

            client.Objects.RequestPayPrice(client.Network.CurrentSim, target);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnPay_Click(object sender, EventArgs e)
        {
            var iprice = (int)nudAmount.Value;

            if (Prim != null && Prim.ID != UUID.Zero)
            {
                var styp = Prim.Properties.SaleType;

                if (styp != SaleType.Not)
                {
                    if (oprice != buyprice && buyprice != -1)
                    {
                        if (oprice < buyprice)
                        {
                            if (styp == SaleType.Contents)
                            {
                                client.Objects.BuyObject(client.Network.CurrentSim, Prim.LocalID, styp, iprice, client.Self.ActiveGroup, client.Inventory.Store.RootFolder.UUID);
                            }
                            else
                            {
                                var folderid = client.Inventory.FindFolderForType(AssetType.Object);   // instance.Config.CurrentConfig.ObjectsFolder;

                                client.Objects.BuyObject(client.Network.CurrentSim, Prim.LocalID, styp, iprice, client.Self.ActiveGroup, folderid);
                            }
                        }
                        else
                        {
                            client.Self.GiveObjectMoney(target, iprice, textBox1.Text.Trim());
                        }
                    }
                    else
                    {
                        if (styp == SaleType.Contents)
                        {
                            client.Objects.BuyObject(client.Network.CurrentSim, Prim.LocalID, styp, iprice, client.Self.ActiveGroup, client.Inventory.Store.RootFolder.UUID);
                        }
                        else
                        {
                            var folderid = client.Inventory.FindFolderForType(AssetType.Object);   // instance.Config.CurrentConfig.ObjectsFolder;

                            client.Objects.BuyObject(client.Network.CurrentSim, Prim.LocalID, styp, iprice, client.Self.ActiveGroup, folderid);
                        }
                    }
                }
                else
                {
                    client.Self.GiveObjectMoney(target, iprice, textBox1.Text.Trim());
                }
            }
            else
            {
                if (target != UUID.Zero)
                {
                    if (string.IsNullOrEmpty(textBox1.Text))
                    {
                        client.Self.GiveAvatarMoney(target, iprice);
                    }
                    else
                    {
                        client.Self.GiveAvatarMoney(target, iprice, textBox1.Text.Trim());
                    }
                }
            }

            Close();
        }

        private void frmPay_Load(object sender, EventArgs e)
        {
            CenterToParent();
        }

        private void frmPay_FormClosing(object sender, FormClosingEventArgs e)
        {
            client.Objects.PayPriceReply -= PayPrice;
        }

        private void PayPrice(object sender, PayPriceReplyEventArgs e)
        {
            if (e.ObjectID != target) return;

            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    PayPrice(sender, e);
                }));

                return;
            }

            if (e.DefaultPrice > 0)
            {
                nudAmount.Value = e.DefaultPrice;
                //SetNud(e.DefaultPrice);
                buyprice = e.DefaultPrice;
                btnPay.Text = "&Pay";
            }
            else if (e.DefaultPrice == -1)
            {
                nudAmount.Value = e.ButtonPrices[0];
                //SetNud(e.ButtonPrices[0]);
                buyprice = e.ButtonPrices[0];
                btnPay.Text = "&Pay";
            }

            if (oprice != buyprice && buyprice != -1 & oprice != -1)
            {
                if (Prim.ClickAction is ClickAction.Buy or ClickAction.Pay)
                {
                    label5.Text = "Buy Price: L$" + oprice;
                    label5.Visible = true;
                }
            }
        }

        private void SetNud(int nprc)
        {
            //this.nudAmount.Minimum = (decimal)nprc;
        }
    }
}