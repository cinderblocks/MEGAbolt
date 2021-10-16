/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2021, Sjofn, LLC
 * All rights reserved.
 *  
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using MEGAbolt.Controls;

namespace MEGAbolt.Controls
{
  public class ExRichTextBox : RichTextBox
  {
    private const int WM_USER = 1024;
    private const int EM_GETCHARFORMAT = 1082;
    private const int EM_SETCHARFORMAT = 1092;
    private const int SCF_SELECTION = 1;
    private const int SCF_WORD = 2;
    private const int SCF_ALL = 4;
    private const uint CFE_BOLD = 1;
    private const uint CFE_ITALIC = 2;
    private const uint CFE_UNDERLINE = 4;
    private const uint CFE_STRIKEOUT = 8;
    private const uint CFE_PROTECTED = 16;
    private const uint CFE_LINK = 32;
    private const uint CFE_AUTOCOLOR = 1073741824;
    private const uint CFE_SUBSCRIPT = 65536;
    private const uint CFE_SUPERSCRIPT = 131072;
    private const int CFM_SMALLCAPS = 64;
    private const int CFM_ALLCAPS = 128;
    private const int CFM_HIDDEN = 256;
    private const int CFM_OUTLINE = 512;
    private const int CFM_SHADOW = 1024;
    private const int CFM_EMBOSS = 2048;
    private const int CFM_IMPRINT = 4096;
    private const int CFM_DISABLED = 8192;
    private const int CFM_REVISED = 16384;
    private const int CFM_BACKCOLOR = 67108864;
    private const int CFM_LCID = 33554432;
    private const int CFM_UNDERLINETYPE = 8388608;
    private const int CFM_WEIGHT = 4194304;
    private const int CFM_SPACING = 2097152;
    private const int CFM_KERNING = 1048576;
    private const int CFM_STYLE = 524288;
    private const int CFM_ANIMATION = 262144;
    private const int CFM_REVAUTHOR = 32768;
    private const uint CFM_BOLD = 1;
    private const uint CFM_ITALIC = 2;
    private const uint CFM_UNDERLINE = 4;
    private const uint CFM_STRIKEOUT = 8;
    private const uint CFM_PROTECTED = 16;
    private const uint CFM_LINK = 32;
    private const uint CFM_SIZE = 2147483648;
    private const uint CFM_COLOR = 1073741824;
    private const uint CFM_FACE = 536870912;
    private const uint CFM_OFFSET = 268435456;
    private const uint CFM_CHARSET = 134217728;
    private const uint CFM_SUBSCRIPT = 196608;
    private const uint CFM_SUPERSCRIPT = 196608;
    private const byte CFU_UNDERLINENONE = 0;
    private const byte CFU_UNDERLINE = 1;
    private const byte CFU_UNDERLINEWORD = 2;
    private const byte CFU_UNDERLINEDOUBLE = 3;
    private const byte CFU_UNDERLINEDOTTED = 4;
    private const byte CFU_UNDERLINEDASH = 5;
    private const byte CFU_UNDERLINEDASHDOT = 6;
    private const byte CFU_UNDERLINEDASHDOTDOT = 7;
    private const byte CFU_UNDERLINEWAVE = 8;
    private const byte CFU_UNDERLINETHICK = 9;
    private const byte CFU_UNDERLINEHAIRLINE = 10;
    private const int MM_TEXT = 1;
    private const int MM_LOMETRIC = 2;
    private const int MM_HIMETRIC = 3;
    private const int MM_LOENGLISH = 4;
    private const int MM_HIENGLISH = 5;
    private const int MM_TWIPS = 6;
    private const int MM_ISOTROPIC = 7;
    private const int MM_ANISOTROPIC = 8;
    private const string FF_UNKNOWN = "UNKNOWN";
    private const int HMM_PER_INCH = 2540;
    private const int TWIPS_PER_INCH = 1440;
    private const string RTF_HEADER = "{\\rtf1\\ansi\\ansicpg1252\\deff0\\deflang1033";
    private const string RTF_DOCUMENT_PRE = "\\viewkind4\\uc1\\pard\\cf1\\f0\\fs20";
    private const string RTF_DOCUMENT_POST = "\\cf0\\fs17}";
    private HybridDictionary rtfColor;
    private HybridDictionary rtfFontFamily;
    private float xDpi;
    private float yDpi;
    private string RTF_IMAGE_POST = "}";

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(
      IntPtr hWnd,
      int msg,
      IntPtr wParam,
      IntPtr lParam);

    public new string Rtf
    {
      get => RemoveBadChars(base.Rtf);
      set => base.Rtf = value;
    }

    public RtfColor TextColor { get; set; }

    public RtfColor HiglightColor { get; set; }

    public ExRichTextBox()
    {
      TextColor = RtfColor.Black;
      HiglightColor = RtfColor.White;
      DetectUrls = false;
      if (rtfColor == null)
        rtfColor = new HybridDictionary();
      ArgumentException argumentException;
      try
      {
        if (!rtfColor.Contains((object) RtfColor.Aqua))
          rtfColor.Add((object) RtfColor.Aqua, (object) "\\red0\\green255\\blue255");
        if (!rtfColor.Contains((object) RtfColor.Black))
          rtfColor.Add((object) RtfColor.Black, (object) "\\red0\\green0\\blue0");
        if (!rtfColor.Contains((object) RtfColor.Blue))
          rtfColor.Add((object) RtfColor.Blue, (object) "\\red0\\green0\\blue255");
        if (!rtfColor.Contains((object) RtfColor.Fuchsia))
          rtfColor.Add((object) RtfColor.Fuchsia, (object) "\\red255\\green0\\blue255");
        if (!rtfColor.Contains((object) RtfColor.Gray))
          rtfColor.Add((object) RtfColor.Gray, (object) "\\red128\\green128\\blue128");
        if (!rtfColor.Contains((object) RtfColor.Green))
          rtfColor.Add((object) RtfColor.Green, (object) "\\red0\\green128\\blue0");
        if (!rtfColor.Contains((object) RtfColor.Lime))
          rtfColor.Add((object) RtfColor.Lime, (object) "\\red0\\green255\\blue0");
        if (!rtfColor.Contains((object) RtfColor.Maroon))
          rtfColor.Add((object) RtfColor.Maroon, (object) "\\red128\\green0\\blue0");
        if (!rtfColor.Contains((object) RtfColor.Navy))
          rtfColor.Add((object) RtfColor.Navy, (object) "\\red0\\green0\\blue128");
        if (!rtfColor.Contains((object) RtfColor.Olive))
          rtfColor.Add((object) RtfColor.Olive, (object) "\\red128\\green128\\blue0");
        if (!rtfColor.Contains((object) RtfColor.Purple))
          rtfColor.Add((object) RtfColor.Purple, (object) "\\red128\\green0\\blue128");
        if (!rtfColor.Contains((object) RtfColor.Red))
          rtfColor.Add((object) RtfColor.Red, (object) "\\red255\\green0\\blue0");
        if (!rtfColor.Contains((object) RtfColor.Silver))
          rtfColor.Add((object) RtfColor.Silver, (object) "\\red192\\green192\\blue192");
        if (!rtfColor.Contains((object) RtfColor.Teal))
          rtfColor.Add((object) RtfColor.Teal, (object) "\\red0\\green128\\blue128");
        if (!rtfColor.Contains((object) RtfColor.White))
          rtfColor.Add((object) RtfColor.White, (object) "\\red255\\green255\\blue255");
        if (!rtfColor.Contains((object) RtfColor.Yellow))
          rtfColor.Add((object) RtfColor.Yellow, (object) "\\red255\\green255\\blue0");
      }
      catch (ArgumentException ex)
      {
        argumentException = ex;
      }
      if (rtfFontFamily == null)
        rtfFontFamily = new HybridDictionary();
      try
      {
        if (!rtfFontFamily.Contains((object) FontFamily.GenericMonospace.Name))
          rtfFontFamily.Add((object) FontFamily.GenericMonospace.Name, (object) "\\fmodern");
        if (!rtfFontFamily.Contains((object) FontFamily.GenericSansSerif))
          rtfFontFamily.Add((object) FontFamily.GenericSansSerif, (object) "\\fswiss");
        if (!rtfFontFamily.Contains((object) FontFamily.GenericSerif))
          rtfFontFamily.Add((object) FontFamily.GenericSerif, (object) "\\froman");
        if (!rtfFontFamily.Contains((object) "UNKNOWN"))
          rtfFontFamily.Add((object) "UNKNOWN", (object) "\\fnil");
      }
      catch (ArgumentException ex)
      {
        argumentException = ex;
      }
      using (Graphics graphics = CreateGraphics())
      {
        xDpi = graphics.DpiX;
        yDpi = graphics.DpiY;
      }
    }

    public ExRichTextBox(RtfColor _textColor)
      : this()
    {
      TextColor = _textColor;
    }

    public ExRichTextBox(RtfColor _textColor, RtfColor _highlightColor)
      : this()
    {
      TextColor = _textColor;
      HiglightColor = _highlightColor;
    }

    public void AppendRtf(string _rtf)
    {
      Select(TextLength, 0);
      SelectedRtf = _rtf;
    }

    public void InsertRtf(string _rtf) => SelectedRtf = _rtf;

    public void AppendTextAsRtf(string _text) => AppendTextAsRtf(_text, Font);

    public void AppendTextAsRtf(string _text, Font _font) => AppendTextAsRtf(_text, _font, TextColor);

    public void AppendTextAsRtf(string _text, Font _font, RtfColor _textColor) => AppendTextAsRtf(_text, _font, _textColor, HiglightColor);

    public void AppendTextAsRtf(
      string _text,
      Font _font,
      RtfColor _textColor,
      RtfColor _backColor)
    {
      Select(TextLength, 0);
      InsertTextAsRtf(_text, _font, _textColor, _backColor);
    }

    public void InsertTextAsRtf(string _text) => InsertTextAsRtf(_text, Font);

    public void InsertTextAsRtf(string _text, Font _font) => InsertTextAsRtf(_text, _font, TextColor);

    public void InsertTextAsRtf(string _text, Font _font, RtfColor _textColor) => InsertTextAsRtf(_text, _font, _textColor, HiglightColor);

    public void InsertTextAsRtf(
      string _text,
      Font _font,
      RtfColor _textColor,
      RtfColor _backColor)
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("{\\rtf1\\ansi\\ansicpg1252\\deff0\\deflang1033");
      stringBuilder.Append(GetFontTable(_font));
      stringBuilder.Append(GetColorTable(_textColor, _backColor));
      stringBuilder.Append(GetDocumentArea(_text, _font));
      SelectedRtf = stringBuilder.ToString();
    }

    private string GetDocumentArea(string _text, Font _font)
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("\\viewkind4\\uc1\\pard\\cf1\\f0\\fs20");
      stringBuilder.Append("\\highlight2");
      if (_font.Bold)
        stringBuilder.Append("\\b");
      if (_font.Italic)
        stringBuilder.Append("\\i");
      if (_font.Strikeout)
        stringBuilder.Append("\\strike");
      if (_font.Underline)
        stringBuilder.Append("\\ul");
      stringBuilder.Append("\\f0");
      stringBuilder.Append("\\fs");
      stringBuilder.Append((int) Math.Round(2.0 * (double) _font.SizeInPoints));
      stringBuilder.Append(" ");
      stringBuilder.Append(_text.Replace("\n", "\\par "));
      stringBuilder.Append("\\highlight0");
      if (_font.Bold)
        stringBuilder.Append("\\b0");
      if (_font.Italic)
        stringBuilder.Append("\\i0");
      if (_font.Strikeout)
        stringBuilder.Append("\\strike0");
      if (_font.Underline)
        stringBuilder.Append("\\ulnone");
      stringBuilder.Append("\\f0");
      stringBuilder.Append("\\fs20");
      stringBuilder.Append("\\cf0\\fs17}");
      return stringBuilder.ToString();
    }

    public void InsertImage(Image _image)
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("{\\rtf1\\ansi\\ansicpg1252\\deff0\\deflang1033");
      stringBuilder.Append(GetFontTable(Font));
      stringBuilder.Append(GetImagePrefix(_image));
      stringBuilder.Append(GetRtfImage(_image));
      stringBuilder.Append(RTF_IMAGE_POST);
      SelectedRtf = stringBuilder.ToString();
    }

    private string GetImagePrefix(Image _image)
    {
      StringBuilder stringBuilder = new StringBuilder();
      int num1 = (int) Math.Round((double) _image.Width / (double) xDpi * 2540.0);
      int num2 = (int) Math.Round((double) _image.Height / (double) yDpi * 2540.0);
      int num3 = (int) Math.Round((double) _image.Width / (double) xDpi * 1440.0);
      int num4 = (int) Math.Round((double) _image.Height / (double) yDpi * 1440.0);
      stringBuilder.Append("{\\pict\\wmetafile8");
      stringBuilder.Append("\\picw");
      stringBuilder.Append(num1);
      stringBuilder.Append("\\pich");
      stringBuilder.Append(num2);
      stringBuilder.Append("\\picwgoal");
      stringBuilder.Append(num3);
      stringBuilder.Append("\\pichgoal");
      stringBuilder.Append(num4);
      stringBuilder.Append(" ");
      return stringBuilder.ToString();
    }

    [DllImport("gdiplus.dll")]
    private static extern uint GdipEmfToWmfBits(
      IntPtr _hEmf,
      uint _bufferSize,
      byte[] _buffer,
      int _mappingMode,
      EmfToWmfBitsFlags _flags);

    private string GetRtfImage(Image _image)
    {
      MemoryStream memoryStream = (MemoryStream) null;
      Graphics graphics = (Graphics) null;
      Metafile metafile = (Metafile) null;
      try
      {
        StringBuilder stringBuilder = new StringBuilder();
        memoryStream = new MemoryStream();
        using (graphics = CreateGraphics())
        {
          IntPtr hdc = graphics.GetHdc();
          metafile = new Metafile((Stream) memoryStream, hdc);
          graphics.ReleaseHdc(hdc);
        }
        using (graphics = Graphics.FromImage((Image) metafile))
          graphics.DrawImage(_image, new Rectangle(0, 0, _image.Width, _image.Height));
        IntPtr henhmetafile = metafile.GetHenhmetafile();
        uint wmfBits = GdipEmfToWmfBits(henhmetafile, 0U, (byte[]) null, 8, EmfToWmfBitsFlags.EmfToWmfBitsFlagsDefault);
        byte[] _buffer = new byte[wmfBits];
        GdipEmfToWmfBits(henhmetafile, wmfBits, _buffer, 8, EmfToWmfBitsFlags.EmfToWmfBitsFlagsDefault);
        foreach (var t in _buffer)
            stringBuilder.Append(string.Format("{0:X2}", (object) t));

        return stringBuilder.ToString();
      }
      finally
      {
        graphics?.Dispose();
        metafile?.Dispose();
        memoryStream?.Close();
      }
    }

    [DefaultValue(false)]
    public new bool DetectUrls
    {
      get => base.DetectUrls;
      set => base.DetectUrls = value;
    }

    public void InsertLink(string text) => InsertLink(text, SelectionStart);

    public void InsertLink(string text, int position)
    {
      if (position < 0 || position > Text.Length)
        throw new ArgumentOutOfRangeException(nameof (position));
      SelectionStart = position;
      SelectedText = text;
      Select(position, text.Length);
      SetSelectionLink(true);
      Select(position + text.Length, 0);
    }

    public void InsertLink(string text, string hyperlink) => InsertLink(text, hyperlink, SelectionStart);

    public void InsertLink(string text, string hyperlink, int position)
    {
      if (position < 0 || position > Text.Length)
        throw new ArgumentOutOfRangeException(nameof (position));
      SelectionStart = position;
      SelectedRtf = "{\\rtf1\\ansi " + text + "\\v #" + hyperlink + "\\v0}";
      Select(position, text.Length + hyperlink.Length + 1);
      SetSelectionLink(true);
      Select(position + text.Length + hyperlink.Length + 1, 0);
    }

    public void SetSelectionLink(bool link) => SetSelectionStyle(32U, link ? 32U : 0U);

    public int GetSelectionLink() => GetSelectionStyle(32U, 32U);

    private void SetSelectionStyle(uint mask, uint effect)
    {
      CHARFORMAT2_STRUCT charformaT2Struct = new CHARFORMAT2_STRUCT();
      charformaT2Struct.cbSize = (uint) Marshal.SizeOf((object) charformaT2Struct);
      charformaT2Struct.dwMask = mask;
      charformaT2Struct.dwEffects = effect;
      IntPtr wParam = new IntPtr(1);
      IntPtr num = Marshal.AllocCoTaskMem(Marshal.SizeOf((object) charformaT2Struct));
      Marshal.StructureToPtr((object) charformaT2Struct, num, false);
      SendMessage(Handle, 1092, wParam, num);
      Marshal.FreeCoTaskMem(num);
    }

    private int GetSelectionStyle(uint mask, uint effect)
    {
      CHARFORMAT2_STRUCT charformaT2Struct = new CHARFORMAT2_STRUCT();
      charformaT2Struct.cbSize = (uint) Marshal.SizeOf((object) charformaT2Struct);
      charformaT2Struct.szFaceName = new char[32];
      IntPtr wParam = new IntPtr(1);
      IntPtr num1 = Marshal.AllocCoTaskMem(Marshal.SizeOf((object) charformaT2Struct));
      Marshal.StructureToPtr((object) charformaT2Struct, num1, false);
      SendMessage(Handle, 1082, wParam, num1);
      CHARFORMAT2_STRUCT structure = (CHARFORMAT2_STRUCT) Marshal.PtrToStructure(num1, typeof (CHARFORMAT2_STRUCT));
      int num2 = ((int) structure.dwMask & (int) mask) != (int) mask ? -1 : (((int) structure.dwEffects & (int) effect) != (int) effect ? 0 : 1);
      Marshal.FreeCoTaskMem(num1);
      return num2;
    }

    private string GetFontTable(Font _font)
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("{\\fonttbl{\\f0");
      stringBuilder.Append("\\");
      if (rtfFontFamily.Contains((object) _font.FontFamily.Name))
        stringBuilder.Append(rtfFontFamily[(object) _font.FontFamily.Name]);
      else
        stringBuilder.Append(rtfFontFamily[(object) "UNKNOWN"]);
      stringBuilder.Append("\\fcharset0 ");
      stringBuilder.Append(_font.Name);
      stringBuilder.Append(";}}");
      return stringBuilder.ToString();
    }

    private string GetColorTable(RtfColor _textColor, RtfColor _backColor)
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("{\\colortbl ;");
      stringBuilder.Append(rtfColor[(object) _textColor]);
      stringBuilder.Append(";");
      stringBuilder.Append(rtfColor[(object) _backColor]);
      stringBuilder.Append(";}\\n");
      return stringBuilder.ToString();
    }

    private string RemoveBadChars(string _originalRtf) => _originalRtf.Replace("\0", "");

    private enum EmfToWmfBitsFlags
    {
      EmfToWmfBitsFlagsDefault = 0,
      EmfToWmfBitsFlagsEmbedEmf = 1,
      EmfToWmfBitsFlagsIncludePlaceable = 2,
      EmfToWmfBitsFlagsNoXORClip = 4,
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    private struct RtfColorDef
    {
      public const string Black = "\\red0\\green0\\blue0";
      public const string Maroon = "\\red128\\green0\\blue0";
      public const string Green = "\\red0\\green128\\blue0";
      public const string Olive = "\\red128\\green128\\blue0";
      public const string Navy = "\\red0\\green0\\blue128";
      public const string Purple = "\\red128\\green0\\blue128";
      public const string Teal = "\\red0\\green128\\blue128";
      public const string Gray = "\\red128\\green128\\blue128";
      public const string Silver = "\\red192\\green192\\blue192";
      public const string Red = "\\red255\\green0\\blue0";
      public const string Lime = "\\red0\\green255\\blue0";
      public const string Yellow = "\\red255\\green255\\blue0";
      public const string Blue = "\\red0\\green0\\blue255";
      public const string Fuchsia = "\\red255\\green0\\blue255";
      public const string Aqua = "\\red0\\green255\\blue255";
      public const string White = "\\red255\\green255\\blue255";
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    private struct RtfFontFamilyDef
    {
      public const string Unknown = "\\fnil";
      public const string Roman = "\\froman";
      public const string Swiss = "\\fswiss";
      public const string Modern = "\\fmodern";
      public const string Script = "\\fscript";
      public const string Decor = "\\fdecor";
      public const string Technical = "\\ftech";
      public const string BiDirect = "\\fbidi";
    }

    private struct CHARFORMAT2_STRUCT
    {
      public uint cbSize;
      public uint dwMask;
      public uint dwEffects;
      public int yHeight;
      public int yOffset;
      public int crTextColor;
      public byte bCharSet;
      public byte bPitchAndFamily;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
      public char[] szFaceName;
      public ushort wWeight;
      public ushort sSpacing;
      public int crBackColor;
      public int lcid;
      public int dwReserved;
      public short sStyle;
      public short wKerning;
      public byte bUnderlineType;
      public byte bAnimation;
      public byte bRevAuthor;
      public byte bReserved1;
    }
  }
}
