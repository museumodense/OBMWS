using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;

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
    public class WSDataContext : System.Data.Linq.DataContext
    {
        public readonly DateTime Created = DateTime.Now;
        public bool IsDisposed { get; private set; }
        
        public WSDataContext(string ConnectionString, MappingSource mappingSource) : base(ConnectionString, mappingSource) { }
        public WSDataContext(System.Data.IDbConnection connection, MappingSource mappingSource) : base(connection, mappingSource) { }

        public IEnumerable<Type> ETypes
        {
            get
            {
                if (_ETypes == null||_ETypes.FirstOrDefault()==null)
                {
                    try
                    {
                        Type EType = typeof(WSDynamicEntity);
                        string DCNamespace = GetType().Namespace;
                        _ETypes = GetType().Assembly.GetTypes().Where(p => DCNamespace.Equals(p.Namespace) && p.IsSameOrSubclassOf(EType));
                    }
                    catch (Exception) { _ETypes = new Type[] { }; }
                }
                return _ETypes;
            }
        }

        public string Caller { get; internal set; }

        public IEnumerable<Type> _ETypes = null;
        internal readonly Guid ID = Guid.NewGuid();

        public void Close()
        {
            base.Connection.Close();
        }
        public new void Dispose()
        {
            DisposeLocal();
        }
        protected override void Dispose(bool disposing)
        {
            DisposeLocal(disposing);
        }
        private void DisposeLocal(bool? disposing = null)
        {
            if (disposing == null) { base.Dispose(); }
            else { base.Dispose((bool)disposing); }
            IsDisposed = true;
        }

        [Function(Name = "GetDate", IsComposable = true)]
        public DateTime GetSystemDate()
        {
            MethodInfo mi = MethodBase.GetCurrentMethod() as MethodInfo;
            return (DateTime)this.ExecuteMethodCall(this, mi, new object[] { }).ReturnValue;
        }
        public override string ToString() { return $"{{{GetType().Name}:{(IsDisposed ? "Disposed" : Connection.State.ToString())}{(string.IsNullOrEmpty(Caller) ? "" : (":"+ Caller))}}}"; }
    }
}