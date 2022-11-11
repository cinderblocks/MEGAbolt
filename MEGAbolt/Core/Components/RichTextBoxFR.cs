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

using System.Windows.Forms;

namespace MEGAbolt
{
    public partial class RichTextBoxFR : RichTextBox
    {
        public void FindAndReplace(string FindText, string ReplaceText)
        {
            Find(FindText);

            if (SelectionLength != 0)
            {
                SelectedText = ReplaceText;
            }
            else
            {
                MessageBox.Show("The following text was not found: " + FindText);
            }
        }


        public void FindAndReplace(string FindText, string ReplaceText, bool ReplaceAll, bool MatchCase, bool WholeWord)
        {
            switch (ReplaceAll)
            {
                case false:
                    if (MatchCase == true)
                    {
                        if (WholeWord == true)
                        {
                            Find(FindText, RichTextBoxFinds.MatchCase | RichTextBoxFinds.WholeWord);
                        }
                        else
                        {
                            Find(FindText, RichTextBoxFinds.MatchCase);
                        }
                    }
                    else
                    {
                        if (WholeWord == true)
                        {
                            Find(FindText, RichTextBoxFinds.WholeWord);
                        }
                        else
                        {
                            Find(FindText);
                        }
                    }

                    if (SelectionLength != 0)
                    {
                        SelectedText = ReplaceText;
                    }
                    else
                    {
                        MessageBox.Show("The following text was not found: " + FindText);
                    }

                    break;

                case true:
                    int i = 0;

                    for (i = 0; i <= TextLength; i++)
                    {
                        if (MatchCase == true)
                        {
                            if (WholeWord == true)
                            {
                                Find(FindText, RichTextBoxFinds.MatchCase | RichTextBoxFinds.WholeWord);
                            }
                            else
                            {
                                Find(FindText, RichTextBoxFinds.MatchCase);
                            }
                        }
                        else
                        {
                            if (WholeWord == true)
                            {
                                Find(FindText, RichTextBoxFinds.WholeWord);
                            }
                            else
                            {
                                Find(FindText);
                            }
                        }

                        if (SelectionLength != 0)
                        {
                            SelectedText = ReplaceText;
                        }
                        else
                        {
                            MessageBox.Show(i + " occurrence(s) replaced");
                            break;
                        }
                    }

                    break;
            }
        }
    }
}