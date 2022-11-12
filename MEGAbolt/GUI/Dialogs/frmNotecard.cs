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
using System.Globalization;

namespace MEGAbolt
{
    public partial class frmNotecard : Form
    {
        //private string lheader = string.Empty;
        //private string notecardContent = string.Empty;  

        int start = 0;
        int indexOfSearchText = 0;
        string prevsearchtxt = string.Empty;
        private bool nreadonly = false;
        private string searchfor = string.Empty;  

        public frmNotecard(MEGAboltInstance instance, string file, string searchfor)
        {
            InitializeComponent();

            //this.instance = instance;            
            Text = $"{file} - MEGAbolt";

            rtbNotecard.LoadFile(file, RichTextBoxStreamType.PlainText);
            this.searchfor = searchfor; 
        }

        private void frmNotecardEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void toolStripDropDownButton2_Click(object sender, EventArgs e)
        {

        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbNotecard.Copy(); 
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbNotecard.SelectAll(); 
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            rtbNotecard.FindAndReplace(tsFindText.Text, tsReplaceText.Text, false, tsChkCase.Checked,
                tsChkWord.Checked);
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            rtbNotecard.FindAndReplace(tsFindText.Text, tsReplaceText.Text, true, tsChkCase.Checked, tsChkWord.Checked);
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Find();
        }

        private void Find()
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

        }

        private void rtbNotecard_SelectionChanged(object sender, EventArgs e)
        {
            GetCurrentLine();
            GetCurrentCol();
        }
       
        private void frmNotecardEditor_Load(object sender, EventArgs e)
        {
            CenterToParent();
            
            //rtbNotecard.ReadOnly = true;
            rtbNotecard.Focus(); 
            tsFindText.Text = searchfor;
            rtbNotecard.Focus();
            Find();
            rtbNotecard.Focus();
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

        private void tsSave_Click(object sender, EventArgs e)
        {

        }

        private void tsStatus_Click(object sender, EventArgs e)
        {

        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close(); 
        }
    }
}