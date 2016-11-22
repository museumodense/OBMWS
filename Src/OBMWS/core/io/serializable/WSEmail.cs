using System;
using System.Collections.Generic;
using System.Text;

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
    public abstract class WSEmail
    {
        public string Subject = null;
        public WSEmailLines Lines = new WSEmailLines();
        public string ToAddress = null;
        public WSEmail() { }
        public WSEmail(string _Subject, WSEmailLines _Lines, string _ToAddress, string _FromAddress = null)
        {
            Subject = _Subject;
            Lines = _Lines;
            ToAddress = _ToAddress;
            FromAddress_ = _FromAddress;
        }
        public string FromAddress { get { return string.IsNullOrEmpty(FromAddress_) ? FromAddress_: Institution.Email; } set { FromAddress_ = value; } }
        private string FromAddress_ = null;
        public abstract WSInstitutionMeta Institution { get; }
        public string BodyHtml
        {
            get
            {
                if (string.IsNullOrEmpty(_BodyHtml))
                {
                    StringBuilder content = new StringBuilder();
                    content.Append("<div style=\"width:600px; max-width:600px; border:1px solid #ddd;padding:15px;\">" + Environment.NewLine);
                    content.Append("<div class=\"[EMAIL-HEADER]\" style=\"background-color: #6f6d60;height: 38px;\">" + Environment.NewLine);
                    content.Append("<div style=\"margin: 0px auto; padding:6px 0px 0px 6px;\">" + Environment.NewLine);
                    content.Append($"<a href=\"{Institution.Url.ToString()}\" style=\"float: left;\">" + Environment.NewLine);
                    content.Append($"<img src=\"{Institution.IconUrl.ToString()}\" alt=\"{Institution.Title}\" border=\"0\"/>" + Environment.NewLine);
                    content.Append("</a>" + Environment.NewLine);
                    content.Append("<div style=\"float: left; left:20px; top:4px;position:relative;\">" + Environment.NewLine);
                    content.Append("<div style=\"margin:0;\" class=\"wrapper\">" + Environment.NewLine);
                    content.Append("<span class=\"shop-title\" style=\"color:white;font-size: 20px;font-family: serif;font-weight: normal;text-transform: uppercase;\">" + Environment.NewLine);
                    content.Append(Institution.Title + Environment.NewLine);
                    content.Append("</span>" + Environment.NewLine);
                    content.Append("</div>" + Environment.NewLine);
                    content.Append("</div>" + Environment.NewLine);
                    content.Append("</div>" + Environment.NewLine);
                    content.Append("</div>" + Environment.NewLine);
                    content.Append("<div class=\"[EMAIL-SUBJECT]\">" + Environment.NewLine);
                    content.Append("<h3 style=\"white-space:nowrap;\">" + Environment.NewLine);
                    content.Append(Subject + Environment.NewLine);
                    content.Append("</h3>" + Environment.NewLine);
                    content.Append("</div>" + Environment.NewLine);
                    content.Append("<div class=\"[EMAIL-BODY]\" style=\"border-top:1px solid #ddd;border-bottom:1px solid #ddd;font-family: Helvetica Neue, Helvetica, Arial, sans-serif;font-size: 12px;\">" + Environment.NewLine);
                    foreach (WSEmailLine line in Lines)
                    {
                        content.Append($"<div class=\"[{line.Key}]\">{line.Value}</div>" + Environment.NewLine);
                    }
                    content.Append("</div>" + Environment.NewLine);
                    content.Append("<div class=\"[EMAIL-FOOTER]\" style=\"font-size: 10px; margin-top:30px;\">" + Environment.NewLine);
                    content.Append($"<div>{Institution.Title}</div>{Environment.NewLine}");
                    content.Append(string.IsNullOrEmpty(Institution.Address.StreetAddress) ? string.Empty : $"<div>{Institution.Address.StreetAddress}</div>{Environment.NewLine}");
                    content.Append($"<div>{Institution.Address.ZIP} {Institution.Address.City}</div>{Environment.NewLine}");
                    content.Append("<br />" + Environment.NewLine);
                    content.Append(string.IsNullOrEmpty(Institution.Phone) ? string.Empty : $"<div>Tlf. {Institution.Phone}</div>{Environment.NewLine}");
                    content.Append(string.IsNullOrEmpty(Institution.Fax) ? string.Empty : $"<div>Fax {Institution.Fax}</div>{Environment.NewLine}");
                    content.Append(string.IsNullOrEmpty(FromAddress) ? string.Empty : $"<div><a href=\"mailto:{FromAddress}\">{FromAddress}</a></div>{Environment.NewLine}");
                    content.Append("</div>" + Environment.NewLine);
                    content.Append("</div>");
                    _BodyHtml = content.ToString();
                }
                return _BodyHtml;
            }
        }
        public string _BodyHtml = string.Empty;
        public bool isVlaid { get { return IsVlaid(this); } }
        public bool IsVlaid(WSEmail email)
        {
            try
            {
                if (email == null) { return false; }
                else if (string.IsNullOrEmpty(email.FromAddress)) { return false; }
                else if (string.IsNullOrEmpty(email.ToAddress)) { return false; }
                else if (string.IsNullOrEmpty(email.Subject)) { return false; }
                else if (email.Lines == null || email.Lines.Count == 0) { return false; }
                else return true;
            }
            catch (Exception) { }
            return false;
        }
        public static object IsVlaidLock = new object();
    }
    public class WSEmailLines : List<WSEmailLine> { }
    public class WSEmailLine
    {
        public string Key = string.Empty;
        public string Value = string.Empty;
    }
    public class WSAddress
    {
        public string StreetAddress = string.Empty;
        public string ZIP = string.Empty;
        public string City = string.Empty;
    }
    public abstract class WSInstitutionMeta
    {
        public abstract string Title { get; }
        public abstract Uri Url { get; }
        public abstract Uri IconUrl { get; }
        public abstract string Email { get; }
        public abstract string Phone { get; }
        public abstract string Fax { get; }
        public abstract WSAddress Address { get; }
        public bool isVlaid
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(Title)) { return false; }
                    else if (Url == null || string.IsNullOrEmpty(Url.ToString())) { return false; }
                    else if (IconUrl == null || string.IsNullOrEmpty(IconUrl.ToString())) { return false; }
                    else return true;
                }
                catch (Exception) { }
                return false;
            }
        }
        public static object IsVlaidLock = new object();
    }
}
