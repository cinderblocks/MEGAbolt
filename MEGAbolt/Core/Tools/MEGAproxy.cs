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
using System.Net;
using OpenMetaverse;
using System.Windows.Forms;

namespace MEGAbolt
{
    class MEGAproxy
    {
        public void SetProxy(bool UseProxy, string proxy_url, string port, string username, string password)
        {
            if (!UseProxy)
            {
                DisableProxy();
                return;
            }

            if (string.IsNullOrEmpty(proxy_url))
            {
                UseProxy = false;
                Logger.Log("Proxy Error: A proxy URI has not been specified", Helpers.LogLevel.Warning);   
            }

            if (UseProxy)
            {
                string purl = proxy_url.Trim();

                if (!purl.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase))
                {
                    purl = @"http://" + purl; 
                }

                try
                {
                    if (port.Length > 1)
                    {
                        purl = purl + ":" + port.Trim() + @"/";
                    }

                    WebProxy proxy = new WebProxy(purl,true)
                    {
                        Credentials = new NetworkCredential(username.Trim(), password.Trim())
                    };
                    WebRequest.DefaultWebProxy = proxy;
                }
                catch (Exception ex)
                {
                    Logger.Log("Proxy: " + ex.Message, Helpers.LogLevel.Error);
                    MessageBox.Show(ex.Message); 
                }
            }
            else
            {
                try
                {
                    DisableProxy();
                }
                catch (Exception ex)
                {
                    Logger.Log("Proxy: " + ex.Message, Helpers.LogLevel.Error);
                    MessageBox.Show(ex.Message); 
                }
            }
        }

        private static void DisableProxy()
        {
            IWebProxy proxy = new WebProxy();
            proxy.Credentials = CredentialCache.DefaultNetworkCredentials;   // null;
            WebRequest.DefaultWebProxy = proxy;
            return;
        }
    }
}
