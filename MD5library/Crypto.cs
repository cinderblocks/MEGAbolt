/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2021-2022, Sjofn, LLC
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

namespace MEGAcrypto
{
    public class Crypto
    {
        private readonly string pkey = "A9B2e3A4m5F6g7Z8";
        private readonly string initVector = "A9B2e3A4m5F6g7Z8";
        private readonly string saltValue = "s@1tMBValueCs";
        private readonly string hashAlgorithm = "SHA1";
        private readonly int passwordIterations = 2;
        private readonly string passPhrase = "MBGenPas5pr@se";
        private readonly int keySize = 256;
        private readonly SymmetricAlgorithm mobjCryptoService;

        public Crypto(SymmProvEnum NetSelected)
        {
            switch (NetSelected)
            {
                case SymmProvEnum.DES:
                    mobjCryptoService = DES.Create();
                    break;
                case SymmProvEnum.RC2:
                    mobjCryptoService = RC2.Create();
                    break;
                case SymmProvEnum.AES:
                    mobjCryptoService = Aes.Create();
                    break;
            }
        }

        public Crypto(SymmetricAlgorithm serviceProvider) => mobjCryptoService = serviceProvider;

        private byte[] GetLegalKey(string key)
        {
            string s;
            if (mobjCryptoService.LegalKeySizes.Length > 0)
            {
                var num = 0;
                int minSize;
                for (minSize = mobjCryptoService.LegalKeySizes[0].MinSize;
                     key.Length * 8 > minSize;
                     minSize += mobjCryptoService.LegalKeySizes[0].SkipSize)
                {
                    num = minSize;
                }

                s = key.PadRight(minSize / 8, ' ');
            }
            else
                s = key;
            return Encoding.ASCII.GetBytes(s);
        }

        public string Encrypting(string Source)
        {
            var pkey = this.pkey;
            var bytes = Encoding.ASCII.GetBytes(Source);
            var memoryStream = new MemoryStream();
            var legalKey = GetLegalKey(pkey);
            mobjCryptoService.Key = legalKey;
            mobjCryptoService.IV = legalKey;
            var transform = mobjCryptoService.CreateEncryptor();
            var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();
            var buffer = memoryStream.GetBuffer();
            var length = 0;
            while (length < buffer.Length && (length / 16 != 1 || buffer[length] != 0))
                ++length;
            return Convert.ToBase64String(buffer, 0, length);
        }

        public string Decrypting(string Source)
        {
            var pkey = this.pkey;
            var buffer = Convert.FromBase64String(Source);
            var memoryStream = new MemoryStream(buffer, 0, buffer.Length);
            var legalKey = GetLegalKey(pkey);
            mobjCryptoService.Key = legalKey;
            mobjCryptoService.IV = legalKey;
            var transform = mobjCryptoService.CreateDecryptor();
            return new StreamReader(new CryptoStream(memoryStream, transform, CryptoStreamMode.Read)).ReadToEnd();
        }

        public string Encrypt(string plainText)
        {
            var bytes1 = Encoding.ASCII.GetBytes(initVector);
            var bytes2 = Encoding.ASCII.GetBytes(saltValue);
            var bytes3 = Encoding.UTF8.GetBytes(plainText);
            var bytes4 = new PasswordDeriveBytes(passPhrase, bytes2, hashAlgorithm, passwordIterations).GetBytes(keySize / 8);
            var rijndaelManaged = Aes.Create();
            rijndaelManaged.Mode = CipherMode.CBC; // yuck
            var encryptor = rijndaelManaged.CreateEncryptor(bytes4, bytes1);
            var memoryStream = new MemoryStream();
            var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(bytes3, 0, bytes3.Length);
            cryptoStream.FlushFinalBlock();
            var array = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return Convert.ToBase64String(array);
        }

        public string Decrypt(string cipherText)
        {
            var bytes1 = Encoding.ASCII.GetBytes(initVector);
            var bytes2 = Encoding.ASCII.GetBytes(saltValue);
            var buffer = Convert.FromBase64String(cipherText);
            var bytes3 = new PasswordDeriveBytes(passPhrase, bytes2, hashAlgorithm, passwordIterations).GetBytes(keySize / 8);
            var rijndaelManaged = Aes.Create();
            rijndaelManaged.Mode = CipherMode.CBC;
            var decryptor = rijndaelManaged.CreateDecryptor(bytes3, bytes1);
            var memoryStream = new MemoryStream(buffer);
            var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            var numArray = new byte[buffer.Length];
            var count = cryptoStream.Read(numArray, 0, numArray.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(numArray, 0, count);
        }

        public enum SymmProvEnum
        {
            DES,
            RC2,
            AES,
        }
    }
}
