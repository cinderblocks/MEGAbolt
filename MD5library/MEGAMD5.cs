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
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace MD5library
{
  public class MEGAMD5
  {
    public string MD5(string password)
    {
      byte[] hash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(password + ":" + (object) 4564467));
      return hash.Aggregate("", (current, t) => current + Convert.ToString(t, 16).PadLeft(2, '0'));
    }

    public string MD5(string password, int length)
    {
      byte[] hash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(password + ":" + (object) 4564467));
      return hash.Aggregate("", (current, t) => current + Convert.ToString(t, length).PadLeft(2, '0'));
    }

    public string MD5Special(string password, int nonce)
    {
      byte[] hash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(password + ":" + (object) nonce));
      return hash.Aggregate("", (current, t) => current + Convert.ToString(t, 16).PadLeft(2, '0'));
    }

    public string MD5Special(string password, int nonce, int length)
    {
      byte[] hash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(password + ":" + (object) nonce));
      return hash.Aggregate("", (current, t) => current + Convert.ToString(t, length).PadLeft(2, '0'));
    }

    public string MachineMD5(string password)
    {
      byte[] hash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(password + "1gen/passkey" + ":" + (object) 93674681));
      return hash.Aggregate("", (current, t) => current + Convert.ToString(t, 16).PadLeft(2, '0'));
    }

    public string GetAvatarKey(string avname, string avUUID) => MachineMD5(avUUID);

    public string GetMachinePassKey()
    {
      IPGlobalProperties globalProperties = IPGlobalProperties.GetIPGlobalProperties();
      NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
      if (networkInterfaces.Length < 1)
        return string.Empty;
      foreach (var networkInterface in networkInterfaces)
      {
        var physicalAddress = networkInterface.GetPhysicalAddress();
        string password = globalProperties.HostName + physicalAddress.ToString();
        return MachineMD5(password);
      }
      return string.Empty;
    }

    public bool ValidateMachinePasscode(string passcode)
    {
      IPGlobalProperties globalProperties = IPGlobalProperties.GetIPGlobalProperties();
      NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
      if (networkInterfaces.Length < 1)
        return false;
      string password = string.Empty;
      foreach (var networkInterface in networkInterfaces)
      {
        var physicalAddress = networkInterface.GetPhysicalAddress();
        password = globalProperties.HostName + physicalAddress;
        break;
      }
      return MachineMD5(MachineMD5(password) + " - Remove MB ads") == passcode;
    }
  }
}
