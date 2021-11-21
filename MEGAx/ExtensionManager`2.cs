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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MEGAx
{
  public class ExtensionManager<ClientInterface, HostInterface>
  {
      public event AssemblyLoadingEventHandler AssemblyLoading;

    private void OnAssemblyLoading(AssemblyLoadingEventArgs e)
    {
      if (AssemblyLoading == null)
        return;
      AssemblyLoading((object) this, e);
    }

    public event AssemblyLoadedEventHandler AssemblyLoaded;

    private void OnAssemblyLoaded(AssemblyLoadedEventArgs e)
    {
      if (AssemblyLoaded == null)
        return;
      AssemblyLoaded((object) this, e);
    }

    public event AssemblyFailedLoadingEventHandler AssemblyFailedLoading;

    private void OnAssemblyFailedLoading(AssemblyFailedLoadingEventArgs e)
    {
      if (AssemblyFailedLoading == null)
        return;
      AssemblyFailedLoading((object) this, e);
    }

    public Dictionary<string, SourceFileLanguage> SourceFileExtensionMappings { get; set; } = new Dictionary<string, SourceFileLanguage>();

    public List<Extension<ClientInterface>> Extensions { get; set; } = new List<Extension<ClientInterface>>();

    public List<string> CompiledFileExtensions { get; set; } = new List<string>();

    public List<string> SourceFileReferencedAssemblies { get; set; } = new List<string>();

    public void UnloadExtension(Extension<ClientInterface> extension)
    {
      Extension<ClientInterface> extension1 = (Extension<ClientInterface>) null;
      foreach (Extension<ClientInterface> extension2 in Extensions)
      {
        if (extension2.Filename.ToLower().Trim() == extension.Filename.ToLower().Trim())
        {
          extension1 = extension2;
          break;
        }
      }
      Extensions.Remove(extension1);
    }

    public void LoadDefaultFileExtensions()
    {
      SourceFileExtensionMappings.Add(".cs", SourceFileLanguage.CSharp);
      SourceFileExtensionMappings.Add(".vb", SourceFileLanguage.Vb);
      SourceFileExtensionMappings.Add(".js", SourceFileLanguage.Javascript);
      CompiledFileExtensions.Add(".dll");
      CompiledFileExtensions.Add(".exe");
    }

    public void LoadExtensions(string folderPath)
    {
      if (!Directory.Exists(folderPath))
        return;
      foreach (string file in Directory.GetFiles(folderPath))
        LoadExtension(file);
    }

    public void LoadExtension(string filename)
    {
      AssemblyLoadingEventArgs e = new AssemblyLoadingEventArgs(filename);
      OnAssemblyLoading(e);
      if (e.Cancel)
        return;
      string lower = new FileInfo(filename).Extension.TrimStart('.').Trim().ToLower();
      if (SourceFileExtensionMappings.ContainsKey(lower) || SourceFileExtensionMappings.ContainsKey("." + lower))
      {
        SourceFileLanguage language = !SourceFileExtensionMappings.ContainsKey(lower) ? SourceFileExtensionMappings["." + lower] : SourceFileExtensionMappings[lower];
        loadSourceFile(filename, language);
      }
      else if (CompiledFileExtensions.Contains(lower) || CompiledFileExtensions.Contains("." + lower))
        loadCompiledFile(filename);
      else
        OnAssemblyFailedLoading(new AssemblyFailedLoadingEventArgs(filename)
        {
          ExtensionType = ExtensionType.Unknown,
          ErrorMessage = "File (" + filename + ") does not match any SourceFileExtensionMappings or CompiledFileExtensions and cannot be loaded."
        });
    }

    private void loadSourceFile(string filename, SourceFileLanguage language)
    {
      bool flag = false;
      string str = "";
      CompilerResults compilerResults = compileScript(filename, SourceFileReferencedAssemblies, getCodeDomLanguage(language));
      if (compilerResults.Errors.Count <= 0)
      {
        foreach (Type type in compilerResults.CompiledAssembly.GetTypes())
        {
          string name = typeof (ClientInterface).ToString();
          if (type.GetInterface(name, true) != null)
          {
            try
            {
              Extensions.Add(new Extension<ClientInterface>(filename, ExtensionType.SourceFile, (ClientInterface) compilerResults.CompiledAssembly.CreateInstance(type.FullName, true))
              {
                InstanceAssembly = compilerResults.CompiledAssembly
              });
              flag = true;
            }
            catch (Exception ex)
            {
              str = "Error Creating Instance of Compiled Source File (" + filename + "): " + ex.Message;
            }
          }
        }
        if (!flag && string.IsNullOrEmpty(str))
          str = "Expected interface (" + typeof (ClientInterface).ToString() + ") was not found in any types in the compiled Source File";
      }
      else
        str = "Source File Compilation Errors were Detected";
      if (!flag)
        OnAssemblyFailedLoading(new AssemblyFailedLoadingEventArgs(filename)
        {
          ExtensionType = ExtensionType.SourceFile,
          SourceFileCompilerErrors = compilerResults.Errors,
          ErrorMessage = str
        });
      else
        OnAssemblyLoaded(new AssemblyLoadedEventArgs(filename));
    }

    private void loadCompiledFile(string filename)
    {
      bool flag = false;
      string str = "";
      Assembly assembly = (Assembly) null;
      byte[] rawAssembly = File.ReadAllBytes(filename);
      try
      {
        assembly = Assembly.Load(rawAssembly);
      }
      catch
      {
        str = "Compiled Assembly (" + filename + ") is not a valid Assembly File to be Loaded.";
      }
      if (assembly != null)
      {
        foreach (Type type in assembly.GetTypes())
        {
          string name = typeof (ClientInterface).ToString();
          if (type.GetInterface(name, true) != null)
          {
            try
            {
              Extensions.Add(new Extension<ClientInterface>(filename, ExtensionType.Compiled, (ClientInterface) assembly.CreateInstance(type.FullName, true))
              {
                InstanceAssembly = assembly
              });
              flag = true;
            }
            catch (Exception ex)
            {
              str = "Error Creating Instance of Compiled Assembly (" + filename + "): " + ex.Message;
            }
          }
        }
        if (!flag && string.IsNullOrEmpty(str))
          str = "Expected interface (" + typeof (ClientInterface).ToString() + ") was not found in Compiled Assembly (" + filename + ")";
      }
      if (!flag)
        OnAssemblyFailedLoading(new AssemblyFailedLoadingEventArgs(filename)
        {
          ExtensionType = ExtensionType.Compiled,
          ErrorMessage = str
        });
      else
        OnAssemblyLoaded(new AssemblyLoadedEventArgs(filename));
    }

    private CompilerResults compileScript(
      string filename,
      List<string> references,
      string language)
    {
      CodeDomProvider provider = CodeDomProvider.CreateProvider(language);
      CompilerParameters options = new CompilerParameters
      {
          GenerateExecutable = false,
          GenerateInMemory = true,
          IncludeDebugInformation = false
      };
      if (references != null)
        options.ReferencedAssemblies.AddRange(references.ToArray());
      return provider.CompileAssemblyFromFile(options, filename);
    }

    private string getCodeDomLanguage(SourceFileLanguage language)
    {
      string str = "C#";
      switch (language)
      {
        case SourceFileLanguage.CSharp:
          str = "C#";
          break;
        case SourceFileLanguage.Vb:
          str = "VB";
          break;
        case SourceFileLanguage.Javascript:
          str = "JS";
          break;
      }
      return str;
    }

    public delegate void AssemblyLoadingEventHandler(object sender, AssemblyLoadingEventArgs e);

    public delegate void AssemblyLoadedEventHandler(object sender, AssemblyLoadedEventArgs e);

    public delegate void AssemblyFailedLoadingEventHandler(
      object sender,
      AssemblyFailedLoadingEventArgs e);
  }
}
