using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using OpenMetaverse;
using System.Text.RegularExpressions;
using MEGAbolt.NetworkComm;
using WeCantSpell.Hunspell;

namespace MEGAbolt
{
    public partial class frmSpelling : Form
    {
        private MEGAboltInstance instance;
        private MEGAboltNetcom netcom;
        private string dir = DataFolder.GetDataFolder() + "\\Spelling\\";
        //private string words = string.Empty;
        private int start = 0;
        private int indexOfSearchText = 0;
        private string[] swords;
        //private int swordind = -1;
        private string currentword = string.Empty;
        private List<string> mistakes = new List<string>();
        private ChatType ctype;

        private string spellLang;
        private WordList spellChecker = null;
        private bool ischat = true;
        //private string tabname = string.Empty;
        private UUID target = UUID.Zero;
        private UUID session = UUID.Zero;
        private bool isgroup = false;

        public frmSpelling(MEGAboltInstance instance, string sentence, string[] swords, ChatType type)
        {
            InitializeComponent();

            this.instance = instance;

            spellLang = instance.Config.CurrentConfig.SpellLanguage;

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

            //words = sentence;
            richTextBox1.Text = sentence;
            this.swords = swords;
            ctype = type;

            ischat = true;
        }

        public frmSpelling(MEGAboltInstance instance, string sentence, string[] swords, bool type, UUID target, UUID session)
        {
            InitializeComponent();

            this.instance = instance;
            netcom = this.instance.Netcom;

            spellLang = instance.Config.CurrentConfig.SpellLanguage;

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

            //words = sentence;
            richTextBox1.Text = sentence;
            this.swords = swords;

            isgroup = type;
            ischat = false;
            this.target = target;
            this.session = session; 
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //instance.TabConsole.chatConsole._textBox = richTextBox1.Text;

            Close(); 
        }

        private void frmSpelling_Load(object sender, EventArgs e)
        {
            CheckSpellings();
        }

        private void CheckSpellings()
        {
            mistakes.Clear();
            listBox1.Items.Clear();  
 
            foreach (string word in swords)
            {
                string cword = Regex.Replace(word, @"[^a-zA-Z0-9]", "");

                bool correct = spellChecker.Check(cword);

                if (!correct)
                {
                    InHighligtWord(cword);

                    mistakes.Add(cword);
                }
            }

            if (mistakes.Count > 0)
            {
                start = 0;
                indexOfSearchText = 0;

                currentword = mistakes[0];

                HighligtWord(currentword);

                var suggestions = spellChecker.Suggest(currentword);

                foreach (var entry in suggestions)
                {
                    listBox1.Items.Add(entry);
                }
            }
        }

        private void ContSearch()
        {
            listBox1.Items.Clear();
            button4.Enabled = false; 

            if (currentword.Contains(currentword))
            {
                mistakes.Remove(currentword);
            }

            if (mistakes.Count < 1)
            {
                //if (ischat)
                //{
                //    instance.TabConsole.chatConsole._textBox = richTextBox1.Text;
                //}
                currentword = string.Empty;

                Close();
            }
            else
            {
                currentword = mistakes[0];

                HighligtWord(mistakes[0]);

                var suggestions = spellChecker.Suggest(mistakes[0]);

                foreach (var entry in suggestions)
                {
                    listBox1.Items.Add(entry);
                }
            }
        }

        private void HighligtWord(string word)
        {
            int startindex = 0;

            startindex = FindText(word.Trim(), start, richTextBox1.Text.Length);

            if (startindex >= 0)
            {
                // Set the highlight color as red
                richTextBox1.SelectionColor = Color.Red;
                richTextBox1.SelectionBackColor = Color.Yellow;   
                // Find the end index. End Index = number of characters in textbox
                int endindex = word.Length;
                // Highlight the search string
                richTextBox1.Select(startindex, endindex);
                // mark the start position after the position of
                // last search string
                start = startindex + endindex;
            }
        }

        private void ReplaceWord(string word)
        {
            if (start >= 0)
            {
                int endindex = currentword.Length;

                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.SelectionBackColor = Color.White;

                richTextBox1.SelectionStart = start;
                richTextBox1.SelectionLength = endindex;

                richTextBox1.SelectedText = word;  

                endindex = word.Length;

                // last search string
                start = start + endindex;

                ContSearch();
            }
        }

        private void InHighligtWord(string word)
        {
            int startindex = 0;

            startindex = FindText(word.Trim(), start, richTextBox1.Text.Length);

            if (startindex >= 0)
            {
                // Set the highlight color as red
                richTextBox1.SelectionColor = Color.Red;
                //richTextBox1.SelectionBackColor = Color.Yellow;
                // Find the end index. End Index = number of characters in textbox
                int endindex = word.Length;
                // Highlight the search string
                richTextBox1.Select(startindex, endindex);
                // mark the start position after the position of
                // last search string
                start = startindex + endindex;
            }
        }

        public int FindText(string txtToSearch, int searchStart, int searchEnd)
        {
            // Unselect the previously searched string
            if (searchStart > 0 && searchEnd > 0 && indexOfSearchText >= 0)
            {
                //richTextBox1.Undo();
                richTextBox1.ForeColor = Color.Black;
                richTextBox1.SelectionBackColor = Color.White; 
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
                    // Find the position of search string in RichTextBox
                    indexOfSearchText = richTextBox1.Find(txtToSearch, searchStart, searchEnd, RichTextBoxFinds.None);
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

        private void button2_Click(object sender, EventArgs e)
        {
            //instance.TabConsole.chatConsole._textBox = richTextBox1.Text;

            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ContSearch();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentword))
            {
                AddWord(currentword);
            }

            ContSearch();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button4.Enabled = (listBox1.SelectedIndex != -1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                start = start - currentword.Length;
                ReplaceWord(listBox1.SelectedItem.ToString());

                listBox1.SelectedIndex = -1;
                button4.Enabled = false; 
            }
        }

        private void frmSpelling_FormClosing(object sender, FormClosingEventArgs e)
        {
            string message = richTextBox1.Text;
            string message1 = string.Empty;
            string message2 = string.Empty;

            if (message.Length == 0) return;

            if (ischat)
            {
                instance.TabConsole.chatConsole.SendChat(richTextBox1.Text, ctype);
            }
            else
            {
                if (!isgroup)
                {
                    //netcom.SendInstantMessage(richTextBox1.Text, target, session);
                    if (message.Length > 1023)
                    {
                        message1 = message.Substring(0, 1022);
                        netcom.SendInstantMessage(message1, target, session);

                        if (message.Length > 2046)
                        {
                            message2 = message.Substring(1023, 2045);
                            netcom.SendInstantMessage(message2, target, session);
                        }
                    }
                    else
                    {
                        netcom.SendInstantMessage(message, target, session); ;
                    }
                }
                else
                {
                    if (message.Length > 1023)
                    {
                        message1 = message.Substring(0, 1022);
                        netcom.SendInstantMessageGroup(message1, target, session);

                        if (message.Length > 2046)
                        {
                            message2 = message.Substring(1023, 2045);
                            netcom.SendInstantMessageGroup(message2, target, session);
                        }
                    }
                    else
                    {
                        netcom.SendInstantMessageGroup(message, target, session); ;
                    }
                }
            }
        }

        private void AddWord(string aword)
        {
            var csvFile = $"{DataFolder.GetDataFolder()}\\{spellLang}.csv";

            using (StreamWriter file = new System.IO.StreamWriter(csvFile, true))
            {
                file.WriteLine(aword + ",");
            }

            richTextBox1.Undo();
            richTextBox1.ForeColor = Color.Black;
            richTextBox1.SelectionBackColor = Color.White;

            instance.Config.ApplyCurrentConfig(); 

            CheckSpellings();
        }
    }
}
