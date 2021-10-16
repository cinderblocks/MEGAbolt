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
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MD5library
{
  public class Crypto
  {
    private string pkey = "A9B2e3A4m5F6g7Z8";
    private string initVector = "A9B2e3A4m5F6g7Z8";
    private string saltValue = "s@1tMBValueCs";
    private string hashAlgorithm = "SHA1";
    private int passwordIterations = 2;
    private string passPhrase = "MBGenPas5pr@se";
    private int keySize = 256;
    private SymmetricAlgorithm mobjCryptoService;

    public Crypto(SymmProvEnum NetSelected)
    {
      switch (NetSelected)
      {
        case SymmProvEnum.DES:
          mobjCryptoService = (SymmetricAlgorithm) new DESCryptoServiceProvider();
          break;
        case SymmProvEnum.RC2:
          mobjCryptoService = (SymmetricAlgorithm) new RC2CryptoServiceProvider();
          break;
        case SymmProvEnum.Rijndael:
          mobjCryptoService = (SymmetricAlgorithm) new RijndaelManaged();
          break;
      }
    }

    public Crypto(SymmetricAlgorithm ServiceProvider) => mobjCryptoService = ServiceProvider;

    private byte[] GetLegalKey(string Key)
    {
      string s;
      if (mobjCryptoService.LegalKeySizes.Length > 0)
      {
        int num = 0;
        int minSize;
        for (minSize = mobjCryptoService.LegalKeySizes[0].MinSize; Key.Length * 8 > minSize; minSize += mobjCryptoService.LegalKeySizes[0].SkipSize)
          num = minSize;
        s = Key.PadRight(minSize / 8, ' ');
      }
      else
        s = Key;
      return Encoding.ASCII.GetBytes(s);
    }

    public string Encrypting(string Source)
    {
      string pkey = this.pkey;
      byte[] bytes = Encoding.ASCII.GetBytes(Source);
      MemoryStream memoryStream = new MemoryStream();
      byte[] legalKey = GetLegalKey(pkey);
      mobjCryptoService.Key = legalKey;
      mobjCryptoService.IV = legalKey;
      ICryptoTransform encryptor = mobjCryptoService.CreateEncryptor();
      CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, encryptor, CryptoStreamMode.Write);
      cryptoStream.Write(bytes, 0, bytes.Length);
      cryptoStream.FlushFinalBlock();
      byte[] buffer = memoryStream.GetBuffer();
      int length = 0;
      while (length < buffer.Length && (length / 16 != 1 || buffer[length] != (byte) 0))
        ++length;
      return Convert.ToBase64String(buffer, 0, length);
    }

    public string Decrypting(string Source)
    {
      string pkey = this.pkey;
      byte[] buffer = Convert.FromBase64String(Source);
      MemoryStream memoryStream = new MemoryStream(buffer, 0, buffer.Length);
      byte[] legalKey = GetLegalKey(pkey);
      mobjCryptoService.Key = legalKey;
      mobjCryptoService.IV = legalKey;
      ICryptoTransform decryptor = mobjCryptoService.CreateDecryptor();
      return new StreamReader((Stream) new CryptoStream((Stream) memoryStream, decryptor, CryptoStreamMode.Read)).ReadToEnd();
    }

    public string Encrypt(string plainText)
    {
      byte[] bytes1 = Encoding.ASCII.GetBytes(initVector);
      byte[] bytes2 = Encoding.ASCII.GetBytes(saltValue);
      byte[] bytes3 = Encoding.UTF8.GetBytes(plainText);
      byte[] bytes4 = new PasswordDeriveBytes(passPhrase, bytes2, hashAlgorithm, passwordIterations).GetBytes(keySize / 8);
      RijndaelManaged rijndaelManaged = new RijndaelManaged();
      rijndaelManaged.Mode = CipherMode.CBC;
      ICryptoTransform encryptor = rijndaelManaged.CreateEncryptor(bytes4, bytes1);
      MemoryStream memoryStream = new MemoryStream();
      CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, encryptor, CryptoStreamMode.Write);
      cryptoStream.Write(bytes3, 0, bytes3.Length);
      cryptoStream.FlushFinalBlock();
      byte[] array = memoryStream.ToArray();
      memoryStream.Close();
      cryptoStream.Close();
      return Convert.ToBase64String(array);
    }

    public string Decrypt(string cipherText)
    {
      byte[] bytes1 = Encoding.ASCII.GetBytes(initVector);
      byte[] bytes2 = Encoding.ASCII.GetBytes(saltValue);
      byte[] buffer = Convert.FromBase64String(cipherText);
      byte[] bytes3 = new PasswordDeriveBytes(passPhrase, bytes2, hashAlgorithm, passwordIterations).GetBytes(keySize / 8);
      RijndaelManaged rijndaelManaged = new RijndaelManaged();
      rijndaelManaged.Mode = CipherMode.CBC;
      ICryptoTransform decryptor = rijndaelManaged.CreateDecryptor(bytes3, bytes1);
      MemoryStream memoryStream = new MemoryStream(buffer);
      CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, decryptor, CryptoStreamMode.Read);
      byte[] numArray = new byte[buffer.Length];
      int count = cryptoStream.Read(numArray, 0, numArray.Length);
      memoryStream.Close();
      cryptoStream.Close();
      return Encoding.UTF8.GetString(numArray, 0, count);
    }

    public enum SymmProvEnum
    {
      DES,
      RC2,
      Rijndael,
    }
  }
}
