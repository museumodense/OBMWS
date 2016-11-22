using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    internal static class WSLog
    {
        private static LogState state = LogState.Ready;
        private static Queue<WSLogRecord> queue = new Queue<WSLogRecord>();
        internal static void Log(WSLogRecord log)
        {
            try
            {
                HttpContext context = HttpContext.Current;
                if (log != null && log.Any() && context != null)
                {
                    queue.Enqueue(log);
                    WriteLog(context);
                }
            }
            catch (Exception) { }
        }
        private static void WriteLog(HttpContext context)
        {
            if (state == LogState.Ready)
            {
                try
                {
                    state = LogState.InProgress;
                    while (queue.Count > 0)
                    {
                        try
                        {
                            WSLogRecord log = queue.Dequeue();
                            DirectoryInfo dirTo = new DirectoryInfo(WSServerMeta.MapPath(WSConstants.LINKS.LogPath));
                            if (!Directory.Exists(dirTo.FullName)) { Directory.CreateDirectory(dirTo.FullName); }
                            if (Directory.Exists(dirTo.FullName))
                            {
                                FileInfo logFile = new FileInfo($"{dirTo}\\{(log.IsError ? "error_" : "")}{ DateTime.Now.ToString("yyyy_MM_dd")}.log");
                                if (logFile.Exists) { logFile.IsReadOnly = false; }
                                using (TextWriter writer = new StreamWriter(logFile.FullName, logFile.Exists))
                                {
                                    writer.WriteLine($"");
                                    writer.WriteLine($"___________{log.date.ToString(WSConstants.DATE_FORMAT)} => New log created [{log.Name}] by [" + (context.Request != null ? context.Request.UserHostAddress : "0.0.0.0") + "]:____________");
                                    foreach (string line in log)
                                    {
                                        writer.WriteLine("\t-\t" + line);
                                    }
                                    writer.WriteLine($"____________log end._________________________________________________________________");
                                    writer.WriteLine($"");
                                }
                                logFile = new FileInfo(logFile.FullName);
                                if (logFile.Exists)
                                {
                                    logFile.IsReadOnly = true;
                                }
                            }
                        } finally { }
                    }
                }
                finally { state = LogState.Ready; }
            }
        }
    }
    internal enum LogState : sbyte { InProgress = 1, Ready = 0 }
    internal class WSLogRecord : List<string>
    {
        internal WSLogRecord(string _Name, bool _IsError=false) { Name = _Name; IsError = _IsError; }

        internal string Name = string.Empty;
        internal bool IsError = false;
        internal DateTime date = DateTime.Now;
        internal void Save() { WSLog.Log(this); }
    }
}