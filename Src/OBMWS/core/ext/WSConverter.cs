using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

#region license
//	GNU General Public License (GNU GPLv3)
 
//	Copyright © 2016 Odense Bys Museer

//	Author: Andriy Volkov

//	This program is free software: you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation, either version 3 of the License, or
//	(at your option) any later version.

//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//	See the GNU General Public License for more details.

//	You should have received a copy of the GNU General Public License
//	along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

namespace OBMWS
{
    public class WSConverter
    {
        public bool IsTrue(string value) { return !string.IsNullOrEmpty(value) && WSConstants.ALIACES.TRUE.Match(value); }

        public string ToMd5Hash(string input, bool IntegrateWithServerPass = false)
        {
            if (!string.IsNullOrEmpty(input))
            {
                using (MD5 md5Hash = MD5.Create())
                {
                    byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes((IntegrateWithServerPass?WSConstants.CONFIG.XPass:"") + input));
                    StringBuilder sBuilder = new StringBuilder();
                    for (int i = 0; i < data.Length; i++)
                    {
                        sBuilder.Append(data[i].ToString("x2"));
                    }
                    return sBuilder.ToString();
                }
            }
            return string.Empty;
        }
        public void ToMd5Hash(string input, out string output, bool IntegrateWithServerPass=false)
        {
            output = ToMd5Hash(input, IntegrateWithServerPass);
        }

        public bool FromBase64(string input, out string output)
        {
            output = string.Empty;
            try { if (!string.IsNullOrEmpty(input)) { output = Encoding.UTF8.GetString(Convert.FromBase64String(input)); return true; } }
            catch (Exception) { }
            return false;
        }
        public string FromBase64(string input)
        {
            string output = string.Empty;
            try { if (!string.IsNullOrEmpty(input)) { output = Encoding.UTF8.GetString(Convert.FromBase64String(input)); } } catch (Exception) { }
            return output;
        }

        public bool ToBase64(string input, out string output)
        {
            output = string.Empty;
            try { if (!string.IsNullOrEmpty(input)) { output = Convert.ToBase64String(Encoding.UTF8.GetBytes(input)); return true; } } catch (Exception) { }
            return false;
        }
        public string ToBase64(string input)
        {
            string output = string.Empty;
            try { if (!string.IsNullOrEmpty(input)) { output = Convert.ToBase64String(Encoding.UTF8.GetBytes(input)); } } catch (Exception) { }
            return output;
        }

        public byte[] GetBytes(string input)
        {
            try { if (!string.IsNullOrEmpty(input)) { return Convert.FromBase64String(input); } } catch (Exception) { }
            return null;
        }

        public string ExceptionToJson(Exception ex)
        {
            string json = "{}";
            try
            {
                WSException ioex = new WSException()
                {
                    Message = ex.Message,
                    Type = ex.GetType().ToString(),
                    HelpLink = ex.HelpLink,
                    #if NET45
                        HResult = ex.HResult,
                    #endif
                    StackTrace = ex.StackTrace,
                    Source = ex.Source,
                };
                Exception bex = ex.GetBaseException();
                if (bex != null)
                {
                    ioex.BaseException = new WSBaseException()
                    {
                        Message = bex.Message,
                        Type = bex.GetType().ToString(),
                        HelpLink = bex.HelpLink,
                        #if NET45
                            HResult = bex.HResult,
                        #endif
                        StackTrace = bex.StackTrace,
                        Source = bex.Source
                    };
                }
                Exception iex = ex.InnerException;
                if (iex != null)
                {
                    ioex.InnerException = new WSBaseException()
                    {
                        Message = iex.Message,
                        Type = iex.GetType().ToString(),
                        HelpLink = iex.HelpLink,
                        #if NET45
                            HResult = iex.HResult,
                        #endif
                        StackTrace = iex.StackTrace,
                        Source = iex.Source
                    };
                }
                json = new JavaScriptSerializer().Serialize(ioex);
            }
            catch (Exception) { }

            return json;
        }

        public bool ToDate(string val, out DateTime? d)
        {
            d = null;
            try
            {
                if (val.IsTrue() || val.IsFalse())
                {
                    d = val.IsTrue() ? (DateTime?)DateTime.Now : null;
                    return true;
                }
                else
                {
                    DateTime d2 = DateTime.MinValue;
                    if (ToDate(val, out d2)) { d = d2; return true; }
                }
            }
            catch (Exception) { }
            return false;
        }

        public bool ToDate(string val, out DateTime d)
        {
            d = DateTime.MinValue;
            try
            {
                int year = DateTime.MinValue.Year;
                int mth = DateTime.MinValue.Month;
                int day = DateTime.MinValue.Day;
                int hour = DateTime.MinValue.Hour;
                int min = DateTime.MinValue.Minute;
                int sec = DateTime.MinValue.Second;
                int ms = DateTime.MinValue.Millisecond;

                string[] date_time = val.ToWords();
                if (date_time != null && date_time.Length > 0)
                {
                    Match dateMatch = new Regex(@"(\d{4})").Match(date_time[0]);
                    if (dateMatch != null && dateMatch.Success && int.TryParse(dateMatch.Value, out year))
                    {
                        int yIndex = dateMatch.Index;
                        string[] md = date_time[0].Replace(dateMatch.Value, "").Trim(WSConstants.DATE_TRIM_CHARS).Split(WSConstants.DATE_TRIM_CHARS, StringSplitOptions.RemoveEmptyEntries);
                        if (md.Length == 2)
                        {
                            int d1 = 0;
                            int d2 = 0;
                            if (int.TryParse(md[0], out d1) && int.TryParse(md[1], out d2))
                            {
                                if (yIndex == 0 || d2 > 12)
                                {
                                    mth = d1;
                                    day = d2;
                                }
                                else
                                {
                                    mth = d2;
                                    day = d1;
                                }
                                if (date_time.Length > 1)
                                {
                                    string[] time = date_time[1].Split(WSConstants.DATE_TRIM_CHARS, StringSplitOptions.RemoveEmptyEntries);
                                    if (time.Length > 0) { hour = int.Parse(time[0]); }
                                    if (time.Length > 1) { min = int.Parse(time[1]); }
                                    if (time.Length > 2) { sec = int.Parse(time[2]); }
                                    if (time.Length > 3) { ms = int.Parse(time[3]); }
                                }
                                d = new DateTime(year,mth,day,hour,min,sec,ms);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            return false;
        }

        //TODO:ANDVO@2015-10-05 :  TEST REQUIRED !!!
        public bool ToTime(string val, out TimeSpan? d)
        {
            d = null;
            try
            {
                if (val.IsTrue() || val.IsFalse())
                {
                    d = val.IsTrue() ? (TimeSpan?)new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second) : null;
                    return true;
                }
                else
                {
                    TimeSpan d2 = TimeSpan.MinValue;
                    if (ToTime(val, out d2)) { d = d2; return true; }
                }
            }
            catch (Exception) { }
            return false;
        }

        internal bool ToTime(string val, out TimeSpan time)
        {
            time = TimeSpan.MinValue;
            try
            {
                Match match = new Regex(WSConstants.TIMESPAN_REGEX_PATTERN).Match(val);
                if (match != null && match.Success)
                {
                    string _day = match.Groups[1].Value;
                    int day = int.TryParse(_day, out day) ? day : 0;
                    string _hour = match.Groups[3].Value;
                    int hour = int.TryParse(_hour, out hour) ? hour : 0;
                    string _min = match.Groups[5].Value;
                    int min = int.TryParse(_min, out min) ? min : 0;
                    string _sec = match.Groups[7].Value;
                    int sec = int.TryParse(_sec, out sec) ? sec : 0;
                    string _ms = match.Groups[9].Value;
                    int ms = int.TryParse(_ms, out ms) ? ms : 0;

                    time = new TimeSpan(day, hour, min, sec, ms);
                    return true;
                }
                else
                {
                    match = new Regex(WSConstants.TIMESPAN_REGEX_PATTERN_SIMPLE).Match(val);
                    string _hour = match.Groups[1].Value;
                    int hour = int.TryParse(_hour, out hour) ? hour : 0;
                    string _min = match.Groups[3].Value;
                    int min = int.TryParse(_min, out min) ? min : 0;
                    string _sec = match.Groups[5].Value;
                    int sec = int.TryParse(_sec, out sec) ? sec : 0;

                    time = new TimeSpan(0, hour, min, sec, 0);
                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }
    }
}
