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
using System.IO;
using System.Diagnostics;
using IWshRuntimeLibrary;


namespace MEGAbolt
{
    public partial class frmBackup : Form
    {
        string currentDirectory =string.Empty;
        //string destinationDirectory = string.Empty;

        public frmBackup()
        {
            InitializeComponent();
        }

        private void frmBackup_Load(object sender, EventArgs e)
        {
            CenterToParent();

            label2.Text = DataFolder.GetDataFolder();    //Application.StartupPath.ToString();
            label8.Text = DataFolder.GetDataFolder();    //Application.StartupPath.ToString();

            currentDirectory = @label2.Text;
            currentDirectory += "\\";

            DirectoryInfo dir = new  DirectoryInfo(currentDirectory);

            FileInfo[] rgFiles = dir.GetFiles("*.cmd");

            foreach (FileInfo fi in rgFiles)
            {
                listBox1.Items.Add(fi.Name);  
            }

            rgFiles = dir.GetFiles("*.bat");

            foreach (FileInfo fi in rgFiles)
            {
                listBox1.Items.Add(fi.Name);
            }

            rgFiles = dir.GetFiles("*.ini");

            foreach (FileInfo fi in rgFiles)
            {
                listBox1.Items.Add(fi.Name);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            label5.Text = string.Empty;

            DialogResult result = folderBrowser.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox1.Text = folderBrowser.SelectedPath;
                textBox1.Text += @"\"; 
                button2.Enabled = true;
                button4.Enabled = true;   
            }
            else
            {
                button2.Enabled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("You must select a destination folder first", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return; 
            }

            string filename = string.Empty;
            string destFile = string.Empty; 

            foreach (var item in listBox1.Items)
            {
                filename = currentDirectory + item;
                destFile = @textBox1.Text + item;

                try
                {
                    System.IO.File.Copy(filename, destFile, true);
                }
                catch (Exception ex)
                {
                    string nexp = ex.InnerException.ToString();
                    label5.Text = nexp; 
                    return; 
                }
            }

            listBox1.Items.Clear();   
            label5.Text = "Backup/s completed to destination folder.";
            //button4.Enabled = true;  
        }

        private void CreateBatFile()
        {
            string cuser = "MEGAbolt";
            string textfile = cuser + ".bat";
            string path = Path.Combine(DataFolder.GetDataFolder(), textfile);
            string scfile = "MEGAbolt BAT.lnk";
            string sc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), scfile);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            using (StreamWriter sr = System.IO.File.CreateText(path))
            {
                string line = "@ECHO OFF";
                sr.Write(line);
                sr.WriteLine("");
                sr.WriteLine("");
                sr.WriteLine("MEGAbolt.exe");

                //for (int a = 0; a < files.Length; a++)
                //{
                //    if (files[a] != null)
                //    {
                //        sr.WriteLine(files[a]);
                //        sr.WriteLine("");
                //    }
                //}

                //sr.Close();
                sr.Dispose();
            }

            //// now create desktop shortcut
            //WshShell shell = new WshShell();
            //IWshShortcut link = (IWshShortcut)shell.CreateShortcut(sc);
            //link.TargetPath = path;
            //link.Description = "Start multiple instances";
            ////link.IconLocation = Environment.CurrentDirectory + ""; 
            //link.Save();

            CreateBatFileShortcut(path, sc);
        }

        private void CreateBatFileShortcut(string path, string scp)
        {
            ////string cuser = "MEGAbolt";
            //string textfile = cuser + ".bat";
            //string path = Path.Combine(Environment.CurrentDirectory, textfile);
            //string scfile = "MEGAbolt- " + cuser + " BAT.lnk";
            string sc = scp;   // Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), scfile);

            // now create desktop shortcut
            WshShell shell = new WshShell();
            IWshShortcut link = (IWshShortcut)shell.CreateShortcut(sc);
            link.TargetPath = path;
            link.WorkingDirectory = DataFolder.GetDataFolder();    //Application.StartupPath.ToString();
            link.Description = "MEGAbolt BAT shortcut";
            //link.IconLocation = Environment.CurrentDirectory + ""; 
            link.Save();
        }

        private void CreateBatFileShortcut(string path, string scp, string fname)
        {
            ////string cuser = "MEGAbolt";
            //string textfile = cuser + ".bat";
            //string path = Path.Combine(Environment.CurrentDirectory, textfile);
            //string scfile = "MEGAbolt- " + cuser + " BAT.lnk";
            string sc = scp;   // Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), scfile);

            // now create desktop shortcut
            WshShell shell = new WshShell();
            IWshShortcut link = (IWshShortcut)shell.CreateShortcut(sc);
            link.TargetPath = path;
            link.WorkingDirectory = DataFolder.GetDataFolder();    //Application.StartupPath.ToString(); 
            link.Description = "MEGAbolt_" + fname + " BAT shortcut";
            //link.IconLocation = Environment.CurrentDirectory + ""; 
            link.Save();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Close();  
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", textBox1.Text); 
        }

        private void button7_Click(object sender, EventArgs e)
        {
            label5.Text = string.Empty;
  
            DialogResult result = folderBrowser.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox2.Text = folderBrowser.SelectedPath;
                textBox2.Text += "\\";
                button6.Enabled = true;
                button5.Enabled = true;  

                SetRestoreDirectory();
            }
            else
            {
                button6.Enabled = false;
                textBox2.Text = "[ select source folder. Click on SELECT button ]";
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox2.Text))
            {
                MessageBox.Show("You must select a source folder first", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string filename = string.Empty;
            string destFile = string.Empty;
            string[] cmdfiles = new string[listBox2.Items.Count];

            for (int a = 0; a < listBox2.Items.Count; a++)
            {
                filename = @textBox2.Text + listBox2.Items[a];
                destFile = currentDirectory + listBox2.Items[a];

                try
                {
                    System.IO.File.Copy(filename, destFile, true);

                    if (destFile.EndsWith(".cmd", StringComparison.CurrentCultureIgnoreCase) || destFile.EndsWith(".bat", StringComparison.CurrentCultureIgnoreCase))
                    {
                        cmdfiles[a] = "CALL " + destFile;

                        if (checkBox1.Checked)
                        {
                            string fname = listBox2.Items[a].ToString();
                            char[] deli = ".".ToCharArray();

                            string[] names = fname.Split(deli);

                            string cuser = names[0];
                            string textfile = cuser + ".bat";
                            string path = Path.Combine(DataFolder.GetDataFolder(), textfile);
                            string scfile = "MEGAbolt_" + cuser + " BAT.lnk";
                            string sc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), scfile);

                            CreateBatFileShortcut(path, sc, cuser);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string nexp = ex.Message;
                    label5.Text = nexp;
                    return;
                }
            }

            if (checkBox2.Checked)
            {
                CreateBatFile();
            }

            listBox2.Items.Clear();
            label5.Text = "Restore complete.";
            //button5.Enabled = true;  
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                listBox1.Items.Clear();

                currentDirectory = @label2.Text;
                currentDirectory += "\\";

                DirectoryInfo dir = new DirectoryInfo(currentDirectory);

                FileInfo[] rgFiles = dir.GetFiles("*.cmd");

                foreach (FileInfo fi in rgFiles)
                {
                    listBox1.Items.Add(fi.Name);
                }

                rgFiles = dir.GetFiles("*.bat");

                foreach (FileInfo fi in rgFiles)
                {
                    listBox1.Items.Add(fi.Name);
                }

                rgFiles = dir.GetFiles("*.ini");

                foreach (FileInfo fi in rgFiles)
                {
                    listBox1.Items.Add(fi.Name);
                }

                label5.Text = string.Empty;
                //button4.Enabled = false; 
            }
            else
            {
                if (textBox2.Text != "[ select source folder. Click on SELECT button ]")
                {
                    SetRestoreDirectory();
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", label8.Text);
        }

        private void SetRestoreDirectory()
        {
            listBox2.Items.Clear();  
            
            string restoreDirectory = @textBox2.Text;

            DirectoryInfo dir = new DirectoryInfo(restoreDirectory);

            FileInfo[] rgFiles = dir.GetFiles("*.cmd");

            foreach (FileInfo fi in rgFiles)
            {
                listBox2.Items.Add(fi.Name);
            }

            rgFiles = dir.GetFiles("*.bat");

            foreach (FileInfo fi in rgFiles)
            {
                listBox2.Items.Add(fi.Name);
            }

            rgFiles = dir.GetFiles("*.ini");

            foreach (FileInfo fi in rgFiles)
            {
                listBox2.Items.Add(fi.Name);
            }

            label5.Text = string.Empty;
            //button5.Enabled = false; 
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
