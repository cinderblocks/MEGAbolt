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
using System.Collections.Generic;
using System.Timers;
using OpenMetaverse;
using MEGAbolt.NetworkComm;

namespace MEGAbolt
{
    public class StateManager
    {
        private MEGAboltInstance instance;
        private GridClient client;
        private MEGAboltNetcom netcom;

        //private bool alwaysrun = false;
        //private bool club = false;
        //private bool salsa = false;
        //private bool fall = false;
        //private bool crouch = false;

        private UUID pointID = UUID.Zero;
        private UUID beamID = UUID.Zero;
        private UUID beamID1 = UUID.Zero;
        private UUID beamID2 = UUID.Zero;
        private UUID effectID = UUID.Zero;
        //private bool looking = false;
        private UUID lookID = UUID.Zero;

        //private bool goingto = false;
        //private string goName = string.Empty;
        //private UUID goid = UUID.Zero;

        //private Dictionary<UUID, FriendInfo> avatarfriends = new Dictionary<UUID,FriendInfo>();  
        private int ccntr = 1;

        //private Primitive sitprim = null;
        private UUID requestedsitprim = UUID.Zero;
        //private ManualResetEvent PrimEvent = new ManualResetEvent(false);

        private Timer pointtimer;
        
        private Vector3d offset = new Vector3d(Vector3d.Zero); 
        private Vector3d beamoffset1 = new Vector3d(0, 0, 0.1);
        private Vector3d beamoffset2 = new Vector3d(0, 0.1, 0);
        private Primitive prim = new Primitive();
        private Color4 mncolour = new Color4(255, 0, 0, 0);   //new Color4(0, 255, 12, 0); // Green
        private Color4 spcolour = new Color4(0, 0, 255, 0);
        private Color4 tdcolour = new Color4(0, 255, 12, 0);
        private Color4 bkcolour = new Color4(255, 255, 255, 255);
        private Color4 ccolur = new Color4(0, 0, 255, 255);
        //private UUID lookattarget = UUID.Zero; 

        public StateManager(MEGAboltInstance instance)
        {
            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;

            AddNetcomEvents();
            AddClientEvents();
            //InitializeAgentUpdateTimer();

            pointtimer = new Timer();
            pointtimer.Elapsed += OnTimedEvent;
            // Set the Interval to 10 seconds.
            pointtimer.Interval = 1000;
            pointtimer.Enabled = false;
        }

        private void AddNetcomEvents()
        {
            netcom.ClientLoggedOut += netcom_ClientLoggedOut;
            netcom.ClientDisconnected += netcom_ClientDisconnected;
        }

        private void RemoveNetcomEvents()
        {
            netcom.ClientLoggedOut -= netcom_ClientLoggedOut;
            netcom.ClientDisconnected -= netcom_ClientDisconnected;
        }

        private void netcom_ClientLoggedOut(object sender, EventArgs e)
        {
            TidyUp();            
        }

        private void netcom_ClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            TidyUp();
        }

        private void TidyUp()
        {
            IsTyping = IsAway = IsBusy = false;
            pointtimer.Dispose();
            pointtimer = null;

            RemoveClientEvents();
            RemoveNetcomEvents();
        }

        private void AddClientEvents()
        {
            client.Objects.TerseObjectUpdate += Objects_OnObjectUpdated;
            client.Network.EventQueueRunning += Network_OnEventQueueRunning;
            client.Self.TeleportProgress += Self_TeleportProgress;
            client.Objects.AvatarUpdate += Objects_OnAvatarUpdate;
            client.Network.SimChanged += Network_OnSimChanged;
        }

        private void RemoveClientEvents()
        {
            client.Objects.TerseObjectUpdate -= Objects_OnObjectUpdated;
            client.Network.EventQueueRunning -= Network_OnEventQueueRunning;
            client.Self.TeleportProgress -= Self_TeleportProgress;
            client.Objects.AvatarUpdate -= Objects_OnAvatarUpdate;
            client.Network.SimChanged -= Network_OnSimChanged;
        }

        private void Objects_OnAvatarUpdate(object sender, AvatarUpdateEventArgs e)
        {
            if (e.Avatar.ID == client.Self.AgentID)
            {
                ResetCamera();
            }
        }

        public void ResetCamera()
        {
            client.Self.Movement.Camera.LookAt(instance.SIMsittingPos() + new Vector3(-5,0,0)  * client.Self.Movement.BodyRotation, instance.SIMsittingPos());
        }

        private void Self_TeleportProgress(object sender, TeleportEventArgs e)
        {
            if (e.Status == TeleportStatus.Finished)
            {
                SetLookat();
            }
        }

        private void Network_OnEventQueueRunning(object sender, EventQueueRunningEventArgs e)
        {
            if (e.Simulator == client.Network.CurrentSim)
            {
                SetLookat();
            }
        }

        public void SetAgentFOV()
        {
            OpenMetaverse.Packets.AgentFOVPacket msg = new OpenMetaverse.Packets.AgentFOVPacket
            {
                AgentData =
                {
                    AgentID = client.Self.AgentID,
                    SessionID = client.Self.SessionID,
                    CircuitCode = client.Network.CircuitCode
                },
                FOVBlock =
                {
                    GenCounter = 0,
                    VerticalAngle = Utils.TWO_PI - 0.05f
                }
            };
            client.Network.SendPacket(msg);

            //client.Self.Movement.SetFOVVerticalAngle(Utils.TWO_PI - 0.05f);
        }

        private void SetLookat()
        {
            Random rnd = new Random();

            client.Self.Movement.UpdateFromHeading(Utils.TWO_PI * rnd.NextDouble(), true);

            Vector3d lkpos = new Vector3d(new Vector3(3, 0, 0) * Quaternion.Identity);
            client.Self.LookAtEffect(client.Self.AgentID, client.Self.AgentID, lkpos, LookAtType.Idle, UUID.Random());
        }

        private void Network_OnSimChanged(object sender, SimChangedEventArgs e)
        {
            SetAgentFOV();
            ResetCamera();
        }

        private void Objects_OnObjectUpdated(object sender, TerseObjectUpdateEventArgs e)
        { 
            if (!e.Update.Avatar) return;

            //if (e.Prim.LocalID == client.Self.LocalID) ResetCamera();

            //if (!following && !goingto) return;
            if (!IsFollowing) return;

            Avatar av = new Avatar();
            client.Network.CurrentSim.ObjectsAvatars.TryGetValue(e.Update.LocalID, out av);

            if (av == null)
            {
                client.Self.AutoPilotCancel();
                Logger.Log("Follow/GoTo cancelled. Could not find the target avatar on the SIM.", Helpers.LogLevel.Warning);
                return;
            }

            if (av.Name != FollowName) return;

            Vector3 pos = new Vector3(Vector3.Zero); ;

            pos = av.Position;

            if (av.ParentID != 0)
            {
                uint oID = av.ParentID;

                if (prim == null)
                {
                    client.Network.CurrentSim.ObjectsPrimitives.TryGetValue(oID, out prim);
                }

                if (prim == null)
                {
                    client.Self.AutoPilotCancel();
                    Logger.Log("Follow/GoTo cancelled. Could not find the object the target avatar is sitting on.", Helpers.LogLevel.Warning);
                    return;
                }

                pos += prim.Position;
            }
            else
            {
                prim = null;
            }

            client.Self.Movement.TurnToward(pos);

            float dist = Vector3.Distance(instance.SIMsittingPos(), pos);

            //if (av.Name == goName)
            //{
            //    if (goingto)
            //    {
            //        if (dist < 3.0f)
            //        {
            //            client.Self.AutoPilotCancel();
            //            client.Self.Movement.TurnToward(pos);

            //            goid = UUID.Zero;
            //            goName = string.Empty;
            //            goingto = false;
            //        }
            //        else
            //        {
            //            client.Self.AutoPilotCancel();
            //            ulong followRegionX = e.Simulator.Handle >> 32;
            //            ulong followRegionY = e.Simulator.Handle & (ulong)0xFFFFFFFF;
            //            ulong xTarget = (ulong)pos.X + followRegionX;
            //            ulong yTarget = (ulong)pos.Y + followRegionY;
            //            float zTarget = pos.Z - 1f;

            //            client.Self.AutoPilot(xTarget, yTarget, zTarget);
            //        }
            //    }

            //    return;
            //}

            //if (av.Name == followName)
            //{
            client.Self.Movement.TurnToward(av.Position);

            if (dist > FollowDistance)
            {
                client.Self.AutoPilotCancel();
                ulong followRegionX = e.Simulator.Handle >> 32;
                ulong followRegionY = e.Simulator.Handle & (ulong)0xFFFFFFFF;
                ulong xTarget = (ulong)pos.X + followRegionX;
                ulong yTarget = (ulong)pos.Y + followRegionY;
                float zTarget = pos.Z - 1f;

                client.Self.AutoPilot(xTarget, yTarget, zTarget);
            }
            //}
        }

        //private void InitializeAgentUpdateTimer()
        //{
        //    agentUpdateTicker = new System.Timers.Timer(1000);
        //    agentUpdateTicker.Elapsed += new ElapsedEventHandler(agentUpdateTicker_Elapsed);
        //    agentUpdateTicker.Enabled = true;
        //}

        //private void agentUpdateTicker_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    UpdateStatus();
        //}

        //private void UpdateStatus()
        //{
        //    if (netcom.IsLoggedIn)
        //    {
        //        AgentUpdatePacket update = new AgentUpdatePacket();
        //        update.Header.Reliable = true;

        //        update.AgentData.AgentID = client.Self.AgentID;
        //        update.AgentData.SessionID = client.Self.SessionID;
        //        update.AgentData.HeadRotation = Quaternion.Identity;
        //        update.AgentData.BodyRotation = Quaternion.Identity;
        //        update.AgentData.Far = (float)instance.Config.CurrentConfig.RadarRange;
        //        update.Type = PacketType.AgentUpdate;
        //        //client.Network.SendPacket(update, client.Network.CurrentSim);
        //        client.Network.CurrentSim.SendPacket(update);
        //    }
        //}

        public void Follow(string name, UUID fid)
        {
            FollowID = fid;
            FollowName = name;
            IsFollowing = !string.IsNullOrEmpty(FollowName);

            //if (!following) client.Self.AutoPilotCancel();
        }

        //public void GoTo(string name, UUID fid)
        //{
        //    goid = fid;
        //    goName = name;
        //    goingto = !string.IsNullOrEmpty(followName);

        //    //if (!goingto) client.Self.AutoPilotCancel();
        //}

        public void SetTyping(bool ttyping)
        {
            Dictionary<UUID, bool> typingAnim = new Dictionary<UUID, bool>();
            typingAnim.Add(TypingAnimationID, ttyping);

            if (!instance.Config.CurrentConfig.DisableTyping)
            {
                client.Self.Animate(typingAnim, false);
            }

            if (ttyping)
                client.Self.Chat(string.Empty, 0, ChatType.StartTyping);
            else
                client.Self.Chat(string.Empty, 0, ChatType.StopTyping);

            IsTyping = ttyping;
        }

        public void SetAway(bool aaway)
        {
            Dictionary<UUID, bool> awayAnim = new Dictionary<UUID, bool>();
            awayAnim.Add(AwayAnimationID, aaway);

            client.Self.Animate(awayAnim, true);
            IsAway = aaway;
        }

        public void SetBusy(bool bbusy)
        {
            Dictionary<UUID, bool> busyAnim = new Dictionary<UUID, bool>();
            busyAnim.Add(BusyAnimationID, bbusy);

            client.Self.Animate(busyAnim, true);
            IsBusy = bbusy;
        }

        public void BellyDance(bool bbelly)
        {
            Dictionary<UUID, bool> bdanceAnim = new Dictionary<UUID, bool>();
            bdanceAnim.Add(BellydanceAnimationID, bbelly);

            client.Self.Animate(bdanceAnim, true);
            IsBelly = bbelly;
        }

        public void ClubDance(bool cclub)
        {
            Dictionary<UUID, bool> cdanceAnim = new Dictionary<UUID, bool>();
            cdanceAnim.Add(ClubdanceAnimationID, cclub);

            client.Self.Animate(cdanceAnim, true);
            //this.club = cclub;
        }

        public void SalsaDance(bool ssalsa)
        {
            Dictionary<UUID, bool> sdanceAnim = new Dictionary<UUID, bool>();
            sdanceAnim.Add(SalsaAnimationID, ssalsa);

            client.Self.Animate(sdanceAnim, true);
            //this.salsa = ssalsa;
        }

        public void FallOnFace(bool ffall)
        {
            Dictionary<UUID, bool> ffallAnim = new Dictionary<UUID, bool>();
            ffallAnim.Add(FallAnimationID, ffall);

            client.Self.Animate(ffallAnim, true);
            //this.fall = ffall;
        }

        public void Crouch(bool ccrouch)
        {
            Dictionary<UUID, bool> crouchAnim = new Dictionary<UUID, bool>();
            crouchAnim.Add(CrouchAnimationID, ccrouch);

            client.Self.Animate(crouchAnim, true);
            //this.crouch = ccrouch;
        }

        public void SetFlying(bool fflying)
        {
            client.Self.AutoPilotCancel();

            client.Self.Fly(fflying);
            client.Self.Movement.Fly = fflying;
            IsFlying = fflying;
        }

        public void SetAlwaysRun(bool aalwaysrun)
        {
            client.Self.AutoPilotCancel();

            //this.alwaysrun = aalwaysrun;
            client.Self.Movement.AlwaysRun = aalwaysrun;            
        }

        public void SetSitting(bool ssitting, UUID target)
        {
            if (ssitting)
            {
                client.Self.AutoPilotCancel();

                IsSitting = false;
                SitPrim = UUID.Zero;

                requestedsitprim = target;

                client.Self.AvatarSitResponse += Self_AvatarSitResponse;

                client.Self.RequestSit(target, Vector3.Zero);
                client.Self.Sit();  
            }
            else
            {
                IsSitting = false;
                client.Self.Stand();
                SitPrim = UUID.Zero;

                StopAnimations();
            }
        }

        void Self_AvatarSitResponse(object sender, AvatarSitResponseEventArgs e)
        {
            client.Self.AvatarSitResponse -= Self_AvatarSitResponse;

            if (e.ObjectID == requestedsitprim)
            {
                IsSitting = true;
                SitPrim = e.ObjectID;
                SittingPos = e.SitPosition;
                //instance.TabConsole.DisplayChatScreen("Auto-sitting on object " + requestedsitprim.ToString());
            }
            else
            {
                // failed to sit
                //instance.TabConsole.DisplayChatScreen("Failed to sit on object " + requestedsitprim.ToString());
            }

            requestedsitprim = UUID.Zero;  
        }

        public void SetGroundSit(bool sit)
        {
            IsSittingOnGround = sit;
        }

        public void SetStanding()
        {
            client.Self.Stand();
            IsSitting = false;
            IsSittingOnGround = false;
            SitPrim = UUID.Zero;

            StopAnimations();
        }

        public void SetPointing(bool ppointing, UUID target)
        {
            IsPointing = ppointing;

            if (ppointing)
            {
                pointID = UUID.Random();
                beamID = UUID.Random();

                client.Self.SphereEffect(offset, ccolur, 1.1f, effectID);

                client.Self.PointAtEffect(client.Self.AgentID, target, Vector3d.Zero, PointAtType.Select, pointID);
                client.Self.BeamEffect(client.Self.AgentID, target, Vector3d.Zero, mncolour, 1.0f, beamID);
            }
            else
            {
                if (pointID == UUID.Zero || beamID == UUID.Zero) return;

                //client.Self.PointAtEffect(client.Self.AgentID, target, Vector3d.Zero, PointAtType.Clear, pointID);
                client.Self.PointAtEffect(client.Self.AgentID, UUID.Zero, Vector3d.Zero, PointAtType.None, pointID);
                client.Self.BeamEffect(client.Self.AgentID, target, Vector3d.Zero, bkcolour, 0, beamID);
                pointID = UUID.Zero;
                beamID = UUID.Zero;
            }
        }

        public void SetPointing(bool ppointing, UUID target, Vector3d ooffset, Vector3 primposition)
        {
            IsPointing = ppointing;

            if (ppointing)
            {
                offset = ooffset;
 
                pointID = UUID.Random();
                beamID = UUID.Random();
                effectID = UUID.Random();

                client.Self.SphereEffect(ooffset, ccolur, 0.80f, effectID);

                client.Self.PointAtEffect(client.Self.AgentID, target, Vector3d.Zero, PointAtType.Select, pointID);

                beamID1 = UUID.Random();                
                beamID2 = UUID.Random();

                client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, ooffset, mncolour, 1.0f, beamID);
                client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, ooffset + beamoffset1, spcolour, 1.0f, beamID1);
                client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, ooffset + beamoffset2, tdcolour, 1.0f, beamID2);

                pointtimer.Enabled = true;
                pointtimer.Start();
            }
            else
            {
                if (pointID == UUID.Zero || beamID == UUID.Zero) return;

                pointtimer.Stop();
                pointtimer.Enabled = false;

                client.Self.PointAtEffect(client.Self.AgentID, UUID.Zero, Vector3d.Zero, PointAtType.None, pointID);
                client.Self.BeamEffect(UUID.Zero, UUID.Zero, Vector3d.Zero, bkcolour, 0.0f, beamID);
                client.Self.SphereEffect(Vector3d.Zero, bkcolour, 0.0f, effectID);

                pointID = UUID.Zero;
                beamID = UUID.Zero;
                effectID = UUID.Zero;

                client.Self.BeamEffect(UUID.Zero, UUID.Zero, Vector3d.Zero, bkcolour, 0.0f, beamID1);
                client.Self.BeamEffect(UUID.Zero, UUID.Zero, Vector3d.Zero, bkcolour, 0.0f, beamID2);

                beamID1 = UUID.Zero;
                beamID2 = UUID.Zero;
            }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            ccntr += 1;

            if (ccntr > 3) ccntr = 1;

            switch (ccntr)
            {
                case 1:
                    client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, offset, mncolour, 1.0f, beamID);
                    client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, offset + beamoffset1, spcolour, 1.0f, beamID1);
                    client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, offset + beamoffset2, tdcolour, 1.0f, beamID2);
                    break;

                case 2:
                    client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, offset, spcolour, 1.0f, beamID);
                    client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, offset + beamoffset1, tdcolour, 1.0f, beamID1);
                    client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, offset + beamoffset2, mncolour, 1.0f, beamID2);
                    break;

                case 3:
                    client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, offset, tdcolour, 1.0f, beamID);
                    client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, offset + beamoffset1, mncolour, 1.0f, beamID1);
                    client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, offset + beamoffset2, spcolour, 1.0f, beamID2);
                    break;
            }

            
        }

        public void SetPointingTouch(bool ppointing, UUID target, Vector3d ooffset, Vector3 primposition)
        {
            IsPointing = ppointing;

            if (ppointing)
            {
                offset = ooffset;

                pointID = UUID.Random();
                beamID = UUID.Random();

                client.Self.PointAtEffect(client.Self.AgentID, target, Vector3d.Zero, PointAtType.Select, pointID);

                beamID1 = UUID.Random();
                beamID2 = UUID.Random();

                client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, ooffset, mncolour, 1.0f, beamID);
                client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, ooffset + beamoffset1, spcolour, 1.0f, beamID1);
                client.Self.BeamEffect(client.Self.AgentID, UUID.Zero, ooffset + beamoffset2, spcolour, 1.0f, beamID2);

                pointtimer.Start();
            }
            else
            {
                if (pointID == UUID.Zero || beamID == UUID.Zero) return;

                pointtimer.Stop();

                client.Self.PointAtEffect(client.Self.AgentID, UUID.Zero, Vector3d.Zero, PointAtType.None, pointID);
                client.Self.BeamEffect(UUID.Zero, UUID.Zero, Vector3d.Zero, bkcolour, 0.0f, beamID);

                pointID = UUID.Zero;
                beamID = UUID.Zero;

                client.Self.BeamEffect(UUID.Zero, UUID.Zero, Vector3d.Zero, bkcolour, 0.0f, beamID1);
                client.Self.BeamEffect(UUID.Zero, UUID.Zero, Vector3d.Zero, bkcolour, 0.0f, beamID2);

                beamID1 = UUID.Zero;
                beamID2 = UUID.Zero;
            }
        }

        public void LookAt(bool llooking, UUID target)
        {
            if (instance.Config.CurrentConfig.DisableLookAt)
                return;

            //this.looking = llooking;

            if (llooking)
            {
                if (lookID == UUID.Zero)
                {
                    lookID = UUID.Random();
                }

                client.Self.LookAtEffect(client.Self.AgentID, target, new Vector3d(0,0,0), LookAtType.Idle, lookID);
                //lookattarget = target;
            }
            else
            {
                //if (lookID == UUID.Zero) return;

                Vector3d lkpos = new Vector3d(new Vector3(3, 0, 0) * Quaternion.Identity);
                client.Self.LookAtEffect(client.Self.AgentID, client.Self.AgentID, lkpos, LookAtType.Idle, lookID);
                //lookID = UUID.Zero;
            }
        }

        public void LookAtObject(bool llooking, UUID target)
        {
            if (instance.Config.CurrentConfig.DisableLookAt)
                return;

            //this.looking = llooking;

            if (llooking)
            {
                if (lookID == UUID.Zero)
                {
                    lookID = UUID.Random();
                }

                client.Self.LookAtEffect(client.Self.AgentID, target, Vector3d.Zero, LookAtType.Select, lookID);
                //lookattarget = target;
            }
            else
            {
                //if (lookID == UUID.Zero) return;

                Vector3d lkpos = new Vector3d(new Vector3(2, 0, 0) * Quaternion.Identity);
                client.Self.LookAtEffect(client.Self.AgentID, client.Self.AgentID, lkpos, LookAtType.Idle, lookID);
                //lookID = UUID.Zero;
            }
        }

        //private void LookIdle(float dist)
        //{
        //    Vector3d lkpos = new Vector3d(new Vector3(dist, 0, 0) * Quaternion.Identity);
        //    lookID = UUID.Random();

        //    client.Self.LookAtEffect(client.Self.AgentID, client.Self.AgentID, lkpos, LookAtType.Idle, lookID);
        //}

        public DateTime GetTimeStamp(DateTime dte)
        {
            //DateTime dte = item.Timestamp;

            if (instance.Config.CurrentConfig.UseSLT)
            {
                string _timeZoneId = "Pacific Standard Time";
                DateTime startTime = DateTime.UtcNow;
                TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
                dte = TimeZoneInfo.ConvertTime(startTime, TimeZoneInfo.Utc, tst);
            }

            return dte;
        }

        public void StopAnimations()
        {
            client.Self.SignaledAnimations.ForEach((UUID anims) =>
            {
                client.Self.AnimationStop(anims, true);
            });
        }

        public UUID TypingAnimationID { get; set; } = new UUID("c541c47f-e0c0-058b-ad1a-d6ae3a4584d9");

        public UUID AwayAnimationID { get; set; } = new UUID("fd037134-85d4-f241-72c6-4f42164fedee");

        public UUID BusyAnimationID { get; set; } = new UUID("efcf670c2d188128973a034ebc806b67");

        public UUID BellydanceAnimationID { get; set; } = new UUID("f2c2f006-69a2-089d-64bc-94efe4f3bb23");

        public UUID ClubdanceAnimationID { get; set; } = new UUID("cb956f10-cc64-71a3-a36c-61823794f7df");

        public UUID SalsaAnimationID { get; set; } = new UUID("6953622f-b308-3c84-4c28-a0cb9d5f9749");

        public UUID FallAnimationID { get; set; } = new UUID("85db9c46-2c49-d4d0-d7eb-b5d954d8d8a3");

        public UUID CrouchAnimationID { get; set; } = new UUID(Animations.CROUCH.ToString());

        public bool IsTyping { get; private set; } = false;

        public bool IsAway { get; private set; } = false;

        public bool IsBusy { get; private set; } = false;

        public bool IsBelly { get; private set; } = false;

        public bool IsFlying { get; private set; } = false;

        public bool IsSitting { get; set; } = false;

        public bool IsSittingOnGround { get; private set; } = false;

        public bool IsPointing { get; private set; } = false;

        public bool IsFollowing { get; private set; } = false;

        public string FollowName { get; set; } = string.Empty;

        //public string GoName
        //{
        //    get { return goName; }
        //    set { goName = value; }
        //}

        public float FollowDistance { get; set; } = 5.0f;

        public UUID FollowID { get; set; } = UUID.Zero;

        public SafeDictionary<UUID, String> GroupStore { get; set; } = new SafeDictionary<UUID, String>();

        public Dictionary<UUID, Group> Groups { get; set; } = new Dictionary<UUID, Group>();

        public List<FriendInfo> AvatarFriends { get; set; } = new List<FriendInfo>();

        public string CurrentTab { get; set; } = "Chat";

        public UUID SitPrim { get; set; } = UUID.Zero;

        public int UnReadIMs { get; set; } = 0;

        public bool FolderRcvd { get; set; } = false;

        public Vector3 SittingPos { get; set; } = new Vector3(0, 0, 0);
    }
}
