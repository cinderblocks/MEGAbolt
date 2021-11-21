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
using System.Text.RegularExpressions;

namespace MEGAbrain
{
  public class mBrain
  {
    public string ProcessInput(string input, string user)
    {
      string empty = string.Empty;
      input = input.Replace("what's", "what is");
      input = input.Replace("what s", "what is");
      input = input.Replace("whats", "what is");
      input = " " + input;
      string str = CheckMaths(input);
      if (str == string.Empty)
        str = CheckDate(input);
      return str;
    }

    public string ProcessSmileys(string input)
    {
      input = input.Replace(":)", "smile");
      input = input.Replace(":-)", "smile");
      input = input.Replace(":(", "sad");
      input = input.Replace(":-(", "sad");
      input = input.Replace(";)", "wink");
      input = input.Replace(";-)", "wink");
      return input;
    }

    private string CheckDate(string input)
    {
      string str = string.Empty;
      DateTime dateTime;
      if (input.IndexOf("what is the date today") > 0)
      {
        dateTime = DateTime.Today;
        dateTime = dateTime.Date;
        str = "it's " + dateTime.ToString();
      }
      if (input.IndexOf("what is todays date") > 0)
      {
        dateTime = DateTime.Today;
        dateTime = dateTime.Date;
        str = "it's " + dateTime.ToString();
      }
      if (input.IndexOf("what is todays's date") > 0)
      {
        dateTime = DateTime.Today;
        dateTime = dateTime.Date;
        str = "it's " + dateTime.ToString();
      }
      if (input.IndexOf("what date is it") > 0)
      {
        dateTime = DateTime.Today;
        dateTime = dateTime.Date;
        str = "it's " + dateTime.ToString();
      }
      if (input.IndexOf("what day is it") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.DayOfWeek.ToString();
      }
      if (input.IndexOf("what is the day today") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.DayOfWeek.ToString();
      }
      if (input.IndexOf("what day of the week is it") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.DayOfWeek.ToString();
      }
      if (input.IndexOf("what time is it") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.TimeOfDay.ToString();
      }
      if (input.IndexOf("what is the time") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.TimeOfDay.ToString();
      }
      if (input.IndexOf("what month is it") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.Month.ToString();
      }
      if (input.IndexOf("which month are we") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.Month.ToString();
      }
      if (input.IndexOf("what month are we") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.Month.ToString();
      }
      if (input.IndexOf("what is the month") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.Month.ToString();
      }
      if (input.IndexOf("which month is it") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.Month.ToString();
      }
      if (input.IndexOf("what year is it") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.Year.ToString();
      }
      if (input.IndexOf("what is the year") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.Year.ToString();
      }
      if (input.IndexOf("which year is it") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.Year.ToString();
      }
      if (input.IndexOf("what year are we") > 0)
      {
        dateTime = DateTime.Today;
        str = "it's " + dateTime.Year.ToString();
      }
      return str;
    }

    private string CheckMaths(string input)
    {
      string str1 = string.Empty;
      int num = 0;
      string empty1 = string.Empty;
      string empty2 = string.Empty;
      bool flag = false;
      string str2 = input.Replace('?', ' ').Replace('.', ' ').Replace("times", "*").Replace("multipliedby", "*").Replace("multiplied by", "*").Replace("multiplied", "*").Replace("plus", "+").Replace("add to", "+").Replace("added to", "+").Replace("add", "+").Replace("minus", "-");
      if (str2.IndexOf("subtracted from") > 0)
      {
        str2 = str2.Replace("subtracted from", "-");
        flag = true;
      }
      if (str2.IndexOf("minus from") > 0)
      {
        str2 = str2.Replace("minus from", "-");
        flag = true;
      }
      if (str2.IndexOf("minused from") > 0)
      {
        str2 = str2.Replace("minused from", "-");
        flag = true;
      }
      string str3 = str2.Replace("subtract", "-").Replace("divided by", "/").Replace("dividedby", "/").Replace("divided", "/");
      if (str3.Contains("x"))
      {
        int length = str3.IndexOf("x");
        num = str3.Length - (length + 1);
        string str4 = str3.Substring(0, length);
        string str5 = str3.Substring(length + 1);
        if (!(str4 == string.Empty) && !(str5 == string.Empty))
        {
          char ch = ' ';
          string[] strArray1 = str4.Split(ch);
          string[] strArray2 = str5.Split(ch);
          string strNumber1 = strArray1[strArray1.Length - 1];
          if (strNumber1 == string.Empty)
            strNumber1 = strArray1[strArray1.Length - 2];
          string strNumber2 = strArray2[0];
          if (strNumber2 == string.Empty)
            strNumber2 = strArray2[1];
          if (IsNumber(strNumber1) && IsNumber(strNumber2))
            str1 = Convert.ToString((float) Convert.ToInt32(strNumber1) * (float) Convert.ToInt32(strNumber2));
        }
      }
      if (str3.Contains("*"))
      {
        int length = str3.IndexOf("*");
        num = str3.Length - (length + 1);
        string str6 = str3.Substring(0, length);
        string str7 = str3.Substring(length + 1);
        if (!(str6 == string.Empty) && !(str7 == string.Empty))
        {
          char ch = ' ';
          string[] strArray3 = str6.Split(ch);
          string[] strArray4 = str7.Split(ch);
          string strNumber3 = strArray3[strArray3.Length - 1];
          if (strNumber3 == string.Empty)
            strNumber3 = strArray3[strArray3.Length - 2];
          string strNumber4 = strArray4[0];
          if (strNumber4 == string.Empty)
            strNumber4 = strArray4[1];
          if (IsNumber(strNumber3) && IsNumber(strNumber4))
            str1 = Convert.ToString((float) Convert.ToInt32(strNumber3) * (float) Convert.ToInt32(strNumber4));
        }
      }
      if (str3.Contains("/"))
      {
        int length = str3.IndexOf("/");
        num = str3.Length - (length + 1);
        string str8 = str3.Substring(0, length);
        string str9 = str3.Substring(length + 1);
        if (!(str8 == string.Empty) && !(str9 == string.Empty))
        {
          char ch = ' ';
          string[] strArray5 = str8.Split(ch);
          string[] strArray6 = str9.Split(ch);
          string strNumber5 = strArray5[strArray5.Length - 1];
          if (strNumber5 == string.Empty)
            strNumber5 = strArray5[strArray5.Length - 2];
          string strNumber6 = strArray6[0];
          if (strNumber6 == string.Empty)
            strNumber6 = strArray6[1];
          if (IsNumber(strNumber5) && IsNumber(strNumber6))
            str1 = Convert.ToString((float) Convert.ToInt32(strNumber5) / (float) Convert.ToInt32(strNumber6));
        }
      }
      if (str3.Contains("+"))
      {
        str3 = str3.Replace('?', ' ').Replace('.', ' ');
        int length = str3.IndexOf("+");
        num = str3.Length - (length + 1);
        string str10 = str3.Substring(0, length);
        string str11 = str3.Substring(length + 1);
        if (!(str10 == string.Empty) && !(str11 == string.Empty))
        {
          char ch = ' ';
          string[] strArray7 = str10.Split(ch);
          string[] strArray8 = str11.Split(ch);
          string strNumber7 = strArray7[strArray7.Length - 1];
          if (strNumber7 == string.Empty)
            strNumber7 = strArray7[strArray7.Length - 2];
          string strNumber8 = strArray8[0];
          if (strNumber8 == string.Empty)
            strNumber8 = strArray8[1];
          if (IsNumber(strNumber7) && IsNumber(strNumber8))
            str1 = Convert.ToString((float) Convert.ToInt32(strNumber7) + (float) Convert.ToInt32(strNumber8));
        }
      }
      if (str3.Contains("-"))
      {
        int length = str3.IndexOf("-");
        num = str3.Length - (length + 1);
        string str12 = str3.Substring(0, length);
        string str13 = str3.Substring(length + 1);
        if (!(str12 == string.Empty) && !(str13 == string.Empty))
        {
          char ch = ' ';
          string[] strArray9 = str12.Split(ch);
          string[] strArray10 = str13.Split(ch);
          string strNumber9 = strArray9[strArray9.Length - 1];
          if (strNumber9 == string.Empty)
            strNumber9 = strArray9[strArray9.Length - 2];
          string strNumber10 = strArray10[0];
          if (strNumber10 == string.Empty)
            strNumber10 = strArray10[1];
          if (IsNumber(strNumber9) && IsNumber(strNumber10))
          {
            float int32_1 = (float) Convert.ToInt32(strNumber9);
            float int32_2 = (float) Convert.ToInt32(strNumber10);
            str1 = flag ? Convert.ToString(int32_2 - int32_1) : Convert.ToString(int32_1 - int32_2);
          }
        }
      }
      string[] strArray11 = new string[3]
      {
        "It's ",
        "It should be ",
        ""
      };
      Random random1 = new Random();
      string str14 = strArray11[random1.Next(0, strArray11.Length)];
      string[] strArray12 = new string[3]
      {
        " I think!",
        " I believe :S",
        ""
      };
      Random random2 = new Random();
      string str15 = strArray12[random2.Next(0, strArray12.Length)];
      return str1 != string.Empty ? str14 + str1 + str15 : string.Empty;
    }

    private bool IsNumber(string strNumber)
    {
      Regex regex1 = new Regex("[^0-9.-]");
      Regex regex2 = new Regex("[0-9]*[.][0-9]*[.][0-9]*");
      Regex regex3 = new Regex("[0-9]*[-][0-9]*[-][0-9]*");
      Regex regex4 = new Regex("(" + "^([-]|[.]|[-.]|[0-9])[0-9]*[.]*[0-9]+$" + ")|(" + "^([-]|[0-9])[0-9]*$" + ")");
      return !regex1.IsMatch(strNumber) && !regex2.IsMatch(strNumber) && !regex3.IsMatch(strNumber) && regex4.IsMatch(strNumber);
    }
  }
}
