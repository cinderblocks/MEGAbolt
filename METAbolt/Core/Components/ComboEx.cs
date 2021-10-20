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

using System.Drawing;
using System.Windows.Forms;

namespace METAbolt
{
    public partial class ComboEx : ComboBox
    {
        public ComboEx()
		{
			//Set DrawMode
			DrawMode = DrawMode.OwnerDrawFixed;	
		}

		/// <summary>
        /// ImageList Property
		/// </summary>
		public ImageList ICImageList { get; set; } = new ImageList();

        /// <summary>
		/// Override OnDrawItem, To Be able To Draw Images, Text, And Font Formatting
		/// </summary>
		/// <param name="e"></param>
		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			e.DrawBackground(); //Draw Background Of Item
			e.DrawFocusRectangle(); //Draw Its rectangle

			if (e.Index < 0) //Do We Have A Valid List ¿

				//Just Draw Indented Text
				e.Graphics.DrawString(Text, e.Font, new SolidBrush(e.ForeColor), e.Bounds.Left + ICImageList.ImageSize.Width, e.Bounds.Top);

			else //We Have A List
			{
				
				if (Items[e.Index].GetType() == typeof(ICItem))  //Is It A ImageCombo Item ¿
				{															
					ICItem ICCurrItem = (ICItem)Items[e.Index]; //Get Current Item

					//Obtain Current Item's ForeColour
                    Color ICCurrForeColour = (ICCurrItem.ICForeColour != Color.FromKnownColor(KnownColor.Transparent)) ? ICCurrItem.ICForeColour : e.ForeColor;

					//Obtain Current Item's Font
                    Font ICCurrFont = ICCurrItem.ICHighLight ? new Font(e.Font, FontStyle.Bold) : e.Font;

                    if (ICCurrItem.ICImageIndex != -1) //If In Actual List ( Which Needs Images )
                    {
                        //Draw Image
                        ICImageList.Draw(e.Graphics, e.Bounds.Left, e.Bounds.Top, ICCurrItem.ICImageIndex);

                        //Then, Draw Text In Specified Bounds
                        e.Graphics.DrawString(ICCurrItem.ICText, ICCurrFont, new SolidBrush(ICCurrForeColour), e.Bounds.Left + ICImageList.ImageSize.Width, e.Bounds.Top);
                    }
                    else //No Image Needed, Index = -1
                    {
                        //Just Draw The Indented Text
                        e.Graphics.DrawString(ICCurrItem.ICText, ICCurrFont, new SolidBrush(ICCurrForeColour), e.Bounds.Left + ICImageList.ImageSize.Width, e.Bounds.Top);
                    }

                    ICCurrFont.Dispose();
				}
				else //Not An ImageCombo Box Item
				
					//Just Draw The Text
					e.Graphics.DrawString(Items[e.Index].ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds.Left + ICImageList.ImageSize.Width, e.Bounds.Top);
				
			}

			base.OnDrawItem (e);
		}

       public class ICItem : object
        {
            public ICItem()
            {
            }

            /// <summary>
            /// Text & Image Index Only
            /// </summary>
            /// <param name="ICIItemText"></param>
            /// <param name="ICImageIndex"></param>
            public ICItem(string ICIItemText, int ICImageIndex) //First Overload
            {
                ICText = ICIItemText; //Text
                this.ICImageIndex = ICImageIndex; //Image Index
            }

            /// <summary>
            /// Text, Image Index, Highlight, ForeColour
            /// </summary>
            /// <param name="ICIItemText"></param>
            /// <param name="ICImageIndex"></param>
            /// <param name="ICHighLight"></param>
            /// <param name="ICForeColour"></param>
            public ICItem(string ICIItemText, int ICImageIndex, bool ICHighLight, Color ICForeColour) //Second Overload
            {
                ICText = ICIItemText; //Text
                this.ICImageIndex = ICImageIndex; //Image Index
                this.ICHighLight = ICHighLight; //Highlighted ¿
                this.ICForeColour = ICForeColour; //ForeColour
            }

            /// <summary>
            /// ImageCombo Item Text
            /// </summary>
            public string ICText { get; set; } = null;

            /// <summary>
            /// Image Index
            /// </summary>
            public int ICImageIndex { get; set; } = -1;

            /// <summary>
            /// Highlighted ¿
            /// </summary>
            public bool ICHighLight { get; set; } = false;

            /// <summary>
            /// ForeColour
            /// </summary>
            public Color ICForeColour { get; set; } = Color.FromKnownColor(KnownColor.Transparent);

            /// <summary>
            /// Override ToString To Return Item Text
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return ICText;
            }
        }
    }
}
