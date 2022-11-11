/**
 * MEGAbolt Metaverse Client
 * Copyright(c) 2009-2014, Radegast Development Team
 * Copyright(c) 2010-2014, www.metabolt.net (METAbolt)
 * Copyright(c) 2016-2021, Sjofn, LLC
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Reflection;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using OpenTK.Graphics.OpenGL;
using System.Threading;
using System.Threading.Tasks;
using MEGAbolt.Controls;
using MEGAbolt.Rendering;
using BugSplatDotNetStandard;
using OpenTK.WinForms;

using Matrix4 = OpenTK.Mathematics.Matrix4;

namespace MEGAbolt
{
    public partial class MEGA3D : Form
    {
        public enum RenderPass
        {
            Picking,
            Simple,
            Alpha,
            Invisible
        }

        #region Public fields
        /// <summary>
        /// The OpenGL surface
        /// </summary>
        public GLControl glControl = null;

        /// <summary>
        /// Use multi sampling (anti aliasing)
        /// </summary>
        public bool UseMultiSampling = true;

        /// <summary>
        /// Is rendering engine ready and enabled
        /// </summary>
        public bool RenderingEnabled = false;

        /// <summary>
        /// Rednder in wireframe mode
        /// </summary>
        public bool Wireframe = false;

        /// <summary>
        /// List of prims in the scene
        /// </summary>
        readonly Dictionary<uint, FacetedMesh> Prims = new();

        /// <summary>
        /// Local ID of the root prim
        /// </summary>
        public uint RootPrimLocalID = 0;
        /// <summary>
        /// Camera center
        /// </summary>
        public Vector3 Center = Vector3.Zero;

        #endregion Public fields

        #region Private fields

        private Popup toolTip;
        private CustomToolTip customToolTip;

        private readonly Dictionary<UUID, TextureInfo> TexturesPtrMap = new();
        private readonly MEGAboltInstance Instance;
        private readonly GridClient Client;
        private MeshmerizerR renderer;
        private GLControlSettings GLMode = null;

        private Task TextureTask;
        private CancellationTokenSource cancellationTokenSource;
        private readonly ConcurrentQueue<TextureLoadItem> PendingTextures = new();

        private bool TakeScreenShot = false;
        private bool snapped = false;
        bool dragging = false;
        int dragX, dragY, downX, downY;
        private Color clearcolour = Color.RoyalBlue;
        public ObjectsListItem objectitem;
        public bool isobject = true;
        public bool enablemipmapd = true;

        private Primitive selitem = new();

        readonly float[] lightPos = { 0f, 0f, 1f, 0f };

        private TextRendering textRendering;
        private Matrix4 ModelMatrix;
        private Matrix4 ProjectionMatrix;
        private int[] Viewport = new int[4];

        #endregion Private fields

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                BugSplat crashReporter = new("radegast", "MEGAbolt",
                    Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                {
                    User = "cinder@cinderblocks.biz",
                    ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                };
                crashReporter.Post(e.Exception);
            }
        }

        void MEGA3D_Disposed(object sender, EventArgs e)
        {
            try
            {
                if (glControl != null)
                {
                    glControl.Dispose();
                    glControl = null;
                }

                textRendering = null;

            }
            catch (Exception ex)
            {
                //string exp = ex.Message;
                Instance.CrashReporter.Post(ex);
            }

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        public MEGA3D(MEGAboltInstance instance, uint rootLocalID, Primitive item)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            Disposed += MEGA3D_Disposed;

            RootPrimLocalID = rootLocalID;

            selitem = item;

            string msg1 = "Drag (left mouse down) to rotate object\n" +
                            "ALT+Drag to Zoom\n" +
                            "Ctrl+Drag to Pan\n" +
                            "Wheel in/out to Zoom in/out\n\n" +
                            "Click camera then object for snapshot";

            toolTip = new Popup(customToolTip = new CustomToolTip(instance, msg1))
            {
                AutoClose = false,
                FocusOnOpen = false
            };
            toolTip.ShowingAnimation = toolTip.HidingAnimation = PopupAnimations.Blend;

            UseMultiSampling = false;

            this.Instance = instance;
            Client = this.Instance.Client;
            isobject = false;

            renderer = new MeshmerizerR();
            textRendering = new TextRendering(instance);
            cancellationTokenSource = new CancellationTokenSource();

            Client.Objects.TerseObjectUpdate += Objects_TerseObjectUpdate;
            Client.Objects.ObjectUpdate += Objects_ObjectUpdate;
            Client.Objects.ObjectDataBlockUpdate += Objects_ObjectDataBlockUpdate;
            Client.Network.SimChanged += SIM_OnSimChanged;
            Client.Self.TeleportProgress += Self_TeleportProgress;
        }

        public MEGA3D(MEGAboltInstance instance, ObjectsListItem obtectitem)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            Disposed += MEGA3D_Disposed;

            RootPrimLocalID = obtectitem.Prim.LocalID;

            selitem = obtectitem.Prim;

            string msg1 = "Drag (left mouse down) to rotate object\n" +
                            "ALT+Drag to Zoom\n" +
                            "Ctrl+Drag to Pan\n" +
                            "Wheel in/out to Zoom in/out\n\n" +
                            "Click camera then object for snapshot";

            toolTip = new Popup(customToolTip = new CustomToolTip(instance, msg1));
            toolTip.AutoClose = false;
            toolTip.FocusOnOpen = false;
            toolTip.ShowingAnimation = toolTip.HidingAnimation = PopupAnimations.Blend;

            UseMultiSampling = false;

            Instance = instance;
            Client = Instance.Client;
            isobject = true;
            objectitem = obtectitem;

            renderer = new MeshmerizerR();
            textRendering = new TextRendering(instance);
            cancellationTokenSource = new CancellationTokenSource();

            Client.Objects.TerseObjectUpdate += Objects_TerseObjectUpdate;
            Client.Objects.ObjectUpdate += Objects_ObjectUpdate;
            Client.Objects.ObjectDataBlockUpdate += Objects_ObjectDataBlockUpdate;
            Client.Network.SimChanged += SIM_OnSimChanged;
            Client.Self.TeleportProgress += Self_TeleportProgress;
        }

        private void ReLoadObject()
        {
            ThreadPool.QueueUserWorkItem(delegate(object sync)
            {
                // Search for the new local id of the object
                List<Primitive> results = Client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                    delegate(Primitive prim)
                    {
                        try
                        {
                            return (prim.ID == selitem.ID);
                        }
                        catch
                        {
                            return false;
                        }
                    });

                if (results != null)
                {
                    try
                    {
                        selitem = results[0];

                        RootPrimLocalID = selitem.LocalID;

                        if (Client.Network.CurrentSim.ObjectsPrimitives.ContainsKey(RootPrimLocalID))
                        {
                            UpdatePrimBlocking(Client.Network.CurrentSim.ObjectsPrimitives[RootPrimLocalID]);
                            var children = Client.Network.CurrentSim.ObjectsPrimitives
                                .FindAll((Primitive p) => p.ParentID == RootPrimLocalID);
                            children.ForEach(UpdatePrimBlocking);
                        }
                    }
                    catch { ; }
                }
                else
                {
                    Dispose();
                }
            });
        }

        #region Network messaage handlers
        void Objects_TerseObjectUpdate(object sender, TerseObjectUpdateEventArgs e)
        {
            if (!IsHandleCreated) return;

            if (Prims.ContainsKey(e.Prim.LocalID))
            {
                UpdatePrimBlocking(e.Prim);
            }
        }

        void Objects_ObjectUpdate(object sender, PrimEventArgs e)
        {
            if (!IsHandleCreated) return;

            if (Prims.ContainsKey(e.Prim.LocalID) || Prims.ContainsKey(e.Prim.ParentID))
            {
                UpdatePrimBlocking(e.Prim);
            }
        }

        void Objects_ObjectDataBlockUpdate(object sender, ObjectDataBlockUpdateEventArgs e)
        {
            if (!IsHandleCreated) return;

            if (Prims.ContainsKey(e.Prim.LocalID))
            {
                UpdatePrimBlocking(e.Prim);
            }
        }

        private void SIM_OnSimChanged(object sender, SimChangedEventArgs e)
        {
            if (!IsHandleCreated) return;

            lock (Prims)
            {
                Prims.Clear();
            }

            lock (TexturesPtrMap)
            {
                TexturesPtrMap.Clear();
            }
        }

        private void Self_TeleportProgress(object sender, TeleportEventArgs e)
        {
            if (!IsHandleCreated) return;

            switch (e.Status)
            {
                case TeleportStatus.Start:
                case TeleportStatus.Progress:
                    RenderingEnabled = false;
                    return;

                case TeleportStatus.Failed:
                case TeleportStatus.Cancelled:
                    RenderingEnabled = true;
                    return;

                case TeleportStatus.Finished:
                    ThreadPool.QueueUserWorkItem(delegate(object sync)
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        Thread.Sleep(6000);
                        ReLoadObject();
                        RenderingEnabled = true;
                        Cursor.Current = Cursors.Default;
                    });

                    return;
            }
        }

        #endregion Network messaage handlers

        #region glControl setup and disposal
        public void SetupGLControl()
        {
            RenderingEnabled = false;

            glControl?.Dispose();
            glControl = null;

            try
            {
                GLMode = GLControlSettings.Default;
                GLMode.AutoLoadBindings = true;
                GLMode.IsEventDriven = false;
                GLMode.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
                GLMode.Profile = OpenTK.Windowing.Common.ContextProfile.Compatability;
                GLMode.NumberOfSamples = UseMultiSampling ? 4 : 0;
            }
            catch
            {
                GLMode = null;
            }
            try
            {
                glControl = GLMode == null ? new GLControl() : new GLControl(GLMode);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, Helpers.LogLevel.Warning, Client);
                glControl = null;
            }

            if (glControl == null)
            {
                Logger.Log("Failed to initialize OpenGL control, cannot continue", Helpers.LogLevel.Error, Client);
                return;
            }

            glControl.Paint += glControl_Paint;
            glControl.Resize += glControl_Resize;
            glControl.MouseDown += glControl_MouseDown;
            glControl.MouseUp += glControl_MouseUp;
            glControl.MouseMove += glControl_MouseMove;
            glControl.MouseWheel += glControl_MouseWheel;
            glControl.Load += glControl_Load;
            glControl.Disposed += glControl_Disposed;
            glControl.Click += glControl_Click;
            glControl.BackColor = clearcolour;

            glControl.Dock = DockStyle.Fill;
            glControl.TabIndex = 0;

            Controls.Add(glControl);

            glControl.BringToFront();
            glControl.Focus();
        }

        void glControl_Disposed(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
            while (!PendingTextures.IsEmpty)
            {
                PendingTextures.TryDequeue(out _);
            }
        }

        void glControl_Click(object sender, EventArgs e)
        {
            if (TakeScreenShot)
            {
                snapped = true;
            }
        }

        void glControl_Load(object sender, EventArgs e)
        {
            try
            {
                GL.ShadeModel(ShadingModel.Smooth);
                GL.ClearColor(clearcolour);
                glControl.BackColor = clearcolour;

                GL.Enable(EnableCap.Lighting);
                GL.Enable(EnableCap.Light0);
                GL.Light(LightName.Light0, LightParameter.Ambient, new float[] { 0.5f, 0.5f, 0.5f, 1f });
                GL.Light(LightName.Light0, LightParameter.Diffuse, new float[] { 0.3f, 0.3f, 0.3f, 1f });
                GL.Light(LightName.Light0, LightParameter.Specular, new float[] { 0.8f, 0.8f, 0.8f, 1.0f });
                GL.Light(LightName.Light0, LightParameter.Position, lightPos);
                //GL.Light(LightName.Light0, LightParameter.LinearAttenuation, lightPos);
                //GL.Light(LightName.Light0, LightParameter.QuadraticAttenuation, lightPos);
                //GL.Light(LightName.Light0, LightParameter.SpotDirection, lightPos);
                //GL.Light(LightName.Light0, LightParameter.SpotExponent, lightPos);

                //GL.Disable(EnableCap.Lighting);
                //GL.Disable(EnableCap.Light0); 

                GL.ClearDepth(1.0d);
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.ColorMaterial);
                GL.Enable(EnableCap.CullFace);
                GL.CullFace(CullFaceMode.Back);
                GL.ColorMaterial(MaterialFace.Front, ColorMaterialParameter.AmbientAndDiffuse);
                GL.ColorMaterial(MaterialFace.Front, ColorMaterialParameter.Specular);

                GL.DepthMask(true);
                GL.DepthFunc(DepthFunction.Lequal);
                GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
                GL.MatrixMode(MatrixMode.Projection);

                GL.Enable(EnableCap.Blend);
                GL.AlphaFunc(AlphaFunction.Greater, 0.5f);
                GL.BlendFunc(0, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                RenderingEnabled = true;

                // Call the resizing function which sets up the GL drawing window
                // and will also invalidate the GL control
                glControl_Resize(null, null);

                glControl.Context.MakeCurrent();

                TextureTask = Task.Factory.StartNew(ProcessTextures,
                    cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                glControl.MakeCurrent();

                GLInvalidate();
            }
            catch (Exception ex)
            {
                RenderingEnabled = false;
                Logger.Log("Failed to initialize OpenGL control", Helpers.LogLevel.Warning, Client, ex);
            }
        }
        #endregion glControl setup and disposal

        #region glControl paint and resize events
        private void glControl_Paint(object sender, PaintEventArgs e)
        {
            if (!RenderingEnabled) return;

            //// A LL
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            glControl.MakeCurrent();

            Render(false);

            GL.ClearColor(clearcolour);

            glControl.SwapBuffers();

            // A LL
            if (TakeScreenShot)
            {
                if (snapped)
                {
                    Instance.MediaManager.PlayUISound(Properties.Resources.camera_clic_with_flash);

                    capScreenBeforeNextSwap();
                    TakeScreenShot = false;
                    snapped = false;
                }
            }
        }

        private void glControl_Resize(object sender, EventArgs e)
        {
            if (!RenderingEnabled) return;

            glControl.MakeCurrent();

            GL.ClearColor(clearcolour);

            if (glControl.ClientSize.Height == 0)
                glControl.ClientSize = new Size(glControl.ClientSize.Width, 1);

            GL.Viewport(0, 0, glControl.ClientSize.Width, glControl.ClientSize.Height);

            GL.PushMatrix();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();


            float dAspRat = glControl.Width / (float)glControl.Height;
            GluPerspective(50f, dAspRat, 0.1f, 100.0f);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();

            //// A LL
            GLInvalidate();
        }
        #endregion glControl paint and resize events

        #region Mouse handling


        private void glControl_MouseWheel(object sender, MouseEventArgs e)
        {
            int newVal = Utils.Clamp(scrollZoom.Value + e.Delta / 10, scrollZoom.Minimum, scrollZoom.Maximum);

            if (scrollZoom.Value != newVal)
            {
                scrollZoom.Value = newVal;
                glControl_Resize(null, null);
                GLInvalidate();
            }
        }

        FacetedMesh RightclickedPrim;
        int RightclickedFaceID;

        private void glControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
            {
                dragging = true;
                downX = dragX = e.X;
                downY = dragY = e.Y;
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (TryPick(e.X, e.Y, out RightclickedPrim, out RightclickedFaceID))
                {
                    ctxObjects.Show(glControl, e.X, e.Y);
                }
            }
        }

        private void glControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                int deltaX = e.X - dragX;
                int deltaY = e.Y - dragY;

                if (e.Button == MouseButtons.Left)
                {
                    if (ModifierKeys == Keys.Control || ModifierKeys == (Keys.Alt | Keys.Control | Keys.Shift))
                    {
                        Center.X -= deltaX / 100f;
                        Center.Z += deltaY / 100f;
                    }

                    if (ModifierKeys == Keys.Alt)
                    {
                        Center.Y -= deltaY / 25f;

                        int newYaw = scrollYaw.Value + deltaX;
                        if (newYaw < 0) newYaw += 360;
                        if (newYaw > 360) newYaw -= 360;

                        scrollYaw.Value = newYaw;
                    }

                    if (ModifierKeys == Keys.None || ModifierKeys == (Keys.Alt | Keys.Control))
                    {
                        int newRoll = scrollRoll.Value + deltaY;
                        if (newRoll < 0) newRoll += 360;
                        if (newRoll > 360) newRoll -= 360;

                        scrollRoll.Value = newRoll;


                        int newYaw = scrollYaw.Value + deltaX;
                        if (newYaw < 0) newYaw += 360;
                        if (newYaw > 360) newYaw -= 360;

                        scrollYaw.Value = newYaw;
                    }
                }
                else if (e.Button == MouseButtons.Middle)
                {
                    Center.X -= deltaX / 100f;
                    Center.Z += deltaY / 100f;

                }

                dragX = e.X;
                dragY = e.Y;

                GLInvalidate();
            }
        }

        private void glControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = false;

                if (e.X == downX && e.Y == downY) // click
                {
                    FacetedMesh picked;
                    int faceID;

                    if (TryPick(e.X, e.Y, out picked, out faceID))
                    {
                        Client.Self.Grab(picked.Prim.LocalID, Vector3.Zero, Vector3.Zero, Vector3.Zero, faceID, Vector3.Zero, Vector3.Zero, Vector3.Zero);
                        Client.Self.DeGrab(picked.Prim.LocalID);
                    }
                }

                GLInvalidate();
            }
        }
        #endregion Mouse handling

        #region Texture thread

        void ProcessTextures()
        {
            Logger.Log("Started MEGA3D Texture Thread", Helpers.LogLevel.Debug);

            try {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    TextureLoadItem item = null;

                    if (!PendingTextures.TryDequeue(out item)) { continue; }

                    if (TexturesPtrMap.ContainsKey(item.TeFace.TextureID))
                    {
                        item.Data.TextureInfo = TexturesPtrMap[item.TeFace.TextureID];
                        continue;
                    }

                    if (LoadTexture(item.TeFace.TextureID, ref item.Data.TextureInfo.Texture, false))
                    {
                        Bitmap bitmap = (Bitmap)item.Data.TextureInfo.Texture;

                        bool hasAlpha = item.Data.TextureInfo.Texture.PixelFormat 
                            is System.Drawing.Imaging.PixelFormat.Format32bppArgb 
                            or System.Drawing.Imaging.PixelFormat.Format32bppPArgb 
                            or System.Drawing.Imaging.PixelFormat.Format16bppArgb1555;
                        item.Data.TextureInfo.HasAlpha = hasAlpha;

                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

                        var loadOnMainThread = new MethodInvoker(() =>
                        {
                            item.Data.TextureInfo.TexturePointer = RHelp.GLLoadImage(bitmap, hasAlpha, RenderSettings.HasMipmap);
                            TexturesPtrMap[item.TeFace.TextureID] = item.Data.TextureInfo;
                            bitmap.Dispose();
                            item.Data.TextureInfo.Texture = null;
                            GLInvalidate();
                        });

                        if (IsHandleCreated)
                        {
                            BeginInvoke(loadOnMainThread);
                        }
                    }
                }
                Logger.Log("Exited MEGA3D Texture Thread", Helpers.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log("MEGA3D TextureThread: " + ex.Message, Helpers.LogLevel.Warning);
            }
        }
        #endregion Texture thread

        private void MEGA3D_Shown(object sender, EventArgs e)
        {
            SetupGLControl();

            ThreadPool.QueueUserWorkItem(sync =>
            {
                if (Client.Network.CurrentSim.ObjectsPrimitives.ContainsKey(RootPrimLocalID))
                {
                    UpdatePrimBlocking(Client.Network.CurrentSim.ObjectsPrimitives[RootPrimLocalID]);
                    var children = Client.Network.CurrentSim.ObjectsPrimitives.FindAll((Primitive p) => { return p.ParentID == RootPrimLocalID; });
                    Logger.Log($"Loading {children.Count} primitives", Helpers.LogLevel.Debug);
                    children.ForEach(p => UpdatePrimBlocking(p));
                }
            }
            );
        }

        #region Private methods (the meat)

        private void RenderText()
        {
            lock (Prims)
            {
                int primNr = 0;

                foreach (FacetedMesh mesh in Prims.Values)
                {
                    primNr++;
                    Primitive prim = mesh.Prim;
                    if (string.IsNullOrEmpty(prim.Text)) continue;

                    string text = System.Text.RegularExpressions.Regex.Replace(prim.Text, "(\r?\n)+", "\n");
                    OpenTK.Mathematics.Vector3 screenPos = OpenTK.Mathematics.Vector3.Zero;
                    OpenTK.Mathematics.Vector3 primPos = OpenTK.Mathematics.Vector3.Zero;

                    // Is it child prim
                    FacetedMesh parent = null;
                    if (Prims.TryGetValue(prim.ParentID, out parent))
                    {
                        var newPrimPos = prim.Position * OpenMetaverse.Matrix4.CreateFromQuaternion(parent.Prim.Rotation);
                        primPos = new OpenTK.Mathematics.Vector3(newPrimPos.X, newPrimPos.Y, newPrimPos.Z);
                    }

                    primPos.Z += prim.Scale.Z * 0.8f;
                    if (!Math3D.GluProject(primPos, ModelMatrix, ProjectionMatrix, Viewport, out screenPos)) continue;
                    screenPos.Y = glControl.Height - screenPos.Y;

                    textRendering.Begin();

                    Color color = Color.FromArgb((int)(prim.TextColor.A * 255), (int)(prim.TextColor.R * 255), (int)(prim.TextColor.G * 255), (int)(prim.TextColor.B * 255));
                    TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.Top;

                    using (Font f = new(FontFamily.GenericSansSerif, 10, FontStyle.Regular))
                    {
                        var size = TextRendering.Measure(text, f, flags);
                        screenPos.X -= size.Width / 2;
                        screenPos.Y -= size.Height;

                        // Shadow
                        if (color != Color.Black)
                        {
                            textRendering.Print(text, f, Color.Black, new Rectangle((int)screenPos.X + 1, (int)screenPos.Y + 1, size.Width, size.Height), flags);
                        }
                        textRendering.Print(text, f, color, new Rectangle((int)screenPos.X, (int)screenPos.Y, size.Width, size.Height), flags);
                    }
                    textRendering.End();
                }
            }
        }

        private void RenderObjects(RenderPass pass)
        {
            lock (Prims)
            {
                int primNr = 0;
                foreach (FacetedMesh mesh in Prims.Values)
                {
                    primNr++;
                    Primitive prim = mesh.Prim;
                    // Individual prim matrix
                    GL.PushMatrix();

                    if (prim.ParentID == RootPrimLocalID)
                    {
                        FacetedMesh parent = null;
                        if (Prims.TryGetValue(prim.ParentID, out parent))
                        {
                            // Apply prim translation and rotation relative to the root prim
                            GL.MultMatrix(Math3D.CreateRotationMatrix(parent.Prim.Rotation));
                            //GL.MultMatrixf(Math3D.CreateTranslationMatrix(parent.Prim.Position));
                        }

                        // Prim roation relative to root
                        GL.MultMatrix(Math3D.CreateTranslationMatrix(prim.Position));
                    }

                    // Prim roation
                    GL.MultMatrix(Math3D.CreateRotationMatrix(prim.Rotation));

                    // Prim scaling
                    GL.Scale(prim.Scale.X, prim.Scale.Y, prim.Scale.Z);

                    // Draw the prim faces
                    for (int j = 0; j < mesh.Faces.Count; j++)
                    {
                        Primitive.TextureEntryFace teFace = mesh.Prim.Textures.FaceTextures[j];
                        Face face = mesh.Faces[j];
                        FaceData data = (FaceData)face.UserData;

                        if (teFace == null)
                            teFace = mesh.Prim.Textures.DefaultTexture;

                        if (pass == RenderPass.Picking)
                        {
                            data.PickingID = primNr;
                            var primNrBytes = Utils.Int16ToBytes((short)primNr);
                            var faceColor = new byte[] { primNrBytes[0], primNrBytes[1], (byte)j, 255 };

                            GL.Color4(faceColor);
                        }
                        else
                        {
                            bool belongToAlphaPass = (teFace.RGBA.A < 0.99) || data.TextureInfo.HasAlpha;

                            if (belongToAlphaPass && pass != RenderPass.Alpha) continue;
                            if (!belongToAlphaPass && pass == RenderPass.Alpha) continue;

                            // Don't render transparent faces
                            if (teFace.RGBA.A <= 0.01f) continue;

                            switch (teFace.Shiny)
                            {
                                case Shininess.High:
                                    GL.Material(MaterialFace.Front, MaterialParameter.Shininess, 94f);
                                    break;
                                case Shininess.Medium:
                                    GL.Material(MaterialFace.Front, MaterialParameter.Shininess, 64f);
                                    break;
                                case Shininess.Low:
                                    GL.Material(MaterialFace.Front, MaterialParameter.Shininess, 24f);
                                    break;
                                case Shininess.None:
                                default:
                                    GL.Material(MaterialFace.Front, MaterialParameter.Shininess, 0f);
                                    break;
                            }

                            var faceColor = new float[] { teFace.RGBA.R, teFace.RGBA.G, teFace.RGBA.B, teFace.RGBA.A };

                            GL.Color4(faceColor);
                            GL.Material(MaterialFace.Front, MaterialParameter.AmbientAndDiffuse, faceColor);
                            GL.Material(MaterialFace.Front, MaterialParameter.Specular, faceColor);

                            if (data.TextureInfo.TexturePointer != 0)
                            {
                                GL.Enable(EnableCap.Texture2D);
                            }
                            else
                            {
                                GL.Disable(EnableCap.Texture2D);
                            }

                            // Bind the texture
                            GL.BindTexture(TextureTarget.Texture2D, data.TextureInfo.TexturePointer);
                        }

                        GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, data.TexCoords);
                        GL.VertexPointer(3, VertexPointerType.Float, 0, data.Vertices);
                        GL.NormalPointer(NormalPointerType.Float, 0, data.Normals);
                        GL.DrawElements(PrimitiveType.Triangles, data.Indices.Length, DrawElementsType.UnsignedShort, data.Indices);

                    }

                    // Pop the prim matrix
                    GL.PopMatrix();
                }
            }
        }

        private void Render(bool picking)
        {
            glControl.MakeCurrent();
            GL.ClearColor(clearcolour);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.LoadIdentity();

            // Setup wireframe or solid fill drawing mode
            if (Wireframe && !picking)
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            }
            else
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }

            var mLookAt = OpenTK.Mathematics.Matrix4d.LookAt(
                    Center.X, scrollZoom.Value * 0.1d + Center.Y, Center.Z,
                    Center.X, Center.Y, Center.Z,
                    0d, 0d, 1d);
            GL.MultMatrix(ref mLookAt);

            // Push the world matrix
            GL.PushMatrix();

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.EnableClientState(ArrayCap.NormalArray);

            // World rotations
            GL.Rotate(scrollRoll.Value, 1f, 0f, 0f);
            GL.Rotate(scrollPitch.Value, 0f, 1f, 0f);
            GL.Rotate(scrollYaw.Value, 0f, 0f, 1f);

            GL.GetInteger(GetPName.Viewport, Viewport);
            GL.GetFloat(GetPName.ModelviewMatrix, out ModelMatrix);
            GL.GetFloat(GetPName.ProjectionMatrix, out ProjectionMatrix);

            if (picking)
            {
                RenderObjects(RenderPass.Picking);
            }
            else
            {
                RenderObjects(RenderPass.Simple);
                RenderObjects(RenderPass.Alpha);
                RenderText();
            }

            // Pop the world matrix
            GL.PopMatrix();

            GL.DisableClientState(ArrayCap.TextureCoordArray);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.NormalArray);

            GL.Flush();
        }

        private void GluPerspective(float fovy, float aspect, float zNear, float zFar)
        {
            float fH = (float)Math.Tan(fovy / 360 * (float)Math.PI) * zNear;
            float fW = fH * aspect;
            GL.Frustum(-fW, fW, -fH, fH, zNear, zFar);
        }

        private bool TryPick(int x, int y, out FacetedMesh picked, out int faceID)
        {
            // Save old attributes
            GL.PushAttrib(AttribMask.AllAttribBits);

            // Disable some attributes to make the objects flat / solid color when they are drawn
            GL.Disable(EnableCap.Fog);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Dither);
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.LineStipple);
            GL.Disable(EnableCap.PolygonStipple);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.AlphaTest);

            Render(true);

            byte[] color = new byte[4];
            GL.ReadPixels(x, glControl.Height - y, 1, 1, 
                OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, color);

            GL.PopAttrib();

            int primID = Utils.BytesToUInt16(color, 0);
            faceID = color[2];

            picked = null;

            lock (Prims)
            {
                foreach (var mesh in Prims.Values)
                {
                    foreach (var face in mesh.Faces)
                    {
                        if (((FaceData)face.UserData).PickingID == primID)
                        {
                            picked = mesh;
                            break;
                        }
                    }

                    if (picked != null) break;
                }
            }

            return picked != null;
        }


        private void UpdatePrimBlocking(Primitive prim)
        {

            FacetedMesh mesh = null;
            FacetedMesh existingMesh = null;

            lock (Prims)
            {
                if (Prims.ContainsKey(prim.LocalID))
                {
                    existingMesh = Prims[prim.LocalID];
                }
            }

            if (prim.Textures == null)
                return;

            try
            {
                if (prim.Sculpt != null && prim.Sculpt.SculptTexture != UUID.Zero)
                {
                    if (prim.Sculpt.Type != SculptType.Mesh)
                    {
                        // Regular sculptie
                        Image img = null;
                        if (!LoadTexture(prim.Sculpt.SculptTexture, ref img, true))
                            return;
                        mesh = renderer.GenerateFacetedSculptMesh(prim, (Bitmap)img, DetailLevel.Highest);
                    }
                    else
                    {
                        // Mesh
                        AutoResetEvent gotMesh = new(false);
                        bool meshSuccess = false;

                        Client.Assets.RequestMesh(prim.Sculpt.SculptTexture, (success, meshAsset) =>
                            {
                                if (!success || !FacetedMesh.TryDecodeFromAsset(prim, meshAsset, DetailLevel.Highest, out mesh))
                                {
                                    Logger.Log("Failed to fetch or decode the mesh asset", Helpers.LogLevel.Warning, Client);
                                }
                                else
                                {
                                    meshSuccess = true;
                                }
                                gotMesh.Set();
                            });

                        if (!gotMesh.WaitOne(20 * 1000, false)) return;
                        if (!meshSuccess) return;
                    }
                }
                else
                {
                    mesh = renderer.GenerateFacetedMesh(prim, DetailLevel.Highest);
                }
            }
            catch
            {
                return;
            }

            // Create a FaceData struct for each face that stores the 3D data
            // in a OpenGL friendly format
            for (int j = 0; j < mesh.Faces.Count; j++)
            {
                Face face = mesh.Faces[j];
                FaceData data = new()
                {
                    Vertices = new float[face.Vertices.Count * 3], 
                    Normals = new float[face.Vertices.Count * 3]
                };

                // Vertices for this face
                for (int k = 0; k < face.Vertices.Count; k++)
                {
                    data.Vertices[k * 3 + 0] = face.Vertices[k].Position.X;
                    data.Vertices[k * 3 + 1] = face.Vertices[k].Position.Y;
                    data.Vertices[k * 3 + 2] = face.Vertices[k].Position.Z;

                    data.Normals[k * 3 + 0] = face.Vertices[k].Normal.X;
                    data.Normals[k * 3 + 1] = face.Vertices[k].Normal.Y;
                    data.Normals[k * 3 + 2] = face.Vertices[k].Normal.Z;
                }

                // Indices for this face
                data.Indices = face.Indices.ToArray();

                // Texture transform for this face
                Primitive.TextureEntryFace teFace = prim.Textures.GetFace((uint)j);
                renderer.TransformTexCoords(face.Vertices, face.Center, teFace, prim.Scale);

                // Texcoords for this face
                data.TexCoords = new float[face.Vertices.Count * 2];
                for (int k = 0; k < face.Vertices.Count; k++)
                {
                    data.TexCoords[k * 2 + 0] = face.Vertices[k].TexCoord.X;
                    data.TexCoords[k * 2 + 1] = face.Vertices[k].TexCoord.Y;
                }

                // Set the UserData for this face to our FaceData struct
                face.UserData = data;
                mesh.Faces[j] = face;


                if (existingMesh != null &&
                    j < existingMesh.Faces.Count &&
                    existingMesh.Faces[j].TextureFace.TextureID == teFace.TextureID &&
                    ((FaceData)existingMesh.Faces[j].UserData).TextureInfo.TexturePointer != 0
                    )
                {
                    FaceData existingData = (FaceData)existingMesh.Faces[j].UserData;
                    data.TextureInfo.TexturePointer = existingData.TextureInfo.TexturePointer;
                }
                else
                {

                    var textureItem = new TextureLoadItem()
                    {
                        Data = data,
                        Prim = prim,
                        TeFace = teFace
                    };

                    PendingTextures.Enqueue(textureItem);
                }
            }

            lock (Prims)
            {
                Prims[prim.LocalID] = mesh;
            }

            GLInvalidate();
        }

        private bool LoadTexture(UUID textureID, ref Image texture, bool removeAlpha)
        {
            if (textureID == UUID.Zero) return false;

            ManualResetEvent gotImage = new(false);
            Image img = null;

            try
            {
                gotImage.Reset();
                Instance.Client.Assets.RequestImage(textureID, (state, assetTexture) =>
                    {
                        try
                        {
                            if (state == TextureRequestState.Finished)
                            {
                                // what the fk is going on here? lol
                                using (var reader = new OpenJpegDotNet.IO.Reader(assetTexture.AssetData))
                                {
                                    if (!reader.ReadHeader())
                                    {
                                        throw new Exception("Failed to decode texture header " + assetTexture.AssetID);
                                    }

                                    try
                                    {
                                        img = reader.Decode().ToBitmap(!removeAlpha);
                                    }
                                    catch (NotSupportedException)
                                    {
                                        img = null;
                                    }
                                }    
                            }
                        }
                        finally
                        {
                            gotImage.Set();                            
                        }
                    }
                );
                gotImage.WaitOne(30 * 1000, false);

                gotImage.Close();

                if (img != null)
                {
                    texture = img;
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.Log(e.Message, Helpers.LogLevel.Error, Instance.Client, e);
                return false;
            }
        }

        private void GLInvalidate()
        {
            if (glControl == null || !RenderingEnabled) return;

            if (InvokeRequired)
            {
                if (IsHandleCreated)
                {
                    BeginInvoke(new MethodInvoker(() => GLInvalidate()));
                }
                return;
            }

            glControl.Invalidate();
        }
        #endregion Private methods (the meat)

        #region Form controls handlers
        private void scroll_ValueChanged(object sender, EventArgs e)
        {
            GLInvalidate();
        }

        private void scrollZoom_ValueChanged(object sender, EventArgs e)
        {
            glControl_Resize(null, null);
            GLInvalidate();
        }

        private void ChkWireFrame_CheckedChanged(object sender, EventArgs e)
        {
            // FIXME: Wireframe = chkWireFrame.Checked;
            GLInvalidate();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            scrollYaw.Value = 90;
            scrollPitch.Value = 0;
            scrollRoll.Value = 0;
            scrollZoom.Value = -30;
            Center = Vector3.Zero;

            GLInvalidate();
        }

        private void oBJToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new() { Filter = "OBJ files (*.obj)|*.obj"};

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (!MeshToOBJ.MeshesToOBJ(Prims, dialog.FileName))
                {
                    MessageBox.Show("Failed to save file " + dialog.FileName +
                        ". Ensure that you have permission to write to that file and it is currently not in use");
                }
            }

            dialog.Dispose();
        }

        #endregion Form controls handlers

        #region Context menu
        private void ctxObjects_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Instance.State.IsSitting)
            {
                sitToolStripMenuItem.Text = "Stand up";
            }
            else if (RightclickedPrim.Prim.Properties != null
                && !string.IsNullOrEmpty(RightclickedPrim.Prim.Properties.SitName))
            {
                sitToolStripMenuItem.Text = RightclickedPrim.Prim.Properties.SitName;
            }
            else
            {
                sitToolStripMenuItem.Text = "Sit";
            }

            sitToolStripMenuItem.Enabled = isobject;

            if (RightclickedPrim.Prim.Properties != null
                && !string.IsNullOrEmpty(RightclickedPrim.Prim.Properties.TouchName))
            {
                touchToolStripMenuItem.Text = RightclickedPrim.Prim.Properties.TouchName;
            }
            else
            {
                touchToolStripMenuItem.Text = "Touch";
            }

            touchToolStripMenuItem.Enabled = (RightclickedPrim.Prim.Flags & PrimFlags.Touch) == PrimFlags.Touch;

            if ((RightclickedPrim.Prim.Flags & PrimFlags.Money) == PrimFlags.Money)
            {
                if (RightclickedPrim.Prim.Properties != null
                && !string.IsNullOrEmpty(RightclickedPrim.Prim.Properties.TouchName))
                {
                    payBuyToolStripMenuItem.Text = RightclickedPrim.Prim.Properties.TouchName;
                }
                else
                {
                    payBuyToolStripMenuItem.Text = "Pay/Buy";
                }

                payBuyToolStripMenuItem.Enabled = true;
            }

            if (!isobject)
            {
                payBuyToolStripMenuItem.Enabled = false;
            }

            if (isobject)
            {
                if (RightclickedPrim.Prim.Properties != null)
                {
                    if (RightclickedPrim.Prim.Properties.OwnerID == Client.Self.AgentID)
                    {
                        takeToolStripMenuItem.Enabled = true;
                        deleteToolStripMenuItem.Enabled = true;
                    }
                    else
                    {
                        takeToolStripMenuItem.Enabled = false;
                        deleteToolStripMenuItem.Enabled = false;
                    }
                }
            }
            else
            {
                takeToolStripMenuItem.Enabled = false;
                deleteToolStripMenuItem.Enabled = false;
                returnToolStripMenuItem.Enabled = false;
            }
        }

        private void touchToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Client.Self.Grab(RightclickedPrim.Prim.LocalID, Vector3.Zero, Vector3.Zero, Vector3.Zero, RightclickedFaceID, Vector3.Zero, Vector3.Zero, Vector3.Zero);
            Thread.Sleep(100);
            Client.Self.DeGrab(RightclickedPrim.Prim.LocalID);
        }

        private void sitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Instance.State.IsSitting)
            {
                Instance.State.SetSitting(true, RightclickedPrim.Prim.ID);
            }
            else
            {
                Instance.State.SetSitting(false, UUID.Zero);
            }
        }

        private void takeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Instance.MediaManager.PlayUISound(UISounds.ObjectDelete);
            Client.Inventory.RequestDeRezToInventory(RightclickedPrim.Prim.LocalID);
            Instance.MediaManager.PlayUISound(UISounds.ObjectDelete);
            Close();
        }

        private void returnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Instance.MediaManager.PlayUISound(UISounds.ObjectDelete);
            Client.Inventory.RequestDeRezToInventory(RightclickedPrim.Prim.LocalID, DeRezDestination.ReturnToOwner, UUID.Zero, UUID.Random());
            Instance.MediaManager.PlayUISound(UISounds.ObjectDelete);
            Close();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (RightclickedPrim.Prim.Properties != null && RightclickedPrim.Prim.Properties.OwnerID != Client.Self.AgentID)
                returnToolStripMenuItem_Click(sender, e);
            else
            {
                Client.Inventory.RequestDeRezToInventory(RightclickedPrim.Prim.LocalID,
                    DeRezDestination.AgentInventoryTake, 
                    Client.Inventory.FindFolderForType(FolderType.Trash), UUID.Random());
                Instance.MediaManager.PlayUISound(UISounds.ObjectDelete);
            }

            Close();
        }
        #endregion Context menu

        private void scrollYaw_Scroll(object sender, ScrollEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Instance.MediaManager.PlayUISound(Properties.Resources.camera_clic_with_flash);

            getScreehShot();
        }

        private void capScreenBeforeNextSwap()
        {
            snapped = false;
            TakeScreenShot = false;

            Bitmap newbmp = new(glControl.Width, glControl.Height);
            Bitmap bmp = newbmp;

            BitmapData data = bmp.LockBits(glControl.ClientRectangle, ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            GL.ReadPixels(0, 0, glControl.Width, glControl.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);

            GL.Finish();

            bmp.UnlockBits(data);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

            string path = DataFolder.GetDataFolder() ;
            string filename = $"Object_Snaphot_{DateTime.Now}.png";
            filename = filename.Replace("/", "-");
            filename = filename.Replace(":", "-");

            saveFileDialog1.Filter = "PNG files (*.png)|*.png";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.FileName = filename;
            saveFileDialog1.InitialDirectory = path;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                bmp.Save(saveFileDialog1.FileName, ImageFormat.Png);
            }

            newbmp.Dispose();
            //bmp.Dispose();  
        }

        private void getScreehShot()
        {
            Bitmap newbmp = new(glControl.Width, glControl.Height);
            Bitmap bmp = newbmp;

            BitmapData data = data = bmp.LockBits(glControl.ClientRectangle, 
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.ReadPixels(0, 0, glControl.Width, glControl.Height,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            GL.Finish();

            bmp.UnlockBits(data);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

            string path = DataFolder.GetDataFolder() ;
            string filename = $"Object_Snaphot_{DateTime.Now}.png";
            filename = filename.Replace("/", "-");
            filename = filename.Replace(":", "-");

            saveFileDialog1.Filter = "PNG files (*.png)|*.png";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.FileName = filename;
            saveFileDialog1.InitialDirectory = path;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                bmp.Save(saveFileDialog1.FileName, ImageFormat.Png);
            }

            newbmp.Dispose();
        }

        private void picAutoSit_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show(picAutoSit);
        }

        private void picAutoSit_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Close();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            clearcolour = Color.RoyalBlue;
            GL.ClearColor(clearcolour);
            GLInvalidate();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            clearcolour = Color.White;
            GL.ClearColor(clearcolour);
            GLInvalidate();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            clearcolour = Color.Black;
            GL.ClearColor(clearcolour);
            GLInvalidate();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            clearcolour = Color.Red;
            GL.ClearColor(clearcolour);
            GLInvalidate();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            clearcolour = Color.Green;
            GL.ClearColor(clearcolour);
            GLInvalidate();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            clearcolour = Color.Yellow;
            GL.ClearColor(clearcolour);
            GLInvalidate();
        }

        private void payBuyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Primitive sPr = new();
            sPr = RightclickedPrim.Prim;

            if (sPr.Properties == null)
            {
                sPr = objectitem.Prim;

                if (sPr.Properties == null)
                {
                    payBuyToolStripMenuItem.Enabled = false;
                    return;
                }
            }

            SaleType styp = sPr.Properties.SaleType;

            //if (sprice > 0)
            //{
            if (styp != SaleType.Not)
            {
                int sprice = sPr.Properties.SalePrice;

                (new frmPay(Instance, sPr.ID, sPr.Properties.Name, sprice, sPr)).Show(this);
            }
            else
            {
                //(new frmPay(instance, sPr.ID, sPr.Properties.Name)).Show(this);
                (new frmPay(Instance, sPr.ID, string.Empty, sPr.Properties.Name, sPr)).Show(this);
            }
            //}
        }

        private void button8_Click(object sender, EventArgs e)
        {
            clearcolour = Color.Transparent;
            GL.ClearColor(clearcolour);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(0, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GLInvalidate();
        }

        private void MEGA3D_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Client.Objects.TerseObjectUpdate -= Objects_TerseObjectUpdate;
                Client.Objects.ObjectUpdate -= Objects_ObjectUpdate;
                Client.Objects.ObjectDataBlockUpdate -= Objects_ObjectDataBlockUpdate;
                Client.Network.SimChanged -= SIM_OnSimChanged;
                Client.Self.TeleportProgress -= Self_TeleportProgress;

                lock (Prims)
                {
                    Prims.Clear();
                }

                lock (TexturesPtrMap)
                {
                    TexturesPtrMap.Clear();
                }
            }
            catch (Exception ex)
            {
                Instance.CrashReporter.Post(ex);
            }
        }

        private void button2_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new();
            ToolTip1.SetToolTip(button2, "Blue background");
        }

        private void button3_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new();
            ToolTip1.SetToolTip(button3, "White background");
        }

        private void button4_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new();
            ToolTip1.SetToolTip(button4, "Black background");
        }

        private void button5_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new();
            ToolTip1.SetToolTip(button5, "Red background");
        }

        private void button6_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new();
            ToolTip1.SetToolTip(button6, "Green background");
        }

        private void button7_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new();
            ToolTip1.SetToolTip(button7, "Yellow background");
        }

        private void button8_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new();
            ToolTip1.SetToolTip(button8, "Transparent background");
        }

        private void button1_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new();
            ToolTip1.SetToolTip(button1, "Take snapshot");
        }
    }
}
