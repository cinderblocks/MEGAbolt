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
using System.Windows.Forms;
using MEGAbolt.NetworkComm;
using OpenMetaverse;
using WeCantSpell.Hunspell;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;


namespace MEGAbolt
{
    public partial class IMTabWindow : UserControl
    {
        private readonly MEGAboltInstance instance;
        private readonly MEGAboltNetcom netcom;

        private bool typing = false;
        //private bool pasted = false;
        //private const int WM_KEYUP = 0x101;
        private const int WM_KEYDOWN = 0x100;
        private readonly TabsConsole tab;

        private WordList spellChecker = null;
        private string avloc = string.Empty;


        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                BugSplat crashReporter = new BugSplat(Generated.BugsplatDatabase, "MEGAbolt",
                    Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                {
                    User = "cinder@cinderblocks.biz",
                    ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                };
                crashReporter.Post(e.Exception);
            }
        }

        public IMTabWindow(MEGAboltInstance instance, UUID target, UUID session, string toName)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            netcom = this.instance.Netcom;

            TargetId = target;
            SessionId = session;
            TargetName = toName;
            tab = instance.TabConsole;

            TextManager = new IMTextManager(this.instance, new RichTextBoxPrinter(instance, rtbIMText), SessionId, TargetName);
            Disposed += IMTabWindow_Disposed;

            AddNetcomEvents();

            tsbTyping.ToolTipText = $"{toName} is typing";

            ApplyConfig(this.instance.Config.CurrentConfig);
            this.instance.Config.ConfigApplied += Config_ConfigApplied;

            CreateSmileys();

            if (this.instance.IMHistyoryExists(TargetName, false))
            {
                toolStripButton2.Enabled = true; 
            }

            tbtnProfile.ToolTipText = $"{toName} 's Profile";
        }

        private void CreateSmileys()
        {
            var _menuItem = new EmoticonMenuItem(Smileys.AngelSmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[0].Tag = "angelsmile;";

            _menuItem = new EmoticonMenuItem(Smileys.AngrySmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[1].Tag = "angry;";

            _menuItem = new EmoticonMenuItem(Smileys.Beer);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[2].Tag = "beer;";

            _menuItem = new EmoticonMenuItem(Smileys.BrokenHeart);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[3].Tag = "brokenheart;";

            _menuItem = new EmoticonMenuItem(Smileys.bye);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[4].Tag = "bye";

            _menuItem = new EmoticonMenuItem(Smileys.clap);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[5].Tag = "clap;";

            _menuItem = new EmoticonMenuItem(Smileys.ConfusedSmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[6].Tag = ":S";

            _menuItem = new EmoticonMenuItem(Smileys.cool);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[7].Tag = "cool;";

            _menuItem = new EmoticonMenuItem(Smileys.CrySmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[8].Tag = "cry;";

            //_menuItem.BarBreak = true;

            _menuItem = new EmoticonMenuItem(Smileys.DevilSmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[9].Tag = "devil;";

            _menuItem = new EmoticonMenuItem(Smileys.duh);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[10].Tag = "duh;";

            _menuItem = new EmoticonMenuItem(Smileys.EmbarassedSmile);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[11].Tag = "embarassed;";

            _menuItem = new EmoticonMenuItem(Smileys.happy0037);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[12].Tag = ":)";

            _menuItem = new EmoticonMenuItem(Smileys.heart);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[13].Tag = "heart;";

            _menuItem = new EmoticonMenuItem(Smileys.kiss);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[14].Tag = "muah;";

            //_menuItem.BarBreak = true;

            _menuItem = new EmoticonMenuItem(Smileys.help);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[15].Tag = "help";

            _menuItem = new EmoticonMenuItem(Smileys.liar);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[16].Tag = "liar;";

            _menuItem = new EmoticonMenuItem(Smileys.lol);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[17].Tag = "lol";

            _menuItem = new EmoticonMenuItem(Smileys.oops);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[18].Tag = "oops";

            _menuItem = new EmoticonMenuItem(Smileys.sad);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[19].Tag = ":(";

            _menuItem = new EmoticonMenuItem(Smileys.shhh);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[20].Tag = "shhh";

            //_menuItem.BarBreak = true;

            _menuItem = new EmoticonMenuItem(Smileys.sigh);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[21].Tag = "sigh";

            _menuItem = new EmoticonMenuItem(Smileys.silenced);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[22].Tag = ":X";

            _menuItem = new EmoticonMenuItem(Smileys.think);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[23].Tag = "thinking;";

            _menuItem = new EmoticonMenuItem(Smileys.ThumbsUp);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[24].Tag = "thumbsup;";

            _menuItem = new EmoticonMenuItem(Smileys.whistle);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[25].Tag = "whistle;";

            _menuItem = new EmoticonMenuItem(Smileys.wink);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[26].Tag = ";)";

            //_menuItem.BarBreak = true;

            _menuItem = new EmoticonMenuItem(Smileys.wow);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[27].Tag = "wow";

            _menuItem = new EmoticonMenuItem(Smileys.yawn);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[28].Tag = "yawn;";

            _menuItem = new EmoticonMenuItem(Smileys.zzzzz);
            _menuItem.Click += cmenu_Emoticons_Click;
            cmenu_Emoticons.Items.Add(_menuItem);
            cmenu_Emoticons.Items[29].Tag = "zzzzz";

            _menuItem.Dispose(); 
        }

        // When an emoticon is clicked, insert its image into to RTF
        private void cmenu_Emoticons_Click(object _sender, EventArgs _args)
        {
            // Write the code here
            EmoticonMenuItem _item = (EmoticonMenuItem)_sender;

            cbxInput.Text += _item.Tag.ToString();
            cbxInput.Select(cbxInput.Text.Length, 0);
            //cbxInput.Focus();
        }

        private void netcom_InstantMessageReceived(object sender, InstantMessageEventArgs e)
        {
            if (e.IM.IMSessionID != SessionId)
            {
                return;
            }

            tsbTyping.Visible = e.IM.Dialog == InstantMessageDialog.StartTyping;

            string avsim = e.Simulator.Name;
            Vector3 avloccords = e.IM.Position;

            if (avloccords != Vector3.Zero)
            {
                avloc = "http://slurl.com/secondlife/" + avsim + "/" + (avloccords.X - 1).ToString(CultureInfo.CurrentCulture) + "/" + avloccords.Y.ToString(CultureInfo.CurrentCulture) + "/" + avloccords.Z.ToString(CultureInfo.CurrentCulture);

                tsbLocation.Enabled = true;
                tsbLocation.ToolTipText = $"{e.IM.FromAgentName}'s Location";
            }
        }

        private void Config_ConfigApplied(object sender, ConfigAppliedEventArgs e)
        {
            ApplyConfig(e.AppliedConfig);
        }

        private void ApplyConfig(Config config)
        {
            if (config.InterfaceStyle == 0) //System
                toolStrip1.RenderMode = ToolStripRenderMode.System;
            else if (config.InterfaceStyle == 1) //Office 2003
                toolStrip1.RenderMode = ToolStripRenderMode.ManagerRenderMode;

            if (instance.Config.CurrentConfig.EnableSpelling)
            {
                var spellLang = instance.Config.CurrentConfig.SpellLanguage;

                var assembly = Assembly.GetExecutingAssembly();
                using var dictResourceStream = assembly.GetManifestResourceStream($"MEGAbolt.Spelling.{spellLang}.dic");
                using var affResourceStream = assembly.GetManifestResourceStream($"MEGAbolt.Spelling.{spellLang}.aff");
                if (dictResourceStream == null || affResourceStream == null)
                {
                    spellChecker = null;
                }
                else
                {
                    var csvFile = $"{DataFolder.GetDataFolder()}\\{spellLang}.csv";

                    if (!File.Exists(csvFile))
                    {
                        using StreamWriter sw = File.CreateText(csvFile);
                        sw.Dispose();
                    }

                    spellChecker = WordList.CreateFromStreams(dictResourceStream, affResourceStream);
                }
            }
            else
            {
                spellChecker = null;
            }
        }

        private void AddNetcomEvents()
        {
            netcom.ClientLoginStatus += netcom_ClientLoginStatus;
            netcom.ClientDisconnected += netcom_ClientDisconnected;
            netcom.InstantMessageReceived += netcom_InstantMessageReceived;
        }

        private void RemoveNetcomEvents()
        {
            netcom.ClientLoginStatus -= netcom_ClientLoginStatus;
            netcom.ClientDisconnected -= netcom_ClientDisconnected;
            instance.Config.ConfigApplied -= Config_ConfigApplied;
            netcom.InstantMessageReceived -= netcom_InstantMessageReceived;
        }

        private void netcom_ClientLoginStatus(object sender, LoginProgressEventArgs e)
        {
            if (e.Status != LoginStatus.Success) return;

            RefreshControls();
        }

        private void netcom_ClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            RefreshControls();
        }

        private void IMTabWindow_Disposed(object sender, EventArgs e)
        {
            CleanUp();
        }

        public void CleanUp()
        {
            cbxInput.Text = string.Empty;  
            instance.TabConsole.RemoveTabEntry(SessionId.ToString());
            TextManager.CleanUp();
            TextManager = null;
            TargetId = UUID.Zero;  
            RemoveNetcomEvents();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendIM(cbxInput.Text);

            //this.ClearIMInput();
        }

        private void cbxInput_TextChanged(object sender, EventArgs e)
        {
            RefreshControls();
        }

        private void RefreshControls()
        {
            if (!netcom.IsLoggedIn)
            {
                cbxInput.Enabled = false;
                btnSend.Enabled = false;
                //this.cbxInput.Text = string.Empty;
                TargetId = UUID.Zero;  
                return;
            }

            if (cbxInput.Text.Length > 0)
            {
                btnSend.Enabled = true;

                if (!typing)
                {
                    netcom.SendIMStartTyping(TargetId, SessionId);
                    typing = true;
                }
            }
            else
            {
                btnSend.Enabled = false;
                netcom.SendIMStopTyping(TargetId, SessionId);
                typing = false;
            }
        }

        private void cbxInput_KeyUp(object sender, KeyEventArgs e)
        {
            //if (pasted)
            //{
            //    int pos = cbxInput.SelectionStart;
            //    cbxInput.SelectionLength = cbxInput.Text.Length - pos;
            //    cbxInput.Text = cbxInput.SelectedText;
            //    pasted = false;
            //}

            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            if (cbxInput.Text.Length == 0) return;

            //netcom.SendInstantMessage(cbxInput.Text, target, session);
            SendIM(cbxInput.Text);

            //this.ClearIMInput();
        }

        public void SendIM(string message)
        {
            if (message.Length == 0) return;

            message = message.TrimEnd();

            //message = message.Replace("http://secondlife:///", "secondlife:///");
            //message = message.Replace("http://secondlife://", "secondlife:///");

            message = instance.CleanReplace("http://secondlife:///", "secondlife:///", message);
            message = instance.CleanReplace("http://secondlife://", "secondlife:///", message);

            if (instance.Config.CurrentConfig.EnableSpelling && spellChecker != null)
            {
                // put preference check here
                //string cword = Regex.Replace(cbxInput.Text, @"[^a-zA-Z0-9]", "");
                //string[] swords = cword.Split(' ');
                string[] swords = message.Split(' ');
                bool hasmistake = false;
                bool correct = true;

                foreach (string word in swords)
                {
                    string cword = Regex.Replace(word, @"[^a-zA-Z0-9]", "");

                    try
                    {
                        correct = spellChecker.Check(cword);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "Dictionary is not loaded")
                        {
                            instance.Config.ApplyCurrentConfig();
                            correct = spellChecker.Check(cword);
                        }
                        else
                        {
                            Logger.Log("Spellcheck error IM: " + ex.Message, Helpers.LogLevel.Error);
                        }
                    }

                    if (!correct)
                    {
                        hasmistake = true;
                        break;
                    }
                }

                if (hasmistake)
                {
                    (new frmSpelling(instance, message, swords, false, TargetId, SessionId)).Show();

                    ClearIMInput();
                    hasmistake = false;
                    return;
                }
            }

            string message1 = string.Empty;
            string message2 = string.Empty;

            if (message.Length > 1023)
            {
                message1 = message.Substring(0, 1022);
                netcom.SendInstantMessage(message1, TargetId, SessionId);

                if (message.Length > 2046)
                {
                    message2 = message.Substring(1023, 2045);
                    netcom.SendInstantMessage(message2, TargetId, SessionId);
                }
            }
            else
            {
                netcom.SendInstantMessage(message, TargetId, SessionId); ;
            }

            ClearIMInput();
        }

        private void ClearIMInput()
        {
            cbxInput.Items.Add(cbxInput.Text);
            cbxInput.Text = string.Empty;
        }

        public void SelectIMInput()
        {
            cbxInput.Select();
        }

        private void tbtnProfile_Click(object sender, EventArgs e)
        {
            (new frmProfile(instance, TargetName, TargetId)).Show();
        }

        private void cbxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) e.SuppressKeyPress = true;

            //if (e.Control && e.KeyCode == Keys.V)
            //{
            //    ClipboardAsync Clipboard2 = new ClipboardAsync();
            //    cbxInput.Text += Clipboard2.GetText(TextDataFormat.UnicodeText).Replace(Environment.NewLine, "\r\n");

            //    //pasted = true;
            //}
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //if ((keyData == (Keys.Control | Keys.C)))
            //{
            //    //your implementation
            //    return true;
            //}

            if ((keyData == (Keys.Control | Keys.V)))
            {
                //ClipboardAsync Clipboard2 = new ClipboardAsync();

                //string ptxt = Clipboard2.GetText(TextDataFormat.UnicodeText).Replace(Environment.NewLine, "\r\n");
                //cbxInput.Text += ptxt;
                //return true;

                ClipboardAsync Clipboard2 = new ClipboardAsync();

                string insertText = Clipboard2.GetText(TextDataFormat.UnicodeText).Replace(Environment.NewLine, "\r\n");
                int selectionIndex = cbxInput.SelectionStart;
                cbxInput.Text = cbxInput.Text.Insert(selectionIndex, insertText);
                cbxInput.SelectionStart = selectionIndex + insertText.Length;

                return true;
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        public UUID TargetId { get; set; }

        public string TargetName { get; set; }

        public UUID SessionId { get; set; }

        public IMTextManager TextManager { get; set; }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void rtbIMText_TextChanged(object sender, EventArgs e)
        {

        }

        private void rtbIMText_LinkClicked_1(object sender, LinkClickedEventArgs e)
        {
            if (e.LinkText.StartsWith("http://slurl.", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    // Open up the TP form here
                    string encoded = HttpUtility.UrlDecode(e.LinkText);
                    string[] split = encoded.Split(new Char[] { '/' });
                    //string[] split = e.LinkText.Split(new Char[] { '/' });
                    string simr = split[4];
                    double x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                    (new frmTeleport(instance, simr, (float)x, (float)y, (float)z, false)).Show();
                }
                catch { ; }

            }
            else if (e.LinkText.StartsWith("http://maps.secondlife", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    // Open up the TP form here
                    string encoded = HttpUtility.UrlDecode(e.LinkText);
                    string[] split = encoded.Split(new Char[] { '/' });
                    //string[] split = e.LinkText.Split(new Char[] { '/' });
                    string simr = split[4];
                    double x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                    double z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                    (new frmTeleport(instance, simr, (float)x, (float)y, (float)z, true)).Show();
                }
                catch { ; }

            }
            else if (e.LinkText.Contains("http://mbprofile:"))
            {
                try
                {
                    string encoded = HttpUtility.UrlDecode(e.LinkText);
                    string[] split = encoded.Split(new Char[] { '/' });
                    //string[] split = e.LinkText.Split(new Char[] { '#' });
                    string aavname = split[0];
                    string[] avnamesplit = aavname.Split(new Char[] { '#' });
                    aavname = avnamesplit[0];

                    split = e.LinkText.Split(new Char[] { ':' });
                    string elink = split[2].ToString(CultureInfo.CurrentCulture);
                    split = elink.Split(new Char[] { '&' });

                    UUID avid = (UUID)split[0];

                    (new frmProfile(instance, aavname, avid)).Show();
                }
                catch { ; }
            }
            else if (e.LinkText.Contains("http://secondlife:///"))
            {
                // Open up the Group Info form here
                string encoded = HttpUtility.UrlDecode(e.LinkText);
                string[] split = encoded.Split(new Char[] { '/' });
                UUID uuid = UUID.Zero;

                try
                {
                    uuid = (UUID)split[7];
                }
                catch
                {
                    uuid = UUID.Zero;
                }

                if (uuid != UUID.Zero && split[6].ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture) == "group")
                {
                    frmGroupInfo frm = new frmGroupInfo(uuid, instance);
                    frm.Show();
                }
                else if (uuid != UUID.Zero && split[6].ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture) == "agent")
                {
                    (new frmProfile(instance, string.Empty, uuid)).Show();
                }
            }
            else if (e.LinkText.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase) || e.LinkText.StartsWith("ftp://", StringComparison.CurrentCultureIgnoreCase) || e.LinkText.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase))
            {
                Utilities.OpenBrowser(e.LinkText);
            }
            else
            {
                Utilities.OpenBrowser("http://" + e.LinkText);
            }
        }

        private void tsbSave_Click(object sender, EventArgs e)
        {
            // Create a SaveFileDialog to request a path and file name to save to.
            SaveFileDialog saveFile1 = new SaveFileDialog();

            string logdir = DataFolder.GetDataFolder();
            logdir += "\\Logs\\";

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
                    rtbIMText.SaveFile(saveFile1.FileName, RichTextBoxStreamType.RichText);
                }
                else
                {
                    rtbIMText.SaveFile(saveFile1.FileName, RichTextBoxStreamType.PlainText);
                }
            }

            saveFile1.Dispose(); 
        }

        private void tsbClear_Click(object sender, EventArgs e)
        {
            TextManager.ClearAllText();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (instance.IsAvatarMuted(TargetId, TargetName))
            {
                MessageBox.Show($"{TargetName} is already in your mute list.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            //DataRow dr = instance.MuteList.NewRow();
            //dr["uuid"] = target;
            //dr["mute_name"] = toName;
            //instance.MuteList.Rows.Add(dr);

            instance.Client.Self.UpdateMuteListEntry(MuteType.Resident, TargetId, instance.avnames[TargetId]);

            MessageBox.Show($"{TargetName} is now muted.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);      
        }

        private void cbxInput_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        protected override bool ProcessKeyPreview(ref Message m)
        {
            const int WM_CHAR = 0x102;
            const int WM_SYSCHAR = 0x106;
            const int WM_SYSKEYDOWN = 0x104;
            //const int WM_SYSKEYUP = 0x105;
            const int WM_IME_CHAR = 0x286;

            KeyEventArgs e = null;

            if ((m.Msg != WM_CHAR) && (m.Msg != WM_SYSCHAR) && (m.Msg != WM_IME_CHAR))
            {
                e = new KeyEventArgs(((Keys)((int)((long)m.WParam))) | ModifierKeys);
                if (m.Msg is WM_KEYDOWN or WM_SYSKEYDOWN)
                {
                    TrappedKeyDown(e);
                }

                if (e.SuppressKeyPress)
                {
                    tab.tabs["chat"].Select();
                    MEGAboltTab stab = tab.GetTab(TargetName);
                    stab.Close(); 
                }

                if (e.Handled)
                {
                    return e.Handled;
                }
            }

            return base.ProcessKeyPreview(ref m);
        }

        private void TrappedKeyDown(KeyEventArgs e)
        {
            if (e.Alt && e.KeyCode == Keys.X) 
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            frmHistory frm = new frmHistory(instance, TargetName, false);
            frm.Show();
        }

        private void tsbLocation_Click(object sender, EventArgs e)
        {
            try
            {
                // Open up the TP form here
                string encoded = HttpUtility.UrlDecode(avloc);
                string[] split = encoded.Split(new Char[] { '/' });
                //string[] split = e.LinkText.Split(new Char[] { '/' });
                string sim = split[4];
                double x = Convert.ToDouble(split[5].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                double y = Convert.ToDouble(split[6].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);
                double z = Convert.ToDouble(split[7].ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                (new frmTeleport(instance, sim, (float)x, (float)y, (float)z, false)).Show();
            }
            catch { ; }
        }

        //private void rtbIMText_Leave(object sender, EventArgs e)
        //{
        //    //rtbIMText.HideSelection = true;
        //}
    }
}
