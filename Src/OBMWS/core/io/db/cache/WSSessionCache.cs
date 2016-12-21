using System;
using System.Collections.Generic;
using System.Linq;

#region license
//	GNU General Public License (GNU GPLv3)

//	Copyright © 2016 Odense Bys Museer

//	Author: Andriy Volkov

//	Source URL:	https://github.com/odensebysmuseer/OBMWS

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
    internal class WSSessionCache
    {
        internal WSSessionCache(string _SessionID) { SessionID = _SessionID; }
        internal string SessionID { get; private set; } = "undefined";
        internal List<WSDCItem> Items { get; private set; } = new List<WSDCItem>();
        
        public override string ToString() { return $"{{{SessionID}:[{(Items.Any() ? Items.Select(x => x.ToString()).Aggregate((a, b) => a + "," + b) : "")}]}}"; }

        internal void Flush()
        {
            if (Items.Any()) { foreach (WSDCItem dc in Items) { if (!dc.Context.IsDisposed) { dc.Context.Dispose(); } } }
            Items = new List<WSDCItem>();
        }
        internal void CleanUp()
        {
            foreach (WSDCItem item in Items) { if ((DateTime.Now.Ticks - item.Created.Ticks) > 600000) { item.Context.Dispose(); } }//dispose all connections older than 1 minute
            Items = Items == null ? new List<WSDCItem>() : Items.Where(x => !x.Context.IsDisposed).ToList();//remove disposed connections
        }

        internal bool GetContext(Type _ContextType, WSRequestID _RequestID, out WSDataContext _Context)
        {
            _Context = null;
            try
            {
                WSDCState State = _ContextType.GetState(Items);
                Func<WSDCItem, bool> iFunc = (x =>
                    x.Context != null
                    && !x.Context.IsDisposed
                    && x.Context.GetType() == _ContextType
                    && (State == WSDCState.Closed || State == WSDCState.Open)
                );

                WSDCItem DCItem = Items.Any(iFunc) ? Items.FirstOrDefault(iFunc) : CreateIfNotValid(_ContextType, _RequestID, State, iFunc);
                
                WSDCItem.Open(DCItem);

                _Context = DCItem == null ? null : DCItem.Context;

                return _Context != null;
            }
            catch (Exception) { return false; }
        }
        private WSDCItem CreateIfNotValid(Type _ContextType, WSRequestID _RequestID, WSDCState State, Func<WSDCItem, bool> iFunc)
        {
            WSDCItem DCItem = null;
            switch (State)
            {
                case WSDCState.Broken://RESET current connection
                    DCItem = new WSDCItem((WSDataContext)Activator.CreateInstance(_ContextType), _RequestID);
                    Items = Items.Select(x => x.Context.GetType().Equals(_ContextType) ? DCItem : x).ToList();//).ToList();
                    break;
                case WSDCState.Disposed://RESET current connection
                    DCItem = new WSDCItem((WSDataContext)Activator.CreateInstance(_ContextType), _RequestID);
                    Items = Items.Select(x => x.Context.GetType().Equals(_ContextType) ? DCItem : x).ToList();//).ToList();
                    break;

                case WSDCState.NotExists://ADD new connection
                    DCItem = new WSDCItem((WSDataContext)Activator.CreateInstance(_ContextType), _RequestID);
                    Items.Add(DCItem);
                    break;
                case WSDCState.Connecting://ADD new connection
                    DCItem = new WSDCItem((WSDataContext)Activator.CreateInstance(_ContextType), _RequestID);
                    Items.Add(DCItem);
                    break;
                case WSDCState.Executing://ADD new connection
                    DCItem = new WSDCItem((WSDataContext)Activator.CreateInstance(_ContextType), _RequestID);
                    Items.Add(DCItem);
                    break;
                case WSDCState.Fetching://ADD new connection
                    DCItem = new WSDCItem((WSDataContext)Activator.CreateInstance(_ContextType), _RequestID);
                    Items.Add(DCItem);
                    break;

                case WSDCState.Closed://SKIP : may not update open connection!!!
                    DCItem = Items.FirstOrDefault(iFunc);
                    break;
                case WSDCState.Open://SKIP : may not update open connection!!!DCItem.Context.Dispose();
                    DCItem = Items.FirstOrDefault(iFunc);
                    break;
                default://SKIP : may not update open connection!!!DCItem.Context.Dispose();
                    DCItem = Items.FirstOrDefault(iFunc);
                    break;
            }
            return DCItem;
        }

        internal static bool IsValid(WSSessionCache sessionCache)
        {
            return sessionCache != null && sessionCache.Items != null;
        }
    }
}
