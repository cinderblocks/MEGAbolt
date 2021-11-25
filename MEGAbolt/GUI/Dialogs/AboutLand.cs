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
using System.Threading;
using System.IO;
using System.Globalization;
using BugSplatDotNetStandard;


namespace MEGAbolt
{
    public partial class frmAboutLand : Form
    {
        private MEGAboltInstance instance;
        private GridClient client;
        //private SLNetCom netcom;
        //private bool parcelowner = false;
        //private bool detsloading = true;
        //private bool detschanged = false;
        private Parcel parcel;
        private bool formloading = true;
        private List<ParcelManager.ParcelAccessEntry> blacklist;
        //private List<ParcelManager.ParcelAccessEntry> whitelist;
        //private Group parcelgroup;
        private UUID grpID = UUID.Zero;

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                BugSplat crashReporter = new BugSplat("radegast", "MEGAbolt",
                    Properties.Resources.MEGAboltVersion)
                {
                    User = "cinder@cinderblocks.biz",
                    ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                };
                crashReporter.Post(e.Exception);
            }
        }

        public frmAboutLand(MEGAboltInstance instance)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            //netcom = this.instance.Netcom;
            client = this.instance.Client;

            while (!IsHandleCreated)
            {
                // Force handle creation
                IntPtr temp = Handle;
            }

            Disposed += AboutLand_Disposed;

            client.Parcels.ParcelDwellReply += Parcels_OnParcelDwell;
            //client.Groups.GroupMembersReply += new EventHandler<GroupMembersReplyEventArgs>(GroupMembersHandler);
            client.Avatars.UUIDNameReply += Avatars_OnAvatarNames;
            client.Parcels.ParcelObjectOwnersReply += Parcel_ObjectOwners;
            client.Parcels.ParcelAccessListReply += Parcels_ParcelAccessListReply;

            if (this.instance.MainForm.parcel != null)
            {
                parcel = this.instance.MainForm.parcel;

                client.Parcels.RequestDwell(client.Network.CurrentSim, parcel.LocalID);

                //PopData();
            }
            else
            {
                PopData();
                MessageBox.Show("Could not retreive current parcel details from SL. Try again later.", "MEGAbolt");  
            }
        }

        private void AboutLand_Disposed(object sender, EventArgs e)
        {
            //client.Parcels.ParcelDwellReply -= new EventHandler<ParcelDwellReplyEventArgs>(Parcels_OnParcelDwell);
            //client.Avatars.UUIDNameReply -= new EventHandler<UUIDNameReplyEventArgs>(Avatars_OnAvatarNames);
            //client.Groups.GroupMembersReply -= new EventHandler<GroupMembersReplyEventArgs>(GroupMembersHandler);
            //client.Parcels.ParcelObjectOwnersReply -= new EventHandler<ParcelObjectOwnersReplyEventArgs>(Parcel_ObjectOwners);
        }

        private void GroupMembersHandler(object sender, GroupMembersReplyEventArgs e)
        {
            if (e.GroupID != parcel.GroupID) return;

            client.Groups.GroupMembersReply -= GroupMembersHandler;
 
            // do the stuff here
            if (e.Members.ContainsKey(client.Self.AgentID))
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    grpID = e.GroupID;
                    RequestParcelDets();

                    SetOwnerProperties();
                }));

                //return;
            }
        }

        private bool HasGroupPower(GroupPowers power, UUID groupID)
        {
            if (!instance.State.Groups.ContainsKey(groupID)) return false;

            return (instance.State.Groups[groupID].Powers & power) != 0;
        }

        private void Parcels_OnParcelDwell(object sender, ParcelDwellReplyEventArgs ea)
        {
            try
            {
                if (ea.LocalID != parcel.LocalID) return;

                client.Parcels.ParcelDwellReply -= Parcels_OnParcelDwell;

                BeginInvoke(new MethodInvoker(delegate()
                {
                    PopData();
                    lblTraffic.Text = ea.Dwell.ToString(CultureInfo.CurrentCulture);
                }));
            }
            catch
            {
                // do nothing
            }
        }

        private void SetOwnerProperties()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    SetOwnerProperties();
                }));

                return;
            }

            bool hasauth = false;

            if (grpID != UUID.Zero)
            {
                if (HasGroupPower(GroupPowers.LandChangeIdentity, grpID))
                {
                    hasauth = true;
                }
                else
                {
                    hasauth = false;
                }
            }

            //parcelowner = true;

            if (parcel.OwnerID == client.Self.AgentID || hasauth)
            {
                txtParceldesc.ReadOnly = false;
                txtParcelname.ReadOnly = false;
                txtPrimreturn.ReadOnly = false;
                txtMusic.ReadOnly = false;
                button1.Visible = true;
                txtMusic.PasswordChar = char.Parse("\0");
                hasauth = false;
            }

            //Populate prim owners
            //callback = new ParcelManager.ParcelPrimOwners ParcelObjectOwnersListReplyCallback(Parcel_ObjectOwners);
            //client.Parcels.OnPrimOwnersListReply += callback;

            if (grpID != UUID.Zero)
            {
                if (HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    hasauth = true;
                }
                else
                {
                    hasauth = false;
                }
            }

            if (parcel.OwnerID == client.Self.AgentID || hasauth)
            {
                cbcreater.Enabled = true;
                cbcreateg.Enabled = true;
                cbplace.Enabled = true;
                cbmature.Enabled = true;
                cbscriptsr.Enabled = true;
                cbsafe.Enabled = true;
                cbscriptsg.Enabled = true;
                cbentryg.Enabled = true;
                cbentryr.Enabled = true;
                cbfly.Enabled = true;
                cblandmark.Enabled = true;
                cbTerrain.Enabled = true;
                cbpush.Enabled = true;
                cbVoice.Enabled = true; 

                lvwPrimOwners.Visible = true;
                hasauth = false;
            }
        }

        private void PopData()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    PopData();
                }));

                return;
            }

            if (!instance.LoggedIn)
            {
                Close();
                return;
            }

            try
            {
                formloading = true;

                if (parcel == null) return;

                //lblTraffic.Text = this.instance.MainForm.dwell;

                if (parcel.OwnerID == client.Self.AgentID)
                {
                    SetOwnerProperties();
                }
                else
                {
                    //parcelowner = false;
                    txtParceldesc.ReadOnly = true;
                    txtParcelname.ReadOnly = true;
                    txtPrimreturn.ReadOnly = true;
                    txtMusic.ReadOnly = true;
                    txtMusic.PasswordChar = '*';
                    button1.Visible = false;

                    cbcreater.Enabled = false;
                    cbcreateg.Enabled = false;
                    cbplace.Enabled = false;
                    cbmature.Enabled = false;
                    cbscriptsr.Enabled = false;
                    cbsafe.Enabled = false;
                    cbscriptsg.Enabled = false;
                    cbentryg.Enabled = false;
                    cbentryr.Enabled = false;
                    cbfly.Enabled = false;
                    cblandmark.Enabled = false;
                    cbTerrain.Enabled = false;
                    cbpush.Enabled = false;
                    cbVoice.Enabled = false; 

                    //lvwPrimOwners.Clear();
                    //lvwPrimOwners.Enabled = false;
                    lvwPrimOwners.Visible = false;
                }

                //main tab
                txtParcelname.Text = parcel.Name;
                txtParceldesc.Text = parcel.Desc.Replace("\n", "\r\n"); //Multi line else we have little boxes for an Enter

                if (parcel.IsGroupOwned)
                {
                    txtOwnerid.Text = "(Group Owned)";
                    txtGroupOwner.Text = instance.MainForm.AboutlandGroupidname; //For some reason on this new code it shows (???)(???) :-/ TODO: fix me
                    pictureBox2.Enabled = false;

                    client.Groups.GroupMembersReply += GroupMembersHandler;
                    client.Groups.RequestGroupMembers(parcel.GroupID);

                    client.Groups.GroupNamesReply += Groups_GroupNamesReply;
                    client.Groups.RequestGroupName(parcel.GroupID);
                }
                else
                {
                    if (parcel.GroupID != UUID.Zero)
                    {
                        txtGroupOwner.Text = instance.MainForm.AboutlandGroupidname; //For some reason on this new code it shows (???)(???) :-/ TODO: fix me

                        client.Groups.GroupMembersReply += GroupMembersHandler;
                        client.Groups.RequestGroupMembers(parcel.GroupID);

                        client.Groups.GroupNamesReply += Groups_GroupNamesReply;
                        client.Groups.RequestGroupName(parcel.GroupID);
                    }

                    txtOwnerid.Text = instance.MainForm.AboutlandOwneridname;
                    txtGroupOwner.Text = "";   //this.instance.MainForm.AboutlandOwneridname;
                    pictureBox2.Enabled = true;
                }

                //if (parcel.GroupID != UUID.Zero)
                //{
                //    client.Groups.GroupNamesReply += new EventHandler<GroupNamesEventArgs>(Groups_GroupNamesReply);
                //    //txtGroupOwner.Text = parcel.GroupID.ToString();
                //    client.Groups.RequestGroupMembers(parcel.GroupID);
                //    client.Groups.RequestGroupName(parcel.GroupID);
                //}

                txtClaimDate.Text = parcel.ClaimDate.ToString(CultureInfo.CurrentCulture);
                txtArea.Text = parcel.Area.ToString(CultureInfo.CurrentCulture) + "sq. m.";

                if (instance.MainForm.Aboutlandforsale)
                {
                    txtForsale.Text = "L$ " + parcel.SalePrice.ToString(CultureInfo.CurrentCulture);
                    btnBuy.Visible = true;
                }
                else
                {
                    txtForsale.Text = "Not For Sale";
                    btnBuy.Visible = false;
                }

                // Obects tab
                txtSimprim.Text = parcel.SimWideTotalPrims.ToString(CultureInfo.CurrentCulture) + " out of " + parcel.SimWideMaxPrims.ToString(CultureInfo.CurrentCulture) + " (" + (parcel.SimWideMaxPrims - parcel.SimWideTotalPrims).ToString(CultureInfo.CurrentCulture) + " available)";
                txtParcelprim.Text = Math.Round(parcel.MaxPrims * parcel.ParcelPrimBonus).ToString(CultureInfo.CurrentCulture);
                txtPrimonparcel.Text = parcel.TotalPrims.ToString(CultureInfo.CurrentCulture);
                txtPrimOwnedowner.Text = parcel.OwnerPrims.ToString(CultureInfo.CurrentCulture);
                txtPrimsettogroup.Text = parcel.GroupPrims.ToString(CultureInfo.CurrentCulture);
                txtPrimownedothers.Text = parcel.OtherPrims.ToString(CultureInfo.CurrentCulture);
                txtPrimsel.Text = "";  // parcel. .SelectedPrims.ToString();
                txtPrimreturn.Text = parcel.OtherCleanTime.ToString(CultureInfo.CurrentCulture);
                lblTraffic.Text = parcel.Dwell.ToString(CultureInfo.CurrentCulture);
                txtMusic.Text = parcel.MusicURL;

                lblLocalID.Text = parcel.LocalID.ToString(CultureInfo.CurrentCulture);

                //pictureBox1.Image = OpenJPEGNet.OpenJPEG.DecodeToImage(parcel.Bitmap);  

                if (parcel.ParcelPrimBonus != 1) txtPrimBonus.Text = "Region Object Bonus Factor: " + parcel.ParcelPrimBonus.ToString(CultureInfo.CurrentCulture);
                else txtPrimBonus.Text = "";
                // Options tab
                if (instance.MainForm.AboutlandCreateObj) cbcreater.Checked = false;
                else cbcreater.Checked = true;

                if (instance.MainForm.AboutlandGroupCreateObj) cbcreateg.Checked = false;
                else cbcreateg.Checked = true;

                if (instance.MainForm.AboutShow) cbplace.Checked = true;
                else cbplace.Checked = true;

                if (instance.MainForm.AboutMature) cbmature.Checked = false;
                else cbmature.Checked = true;

                if (instance.MainForm.AllowOtherScripts) cbscriptsr.Checked = false;
                else cbscriptsr.Checked = true;

                if (!instance.MainForm.AboutlandAllowDamage) cbsafe.Checked = false;
                else cbsafe.Checked = true;

                if (instance.MainForm.AllowGroupScripts) cbscriptsg.Checked = false;
                else cbscriptsg.Checked = true;

                if (instance.MainForm.AboutAllowGroupObjectEntry) cbentryg.Checked = true;
                else cbentryg.Checked = false;

                if (instance.MainForm.AboutAllowAllObjectEntry) cbentryr.Checked = true;
                else cbentryr.Checked = false;

                if (instance.MainForm.AboutlandAllowFly) cbfly.Checked = false;
                else cbfly.Checked = true;

                if (instance.MainForm.Allowcreatelm) cblandmark.Checked = true;
                else cblandmark.Checked = false;

                if (instance.MainForm.AllowTerraform) cbTerrain.Checked = true;
                else cbTerrain.Checked = false;

                if (instance.MainForm.AboutlandRestrictPush) cbpush.Checked = true;
                else cbpush.Checked = false;

                if (instance.AllowVoice) cbVoice.Checked = true;
                else cbVoice.Checked = false;

                formloading = false;
            }
            catch (Exception ex)
            {
                Logger.Log("About Land error: " + ex.Message, Helpers.LogLevel.Error);
            }
        }

        void Groups_GroupNamesReply(object sender, GroupNamesEventArgs e)
        {
            if (!e.GroupNames.ContainsKey(parcel.GroupID)) return;

            client.Groups.GroupNamesReply -= Groups_GroupNamesReply;

            BeginInvoke(new MethodInvoker(delegate()
            {
                txtGroupOwner.Text = e.GroupNames[parcel.GroupID];
                pictureBox3.Visible = true;
            }));
        }

        private void Parcels_ParcelAccessListReply(object sender, ParcelAccessListReplyEventArgs e)
        {
            //bool hasauth = false;

            //if (grpID != UUID.Zero)
            //{
            //    if (HasGroupPower(GroupPowers.LandManageBanned, grpID))
            //    {
            //        hasauth = true;
            //    }
            //    else
            //    {
            //        hasauth = false;
            //    }
            //}

            //if (parcel.OwnerID != client.Self.AgentID && !hasauth) return;

            BeginInvoke(new MethodInvoker(delegate()
            {
                blacklist = e.AccessList;
                lvwBlackList.BeginUpdate();
                lvwBlackList.Items.Clear();

                foreach (ParcelManager.ParcelAccessEntry pe in blacklist)
                {
                    if (pe.AgentID != UUID.Zero) 
                    {
                    	ListViewItem item = lvwBlackList.Items.Add(pe.AgentID.ToString());
                    	item.Tag = pe;

                    	if (!instance.avnames.ContainsKey(pe.AgentID))
                    	{
                    	    client.Avatars.RequestAvatarName(pe.AgentID);
                    	}
                    	else
                    	{
                    	    ListViewItem foundItem = lvwBlackList.FindItemWithText(pe.AgentID.ToString());

                     	   if (foundItem != null)
                     	   {
                    	        foundItem.Text = instance.avnames[pe.AgentID].ToString();
                    	    }
                    	}
                    }
                }

                lvwBlackList.EndUpdate();

                if (lvwBlackList.Items.Count > 0)
                {
                    button4.Enabled = true;
                }
                else
                    button4.Enabled = false;

                //lvwBlackList.Sort();
            }));
        }

        private void Parcel_ObjectOwners(object sender, ParcelObjectOwnersReplyEventArgs ea)
        {
            BeginInvoke(new MethodInvoker(delegate()
            {
                lvwPrimOwners.BeginUpdate();
                lvwPrimOwners.Items.Clear();
                lvwPrimOwners.Enabled = true;

                List<ParcelManager.ParcelPrimOwners> primOwners = ea.PrimOwners;

                for (int i = 0; i < primOwners.Count; i++)
                {
                    if (primOwners[i].OwnerID != UUID.Zero)
                    {
                        try
                        {
                            ListViewItem item = lvwPrimOwners.Items.Add(primOwners[i].OwnerID.ToString());
                            item.Tag = primOwners[i].OwnerID;
                            item.SubItems.Add(primOwners[i].Count.ToString(CultureInfo.CurrentCulture));

                            //if (!instance.avnames.ContainsKey(primOwners[i].OwnerID))
                            //{
                            //    //foundItem.Text = primOwners[i].OwnerID.ToString(); 
                            //    client.Avatars.RequestAvatarName(primOwners[i].OwnerID);
                            //}
                            //else
                            //{
                            //    ListViewItem foundItem = lvwPrimOwners.FindItemWithText(instance.avnames[primOwners[i].OwnerID].ToString());

                            //    if (foundItem != null)
                            //    {
                            //        foundItem.Text = instance.avnames[primOwners[i].OwnerID].ToString();
                            //    }
                            //}

                            client.Avatars.RequestAvatarName(primOwners[i].OwnerID);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("About Land (object owners) error: " + ex.Message, Helpers.LogLevel.Error);
                        }
                    }
                }

                //button2.Enabled = true;

                lvwPrimOwners.EndUpdate();
                //lvwPrimOwners.Sort();
            }));

            //client.Parcels.ParcelObjectOwnersReply -= new EventHandler<ParcelObjectOwnersReplyEventArgs>(Parcel_ObjectOwners);
        }

        // separate thread
        private void Avatars_OnAvatarNames(object sender, UUIDNameReplyEventArgs names)
        {
            //BeginInvoke(new MethodInvoker(delegate()
            //{
            //    OwnerReceived(names.Names);
            //}));

            OwnerReceived(names.Names);
        }

        //runs on the GUI thread
        private void OwnerReceived(Dictionary<UUID, string> names)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    OwnerReceived(names);
                }));

                return;
            }

            lvwPrimOwners.BeginUpdate();

            foreach (KeyValuePair<UUID, string> av in names)
            {
                if (!instance.avnames.ContainsKey(av.Key))
                {
                    instance.avnames.Add(av.Key, av.Value);
                }

                ListViewItem foundItem = lvwPrimOwners.FindItemWithText(av.Key.ToString());
                //ListViewItem foundItem = lvwPrimOwners.FindItemWithText(instance.avnames[primOwners[i].OwnerID].ToString());

                try
                {
                    if (foundItem != null)
                    {
                        foundItem.Text = av.Value;
                    }
                }
                catch
                {
                    ;
                }
            }

            lvwPrimOwners.EndUpdate();

            lvwBlackList.BeginUpdate();

            foreach (KeyValuePair<UUID, string> av in names)
            {
                ListViewItem foundItem = lvwBlackList.FindItemWithText(av.Key.ToString());

                try
                {
                    if (foundItem != null)
                    {
                        foundItem.Text = av.Value;
                    }
                }
                catch
                {
                    ;
                }
            }

            lvwBlackList.EndUpdate();

            lvwBlackList.Sort();
        }

        private void tabGenral_Click(object sender, EventArgs e)
        {

        }

        private void UpdatePName()
        {
            if (formloading) return;

            parcel.Name = txtParcelname.Text;
            parcel.Update(client.Network.CurrentSim, false);
        }

        private void UpdateDesc()
        {
            if (formloading) return;

            parcel.Desc = txtParceldesc.Text.Replace("\r\n", "\n");
            parcel.Update(client.Network.CurrentSim, false);
        }

        private void txtParceldesc_Leave(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandChangeIdentity, grpID))
                {
                    return;
                }
            }

            UpdateDesc();
        }

        private void txtParceldesc_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtParcelname_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtParcelname_Leave(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandChangeIdentity, grpID))
                {
                    return;
                }
            }

            UpdatePName();
        }

        private void tabObjects_Click(object sender, EventArgs e)
        {

        }

        private void lvwPrimOwners_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvwPrimOwners.SelectedItems == null || lvwPrimOwners.SelectedItems.Count == 0)
            {
                btnReturn.Enabled = false;
                return;
            }

            btnReturn.Enabled = true;
        }

        private void btnReturn_Click(object sender, EventArgs e)
        {
            List<UUID> ownersID = new List<UUID>();

            UUID oitem = (UUID)lvwPrimOwners.SelectedItems[0].Tag;

            if (client.Self.AgentID == oitem)
            {
                DialogResult res = MessageBox.Show("You are about to return ALL your prims.\n" +
                                                   "Are you sure you want to continue?", 
                    "MEGAbolt Object Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (DialogResult.No == res)
                {
                    return;
                }
            }
            else
            {
                DialogResult res = MessageBox.Show($"You are about to return ALL prims owned by {lvwPrimOwners.SelectedItems[0].Text}.\n" +
                                                   $"Are you sure you want to continue?", 
                    "MEGAbolt Object Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (DialogResult.No == res)
                {
                    return;
                }
            }

            // TODO: disabled for user safety
            // this function returns all the objects belonging to the landowner regardless of what avatar UUID is passed!!
            // either a bug in libopenmv and/or SL

            ownersID.Add(oitem);

            ////client.Parcels.RequestSelectObjects(parcel.LocalID, ObjectReturnType.Owner, oitem);

            client.Parcels.ReturnObjects(client.Network.CurrentSim, parcel.LocalID, ObjectReturnType.List, ownersID);
            lvwPrimOwners.SelectedItems[0].Remove();
            //client.Parcels.RequestObjectOwners(client.Network.CurrentSim, parcel.LocalID);
            PopData();
            //btnSelect.Enabled = false;
            btnReturn.Enabled = false;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmAboutLand_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                client.Parcels.ParcelDwellReply -= Parcels_OnParcelDwell;
                client.Parcels.ParcelAccessListReply -= Parcels_ParcelAccessListReply;
                client.Parcels.ParcelObjectOwnersReply -= Parcel_ObjectOwners;
                client.Avatars.UUIDNameReply -= Avatars_OnAvatarNames;
                client.Groups.GroupMembersReply -= GroupMembersHandler;
            }
            catch { ; }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void cbTerrain_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cbTerrain.Checked)
            {
                parcel.Flags |= ParcelFlags.AllowTerraform;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.AllowTerraform;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void cblandmark_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cblandmark.Checked)
            {
                parcel.Flags |= ParcelFlags.AllowLandmark;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.AllowLandmark;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void txtMusic_Leave(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            //if (formloading) return;

            //this.parcel.MusicURL = txtMusic.Text;
            //this.parcel.Update(client.Network.CurrentSim, false);
        }

        private void cbfly_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cbfly.Checked)
            {
                parcel.Flags |= ParcelFlags.AllowFly;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.AllowFly;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void cbcreater_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cbcreater.Checked)
            {
                parcel.Flags |= ParcelFlags.CreateObjects;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.CreateObjects;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void cbcreateg_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cbcreateg.Checked)
            {
                parcel.Flags |= ParcelFlags.CreateGroupObjects;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.CreateGroupObjects;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void cbentryr_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cbentryr.Checked)
            {
                parcel.Flags |= ParcelFlags.AllowAPrimitiveEntry;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.AllowAPrimitiveEntry;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void cbentryg_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;


            if (cbentryg.Checked)
            {
                parcel.Flags |= ParcelFlags.AllowGroupObjectEntry;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.AllowGroupObjectEntry;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void cbscriptsr_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cbscriptsr.Checked)
            {
                parcel.Flags |= ParcelFlags.AllowOtherScripts;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.AllowOtherScripts;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void cbscriptsg_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cbscriptsg.Checked)
            {
                parcel.Flags |= ParcelFlags.AllowGroupScripts;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.AllowGroupScripts;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void cbsafe_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (!cbsafe.Checked)
            {
                parcel.Flags |= ParcelFlags.AllowDamage;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.AllowDamage;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void cbpush_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cbpush.Checked)
            {
                parcel.Flags |= ParcelFlags.RestrictPushObject;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.RestrictPushObject;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void cbplace_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cbplace.Checked)
            {
                parcel.Flags |= ParcelFlags.ShowDirectory;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.ShowDirectory;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void cbmature_CheckedChanged(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cbmature.Checked)
            {
                parcel.Flags |= ParcelFlags.MaturePublish;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.MaturePublish;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //if (!parcelowner) return;

            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            parcel.MusicURL = txtMusic.Text;
            parcel.Update(client.Network.CurrentSim, false);
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void lvwBlackList_SelectedIndexChanged(object sender, EventArgs e)
        {
            button2.Enabled = (lvwBlackList.SelectedItems.Count > 0);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (lvwBlackList.SelectedItems.Count > 0)
            {
                try
                {
                    //ParcelManager.ParcelAccessEntry p1 = new ParcelManager.ParcelAccessEntry();
                    //p1.AgentID = (UUID)lvwBlackList.SelectedItems[0].Tag;
                    //p1.Flags = AccessList.Ban;

                    ParcelManager.ParcelAccessEntry person = new ParcelManager.ParcelAccessEntry();

                    person = (ParcelManager.ParcelAccessEntry)lvwBlackList.SelectedItems[0].Tag;
                    //person.Flags = AccessList.Access;

                    blacklist.Remove(person);
                    parcel.AccessBlackList = blacklist;
                    parcel.AccessBlackList.Remove(person);
                    parcel.Update(client.Network.CurrentSim, false);

                    client.Parcels.RequestParcelAccessList(client.Network.CurrentSim, parcel.LocalID, AccessList.Ban, 1);

                    button2.Enabled = false;
                }
                catch { ; }
            }
        }

        private void chkPublic_CheckedChanged(object sender, EventArgs e)
        {
            //if (chkPublic.Checked)
            //{
            //    this.parcel.Flags |= ParcelFlags.;
            //}
            //else
            //{
            //    this.parcel.Flags &= ~ParcelFlags.MaturePublish;
            //}

            //this.parcel.Update(client.Network.CurrentSim, false);
        }

        private void frmAboutLand_Load(object sender, EventArgs e)
        {
            CenterToParent();
        }

        private void lvwBlackList_DoubleClick(object sender, EventArgs e)
        {
            if (lvwBlackList.SelectedItems.Count > 0)
            {
                ParcelManager.ParcelAccessEntry person = (ParcelManager.ParcelAccessEntry)lvwBlackList.SelectedItems[0].Tag;

                (new frmProfile(instance, lvwBlackList.SelectedItems[0].Text, person.AgentID)).Show();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (lvwBlackList.Items.Count > 0)   // && lstMembers2.Columns[0].ToString != "none")
            {
                Stream tstream;
                saveFileDialog1.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 1;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if ((tstream = saveFileDialog1.OpenFile()) != null)
                        {
                            StreamWriter SW = new StreamWriter(tstream);

                            string parcelname = client.Network.CurrentSim.ToString() + "/" + txtParcelname.Text.Replace(",", " ");

                            SW.WriteLine("Land: " + parcelname + ",List Type: Blacklist,Ttl avatars: " + lvwBlackList.Items.Count.ToString(CultureInfo.CurrentCulture));
                            SW.WriteLine(",,");
                            SW.WriteLine("Name,UUID,");

                            for (int i = 0; i < lvwBlackList.Items.Count; i++)
                            {
                                ParcelManager.ParcelAccessEntry person = (ParcelManager.ParcelAccessEntry)lvwBlackList.Items[i].Tag;
                                string personame = lvwBlackList.Items[i].Text;

                                string line = personame + "," + person.AgentID.ToString() + ",";
                                SW.WriteLine(line);
                            }

                            SW.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                        instance.CrashReporter.Post(ex);
                    }
                }
            }
        }

        private void btnBuy_Click(object sender, EventArgs e)
        {
            if (parcel != null)
            {
                int localid = parcel.LocalID;
                int saleprice = parcel.SalePrice;
                int parcelarea = parcel.Area;
                Simulator simulator = client.Network.CurrentSim;
                UUID buyerid = parcel.AuthBuyerID;

                if ((((buyerid == UUID.Zero) || (buyerid == client.Self.AgentID))) && ((parcel.Flags & ParcelFlags.ForSale) == ParcelFlags.ForSale))
                {
                    client.Parcels.Buy(simulator, localid, false, UUID.Zero, false, parcelarea, saleprice);
                }
                else
                {
                    MessageBox.Show("Parcel could not be bought! It is either not for sale\n or it is on sale to a specific avatar.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void frmAboutLand_Shown(object sender, EventArgs e)
        {
            RequestParcelDets();
        }

        private void RequestParcelDets()
        {
            client.Parcels.RequestObjectOwners(client.Network.CurrentSim, parcel.LocalID);
            client.Parcels.RequestParcelAccessList(client.Network.CurrentSim, parcel.LocalID, AccessList.Ban, 1);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            (new frmProfile(instance, instance.MainForm.AboutlandOwneridname, parcel.OwnerID)).Show();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            (new frmGroupInfo(parcel.GroupID, instance)).Show();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (grpID != UUID.Zero)
            {
                if (!HasGroupPower(GroupPowers.LandOptions, grpID))
                {
                    return;
                }
            }

            if (formloading) return;

            if (cbVoice.Checked)
            {
                parcel.Flags |= ParcelFlags.AllowVoiceChat;
            }
            else
            {
                parcel.Flags &= ~ParcelFlags.AllowVoiceChat;
            }

            parcel.Update(client.Network.CurrentSim, false);
        }
    }
}