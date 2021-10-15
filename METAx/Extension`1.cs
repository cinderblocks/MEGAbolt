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
using System.Reflection;

namespace METAx
{
  public class Extension<ClientInterface>
  {
    private ExtensionType extensionType = ExtensionType.Unknown;
    private string filename = "";
    private SourceFileLanguage language = SourceFileLanguage.Unknown;
    private ClientInterface instance = default (ClientInterface);
    private Assembly instanceAssembly = (Assembly) null;

    public Extension()
    {
    }

    public Extension(string filename, ExtensionType extensionType, ClientInterface instance)
    {
      this.extensionType = extensionType;
      this.instance = instance;
      this.filename = filename;
    }

    public ExtensionType ExtensionType
    {
      get => this.extensionType;
      set => this.extensionType = value;
    }

    public string Filename
    {
      get => this.filename;
      set => this.filename = value;
    }

    public SourceFileLanguage Language
    {
      get => this.language;
      set => this.language = value;
    }

    public ClientInterface Instance
    {
      get => this.instance;
      set => this.instance = value;
    }

    public Assembly InstanceAssembly
    {
      get => this.instanceAssembly;
      set => this.instanceAssembly = value;
    }

    public Type GetType(string name) => this.instanceAssembly.GetType(name, false, true);
  }
}
