using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    internal class WSDCItem
    {
        internal WSDataContext Context = null;
        internal readonly DateTime Created = DateTime.Now;
        internal DateTime LastModified = DateTime.Now;

        public WSDCItem(WSDataContext _Context, WSRequestID _ID)
        {
            Context = _Context;
            ID = _ID;
        }

        internal WSRequestID ID { get; private set; }

        public override string ToString() { return $"{{{(Context.IsDisposed ? "\"disposed\"" : ("{" + Context.Connection.Database + ":" + Context.Connection.State.ToString() + "}"))}}}"; }

        internal void Dispose() { if (Context != null) Context.Dispose(); Context = null; ID = null; }

        internal static void Dispose(WSDCItem DCItem) { if (DCItem != null) { DCItem.Dispose(); DCItem.LastModified = DateTime.Now; } }

        internal void Open() { if (Context != null && Context.Connection.State == System.Data.ConnectionState.Closed) { Context.Connection.Open(); } }

        internal static void Open(WSDCItem DCItem) { DCItem.Open(); DCItem.LastModified = DateTime.Now; }
    }
}
