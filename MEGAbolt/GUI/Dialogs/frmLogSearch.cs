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
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;

namespace MEGAbolt
{
    public partial class frmLogSearch : Form
    {

        private MEGAboltInstance instance;
        private List<string> LogFiles;
        private List<string> FoundFiles;
        private string LogPath = string.Empty;
        private string filetype = "ALL";

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                BugSplat crashReporter = new BugSplat("radegast", "MEGAbolt",
                    Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                {
                    User = "cinder@cinderblocks.biz",
                    ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                };
                crashReporter.Post(e.Exception);
            }
        }

        public frmLogSearch(MEGAboltInstance instance)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            LogFiles = new List<string>();
            FoundFiles = new List<string>();

            LogPath = instance.Config.CurrentConfig.LogDir;
        }

        private void frmLogSearch_Load(object sender, EventArgs e)
        {
            CenterToParent();
            GetLogFiles(filetype);
            textBox1.Focus();
        }

        private void GetLogFiles(string type)
        {

            DirectoryInfo di = new DirectoryInfo(LogPath);
            FileInfo[] files = di.GetFiles();

            Array.Sort(files, CompareFileByDate);
            //Array.Reverse(files);   // Descending

            listBox1.Items.Clear();
            LogFiles.Clear();

            foreach (FileInfo fi in files)
            {
                string inFile = fi.FullName;
                string finname = fi.Name;

                if (filetype != "ALL")
                {
                    if (finname.ToUpper(CultureInfo.CurrentCulture).StartsWith(filetype, StringComparison.CurrentCultureIgnoreCase))
                    {
                        LogFiles.Add(inFile);
                        listBox1.Items.Add(finname);
                    }
                }
                else
                {
                    LogFiles.Add(inFile);
                    listBox1.Items.Add(finname);
                }
            }

            label3.Text = "Total " + listBox1.Items.Count.ToString(CultureInfo.CurrentCulture) + " files.";   
        }

        private static int CompareFileByDate(FileSystemInfo f1, FileSystemInfo f2)
        {
            return -1 *  DateTime.Compare(f1.CreationTimeUtc, f2.CreationTimeUtc);
            //return f1.CreationTime.CompareTo(f2.CreationTime); 
        }

        private void FindText(string fName)
        {
            string[] s_arr = Regex.Split(fName, @"(\\)");
            string name = s_arr[s_arr.Length - 1];   

            StreamReader testTxt = new StreamReader(fName);
            string allRead = testTxt.ReadToEnd().ToLower(CultureInfo.CurrentCulture);
            //testTxt.Close();

            string regMatch = textBox1.Text.ToLower(CultureInfo.CurrentCulture); 

            if (Regex.IsMatch(allRead, regMatch))
            {
                FoundFiles.Add(name); 
            }

            testTxt.Dispose(); 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("You must enter a search term first.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);  
                return;
            }

            listBox2.Items.Clear();
            FoundFiles.Clear();
            label4.Text = string.Empty;

            // Iterate log files here
            foreach (string term in LogFiles)
            {
                FindText(term);
            }

            if (FoundFiles.Count > 0)
            {
                foreach (string term in FoundFiles)
                {
                    listBox2.Items.Add(term);  
                }

                label4.Text = "Search term found in " + FoundFiles.Count.ToString(CultureInfo.CurrentCulture) + " files:";  
                //button2.Enabled = button3.Enabled = true;

                if (FoundFiles.Count > 1)
                {
                    button3.Enabled = true;
                }
            }
            else
            {
                label4.Text = "Zero results found";  
                button2.Enabled = button3.Enabled = false;
            }   
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Close(); 
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                string fullfile = LogPath + listBox1.Items[listBox1.SelectedIndex];

                if (checkBox1.Checked)
                {
                    Process.Start(fullfile);
                }
                else
                {
                    (new frmNotecard(instance, fullfile, textBox1.Text)).Show();
                }
            }
        }

        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex != -1)
            {
                string fullfile = LogPath + listBox2.Items[listBox2.SelectedIndex];

                if (checkBox1.Checked)
                {
                    Process.Start(fullfile);
                }
                else
                {
                    (new frmNotecard(instance, fullfile, textBox1.Text)).Show();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex != -1)
            {
                string fullfile = LogPath + listBox2.Items[listBox2.SelectedIndex];

                if (checkBox1.Checked)
                {
                    Process.Start(fullfile);
                }
                else
                {
                    (new frmNotecard(instance, fullfile, textBox1.Text)).Show();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (string fnd in FoundFiles)
            {
                string fullfile = LogPath + fnd;

                if (checkBox1.Checked)
                {
                    Process.Start(fullfile);
                }
                else
                {
                    (new frmNotecard(instance, fullfile, textBox1.Text)).Show();
                }
            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedItems.Count > 0)
            {
                button2.Enabled = true;
            }
            else
            {
                button2.Enabled = false;
            }
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            textBox1.SelectionStart = 0;
            textBox1.SelectionLength = textBox1.Text.Length;
            textBox1.SelectAll(); 
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", instance.Config.CurrentConfig.LogDir);
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            filetype = "ALL";
            GetLogFiles(filetype);
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            filetype = "CHAT";
            GetLogFiles(filetype);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            filetype = "IM";
            GetLogFiles(filetype);
        }
    }
}
