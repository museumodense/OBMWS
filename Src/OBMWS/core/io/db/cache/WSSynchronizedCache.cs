using OBMWS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
    public class WSSynchronizedCache
    {
        private const int DefaultTimeout = 10000;//milliseconds
        private ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        private Dictionary<string, DynamicSourcesCache> innerCache = new Dictionary<string, DynamicSourcesCache>();

        public int Count { get { return innerCache.Count; } }

        private bool TryEnterWriteLock(int timeout) { return cacheLock.TryEnterWriteLock(timeout); }
        private bool TryEnterUpgradeableReadLock(int timeout) { return cacheLock.TryEnterUpgradeableReadLock(timeout); }

        public DynamicSourcesCache Read(string SessionID)
        {
            if (SessionID != null)
            {
                try { if (TryEnterUpgradeableReadLock(DefaultTimeout) && !innerCache.ContainsKey(SessionID)) { Add(SessionID, null); } } finally { cacheLock.ExitUpgradeableReadLock(); }

                cacheLock.EnterReadLock(); try { return innerCache[SessionID]; } finally { cacheLock.ExitReadLock(); }
            }
            return null;
        }

        private bool Add(string SessionID, DynamicSourcesCache value, int? timeout = DefaultTimeout)
        {
            if (SessionID!=null) {
                bool writeAllowed = false;
                try
                {
                    writeAllowed = TryEnterWriteLock((int)timeout);
                    if (writeAllowed)
                    {
                        innerCache.Add(SessionID, value);
                        return true;
                    }
                    else { }
                }
                finally
                {
                    if (writeAllowed) cacheLock.ExitWriteLock();
                }
            }
            return false;
        }

        private bool Update(string SessionID, DynamicSourcesCache value, int? timeout = DefaultTimeout)
        {
            if (SessionID != null) { try { if (TryEnterWriteLock((int)timeout)) { innerCache[SessionID] = value; return true; } } finally { cacheLock.ExitWriteLock(); } }
            return false;
        }

        public SaveStatus Save(string SessionID, DynamicSourcesCache value, int? timeout = DefaultTimeout)
        {
            SaveStatus status = SaveStatus.Undefined;
            try
            {
                if (TryEnterUpgradeableReadLock((int)timeout))
                {
                    DynamicSourcesCache result = null;
                    if (innerCache.TryGetValue(SessionID, out result))
                    {
                        if (result == value) status = SaveStatus.Unchanged;
                        else if (Update(SessionID, value, timeout)) status = SaveStatus.Updated;
                    }
                    else
                    {
                        if (Add(SessionID, value, timeout)) status = SaveStatus.Added;
                    }
                }
            }
            finally { cacheLock.ExitUpgradeableReadLock(); }
            return status;
        }
        internal bool ContainsKey(string sessionID)//check if cache key is present
        {
            cacheLock.EnterReadLock();
            try { return innerCache.ContainsKey(sessionID); } finally { cacheLock.ExitReadLock(); }
        }
        internal bool Clear(string key = null, int? timeout = DefaultTimeout)//removes cache by specific 'session key' or clear all cache if 'key' is null
        {
            try
            {
                if (timeout != null) { if (TryEnterWriteLock((int)timeout)) { return ClearInternal(key); } }
                else
                {
                    cacheLock.EnterWriteLock();
                    return ClearInternal(key);
                }
                RefreshInternal();
            }
            finally { cacheLock.ExitWriteLock(); }
            return false;
        }
        
        private bool ClearInternal(string key = null)//removes cache by specific 'session key' or clear all cache if 'key' is null
        {
            List<bool> statuses = new List<bool>();
            if (key == null) {
                List<string> keys = innerCache.Keys.ToList();

                foreach (string sid in keys) { statuses.Add(DeleteInternal(sid)); }
            }
            else { statuses.Add(DeleteInternal(key)); }

            return statuses.Any(x => !x);
        }
        private bool RefreshInternal()//removes all cache older then 1 hour
        {
            List<bool> statuses = new List<bool>();          
            IEnumerable<string> oldSIDs =
                innerCache.Any(c => c.Value.timestapt < DateTime.Now.AddHours(-1)) ?
                innerCache.Where(c => c.Value.timestapt < DateTime.Now.AddHours(-1)).Select(c => c.Key) :
                new List<string>();
            foreach (string sid in oldSIDs)
            {
                statuses.Add(DeleteInternal(sid));
            }
            return statuses.Any(x => !x);            
        }
        private bool DeleteInternal(string SessionID) {
            try {
                if (SessionID != null) {
                    DynamicSourcesCache result = null;
                    if (!innerCache.TryGetValue(SessionID, out result) || result == null) { return true; }
                    else if (SessionID != null) { innerCache.Remove(SessionID); return true; }
                }
                else { return true; }
            }
            catch (Exception e) { }
            return false;
        }
        ~WSSynchronizedCache() { if (cacheLock != null) cacheLock.Dispose(); }
        public enum SaveStatus { Added, Updated, Unchanged, Undefined };
    }
}