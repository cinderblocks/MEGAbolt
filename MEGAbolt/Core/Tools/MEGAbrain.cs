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
using MEGAbrain;
using System.Timers;
using MEGAbolt.NetworkComm;
using OpenMetaverse;
using System.Globalization;

namespace MEGAbolt
{
    public class MEGAbrain
    {
        private MEGAboltInstance instance;
        private MEGAboltNetcom netcom;
        public global::MEGAbrain.MEGAbrain answer;

        //private ArrayList textBuffer;
        //private bool showTimestamps;
        private AIMLbot.Bot myBot = null;
        private AIMLbot.User myUser = null;
        private Timer metaTimer;
        private InstantMessageEventArgs emt;
        //private int cnt = 0;

        public MEGAbrain(MEGAboltInstance instance, AIMLbot.Bot myBot)
        {
            this.instance = instance;
            //client = this.instance.Client;
            netcom = this.instance.Netcom;
            this.myBot = myBot;

            answer = new global::MEGAbrain.MEGAbrain();
        }

        public void StartProcess(InstantMessageEventArgs e)
        {
            emt = e;
            InitializeMetaTimer(emt.IM.Message.Length);
        }

        private void InitializeMetaTimer(int counter)
        {
            double timer_int = 3000;

            // Determine response time
            if (counter < 11)
            {
                timer_int = 2500;
            }
            else if ((counter > 10) && (counter < 31))
            {
                timer_int = 3000;
            }
            else if ((counter > 30) && (counter < 61))
            {
                timer_int = 4000;
            }
            else
            {
                timer_int = 6000;
            }

            //cnt += 1;

            // this is to buffer IM flood attacks
            //if (cnt < 16)
            //{
            metaTimer = new Timer(timer_int);

            metaTimer.Elapsed += metaTimer_Elapsed;

            metaTimer.Interval = timer_int;
            metaTimer.Enabled = true;
            metaTimer.Start();
            //}
        }

        private void metaTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            StartAI();
            //cnt -= 1;
        }

        private void StartAI()
        {
            metaTimer.Stop();
            metaTimer.Enabled = false;
            metaTimer.Dispose();

            string sanswer = answer.ProcessInput(emt.IM.Message.ToLower(CultureInfo.CurrentCulture), "");

            if (string.IsNullOrEmpty(sanswer))
            {
                string imsg = emt.IM.Message;
                imsg = answer.ProcessSmileys(imsg);

                ProcessAI(imsg, emt.IM.FromAgentName, emt.IM.FromAgentID, emt.IM.IMSessionID);
            }
            else
            {
                if (sanswer.Length > 0)
                    netcom.SendInstantMessage(sanswer, emt.IM.FromAgentID, emt.IM.IMSessionID);
            }
        }

        private void ProcessAI(string msg, string user, UUID target, UUID sess)
        {
            //string dland = "en";
            //MB_Translation_Utils.Utils trans = new MB_Translation_Utils.Utils();

            //if (instance.Config.CurrentConfig.MultiLingualAI)
            //{
            //    //dland = trans.DetectLanguageShortName(msg);

            //    //if (dland != "en")
            //    //{
            //    //    // translate to english
            //    //    msg = trans.Translate(msg, dland + "|en");
            //    //}
            //}

            string reply = GetResp(msg, user);

            if (string.IsNullOrEmpty(reply))
            {
                if (instance.Config.CurrentConfig.ReplyAI)
                {
                    reply = instance.Config.CurrentConfig.ReplyText;

                    if (string.IsNullOrEmpty(reply))
                    {
                        reply = "I am sorry but I didn't understand what you said or I haven't been taught a response for it. Can you try again, making sure your sentences are short and clear.";
                    }
                }
            }

            //if (instance.Config.CurrentConfig.MultiLingualAI)
            //{
            //    if (dland != "en")
            //    {
            //        reply = trans.Translate(reply, "en|" + dland);
            //    }
            //}

            netcom.SendInstantMessage(reply, target, sess);

            //trans = null;
        }

        public string GetResp(string msg, string user)
        {
            try
            {
                myUser = null;
                //GC.Collect();  

                myUser = new AIMLbot.User(user, myBot);

                AIMLbot.Request myRequest = new AIMLbot.Request(msg, myUser, myBot);
                AIMLbot.Result myResult = myBot.Chat(myRequest);

                string reply = myResult.Output;

                if (reply.Length > 5)
                {
                    if (reply.Substring(0, 5).ToLower(CultureInfo.CurrentCulture) == "error")
                    {
                        return string.Empty;
                    }

                    return reply;
                }

                return reply;
            }
            catch (Exception ex)
            {
                Logger.Log("There has been an error starting AI.", Helpers.LogLevel.Warning, ex);
                return string.Empty;
            }
        }
    }
}
