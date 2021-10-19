/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2009-2014, Radegast Development Team
 * Copyright(c) 2016-2021, Sjofn, LLC
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
using System.Runtime.InteropServices;
using System.Threading;
using FMOD;
using OpenMetaverse;

namespace MEGAbolt.Media
{
    public class StreamInfoArgs : EventArgs
    {
        public string Key;
        public string Value;

        public StreamInfoArgs(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    public class Stream : MediaObject
    {
        /// <summary>
        /// Fired when a stream meta data is received
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Key, value are sent in e</param>
        public delegate void StreamInfoCallback(object sender, StreamInfoArgs e);

        /// <summary>
        /// Fired when a stream meta data is received
        /// </summary>
        public event StreamInfoCallback OnStreamInfo;

        Timer updateTimer = null;
        uint updateIntervl = 500;

        /// <summary>
        /// Releases resources of this sound object
        /// </summary>
        public override void Dispose()
        {
            StopStream();
            base.Dispose();
        }

        public void StopStream()
        {
            if (updateTimer != null)
            {
                updateTimer.Dispose();
                updateTimer = null;
            }

            if (!channel.hasHandle()) return;
            ManualResetEvent stopped = new ManualResetEvent(false);
            invoke(delegate
            {
                try
                {
                    FMODExec(channel.stop());
                    channel.clearHandle();
                    UnRegisterSound();
                    FMODExec(sound.release());
                    sound.clearHandle();
                }
                catch { }
                stopped.Set();
            });
            stopped.WaitOne();
        }

        /// <summary>
        /// Plays audio stream
        /// </summary>
        /// <param name="url">URL of the stream</param>
        public void PlayStream(string url)
        {
            // Stop old stream first.
            StopStream();

            extraInfo.format = SOUND_FORMAT.PCM16;

            invoke(delegate
            {
                try
                {
                    FMODExec(
                        system.setStreamBufferSize(4 * 128 * 128, TIMEUNIT.RAWBYTES));

                    FMODExec(
                        system.createSound(url,
                            (MODE._2D | MODE.CREATESTREAM),
                            ref extraInfo,
                            out sound), "Stream load");
                    // Register for callbacks.
                    RegisterSound(sound);

                    // Allocate a channel and set initial volume.
                    ChannelGroup masterChannelGroup;
                    system.getMasterChannelGroup(out masterChannelGroup);
                    FMODExec(system.playSound(sound, masterChannelGroup, false,
                        out channel), "Stream channel");
                    FMODExec(channel.setVolume(volume), "Stream volume");

                    if (updateTimer == null)
                    {
                        updateTimer = new Timer(Update);
                    }
                    updateTimer.Change(0, updateIntervl);
                }
                catch (Exception ex)
                {
                    Logger.Log("Error playing stream: " + ex, Helpers.LogLevel.Debug);
                }
            });
        }


        private void Update(object sender)
        {
            if (!sound.hasHandle()) return;

            invoke(() =>
            {
                try
                {
                    FMODExec(system.update());

                    TAG tag = new TAG();
                    var numTags = 0;
                    var numTagsUpdated = 0;

                    var res = sound.getNumTags(out numTags, out numTagsUpdated);

                    if (res != RESULT.OK || numTagsUpdated <= 0) return;
                    for (var i=0; i < numTags; i++)
                    {
                        if (sound.getTag(null, i, out tag) != RESULT.OK)
                        {
                            continue;
                        }

                        if (tag.type == TAGTYPE.FMOD && tag.name == "Sample Rate Change")
                        {
                            float newfreq = (float)Marshal.PtrToStructure(tag.data, typeof(float));
                            Logger.DebugLog("New stream frequency: " + newfreq.ToString("F" + 0));
                            channel.setFrequency(newfreq);
                        }

                        if (tag.datatype != TAGDATATYPE.STRING) continue;

                        // Tell listeners about the Stream tag.  This can be
                        // displayed to the user.
                        OnStreamInfo?.Invoke(this, new StreamInfoArgs(tag.name.ToString().ToLower(), Marshal.PtrToStringAnsi(tag.data)));
                    }
                }
                catch (Exception ex)
                {
                    Logger.DebugLog("Error getting stream tags: " + ex.Message);
                }
            });
        }
    }
}
