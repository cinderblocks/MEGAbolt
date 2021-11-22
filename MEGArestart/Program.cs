/*
 * MEGAbolt Metaverse Client
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
using System.Diagnostics;
using System.Threading;

namespace MEGArestart
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            int millisecondsTimeout = 600000;
            int num = millisecondsTimeout / 1000 / 60;
            DateTime now = DateTime.Now;
            if (args.Length > 0)
            {
                string str1 = args[0].ToString();
                string str2 = args[1].ToString();
                string str3 = args[2].ToString();
                string str4 = "Unknown";
                if (args.Length > 3)
                    str4 = args[3].ToString().Replace("|", " ");
                if (args.Length > 4)
                {
                    num = Convert.ToInt32(args[4].ToString()) / 60;
                    millisecondsTimeout = Convert.ToInt32(args[4].ToString()) * 1000;
                }
                Console.WriteLine("----------------------------<<< MEGArestart >>>----------------------------");
                Console.WriteLine("");
                Console.WriteLine("Re-Start Reason:");
                Console.WriteLine(str4);
                Console.WriteLine("");
                Console.WriteLine("Parameters used- Fistname:" + str1 + " Lastname:" + str2 + " Password: ***********");
                Console.Write(now.ToString() + ">>> MEGAbolt restarting...@ " + (object)now.AddMinutes((double)num));
                Thread.Sleep(millisecondsTimeout);
                new Process()
                {
                    StartInfo = {
                        FileName = "MEGAbolt.exe",
                        Arguments = (str1 + " " + str2 + " " + str3)
                    }
                }.Start();
            }
            else
            {
                Console.Write(now.ToString() + ">>> MEGAbolt restarting...@ " + (object)now.AddMinutes((double)num));
                Thread.Sleep(millisecondsTimeout);
                new Process() { StartInfo = { FileName = "MEGAbolt.exe" } }.Start();
            }
        }
    }
}
