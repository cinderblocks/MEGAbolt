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

namespace MEGAbolt
{
    public partial class MEGAboltTab
    {
        public event EventHandler TabSelected;
        public event EventHandler TabDeselected;
        public event EventHandler TabHighlighted;
        public event EventHandler TabUnhighlighted;
        public event EventHandler TabPartiallyHighlighted;
        public event EventHandler TabMerged;
        public event EventHandler TabSplit;
        public event EventHandler TabDetached;
        public event EventHandler TabAttached;
        public event EventHandler TabClosed;

        protected virtual void OnTabSelected(EventArgs e)
        {
            if (TabSelected != null) TabSelected(this, e);
        }

        protected virtual void OnTabDeselected(EventArgs e)
        {
            if (TabDeselected != null) TabDeselected(this, e);
        }

        protected virtual void OnTabHighlighted(EventArgs e)
        {
            if (TabHighlighted != null) TabHighlighted(this, e);
        }

        protected virtual void OnTabUnhighlighted(EventArgs e)
        {
            if (TabUnhighlighted != null) TabUnhighlighted(this, e);
        }

        protected virtual void OnTabPartiallyHighlighted(EventArgs e)
        {
            if (TabPartiallyHighlighted != null) TabPartiallyHighlighted(this, e);
        }

        protected virtual void OnTabMerged(EventArgs e)
        {
            if (TabMerged != null) TabMerged(this, e);
        }

        protected virtual void OnTabSplit(EventArgs e)
        {
            if (TabSplit != null) TabSplit(this, e);
        }

        protected virtual void OnTabDetached(EventArgs e)
        {
            if (TabDetached != null) TabDetached(this, e);
        }

        protected virtual void OnTabAttached(EventArgs e)
        {
            if (TabAttached != null) TabAttached(this, e);
        }

        protected virtual void OnTabClosed(EventArgs e)
        {
            if (TabClosed != null) TabClosed(this, e);
        }
    }
}
