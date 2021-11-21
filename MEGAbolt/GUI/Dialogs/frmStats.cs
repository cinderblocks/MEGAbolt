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
using MEGAbolt.Controls;
using System.Globalization;

namespace MEGAbolt
{
    public partial class frmStats : Form
    {
        private MEGAboltInstance instance;
        private GridClient client;
        private Simulator sim;
        private int score = 10;
        private Popup toolTip;
        private CustomToolTip customToolTip;

        public frmStats(MEGAboltInstance instance)
        {
            InitializeComponent();

            string msg1 = "Click for online help/guidance";
            toolTip = new Popup(customToolTip = new CustomToolTip(instance, msg1));
            toolTip.AutoClose = false;
            toolTip.FocusOnOpen = false;
            toolTip.ShowingAnimation = toolTip.HidingAnimation = PopupAnimations.Blend;

            this.instance = instance;
            client = this.instance.Client;

            sim = client.Network.CurrentSim;

            client.Network.SimChanged += Network_SimChanged;
        }

        void Network_SimChanged(object sender, SimChangedEventArgs e)
        {
            sim = client.Network.CurrentSim;
        }

        private void frmStats_Load(object sender, EventArgs e)
        {
            CenterToParent();
            
            GetStats();
        }

        private void GetStats()
        {
            lbIssues.Items.Clear();

            try
            {
                label1.Text = "Name: " + sim.ToString();

                label22.Text = "Version: " + sim.SimVersion;
                label23.Text = "Location: " + sim.ColoLocation;

                if (client.Appearance.ServerBakingRegion())
                {
                    label45.Text = "SSA Enabled";
                }
                else
                {
                    label45.Text = "SSA Disabled";
                }

                label2.Text = "Dilation: " + sim.Stats.Dilation.ToString(CultureInfo.CurrentCulture);
                progressBar1.Value = (int)(sim.Stats.Dilation * 10);

                label24.Text = sim.Stats.INPPS.ToString(CultureInfo.CurrentCulture);
                label25.Text = sim.Stats.OUTPPS.ToString(CultureInfo.CurrentCulture);

                label26.Text = sim.Stats.ResentPackets.ToString(CultureInfo.CurrentCulture);
                label27.Text = sim.Stats.ReceivedResends.ToString(CultureInfo.CurrentCulture);

                label43.Text = sim.Stats.LastLag.ToString(CultureInfo.CurrentCulture);

                label28.Text = client.Network.InboxCount.ToString(CultureInfo.CurrentCulture);

                label7.Text = "FPS: " + sim.Stats.FPS.ToString(CultureInfo.CurrentCulture);
                progressBar7.Value = (int)sim.Stats.FPS;

                label8.Text = "Physics FPS: " + sim.Stats.PhysicsFPS.ToString(CultureInfo.CurrentCulture);
                progressBar8.Value = (int)sim.Stats.PhysicsFPS;

                label29.Text = sim.Stats.AgentUpdates.ToString(CultureInfo.CurrentCulture);

                label10.Text = "Objects: " + sim.Stats.Objects.ToString(CultureInfo.CurrentCulture);
                progressBar10.Value = (int)sim.Stats.Objects;

                label30.Text = sim.Stats.ScriptedObjects.ToString(CultureInfo.CurrentCulture);

                label12.Text = "Frame Time: " + sim.Stats.FrameTime.ToString(CultureInfo.CurrentCulture);
                progressBar15.Value = (int)sim.Stats.FrameTime;

                label34.Text = sim.Stats.NetTime.ToString(CultureInfo.CurrentCulture);

                label35.Text = sim.Stats.ImageTime.ToString(CultureInfo.CurrentCulture);

                label36.Text = sim.Stats.PhysicsTime.ToString(CultureInfo.CurrentCulture);

                label37.Text = sim.Stats.ScriptTime.ToString(CultureInfo.CurrentCulture);

                label38.Text = sim.Stats.OtherTime.ToString(CultureInfo.CurrentCulture);

                label32.Text = sim.Stats.Agents.ToString(CultureInfo.CurrentCulture);

                label33.Text = sim.Stats.ChildAgents.ToString(CultureInfo.CurrentCulture);

                label31.Text = sim.Stats.ActiveScripts.ToString(CultureInfo.CurrentCulture);

                ScorePerformance();
            }
            catch
            {
                ;
            }
        }

        private void ScorePerformance()
        {
            float dil = sim.Stats.Dilation;
            int fm = sim.Stats.FPS;

            if (sim.Stats.PendingDownloads > 1)
            {
                lbIssues.Items.Add("Expect rezzing issues and delays in viewing notecards/scripts.");
            }

            if (sim.Stats.PendingUploads > 0)
            {
                lbIssues.Items.Add("Expect teleport issues.");
            }

            if (sim.Stats.PhysicsFPS > 5000)
            {
                lbIssues.Items.Add("SIM wide physics issues");
            }

            if (dil > 0.94)
            {
                //lblScore.Text = "Excellent";
                // Excellent
                if (fm > 29)
                {
                    // Excellent
                    //lblScore.Text = "Excellent";
                    lblScore.ForeColor = Color.Green;
                    score = 5;
                }
                else if (fm > 14 && fm < 30)
                {
                    // Good
                    //lblScore.Text = "Good";
                    lblScore.ForeColor = Color.RoyalBlue;
                    score = 3;
                }
                else
                {
                    // Poor
                    //lblScore.Text = "Poor";
                    lbIssues.Items.Add("Physics running-speed is almost zero i.e. barely running");
                    lblScore.ForeColor = Color.Red;
                    score = 1;
                }
            }
            else if (dil < 0.95 && dil > 0.5)
            {
                lbIssues.Items.Add("Physics is running at half speed.");

                // Good
                if (fm > 29)
                {
                    // Excellent
                    //lblScore.Text = "Good";
                    lblScore.ForeColor = Color.RoyalBlue;
                    score = 3;
                }
                else if (fm > 14 && fm < 30)
                {
                    lbIssues.Items.Add("Script running speed is reduced due to low dilation.");
                    // Good
                    //lblScore.Text = "Average";
                    lblScore.ForeColor = Color.Black;
                    score = 2;
                }
                else
                {
                    // Poor
                    //lblScore.Text = "Very Poor";
                    
                    lblScore.ForeColor = Color.Red;
                    score = 1;
                }
            }
            else
            {
                // Poor
                //lblScore.Text = "Extremely Poor";
                lbIssues.Items.Add("Physics running-speed is almost zero i.e. barely running");
                lblScore.ForeColor = Color.Red;
                score = 1;
            }

            score += CalcFT();

            pbScore.Value = score;
            lblScore.Text = score.ToString(CultureInfo.CurrentCulture);
        }

        private int CalcFT()
        {
            float ft = sim.Stats.FrameTime;

            if (ft < 22.1)
            {
                return 5;
            }
            else if (ft > 22.1 && ft < 22.5)
            {
                lbIssues.Items.Add("Healthy SIM but too many scripts/agents is causing script execution slow-down.");   
                return 3;
            }
            else
            {
                lbIssues.Items.Add("There is a severe load on the SIM due to physics or too many agents. Expect lag.");
                return 1;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close(); 
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetStats();
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void progressBar3_Click(object sender, EventArgs e)
        {

        }

        private void progressBar9_Click(object sender, EventArgs e)
        {

        }

        private void pbScore_Click(object sender, EventArgs e)
        {

        }

        private void pbHelp_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show(pbHelp);
        }

        private void pbHelp_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Close();
        }

        private void pbHelp_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"http://wiki.secondlife.com/wiki/Statistics_Bar_Guide");
        }
    }
}
