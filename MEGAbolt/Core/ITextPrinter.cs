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

namespace MEGAbolt
{
    public interface ITextPrinter
    {
        void PrintHeader(string text);
        void PrintText(string text);
        void PrintDate(string text);
        void PrintTextLine(string text);
        void PrintLinkHeader(string text, string link);
        void PrintLinkHeader(string text, string uuid, string link);
        void PrintLink(string text, string link);
        void PrintClassicTextDate(string text);
        //void PrintLine();
        void ClearText();

        //string Content { get; set; }
        //Color ForeColor { get; set; }
        //Color BackColor { get; set; }

        void SetSelectionForeColor(System.Drawing.Color color);
        void SetSelectionBackColor(System.Drawing.Color color);
        void SetFont(System.Drawing.Font font);
        void SetFontStyle(System.Drawing.FontStyle fontstyle);
        void SetFontSize(float size);
        void SetOffset(int offset);

        //Font Font { get; set; }
        //float FontSize { set; }
        //FontStyle Fontstyle { get; set; }
        //int OffSet { set; }

        //int TLength { get; }
        int TLength();
    }
}
