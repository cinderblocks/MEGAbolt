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
using MEGAbolt.NetworkComm;
using OpenMetaverse;
using OpenMetaverse.Assets;
using System.Globalization;

namespace METAbolt
{
    public partial class frmNotecardEditor : Form
    {
        private METAboltInstance instance;
        private MEGAboltNetcom netcom;
        private GridClient client;
        private InventoryItem item;
        //private UUID uploadID;
        //private UUID transferID;
        private AssetNotecard receivedAsset;

        private bool closePending = false;
        private bool saving = false;
        private bool changed = false;
        //private UUID aid = UUID.Zero;
        private string lheader = string.Empty;
        private UUID assetUUID = UUID.Zero;
        private string notecardContent = string.Empty;  

        int start = 0;
        int indexOfSearchText = 0;
        string prevsearchtxt = string.Empty;
        private UUID objectid = UUID.Zero;
        private bool istaskobj = false;
        private bool nreadonly = false;

        public frmNotecardEditor(METAboltInstance instance, InventoryItem item)
        {
            InitializeComponent();

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;
            this.item = item;

            Disposed += Notecard_Disposed;

            AddNetcomEvents();

            objectid = UUID.Zero;
            istaskobj = false; 
            
            Text = $"{item.Name} (notecard) - MEGAbolt";

            assetUUID = item.AssetUUID;

            rtbNotecard.TextChanged += rtbNotecard_TextChanged;

            var transferID = UUID.Random();
            client.Assets.RequestInventoryAsset(assetUUID, item.UUID, UUID.Zero,
                item.OwnerID, item.AssetType, true, transferID, Assets_OnAssetReceived);
        }

        public frmNotecardEditor(METAboltInstance instance, InventoryItem item, bool nreadonly)
        {
            InitializeComponent();

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;
            this.item = item;
            this.nreadonly = nreadonly;

            Disposed += Notecard_Disposed;

            AddNetcomEvents();

            objectid = UUID.Zero;
            istaskobj = false;

            Text = $"{item.Name} (notecard) - MEGAbolt";

            assetUUID = item.AssetUUID;

            rtbNotecard.TextChanged += rtbNotecard_TextChanged;

            var transferID = UUID.Random();
            client.Assets.RequestInventoryAsset(assetUUID, item.UUID, UUID.Zero, 
                item.OwnerID, item.AssetType, true, transferID, Assets_OnAssetReceived);

            if (nreadonly)
            {
                rtbNotecard.ReadOnly = true;
                btnSave.Enabled = false;
                toolStripDropDownButton1.Enabled = false;
                toolStripDropDownButton2.Enabled = false;
                rtbNotecard.BackColor = Color.AliceBlue;
            }
        }

        public frmNotecardEditor(METAboltInstance instance, InventoryNotecard item, Primitive obj)
        {
            InitializeComponent();

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;
            this.item = item;
            AddNetcomEvents();
            objectid = obj.ID;
            istaskobj = true; 

            Text = $"{item.Name} (notecard) - MEGAbolt";

            assetUUID = item.AssetUUID;

            rtbNotecard.TextChanged += rtbNotecard_TextChanged;

            var transferID = UUID.Random();
            client.Assets.RequestInventoryAsset(assetUUID, item.UUID, obj.ID, 
                item.OwnerID, item.AssetType, true, transferID, Assets_OnAssetReceived);
        }

        //Separate thread
        private void Assets_OnAssetReceived(AssetDownload transfer, Asset asset)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                    {
                        Assets_OnAssetReceived(transfer, asset);
                    }
                ));

                return;
            }

            //if (transfer.AssetType != AssetType.Notecard) return;

            if (!transfer.Success)
            {
                notecardContent = "Unable to download notecard.";
                SetNotecardText(notecardContent, false);
                return;
            }

            receivedAsset = (AssetNotecard)asset;
            notecardContent = Utils.BytesToString(transfer.AssetData);

            SetNotecardText(notecardContent, false);
        }

        private void SetNotecardText(string text, bool readOnly)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    SetNotecardText(text, readOnly);
                }
                ));

                return;
            }

            rtbNotecard.Clear();

            if (readOnly)
            {
                rtbNotecard.Text = notecardContent; 
                rtbNotecard.ReadOnly = true;
                rtbNotecard.BackColor = Color.FromKnownColor(KnownColor.Control);
            }
            else
            {
                rtbNotecard.Text = GetBody(text);

                rtbNotecard.ReadOnly = false;
                rtbNotecard.BackColor = Color.White;
            }

            btnClose.Enabled = true;
            tsStatus.Text = "Ready.";
            PB1.Visible = false;

            if (!nreadonly)
            {
                btnSave.Enabled = true;
                tsSave.Enabled = true;
                tsSaveDisk.Enabled = true;
                rtbNotecard.BackColor = Color.White;
            }
            else
            {
                rtbNotecard.ReadOnly = true;
                btnSave.Enabled = false;
                toolStripDropDownButton1.Enabled = false;
                toolStripDropDownButton2.Enabled = false;
                rtbNotecard.BackColor = Color.AliceBlue;
            }
        }

        private string GetBody(string body)
        {
            string text = string.Empty;
  
            try
            {
                text = body.Trim();

                int pos = text.IndexOf("Text length", 0, StringComparison.CurrentCultureIgnoreCase);
                pos += 11;

                lheader = text.Substring(0, pos).ToString(CultureInfo.CurrentCulture);

                text = text.Substring(pos).Trim();

                // get the first lf
                pos = text.IndexOf("\n", 0, StringComparison.CurrentCultureIgnoreCase);

                if (pos > -1)
                {
                    // Get the text
                    text = text.Substring(pos).Trim();
                }

                // Get rid of the }
                text = text.Substring(0, text.Length - 1).Trim();
            }
            catch { return text; }

            return text;
        }

        private void AddNetcomEvents()
        {
            netcom.ClientLoggedOut += netcom_ClientLoggedOut;
        }

        private void Notecard_Disposed(object sender, EventArgs e)
        {
            netcom.ClientLoggedOut -= netcom_ClientLoggedOut;
            rtbNotecard.TextChanged -= rtbNotecard_TextChanged;
        }

        private void netcom_ClientLoggedOut(object sender, EventArgs e)
        {
            closePending = false;
            Close();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private static DialogResult AskForSave()
        {
            return MessageBox.Show(
                "Your changes have not been saved. Save the notecard?",
                "MEGAbolt",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
        }

        private void SaveNotecard()
        {
            try
            {
                saving = true;

                rtbNotecard.ReadOnly = true;
                rtbNotecard.BackColor = Color.FromKnownColor(KnownColor.Control);

                tsStatus.Text = "Saving notecard...";
                //lblSaveStatus.Visible = true;
                btnSave.Enabled = false;
                btnClose.Enabled = false;
                tsSave.Enabled = false;

                receivedAsset.AssetData = CreateNotecardAsset(rtbNotecard.Text);

                if (istaskobj)
                {
                    client.Inventory.RequestUpdateNotecardTask(receivedAsset.AssetData, item.UUID, objectid, OnNoteUpdate);
                }
                else
                {
                    client.Inventory.RequestUploadNotecardAsset(receivedAsset.AssetData, item.UUID, OnNoteUpdate);
                }
            }
            catch { ; }

            changed = false;
        }

        /// <summary>
        /// </summary>
        /// <param name="body"></param>
        public byte[] CreateNotecardAsset(string body)
        {
            body = body.Trim();
 
            //// Format the string body into Linden text
            //string lindenText = "Linden text version 1\n";
            //lindenText += "{\n";
            //lindenText += "LLEmbeddedItems version 1\n";
            //lindenText += "{\n";
            //lindenText += "count 0\n";
            //lindenText += "}\n";
            //lindenText += "Text length " + body.Length + "\n";
            //lindenText += body;
            //lindenText += "}\n";

            string lindenText = lheader; 
            lindenText += " " + body.Length + "\n";
            lindenText += body;
            lindenText += "}\n";


            // Assume this is a string, add 1 for the null terminator
            byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes(lindenText);
            byte[] assetData = new byte[stringBytes.Length]; //+ 1];
            Array.Copy(stringBytes, 0, assetData, 0, stringBytes.Length);

            return assetData;
        }

        public string CreateNotecardTextAsset(string body)
        {
            body = body.Trim();

            // Format the string body into Linden text
            //string lindenText = "Linden text version 1\n";
            //lindenText += "{\n";
            //lindenText += "LLEmbeddedItems version 1\n";
            //lindenText += "{\n";
            //lindenText += "count 0\n";
            //lindenText += "}\n";
            //lindenText += "Text length " + body.Length + "\n";
            //lindenText += body;
            //lindenText += "}\n";

            string lindenText = lheader;
            lindenText += " " + body.Length + "\n";
            lindenText += body;
            lindenText += "}\n";

            return lindenText;
        }


        void OnNoteUpdate(bool success, string status, UUID itemID, UUID assetID)
        {
            if (success)
            {
                saving = false;

                if (closePending)
                {
                    closePending = false;
                    Close();
                    return;
                }

                BeginInvoke(new MethodInvoker(SaveComplete));
            }    
        }

        private void SaveComplete()
        {
            rtbNotecard.ReadOnly = false;
            rtbNotecard.BackColor = Color.White;
            btnClose.Enabled = true;
            btnSave.Enabled = false;
            tsSave.Enabled = false;

            tsStatus.Text = "Save completed.";
            //lblSaveStatus.Visible = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveNotecard();
        }

        private void frmNotecardEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (closePending || saving)
                e.Cancel = true;
            else if (changed)
            {
                DialogResult result = AskForSave();

                switch (result)
                {
                    case DialogResult.Yes:
                        e.Cancel = true;
                        closePending = true;
                        SaveNotecard();
                        break;

                    case DialogResult.No:
                        e.Cancel = false;
                        break;

                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void tsSaveDisk_Click(object sender, EventArgs e)
        {
            SaveToDisk();
        }

        private void SaveToDisk()
        {
            // Create a SaveFileDialog to request a path and file name to save to.
            SaveFileDialog saveFile1 = new SaveFileDialog();

            string logdir = DataFolder.GetDataFolder();

            saveFile1.InitialDirectory = logdir;

            // Initialize the SaveFileDialog to specify the RTF extension for the file.
            saveFile1.DefaultExt = "*.rtf";
            saveFile1.Filter = "txt files (*.txt)|*.txt|RTF Files (*.rtf)|*.rtf";  //"RTF Files|*.rtf";
            saveFile1.Title = "Save chat contents to hard disk...";

            // Determine if the user selected a file name from the saveFileDialog.
            if (saveFile1.ShowDialog() == DialogResult.OK &&
               saveFile1.FileName.Length > 0)
            {
                if (saveFile1.FileName.Substring(saveFile1.FileName.Length - 3) == "rtf")
                {
                    // Save the contents of the RichTextBox into the file.
                    rtbNotecard.SaveFile(saveFile1.FileName, RichTextBoxStreamType.RichText);
                }
                else
                {
                    rtbNotecard.SaveFile(saveFile1.FileName, RichTextBoxStreamType.PlainText);
                }
            }

            saveFile1.Dispose(); 
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            SaveNotecard();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void toolStripDropDownButton2_Click(object sender, EventArgs e)
        {

        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbNotecard.Undo(); 
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbNotecard.Redo(); 
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbNotecard.Cut(); 
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbNotecard.Copy(); 
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbNotecard.Paste(); 
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbNotecard.SelectAll(); 
        }

        private void indentRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbNotecard.SelectionIndent += 50;
        }

        private void indentLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbNotecard.SelectionIndent += -50;

            if (rtbNotecard.SelectionIndent < 0)
                rtbNotecard.SelectionIndent = 10;
        }

        private void findReplaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStrip1.Visible = true;
            toolStrip2.Visible = true;
            tsReplaceText.Focus();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tsFindText.Text)) return;
  
            if (tsChkCase.Checked)
            {
                if (tsChkWord.Checked)
                {
                    rtbNotecard.FindAndReplace(tsFindText.Text, tsReplaceText.Text, false, true, true);
                }
                else
                {
                    rtbNotecard.FindAndReplace(tsFindText.Text, tsReplaceText.Text, false, true, false);
                }
            }
            else
            {
                if (tsChkWord.Checked)
                {
                    rtbNotecard.FindAndReplace(tsFindText.Text, tsReplaceText.Text, false, false, true);
                }
                else
                {
                    rtbNotecard.FindAndReplace(tsFindText.Text, tsReplaceText.Text, false, false, false);
                }
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tsFindText.Text)) return;

            if (tsChkCase.Checked)
            {
                if (tsChkWord.Checked)
                {
                    rtbNotecard.FindAndReplace(tsFindText.Text, tsReplaceText.Text, true, true, true);
                }
                else
                {
                    rtbNotecard.FindAndReplace(tsFindText.Text, tsReplaceText.Text, true, true, false);
                }
            }
            else
            {
                if (tsChkWord.Checked)
                {
                    rtbNotecard.FindAndReplace(tsFindText.Text, tsReplaceText.Text, true, false, true);
                }
                else
                {
                    rtbNotecard.FindAndReplace(tsFindText.Text, tsReplaceText.Text, true, false, false);
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            toolStrip2.Visible = false;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            // All this could go into the extended rtb component in the future

            int startindex = 0;

            if (!string.IsNullOrEmpty(prevsearchtxt))
            {
                if (prevsearchtxt != tsFindText.Text.Trim())
                {
                    startindex = 0;
                    start = 0;
                    indexOfSearchText = 0;
                }
            }

            prevsearchtxt = tsFindText.Text.Trim();

            //int linenumber = rtbScript.GetLineFromCharIndex(rtbScript.SelectionStart) + 1;
            //Point pnt = rtbScript.GetPositionFromCharIndex(rtbScript.SelectionStart);

            if (tsFindText.Text.Length > 0)
                startindex = FindNext(tsFindText.Text.Trim(), start, rtbNotecard.Text.Length);

            // If string was found in the RichTextBox, highlight it
            if (startindex > 0)
            {
                // Set the highlight color as red
                rtbNotecard.SelectionColor = Color.LightBlue;
                // Find the end index. End Index = number of characters in textbox
                int endindex = tsFindText.Text.Length;
                // Highlight the search string
                rtbNotecard.Select(startindex, endindex);
                // mark the start position after the position of 
                // last search string
                start = startindex + endindex;

                if (start == rtbNotecard.TextLength || start > rtbNotecard.TextLength)
                {
                    startindex = 0;
                    start = 0;
                    indexOfSearchText = 0;
                }
            }
            else if (startindex == -1)
            {
                startindex = 0;
                start = 0;
                indexOfSearchText = 0;
            }
        }

        public int FindNext(string txtToSearch, int searchStart, int searchEnd)
        {
            // Unselect the previously searched string
            if (searchStart > 0 && searchEnd > 0 && indexOfSearchText >= 0)
            {
                rtbNotecard.Undo();
            }

            // Set the return value to -1 by default.
            int retVal = -1;

            // A valid starting index should be specified.
            // if indexOfSearchText = -1, the end of search
            if (searchStart >= 0 && indexOfSearchText >= 0)
            {
                // A valid ending index 
                if (searchEnd > searchStart || searchEnd == -1)
                {
                    // Determine if it's a match case or what
                    RichTextBoxFinds mcase = RichTextBoxFinds.None;

                    if (tsChkCase.Checked)
                    {
                        mcase = RichTextBoxFinds.MatchCase;
                    }


                    if (tsChkWord.Checked)
                    {
                        mcase |= RichTextBoxFinds.WholeWord;
                    }

                    // Find the position of search string in RichTextBox
                    indexOfSearchText = rtbNotecard.Find(txtToSearch, searchStart, searchEnd, mcase);
                    // Determine whether the text was found in richTextBox1.
                    if (indexOfSearchText != -1)
                    {
                        // Return the index to the specified search text.
                        retVal = indexOfSearchText;
                    }
                }
            }

            return retVal;
        }

        private void GetCurrentLine()
        {
            int linenumber = rtbNotecard.GetLineFromCharIndex(rtbNotecard.SelectionStart) + 1;
            tsLn.Text = "Ln " + linenumber.ToString(CultureInfo.CurrentCulture);
        }

        private void GetCurrentCol()
        {
            int colnumber = rtbNotecard.SelectionStart - rtbNotecard.GetFirstCharIndexOfCurrentLine() + 1;
            tsCol.Text = "Ln " + colnumber.ToString(CultureInfo.CurrentCulture);
        }

        private void rtbNotecard_TextChanged(object sender, EventArgs e)
        {
            if (!rtbNotecard.ReadOnly)
            {
                btnSave.Enabled = true;
                tsSave.Enabled = true;
                tsSaveDisk.Enabled = true;
                changed = true;
            }
            else
            {
                btnSave.Enabled = false;
            }
        }

        private void rtbNotecard_SelectionChanged(object sender, EventArgs e)
        {
            if (nreadonly)
            {
                rtbNotecard.Select(0, 0); 
                return;
            }

            GetCurrentLine();
            GetCurrentCol();
        }
       
        private void frmNotecardEditor_Load(object sender, EventArgs e)
        {
            CenterToParent();
            
            if (nreadonly)
            {
                rtbNotecard.ReadOnly = true;
                btnSave.Enabled = false;
                toolStripDropDownButton1.Enabled = false;
                toolStripDropDownButton2.Enabled = false;
                rtbNotecard.BackColor = Color.AliceBlue; 
            }
        }

        private void rtbNotecard_KeyDown(object sender, KeyEventArgs e)
        {
            if (nreadonly)
            {
                if ((e.Control) && (e.KeyCode == Keys.C))
                {
                    e.Handled = true;
                }
            }
        }
    }
}