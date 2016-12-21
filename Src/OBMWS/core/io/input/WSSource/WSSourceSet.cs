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
    public class WSSourceSet : Dictionary<string, WSSources<WSSource>>
    {
        public WSSourceSet(WSSources<WSSource> sources, string _CollectionName)
        {
            foreach (WSSource src in sources)
            {
                if (!this.Any(x => x.Key.Equals(_CollectionName))) { this.Add(_CollectionName, new WSSources<WSSource>()); }
                if (this.Any(x => x.Key.Equals(_CollectionName) && !x.Value.Any(v => v.NAME.Equals(src.NAME))))
                {
                    this[_CollectionName].Add(src);
                }
            }
        }
        public WSSourceSet(WSSources<WSTableSource> sources)
        {
            foreach (WSTableSource src in sources)
            {
                if (!this.Any(x => x.Key.Equals(src.DBName))) { this.Add(src.DBName, new WSSources<WSSource>()); }
                if (this.Any(x => x.Key.Equals(src.DBName) && !x.Value.Any(v => v.NAME.Equals(src.NAME))))
                {
                    this[src.DBName].Add(src);
                }
            }
        }
    }
}
