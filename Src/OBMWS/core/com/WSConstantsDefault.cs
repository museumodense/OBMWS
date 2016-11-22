using System;
using System.Collections.Generic;
using System.Web;

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
    public class WSConstantsDefault : WSClientMeta
    {
        public WSConstantsDefault(HttpContext context) : base(context) { }
        protected override void InitServer() { SecurityMap = new Dictionary<string, WSSecurityMeta>(); }

        public override string SrcBinaryKey { get { return ""; } }

        public override void InnerDispose() { }
        
        public override void loadStaticSources() { }
        public override WSEmail CreateEmail(string _Subject, WSEmailLines _Lines, string _ToAddress, string _FromAddress = null)
        {
            return new IEmail(_Subject, _Lines, _ToAddress, _FromAddress);
        }
        public override void RegError(Type caller, Exception e, ref WSStatus status, string errorMsg = null)
        {
            try
            {

            }
            catch (Exception) { }
        }
    }
    public class IEmail : WSEmail
    {
        public IEmail(string _Subject, WSEmailLines _Lines, string _ToAddress, string _FromAddress = null) : base(_Subject, _Lines, _ToAddress, _FromAddress) { }
        public override WSInstitutionMeta Institution { get { return new IInstitutionMeta(); } }
    }
    public class IInstitutionMeta : WSInstitutionMeta
    {
        public override string Phone { get { return "+45 65 51 46 01"; } }
        public override string Fax { get { return "+45 65 90 86 00"; } }
        public override string Email { get { return "museum@odense.dk"; } }
        public override string Title { get { return "DWS Service"; } }
        public override Uri Url { get { return new Uri("http://museum.odense.dk/"); } }
        public override Uri IconUrl { get { return new Uri("http://museum.odense.dk/media/11472973/odense_logo_small.png"); } }
        public override WSAddress Address { get { return _Address; } }
        private WSAddress _Address = new WSAddress() { City = "Odense C", StreetAddress = "Overgade 48", ZIP = "DK-5000" };
    }
}