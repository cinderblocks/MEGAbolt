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
using System.ComponentModel;
using System.Windows.Forms;

namespace MEGAbolt
{
    public partial class DifuseForm : Form
    {
        //private IContainer components = null;
        private Timer m_clock;
        private bool m_bShowing = true;
        private bool m_bForceClose = false;
        private DialogResult m_origDialogResult;
        private bool m_bDisposeAtEnd = false;

        #region Constructor
        public DifuseForm()
        {
            InitializeComponents();
        }

        public DifuseForm(bool disposeAtEnd)
        {
            m_bDisposeAtEnd = disposeAtEnd;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            components = new Container();
            m_clock = new Timer(components);
            m_clock.Interval = 1000;
            SuspendLayout();

            m_clock.Tick += Animate;

            Load += DifuseForm_Load;
            Closing += DifuseForm_Closing;
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        #region Event handlers
        private void DifuseForm_Load(object sender, EventArgs e)
        {
            Opacity = 0.0;
            m_bShowing = true;

            m_clock.Start();
        }

        private void DifuseForm_Closing(object sender, CancelEventArgs e)
        {
            if (!m_bForceClose)
            {
                m_origDialogResult = DialogResult;
                e.Cancel = true;
                m_bShowing = false;
                m_clock.Start();
            }
            else
            {
                DialogResult = m_origDialogResult;
            }
        }

        #endregion

        #region Private methods
        private void Animate(object sender, EventArgs e)
        {
            if (m_bShowing)
            {
                if (Opacity < 1)
                {
                    Opacity += 0.1;
                }
                else
                {
                    m_clock.Stop();
                }
            }
            else
            {
                if (Opacity > 0)
                {
                    Opacity -= 0.1;
                }
                else
                {
                    m_clock.Stop();
                    m_bForceClose = true;
                    Close();
                    if (m_bDisposeAtEnd)
                        Dispose();
                }
            }
        }

        #endregion

        private void DifuseForm_Load_1(object sender, EventArgs e)
        {

        }

    }
}