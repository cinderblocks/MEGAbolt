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

// **************************
// *** INIFile class V1.0    ***
// **************************
// *** (C)2009 S.T.A. snc ***
// **************************
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace MEGAbolt.FileINI
{
    internal class INIFile
    {
        #region "Declarations"

        // *** Lock for thread-safe access to file and local cache ***
        private readonly Lock m_Lock = new Lock();

        // *** File name ***
        internal string FileName { get; private set; } = null;

        // *** Lazy loading flag ***
        private bool m_Lazy = false;

        // *** Local cache ***
        public Dictionary<string, Dictionary<string, string>> m_Sections = new Dictionary<string, Dictionary<string, string>>();

        // *** Local cache modified flag ***
        private bool m_CacheModified = false;

        #endregion

        #region "Methods"

        // *** Constructor ***
        public INIFile(string FileName)
        {
            Initialize(FileName, false);
        }

        public INIFile(string FileName, bool Lazy)
        {
            Initialize(FileName, Lazy);
        }

        // *** Initialization ***
        private void Initialize(string FileName, bool Lazy)
        {
            this.FileName = FileName;
            m_Lazy = Lazy;
            if (!m_Lazy) Refresh();
        }

        // *** Read file contents into local cache ***
        internal void Refresh()
        {
            lock (m_Lock)
            {
                StreamReader sr = null;
                try
                {
                    // *** Clear local cache ***
                    m_Sections.Clear();

                    // *** Open the INI file ***
                    try
                    {
                        sr = new StreamReader(FileName);
                    }
                    catch (FileNotFoundException)
                    {
                        return;
                    }

                    // *** Read up the file content ***
                    Dictionary<string, string> CurrentSection = null;
                    while (sr.ReadLine() is { } s)
                    {
                        s = s.Trim();

                        // *** Check for section names ***
                        if (s.StartsWith("[", StringComparison.CurrentCultureIgnoreCase) && s.EndsWith("]", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (s.Length > 2)
                            {
                                var SectionName = s.Substring(1, s.Length - 2);

                                // *** Only first occurrence of a section is loaded ***
                                if (m_Sections.ContainsKey(SectionName))
                                {
                                    CurrentSection = null;
                                }
                                else
                                {
                                    CurrentSection = new Dictionary<string, string>();
                                    m_Sections.Add(SectionName, CurrentSection);
                                }
                            }
                        }
                        else if (CurrentSection != null)
                        {
                            // *** Check for key+value pair ***
                            int i;
                            if ((i = s.IndexOf('=')) > 0)
                            {
                                var j = s.Length - i - 1;
                                var Key = s[..i].Trim();
                                if (Key.Length > 0)
                                {
                                    // *** Only first occurrence of a key is loaded ***
                                    if (!CurrentSection.ContainsKey(Key))
                                    {
                                        var Value = (j > 0) ? (s.Substring(i + 1, j).Trim()) : ("");
                                        CurrentSection.Add(Key, Value);
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    // *** Cleanup: close file ***
                    sr?.Close();
                    sr = null;
                }
            }
        }

        // *** Flush local cache content ***
        internal void Flush()
        {
            lock (m_Lock)
            {
                // *** If local cache was not modified, exit ***
                if (!m_CacheModified) return;
                m_CacheModified = false;

                // *** Open the file ***
                using var sw = new StreamWriter(FileName);

                try
                {
                    // *** Cycle on all sections ***
                    var First = false;
                    foreach (var SectionPair in m_Sections)
                    {
                        var Section = SectionPair.Value;
                        if (First) sw.WriteLine();
                        First = true;

                        // *** Write the section name ***
                        sw.Write('[');
                        sw.Write(SectionPair.Key);
                        sw.WriteLine(']');

                        // *** Cycle on all key+value pairs in the section ***
                        foreach (var ValuePair in Section)
                        {
                            // *** Write the key+value pair ***
                            sw.Write(ValuePair.Key);

                            if (!string.IsNullOrEmpty(ValuePair.Key))
                            {
                                sw.Write('=');
                            }
                            sw.WriteLine(ValuePair.Value);
                        }
                    }
                }
                finally
                {
                    // *** Cleanup: close file ***
                    sw?.Close();
                }
            }
        }

        // *** Read a value from local cache ***
        internal string GetValue(string SectionName, string Key, string DefaultValue)
        {
            // *** Lazy loading ***
            if (m_Lazy)
            {
                m_Lazy = false;
                Refresh();
            }

            lock (m_Lock)
            {
                return !m_Sections.TryGetValue(SectionName, out var Section) ? DefaultValue
                    : Section.GetValueOrDefault(Key, DefaultValue);

                // *** Return the found value ***
            }
        }

        // *** Insert or modify a value in local cache ***
        internal void SetValue(string SectionName, string Key, string Value)
        {
            // *** Lazy loading ***
            if (m_Lazy)
            {
                m_Lazy = false;
                Refresh();
            }

            lock (m_Lock)
            {
                // *** Flag local cache modification ***
                m_CacheModified = true;

                //// *** Check if the section exists ***
                //Dictionary<string, string> Section;
                //if (!m_Sections.TryGetValue(SectionName, out Section))
                //{
                //    // *** If it doesn't, add it ***
                //    Section = new Dictionary<string, string>();
                //    m_Sections.Add(SectionName, Section);
                //}

                var Section = CreateSection(SectionName);

                // *** Modify the value ***
                Section.Remove(Key);
                Section.Add(Key, Value);
            }
        }

        internal void RemoveValue(string SectionName, string Key)
        {
            if (!m_Sections.TryGetValue(SectionName, out var Section))
            {
                Section.Remove(Key);
            }
        }

        internal Dictionary<string, string> CreateSection(string SectionName)
        {
            if (!m_Sections.TryGetValue(SectionName, out var Section))
            {
                // *** If it doesn't, add it ***
                Section = new Dictionary<string, string>();
                m_Sections.Add(SectionName, Section);
            }

            return Section;
        }

        // *** Encode byte array ***
        private string EncodeByteArray(byte[] Value)
        {
            if (Value == null) return null;

            var sb = new StringBuilder();
            foreach (var b in Value)
            {
                var hex = Convert.ToString(b, 16);
                var l = hex.Length;
                if (l > 2)
                {
                    sb.Append(hex.Substring(l - 2, 2));
                }
                else
                {
                    if (l < 2) sb.Append("0");
                    sb.Append(hex);
                }
            }
            return sb.ToString();
        }

        // *** Decode byte array ***
        private byte[] DecodeByteArray(string Value)
        {
            if (Value == null) return null;

            var l = Value.Length;
            if (l < 2) return new byte[] { };

            l /= 2;
            var Result = new byte[l];
            for (var i = 0; i < l; i++) Result[i] = Convert.ToByte(Value.Substring(i * 2, 2), 16);
            return Result;
        }

        // *** Getters for various types ***
        internal bool GetValue(string SectionName, string Key, bool DefaultValue)
        {
            var StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
            if (int.TryParse(StringValue, out var Value)) return (Value != 0);
            return DefaultValue;
        }

        internal int GetValue(string SectionName, string Key, int DefaultValue)
        {
            var StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
            if (int.TryParse(StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var Value)) return Value;
            return DefaultValue;
        }

        internal double GetValue(string SectionName, string Key, double DefaultValue)
        {
            var StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
            if (double.TryParse(StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var Value)) return Value;
            return DefaultValue;
        }

        internal byte[] GetValue(string SectionName, string Key, byte[] DefaultValue)
        {
            var StringValue = GetValue(SectionName, Key, EncodeByteArray(DefaultValue));
            try
            {
                return DecodeByteArray(StringValue);
            }
            catch (FormatException)
            {
                return DefaultValue;
            }
        }

        // *** Setters for various types ***
        internal void SetValue(string SectionName, string Key, bool Value)
        {
            SetValue(SectionName, Key, (Value) ? ("1") : ("0"));
        }

        internal void SetValue(string SectionName, string Key, int Value)
        {
            SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
        }

        internal void SetValue(string SectionName, string Key, double Value)
        {
            SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
        }

        internal void SetValue(string SectionName, string Key, byte[] Value)
        {
            SetValue(SectionName, Key, EncodeByteArray(Value));
        }

        #endregion
    }
}