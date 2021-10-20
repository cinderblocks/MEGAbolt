﻿/*
 * MEGAbolt Metaverse Client
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
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;


namespace METAbolt
{
    public class TextRendering
    {
        class CachedInfo
        {
            public int TextureID;
            public int LastUsed;
            public int Width;
            public int Height;
        }

        class TextItem
        {
            public String Text;
            public Font Font;
            public Color Color;
            public Rectangle Box;
            public TextFormatFlags Flags;

            public int ImgWidth;
            public int ImgHeight;

            public int TextureID = -1;

            public TextItem(string text, Font font, Color color, Rectangle box, TextFormatFlags flags)
            {
                Text = text;
                Font = font;
                Color = color;
                Box = box;
                Flags = flags | TextFormatFlags.NoPrefix;
            }
        }

        public static Size MaxSize = new Size(8192, 8192);

        METAboltInstance Instance;
        List<TextItem> textItems;
        int[] Viewport = new int[4];
        int ScreenWidth { get; set; }
        int ScreenHeight { get; set; }
        Dictionary<int, CachedInfo> Cache = new Dictionary<int, CachedInfo>();

        public TextRendering(METAboltInstance instance)
        {
            Instance = instance;
            textItems = new List<TextItem>();
        }

        public void Print(string text, Font font, Color color, Rectangle box, TextFormatFlags flags)
        {
            textItems.Add(new TextItem(text, font, color, box, flags));
        }

        public static Size Measure(string text, Font font, TextFormatFlags flags)
        {
            return TextRenderer.MeasureText(text, font, MaxSize, flags);
        }

        public void Begin()
        {
        }

        public static void Draw2DBox(float x, float y, float width, float height, float depth)
        {
            GL.Begin(BeginMode.Quads);
            {
                GL.TexCoord2(0, 1);
                GL.Vertex3(x, y, depth);
                GL.TexCoord2(1, 1);
                GL.Vertex3(x + width, y, depth);
                GL.TexCoord2(1, 0);
                GL.Vertex3(x + width, y + height, depth);
                GL.TexCoord2(0, 0);
                GL.Vertex3(x, y + height, depth);
            }
            GL.End();
        }

        public void End()
        {
            GL.GetInteger(GetPName.Viewport, Viewport);
            ScreenWidth = Viewport[2];
            ScreenHeight = Viewport[3];
            int stamp = Environment.TickCount;

            GL.Enable(EnableCap.Texture2D);
            GLHUDBegin();
            {
                foreach (TextItem item in textItems)
                {
                    int hash = GetItemHash(item);
                    CachedInfo tex = new CachedInfo() { TextureID = -1 };
                    if (Cache.ContainsKey(hash))
                    {
                        tex = Cache[hash];
                        tex.LastUsed = stamp;
                    }
                    else
                    {
                        PrepareText(item);
                        if (item.TextureID != -1)
                        {
                            Cache[hash] = tex = new CachedInfo()
                            {
                                TextureID = item.TextureID,
                                Width = item.ImgWidth,
                                Height = item.ImgHeight,
                                LastUsed = stamp
                            };
                        }
                    }
                    if (tex.TextureID == -1) continue;
                    GL.Color4(item.Color);
                    GL.BindTexture(TextureTarget.Texture2D, tex.TextureID);
                    Draw2DBox(item.Box.X, ScreenHeight - item.Box.Y - tex.Height, tex.Width, tex.Height, 0f);
                }

                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.Disable(EnableCap.Texture2D);
                GL.Color4(1f, 1f, 1f, 1f);
            }
            GLHUDEnd();

            textItems.Clear();
        }

        static int GetItemHash(TextItem item)
        {
            int ret = 17;
            ret = ret * 31 + item.Text.GetHashCode();
            ret = ret * 31 + item.Font.GetHashCode();
            ret = ret * 31 + (int)item.Flags;
            return ret;
        }

        //OpenTK.Graphics.IGraphicsContextInternal context = glControl.Context as OpenTK.Graphics.IGraphicsContextInternal;

        public int GLLoadImage(Bitmap bitmap, bool hasAlpha, bool useMipmap)
        {
            if (Instance.Config.CurrentConfig.DisableMipmaps)
            {
                useMipmap = false;
            }
            else
            {
                useMipmap = true;
            }

            int ret = -1;
            GL.GenTextures(1, out ret);
            GL.BindTexture(TextureTarget.Texture2D, ret);

            Rectangle rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            BitmapData bitmapData =
                bitmap.LockBits(
                rectangle,
                ImageLockMode.ReadOnly,
                hasAlpha ? System.Drawing.Imaging.PixelFormat.Format32bppArgb : System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                hasAlpha ? PixelInternalFormat.Rgba : PixelInternalFormat.Rgb8,
                bitmap.Width,
                bitmap.Height,
                0,
                hasAlpha ? OpenTK.Graphics.OpenGL.PixelFormat.Bgra : OpenTK.Graphics.OpenGL.PixelFormat.Bgr,
                PixelType.UnsignedByte,
                bitmapData.Scan0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            if (useMipmap)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            }
            else
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            }

            bitmap.UnlockBits(bitmapData);
            return ret;
        }

        void PrepareText(TextItem item)
        {

            // If we're modified and have texture already delete it from graphics card
            if (item.TextureID > 0)
            {
                //GL.DeleteTexture(item.TextureID);
                item.TextureID = -1;
            }

            Size s;

            try
            {
                s = TextRenderer.MeasureText(
                   item.Text,
                   item.Font,
                   MaxSize,
                   item.Flags);
            }
            catch
            {
                return;
            }

            item.ImgWidth = s.Width;
            item.ImgHeight = s.Height;

            //if (!RenderSettings.TextureNonPowerOfTwoSupported)
            //{
            //    item.ImgWidth = RHelp.NextPow2(s.Width);
            //    item.ImgHeight = RHelp.NextPow2(s.Height);
            //}

            Bitmap img = new Bitmap(
                item.ImgWidth,
                item.ImgHeight,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Graphics g = Graphics.FromImage(img);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            TextRenderer.DrawText(
                g,
                item.Text,
                item.Font,
                new Rectangle(0, 0, s.Width + 2, s.Height + 2),
                Color.White,
                Color.Transparent,
                item.Flags);

            item.TextureID = GLLoadImage(img, true, false);
            g.Dispose();
            img.Dispose();
        }


        bool depthTestEnabled;
        bool lightningEnabled;

        // Switch to ortho display mode for drawing hud
        void GLHUDBegin()
        {
            depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            lightningEnabled = GL.IsEnabled(EnableCap.Lighting);

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Lighting);
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(0, ScreenWidth, 0, ScreenHeight, 1, -1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
        }

        // Switch back to frustrum display mode
        void GLHUDEnd()
        {
            if (depthTestEnabled)
            {
                GL.Enable(EnableCap.DepthTest);
            }
            if (lightningEnabled)
            {
                GL.Enable(EnableCap.Lighting);
            }
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
        }

    }
}