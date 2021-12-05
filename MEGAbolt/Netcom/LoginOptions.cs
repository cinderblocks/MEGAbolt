/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2009-2014, Radegast Development Team
 * Copyright(c) 2016-2020, Sjofn, LLC
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

using OpenMetaverse;

namespace MEGAbolt.NetworkComm
{
    public class LoginOptions
    {
        /// <summary>
        /// Method for generating a stupid Second Life password hash. 
        /// That is to say MD5 hash with input truncated at 16 characters.
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <returns>MD5 sum of password</returns>
        public static string SecondLifePassHashIfNecessary(string password)
        {
            if (password.Length == 35 && password.StartsWith("$1$"))
            {
                return password;
            }
            else
            {
                return Utils.MD5(password.Length > 16 ? password.Substring(0, 16) : password);
            }
        }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName))
                    return string.Empty;
                else
                    return FirstName + " " + LastName;
            }
        }

        public string Password { get; set; }

        public StartLocationType StartLocation { get; set; } = StartLocationType.Home;

        public string StartLocationCustom { get; set; } = string.Empty;

        public string Channel { get; set; } = "MEGAbolt";

        public string Version { get; set; } = string.Empty;

        public LoginGrid Grid { get; set; }

        public string GridCustomLoginUri { get; set; } = string.Empty;
    }
}
