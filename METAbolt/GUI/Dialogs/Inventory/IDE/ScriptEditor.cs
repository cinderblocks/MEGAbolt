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
using System.Text;
using System.Windows.Forms;
using MEGAbolt.NetworkComm;
using OpenMetaverse;
using OpenMetaverse.Assets;
using ScintillaNET;
using System.IO;
using System.Globalization;

namespace METAbolt
{
    public partial class frmScriptEditor : Form
    {
        private METAboltInstance instance;
        private MEGAboltNetcom netcom;
        private GridClient client;
        private InventoryItem item;
        
        private bool closePending = false;
        private bool saving = false;
        private bool ointernal = false;
        private bool istaskobj = false;
        private UUID objectid = UUID.Zero;  
        
        private const int LINE_NUMBERS_MARGIN_WIDTH = 35;
        private UUID assetUUID = UUID.Zero;
        private UUID itemUUID = UUID.Zero;
        private List<string> calltip = new();
        private List<string> calltipheader = new();
        private bool showingcalltip = false;


        public frmScriptEditor(METAboltInstance instance, InventoryItem item)
        {
            InitializeComponent();
            Disposed += Script_Disposed;

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;
            this.item = item;
            istaskobj = false;
            panel1.Visible = false;
            //checkBox2.Visible = false;

            AddNetcomEvents();
            
            Text = item.Name + " (script) - MEGAbolt";

            SetScintilla();
            GetCallTips();

            assetUUID = item.AssetUUID;
            var transferID = UUID.Random();
            client.Assets.RequestInventoryAsset(assetUUID, item.UUID, UUID.Zero, 
                item.OwnerID, item.AssetType, true, transferID, Assets_OnAssetReceived);
        }

        public frmScriptEditor(METAboltInstance instance, InventoryLSL item, Primitive obj)
        {
            InitializeComponent();
            Disposed += Script_Disposed;

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;
            this.item = item;
            istaskobj = true;
            panel1.Visible = true;
            //checkBox2.Visible = false;

            AddNetcomEvents();

            Text = item.Name + " (script) - MEGAbolt";

            SetScintilla();
            GetCallTips();

            assetUUID = item.AssetUUID;
            objectid = obj.ID;
            itemUUID = item.UUID;

            var transferID = UUID.Random();
            client.Assets.RequestInventoryAsset(assetUUID, item.UUID, obj.ID, 
                obj.OwnerID, item.AssetType, true, transferID, Assets_OnAssetReceived);
        }

        public frmScriptEditor(METAboltInstance instance)
        {
            InitializeComponent();
            Disposed += Script_Disposed;

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;
            panel1.Visible = false;

            AddNetcomEvents();

            PB1.Visible = false;
            tsStatus.Text = "Ready.";

            SetScintilla();
            GetCallTips();
        }

        private void SetScintilla()
        {
            try
            {
                SetLanguage("lsl");
                tscboLanguage.SelectedIndex = 0;

                rtbScript.Margins[0].Width = LINE_NUMBERS_MARGIN_WIDTH;
                rtbScript.CaretForeColor = Color.Red;
                
                rtbScript.CaretLineBackColor = Color.Linen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                ex.Message,
                "MEGAbolt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            }
        }

        private void GetCallTips()
        {
            string fileContent = Properties.Resources.LSL_Functions;
            using (var reader = new StringReader(fileContent))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    calltip.Add(line);

                    string[] split = line.Split('|');
                    calltipheader.Add(split[0]);
                }
            }
        }

        private void SetupSort()
        {
            rtbScript.CharAdded += Document_CharAdded;
        }

        private void Document_CharAdded(object sender, CharAddedEventArgs e)
        {
            if (e.Char == '(')
            {
                showingcalltip = true;

                return;
            }
            else if (e.Char == ')')
            {
                showingcalltip = false;
            }

            if (showingcalltip)
            {
                return;
            }

            Line ln = rtbScript.Lines[rtbScript.CurrentLine];

            if (ln.Text.Contains("//"))
            {
                //int lng = ln.Length;
                int idx = ln.Text.IndexOf("//", StringComparison.CurrentCultureIgnoreCase);

                int cpos = rtbScript.GetColumn(rtbScript.CurrentPosition);

                if (cpos > idx)
                {
                    return;
                }
            }

            if (e.Char == ' ')
                return;

            int pos = rtbScript.CurrentPosition;
            string word = rtbScript.GetWordFromPosition(pos);

            if (string.IsNullOrEmpty(word)) { return; }
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

            if (transfer.AssetType != AssetType.LSLText) return;

            try
            {
                string scriptContent;

                if (!transfer.Success)
                {
                    scriptContent = "Unable to download script. Make sure you have the proper permissions!";
                    SetScriptText(scriptContent, false);
                    return;
                }
                
                scriptContent = Utils.BytesToString(transfer.AssetData);
                SetScriptText(scriptContent, false);

                if (istaskobj)
                {
                    client.Inventory.ScriptRunningReply += Inventory_ScriptRunningReply;
                    client.Inventory.RequestGetScriptRunning(objectid, itemUUID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                ex.Message,
                "MEGAbolt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            }
        }

        private void Inventory_ScriptRunningReply(object sender, ScriptRunningReplyEventArgs e)
        {
            BeginInvoke(new MethodInvoker(delegate()
            {
                checkBox1.Checked = e.IsMono;
                checkBox2.Checked = e.IsRunning;
            }));
             
            client.Inventory.ScriptRunningReply -= Inventory_ScriptRunningReply;  
        }

        //UI thread
        private delegate void OnSetScriptText(string text, bool readOnly);
        private void SetScriptText(string text, bool readOnly)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    SetScriptText(text, readOnly);
                }));

                return;
            }

            try
            {
                rtbScript.EmptyUndoBuffer();
                rtbScript.SetSavePoint();
                rtbScript.Text = text;

                if (readOnly)
                {
                    rtbScript.ReadOnly = true;
                    rtbScript.BackColor = Color.FromKnownColor(KnownColor.Control);
                }
                else
                {
                    rtbScript.ReadOnly = false;
                    rtbScript.BackColor = Color.White;
                }
                
                PB1.Visible = false;
                tsStatus.Text = "Ready.";

                if (!rtbScript.ReadOnly)
                {
                    if (!ointernal)
                    {
                        tsSave.Enabled = true;
                        tsbSave.Enabled = true; 
                    }

                    ointernal = false;
                    tsSaveDisk.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                ex.Message,
                "MEGAbolt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            }
        }

        private void AddNetcomEvents()
        {
            netcom.ClientLoggedOut += netcom_ClientLoggedOut;
        }

        private void Script_Disposed(object sender, EventArgs e)
        {
            netcom.ClientLoggedOut -= netcom_ClientLoggedOut;
            rtbScript.CharAdded -= Document_CharAdded;
        }

        private void netcom_ClientLoggedOut(object sender, EventArgs e)
        {
            closePending = false;
            Close();
        }

        private static DialogResult AskForSave()
        {
            return MessageBox.Show(
                "Your changes have not been saved. Save the script?",
                "MEGAbolt",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

        }

        private void SaveScript()
        {
            if (!netcom.IsLoggedIn)
            {
                tsSave.Enabled = false;
                tsbSave.Enabled = false;
                return;
            }
            try
            {
                PB1.Visible = true;
                saving = true;

                rtbScript.ReadOnly = true;
                rtbScript.BackColor = Color.FromKnownColor(KnownColor.Control);

                tsStatus.Text = "Saving script...";
                tsStatus.Visible = true;
                tsSave.Enabled = false;
                tsbSave.Enabled = false;
                tsSaveDisk.Enabled = false;

                if (istaskobj)
                {
                    client.Inventory.RequestUpdateScriptTask(CreateScriptAsset(rtbScript.Text), item.UUID, objectid, checkBox1.Checked, checkBox2.Checked, OnScriptUpdate);
                }
                else
                {
                    client.Inventory.RequestUpdateScriptAgentInventory(CreateScriptAsset(rtbScript.Text), item.UUID, true, OnScriptUpdate);
                }

                //changed = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                ex.Message,
                "MEGAbolt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            }
        }

        private static byte[] CreateScriptAsset(string body)
        {
            try
            {
                body = body.Trim();

                // Format the string body into Linden text
                string lindenText = body;

                // Assume this is a string, add 1 for the null terminator
                byte[] stringBytes = Encoding.UTF8.GetBytes(lindenText);
                byte[] assetData = new byte[stringBytes.Length]; //+ 1];
                Array.Copy(stringBytes, 0, assetData, 0, stringBytes.Length);
                return assetData;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                ex.Message,
                "MEGAbolt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            }

            return null;
        }

        void OnScriptUpdate(bool success, string status, bool compile, List<string> messages, UUID itemID, UUID assetID)
        {
            if (!success) { return; }

            BeginInvoke((MethodInvoker)delegate
            {
                label1.Text = string.Empty;
            });

            if (!compile)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    string line = messages[0];
                    string[] errs = line.Split(':');
                    string pos = errs[0].Trim().Replace("(", "");
                    pos = pos.Trim().Replace(")", "");
                    string[] posxy = pos.Split(',');

                    int posx = Convert.ToInt32(posxy[0].Trim(), CultureInfo.CurrentCulture);

                    rtbScript.GotoPosition(rtbScript.Lines[posx-1].Position);

                    label1.Text = $"Compile {errs[1].Trim()}: {errs[2].Trim().Replace("\n", "")} @ line: {posxy[0].Trim()}";
                });
            }

            saving = false;
            BeginInvoke(new MethodInvoker(SaveComplete));

            if (closePending)
            {
                closePending = false;
                Close();
                return;
            }
        }

        //UI thread
        private void SaveComplete()
        {
            rtbScript.ReadOnly = false;
            rtbScript.BackColor = Color.White;
            tsSave.Enabled = false;
            tsbSave.Enabled = false;
            tsSaveDisk.Enabled = true;
            rtbScript.SetSavePoint();
            PB1.Visible = false;

            tsStatus.Text = "Save completed.";
        }

        private void frmScriptEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (closePending || saving)
            {
                e.Cancel = true;
            }
            else if (rtbScript.Modified)
            {
                if (!ointernal)
                {
                    DialogResult result = AskForSave();

                    switch (result)
                    {
                        case DialogResult.Yes:
                            closePending = true;
                            SaveScript();

                            e.Cancel = saving;
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
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.Paste();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.SelectAll(); 
        }       

        private void SaveToDisk()
        {
            // Create a SaveFileDialog to request a path and file name to save to.
            SaveFileDialog saveFile1 = new SaveFileDialog();

            string logdir = DataFolder.GetDataFolder();

            saveFile1.InitialDirectory = logdir;

            // Initialize the SaveFileDialog to specify the RTF extension for the file.
            saveFile1.DefaultExt = "*.lsl";
            saveFile1.Filter = "LSL Files (*.lsl)|*.lsl|C# Class (*.cs)|*.cs|XML Files (*.xml)|*.xml|HTML Files (*.html)|*.html|Java Script (*.js)|*.js|VB Script (*.vb)|*.vb|PHP Files (*.php)|*.php|INI Files (*.ini)|*.ini|AIML Files (*.aiml)|*.aiml|TXT Files (*.txt)|*.txt|RTF Files (*.rtf)|*.rtf|All Files (*.*)|*.*";  //"RTF Files|*.rtf";
            saveFile1.Title = "Save to hard disk...";

            // Determine if the user selected a file name from the saveFileDialog.
            if (saveFile1.ShowDialog() == DialogResult.OK &&
               saveFile1.FileName.Length > 0)
            {
                using (FileStream fs = File.Create(saveFile1.FileName))
                using (BinaryWriter bw = new BinaryWriter(fs))
                    bw.Write(rtbScript.Text.ToCharArray(), 0, rtbScript.Text.Length - 1); // Omit trailing NULL

                rtbScript.SetSavePoint();
                tsSaveDisk.Enabled = false;
            }

            saveFile1.Dispose(); 
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.Redo(); 
        }

        private void GetCurrentLine()
        {
            int lnm = rtbScript.CurrentLine + 1;
            tsLn.Text = "Ln " + lnm.ToString(CultureInfo.CurrentCulture);
        }

        private void GetCurrentCol()
        {
            tsCol.Text = "Col " + rtbScript.GetColumn(rtbScript.CurrentPosition).ToString(CultureInfo.CurrentCulture);
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close(); 
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            toolStrip2.Visible = false; 
        }

        private void tsSaveDisk_Click(object sender, EventArgs e)
        {
            SaveToDisk();
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            SaveScript();
        }

        private void SetLanguage(string language)
        {
            if ("ini".Equals(language, StringComparison.OrdinalIgnoreCase))
            {
                // Reset/set all styles and prepare scintilla for custom lexing
                //this.IniLexer = true;
                IniLexer.Init(rtbScript);
            }
            else
            {
                // Use a built-in lexer and configuration
                //ActiveDocument.IniLexer = false;

                rtbScript.LexerLanguage = language;
            }

            SetupSort();
        }

        private void frmScriptEditor_Load(object sender, EventArgs e)
        {
            CenterToParent();
        }

        private void goToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //rtbScript.GoTo.ShowGoToDialog();
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //rtbScript.FindReplace.ShowFind(); 
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //rtbScript.FindReplace.ShowReplace(); 
        }

        private void advancedToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void previousBookmarkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int markerPos = rtbScript.Lines[rtbScript.CurrentLine].MarkerPrevious(1);
            if (markerPos != -1)
            {
                rtbScript.GotoPosition(rtbScript.Lines[markerPos].Position);
            }
        }

        private void toggleBookmarkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Line currentLine = rtbScript.Lines[rtbScript.CurrentLine];
            if (currentLine.MarkerGet() == 0)
            {
                currentLine.MarkerAdd(0);
            }
			else
			{
				currentLine.MarkerDelete(0);
			}
        }

        private void nextBookmarkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int markerPos = rtbScript.Lines[rtbScript.CurrentLine].MarkerNext(1);
            if (markerPos != -1)
            {
                rtbScript.GotoPosition(rtbScript.Lines[markerPos].Position);
            }
        }

        private void clearBookmarksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.MarkerDeleteAll(0);
        }

        private void dropToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //rtbScript.DropMarkers.Drop();
        }

        private void collectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //rtbScript.DropMarkers.Collect();
        }

        private void insertSnippetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //rtbScript.Snippets.ShowSnippetList();
        }

        private void makeUpperCaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.ExecuteCmd(Command.Uppercase);
        }

        private void makeLowerCaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.ExecuteCmd(Command.Lowercase);
        }

        private void commentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //rtbScript.ExecuteCmd(BindableCommand.LineComment);
        }

        private void uncommentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //rtbScript.ExecuteCmd(BindableCommand.LineUncomment);
        }

        private void contectHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tscboLanguage.SelectedItem.ToString()?.ToLower(CultureInfo.CurrentCulture) == "lsl")
            {
                string hword = rtbScript.GetWordFromPosition(rtbScript.CurrentPosition);
                string surl = "http://wiki.secondlife.com/wiki/" + hword;
                System.Diagnostics.Process.Start(@surl);
            }
        }

        private void tscboLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            string lang = string.Empty;

            switch (tscboLanguage.SelectedItem.ToString()?.ToLower(CultureInfo.CurrentCulture))
            {
                case "c#":
                    lang = "cs";
                    break;

                case "html":
                    lang = "html";
                    break;

                case "sql":
                    lang = "mssql";
                    break;

                case "vbscript":
                    lang = "vbscript";
                    break;

                case "xml":
                    lang = "xml";
                    break;

                case "java":
                    lang = "js";
                    break;

                case "lsl":
                    lang = "lsl";
                    break;

                case "php":
                    lang = "php";
                    break;

                case "ini":
                    lang = "ini";
                    break;

                case "aiml":
                    lang = "xml";
                    break;

                case "text":
                    lang = "default";
                    break;

                case "bat/cmd":
                    lang = "bat";
                    break;
            }

            SetLanguage(lang);
        }

        private void tscboLanguage_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton2_Click_1(object sender, EventArgs e)
        {
            //rtbScript.ExecuteCmd(BindableCommand.LineComment);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            //rtbScript.ExecuteCmd(BindableCommand.LineUncomment);
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            var markerPos = rtbScript.Lines[rtbScript.CurrentLine].MarkerPrevious(1);
            if (markerPos != -1)
            {
                rtbScript.GotoPosition(rtbScript.Lines[markerPos].Position);
            }
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            var markerPos = rtbScript.Lines[rtbScript.CurrentLine].MarkerNext(1);
            if (markerPos != -1)
            {
                rtbScript.GotoPosition(rtbScript.Lines[markerPos].Position);
            }
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            rtbScript.ExecuteCmd(Command.BackTab);
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            rtbScript.ExecuteCmd(Command.Tab);
        }

        private void whitespaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.ViewWhitespace = whitespaceToolStripMenuItem.Checked ? WhitespaceMode.VisibleAlways : WhitespaceMode.Invisible;
        }

        private void wordWrapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.WrapMode = wordWrapToolStripMenuItem.Checked ? WrapMode.Word : WrapMode.None;
        }

        private void endOfLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.EndAtLastLine = endOfLineToolStripMenuItem.Checked;
        }

        private void lineNumbersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.Margins[0].Width = lineNumbersToolStripMenuItem.Checked ? LINE_NUMBERS_MARGIN_WIDTH : 0;
        }

        private void foldLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.Lines[rtbScript.CurrentLine].FoldLine(FoldAction.Contract);
        }

        private void unfoldLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.Lines[rtbScript.CurrentLine].FoldLine(FoldAction.Expand);
        }

        private void foldAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.FoldAll(FoldAction.Contract);
        }

        private void unfoldAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.FoldAll(FoldAction.Expand);
        }

        private void navigateForwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //rtbScript.DocumentNavigation.NavigateForward();
        }

        private void navigateBackwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //rtbScript.DocumentNavigation.NavigateBackward();
        }

        private void tsLn_DoubleClick(object sender, EventArgs e)
        {
            //rtbScript.GoTo.ShowGoToDialog();
        }

        private void tsCol_DoubleClick(object sender, EventArgs e)
        {
            //rtbScript.GoTo.ShowGoToDialog();
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            SaveToDisk();
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            rtbScript.Cut();
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            rtbScript.Copy();
        }

        private void toolStripButton15_Click(object sender, EventArgs e)
        {
            rtbScript.Paste();
        }

        private void toolStripButton16_Click(object sender, EventArgs e)
        {
            rtbScript.Undo();
        }

        private void toolStripButton17_Click(object sender, EventArgs e)
        {
            rtbScript.Redo();
        }

        private void lSLFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ointernal = true;

            string lslb = @"
default
{
    state_entry()
    {
        llSay(0,'Hello MEGAbolt user');
     }
}";

            rtbScript.Text = lslb;
            SetLanguage("lsl");
            tscboLanguage.SelectedIndex = 0;
        }

        private void cClassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tsSaveDisk.Enabled = true;

            string csb = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using METAbolt;  // don't forget to add METAbolt as a reference

namespace MEGAbolt
{
    class Class1
    {
    }
}";

            rtbScript.Text = csb;
            SetLanguage("c#");
            tscboLanguage.SelectedIndex = 1;
        }

        private void xMLFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tsSaveDisk.Enabled = true;

            string xmlb = @"
<?xml version=""1.0""?>
    <Level1>
      <level2>
      </level2>
    </Level1>";

            rtbScript.Text = xmlb;
            SetLanguage("xml");
            tscboLanguage.SelectedIndex = 6;
        }

        private void hTNLFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tsSaveDisk.Enabled = true;

            string htmlb = @"
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">

<head>
<meta content=""text/html; charset=utf-8"" http-equiv=""Content-Type"" />
<title>Untitled 1</title>
</head>

<body>

</body>

</html>";

            rtbScript.Text = htmlb;
            SetLanguage("html");
            tscboLanguage.SelectedIndex = 7;
        }

        private void textFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbScript.Text = string.Empty;
            SetLanguage("text");
            tscboLanguage.SelectedIndex = 10;
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = @"LSL Files (*.lsl)|*.lsl|C# Class (*.cs)|*.cs|XML Files (*.xml)|*.xml|HTML Files (*.html)|*.html|Java Script (*.js)|*.js|VB Script (*.vb)|*.vb|PHP Files (*.php)|*.php|INI Files (*.ini)|*.ini|AIML Files (*.aiml)|*.aiml|TXT Files (*.txt)|*.txt|RTF Files (*.rtf)|*.rtf|All Files (*.*)|*.*";  //"RTF Files|*.rtf"; 
            OpenFile();
        }

        private void OpenFile()
        {
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            int ind = openFileDialog.FilterIndex;
            openFileDialog.FilterIndex = 1;
            openFileDialog.InitialDirectory = DataFolder.GetDataFolder();

            OpenFile(openFileDialog.FileName, ind);
        }

        private void OpenFile(string filePath, int ind)
        {
            rtbScript.Text = File.ReadAllText(filePath);
            rtbScript.EmptyUndoBuffer();
            rtbScript.SetSavePoint();
            Text = filePath;   //Path.GetFileName(filePath);

            string ext = Path.GetExtension(filePath).ToLower(CultureInfo.CurrentCulture);

            switch (ext)
            {
                case ".cs":
                    tscboLanguage.SelectedIndex = 1;
                    break;

                case ".html":
                    tscboLanguage.SelectedIndex = 7;
                    break;

                case ".htm":
                    tscboLanguage.SelectedIndex = 7;
                    break;

                case ".sql":
                    tscboLanguage.SelectedIndex = 2;
                    break;

                case ".vb":
                    tscboLanguage.SelectedIndex = 3;
                    break;

                case ".xml":
                    tscboLanguage.SelectedIndex = 6;
                    break;

                case ".js":
                    tscboLanguage.SelectedIndex = 4;
                    break;

                case ".lsl":
                    tscboLanguage.SelectedIndex = 0;
                    break;

                case ".php":
                    tscboLanguage.SelectedIndex = 5;
                    break;

                case ".ini":
                    tscboLanguage.SelectedIndex = 8;
                    break;

                case ".aiml":
                    tscboLanguage.SelectedIndex = 9;
                    break;

                case ".txt":
                    tscboLanguage.SelectedIndex = 10;
                    break;

                default:
                    tscboLanguage.SelectedIndex = 10;
                    break;
            }

            tsSaveDisk.Enabled = true;
        }

        private void rtbScript_SelectionChanged(object sender, EventArgs e)
        {
            GetCurrentLine();
            GetCurrentCol();
        }

        private void rtbScript_TextChanged(object sender, EventArgs e)
        {
            if (!rtbScript.ReadOnly)
            {
                if (!ointernal)
                {
                    if (netcom.IsLoggedIn)
                    {
                        tsSave.Enabled = true;
                        tsbSave.Enabled = true;
                    }
                    else
                    {
                        tsSave.Enabled = false;
                        tsbSave.Enabled = false;
                    }
                }

                tsSaveDisk.Enabled = true;
                //changed = true;
            }
        }

        private void PB1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripDropDownButton1_Click(object sender, EventArgs e)
        {

        }

        private void tsbSave_Click(object sender, EventArgs e)
        {
            SaveScript();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void rtbScript_MouseMove(object sender, MouseEventArgs e)
        {
            int pos = rtbScript.CharPositionFromPoint(e.X, e.Y);
            string word = rtbScript.GetWordFromPosition(pos);

            if (string.IsNullOrEmpty(word))
            {
                rtbScript.CallTipCancel();
                return;
            }

            int idx = calltipheader.IndexOf(word);

            if (idx == -1)
            {
                rtbScript.CallTipCancel();
                return;
            }

            string tip = calltip[idx];

            string[] split = tip.Split('|');
            string function = @split[1];
            string cti = @split[2];

            if (function.Length > 50)
            {
                string lo = function.Substring(0, 50);
                int ind = lo.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase);
                function = function.Insert(ind + 1, "\n");
            }

            if (function.Length > 100)
            {
                string lo = function.Substring(0, 100);
                int ind = lo.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase);
                function = function.Insert(ind + 1, "\n");
            }

            if (function.Length > 150)
            {
                string lo = function.Substring(0, 150);
                int ind = lo.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase);
                function = function.Insert(ind + 1, "\n");
            }

            if (cti.Length > 50)
            {
                string lo = cti.Substring(0, 50);
                int ind = lo.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase);
                cti = cti.Insert(ind + 1, "\n");
            }

            if (cti.Length > 100)
            {
                string lo = cti.Substring(0, 100);
                int ind = lo.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase);
                cti = cti.Insert(ind + 1, "\n");
            }

            if (cti.Length > 150)
            {
                string lo = cti.Substring(0, 150);
                int ind = lo.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase);
                cti = cti.Insert(ind + 1, "\n");
            }

            if (cti.Length > 200)
            {
                string lo = cti.Substring(0, 200);
                int ind = lo.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase);
                cti = cti.Insert(ind + 1, "\n");
            }

            if (cti.Length > 250)
            {
                string lo = cti.Substring(0, 250);
                int ind = lo.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase);
                cti = cti.Insert(ind + 1, "\n");
            }

            if (cti.Length > 300)
            {
                string lo = cti.Substring(0, 300);
                int ind = lo.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase);
                cti = cti.Insert(ind + 1, "\n");
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(function);
            sb.AppendLine(string.Empty);
            sb.AppendLine(cti);
            sb.AppendLine(string.Empty);
            sb.AppendLine("[Click on function/event & press F1 for info/sample code]");

            rtbScript.CallTipShow(pos, sb.ToString());
        }

        private void rtbScript_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Back or Keys.Delete)
            {
                Line lnt = rtbScript.Lines[rtbScript.CurrentLine];
                int aind = lnt.Text.IndexOf("(", 0, StringComparison.CurrentCultureIgnoreCase);

                if (aind == -1)
                {
                    showingcalltip = false;
                    rtbScript.CallTipCancel();
                    return;
                }
            }
        }
    }
}