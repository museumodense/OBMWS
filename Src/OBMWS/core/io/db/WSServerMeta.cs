using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq.Mapping;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

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
    public abstract class WSServerMeta
    {
        public WSServerMeta()
        {
            //ObjectState appPoolState = Recycle();
            //if (appPoolState == ObjectState.Started)
            //{
                InitServer();
            //}
            //else { LoadStatusStatic.AddNote(new WSStatusNote($"Server state :[{appPoolState}]")); }
        }
        
        internal static WSSynchronizedCache Cache = new WSSynchronizedCache();
        protected static Dictionary<string, WSSecurityMeta> SecurityMap = null;
        protected abstract void InitServer();
        public abstract bool ValidateOneMinTicket(string _1MinTicket);

        public abstract WSEmail CreateEmail(string _Subject, WSEmailLines _Lines, string _ToAddress, string _FromAddress = null);

        #region STATIC AREA
        public static WSStatus LoadStatusStatic = WSStatus.NONE_Copy();

        protected static int next_code { get { return _next_code++; } }
        private static int _next_code = 0;

        #region EntityTypes
        private static List<Type> EntityTypes
        {
            get
            {
                if (_EntityTypes == null || !_EntityTypes.Any())
                {
                    try
                    {
                        _EntityTypes = new List<Type>();

                        string fullPath = MapPath(WSConstants.LINKS.BinPath);
                        DirectoryInfo binDir = new DirectoryInfo(fullPath);

                        var ext = new List<string> { ".dll" };
                        IEnumerable<Assembly> assemblies = Directory.GetFiles(binDir.FullName, "*.*", SearchOption.AllDirectories).Where(s => ext.Any(e => s.EndsWith(e))).Select(x => Assembly.LoadFrom(x));
                        //assemblies = assemblies.Where(ba => AppDomain.CurrentDomain.GetAssemblies().Any(ra => ra.GetType() == ba.GetType()));
                        //assemblies = assemblies.Where(ba => Assembly.GetExecutingAssembly().GetReferencedAssemblies().Any(ra => ra.GetType() == ba.GetType()));

                        LoadStatusStatic.AddNote("Assemblies loaded:[" + (assemblies.Any() ? assemblies.Count() : 0) + "]");

                        foreach (Assembly assembly in assemblies)
                        {
                            try
                            {
                                Type[] ATypes = assembly.GetTypes();
                                IEnumerable<Type> AssemblyTypes = ATypes.Where(t => t.IsValidDynamicEntity());

                                LoadStatusStatic.AddNote($"Assembly[{assembly.GetName().Name}] Types loaded:[" + (AssemblyTypes.Any() ? AssemblyTypes.Count() : 0) + "]");

                                if (AssemblyTypes.Any())
                                {
                                    AssemblyTypes = AssemblyTypes.Where(at => !_EntityTypes.Any(et => et.FullName.Equals(at.FullName)));
                                    LoadStatusStatic.AddNote($"Assembly[{assembly.GetName().Name}] Types filtered:[" + (AssemblyTypes.Any() ? AssemblyTypes.Count() : 0) + "]");

                                    if (AssemblyTypes.Any())
                                    {
                                        _EntityTypes.AddRange(AssemblyTypes);
                                    }
                                }
                            }
                            catch (Exception) { }
                        }
                        _EntityTypes = _EntityTypes.Any() ? _EntityTypes.Distinct().ToList() : _EntityTypes;
                        LoadStatusStatic.AddNote("Entity types loaded:[" + (_EntityTypes.Any() ? _EntityTypes.Select(x => x.Namespace + "." + x.Name).Aggregate((a, b) => a + "," + b) : "") + "]");
                    }
                    catch (Exception) { }
                }
                return _EntityTypes;
            }
        }
        private static List<Type> _EntityTypes = null;
        #endregion

        #region CoreSources
        private static DirectoryInfo CORE_SOURCES_CONFIG_DIR
        {
            get
            {
                string path = MapPath(WSConstants.LINKS.CoreSchemaPath);
                DirectoryInfo dir = new DirectoryInfo(path);
                if (!dir.Exists) dir.Create();
                return dir.Exists ? dir : null;
            }
        }
        private static bool CoreEmpty { get { return _CoreSources == null || !_CoreSources.Any(); } }
        private static bool isValidConfigDir { get { return CORE_SOURCES_CONFIG_DIR != null && Directory.Exists(CORE_SOURCES_CONFIG_DIR.FullName); } }

        private static Dictionary<string, long> _ReloadXMLSet = new Dictionary<string, long>();
        private static Dictionary<string, long> ReloadXMLSet
        {
            get
            {
                if (isValidConfigDir)
                {
                    Dictionary<string, long> temp_ReloadXMLSet = new Dictionary<string, long>();
                    foreach (FileInfo file in CORE_SOURCES_CONFIG_DIR.GetFiles().Where(x => x.Extension.Equals(".config")))
                    {
                        string fileName = file.Name.ToLower();
                        temp_ReloadXMLSet.Add(fileName, (_ReloadXMLSet == null || !_ReloadXMLSet.ContainsKey(fileName)) ? file.LastWriteTime.Ticks : _ReloadXMLSet[fileName]);
                    }
                    _ReloadXMLSet = temp_ReloadXMLSet;
                }
                return _ReloadXMLSet;
            }
        }
        private static bool ReloadXMLRequired
        {
            get
            {
                foreach (FileInfo file in CORE_SOURCES_CONFIG_DIR.GetFiles().Where(x => x.Extension.Equals(".config")))
                {
                    string fileName = file.Name.ToLower();
                    if (!_ReloadXMLSet.ContainsKey(fileName) || file.LastWriteTime.Ticks != ReloadXMLSet[fileName])
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        internal static bool ReloadRAMRequired
        {
            get
            {
                return CoreEmpty || ReloadXMLRequired;
            }
        }
        internal static WSDBSet CoreSources
        {
            get
            {
                if (ReloadRAMRequired)
                {
                    ClearCache();

                    RoleSet = null;
                    _CoreSources = new WSDBSet(WSConstants.ACCESS_LEVEL.LOCK); 
                    try
                    {
                        WSSources<WSTableSource> temp_sources = LoadMetaSources(); 

                        LoadStatusStatic.AddNote("init_sources:" + (temp_sources != null ? temp_sources.Count : 0) + "");

                        if (isValidConfigDir)
                        {
                            List<string> dbs = temp_sources.Select(x => x.DBName).Distinct().ToList();
                            if (dbs != null && dbs.Any())
                            {
                                foreach (string db in dbs)
                                {
                                    if (string.IsNullOrEmpty(db)) { LoadStatusStatic.AddNote($"DB ['{db}'] failed to read", WSConstants.ACCESS_LEVEL.ADMIN, WSStatus.ERROR.CODE); }
                                    else
                                    {
                                        LoadStatusStatic.AddNote($"DB ['{db}'] Sources start loading...");

                                        WSSources<WSTableSource> sources = new WSSources<WSTableSource>(temp_sources.Where(x => x.DBName.Equals(db)));

                                        FileInfo CORE_SOURCES_CONFIG = new FileInfo(CORE_SOURCES_CONFIG_DIR.FullName + "\\" + db + ".config");

                                        string fileName = CORE_SOURCES_CONFIG.Name.ToLower();

                                        if (File.Exists(CORE_SOURCES_CONFIG.FullName))
                                        {
                                            bool _ForceReload = false;
                                            if (sources.Configure(CORE_SOURCES_CONFIG, out _ForceReload))
                                            {
                                                _CoreSources.Add(db, sources);
                                            }
                                            else { LoadStatusStatic.AddNote($"DB ['{db}'] failed to configure", WSConstants.ACCESS_LEVEL.ADMIN, WSStatus.ERROR.CODE); }
                                            
                                            ReloadXMLSet[fileName] = _ForceReload ? 0 : ReloadXMLSet[fileName];
                                        }
                                        else { ReloadXMLSet[fileName] = 0; }

                                        LoadStatusStatic.AddNote($"ReloadXMLSet[{fileName}] exists : {(ReloadXMLSet.ContainsKey(fileName))}");
                                        
                                        if (ReloadXMLSet.ContainsKey(fileName) && CORE_SOURCES_CONFIG.LastWriteTime.Ticks != ReloadXMLSet[fileName])
                                        {
                                            resetSchema(CORE_SOURCES_CONFIG, sources);
                                        }

                                        LoadStatusStatic.AddNote($"DB ['{db}'] Sources:[" + (_CoreSources != null && _CoreSources[db].Any() ? _CoreSources[db].Select(x => x.NAME).Aggregate((a, b) => a + "," + b) : "") + "]");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e) { LoadStatusStatic.AddNote($"_CoreSources.Error: ['{e.Message}']"); _CoreSources = null; }

                    if (WSServerCache.IsValid(DBCache)) DBCache.Flush();
                }
                return _CoreSources;
            }
        }
        private static WSDBSet _CoreSources = null;
        internal static bool reloadCoreSources() { _EntityTypes = null; _CoreSources = null; _RoleSet = null; LoadStatusStatic = WSStatus.NONE_Copy(); return RoleSet != null && RoleSet.Count > 0; }
        private static WSSources<WSTableSource> LoadMetaSources()
        {
            LoadStatusStatic.AddNote($"IOSB.LoadMetaSources()");
            WSSources<WSTableSource> ORG_SOURCES = new WSSources<WSTableSource>();
            Dictionary<Type, WSDataContext> dbList = new Dictionary<Type, WSDataContext>();
            try
            {
                if (EntityTypes != null && EntityTypes.Any())
                {
                    IEnumerable<string> namespaces_ = EntityTypes.Select(t => t.Namespace).Distinct();
                    Dictionary<string, List<Type>> nsList = namespaces_.ToDictionary(key => key, val => new List<Type>());

                    foreach (Type type in EntityTypes)
                    {
                        nsList[type.Namespace].Add(type);
                    }
                    foreach (KeyValuePair<string, List<Type>> ns in nsList)
                    {
                        if (ns.Value.Any())
                        {
                            foreach (Type type in ns.Value)
                            {
                                try
                                {
                                    Type DCType = GetDCTypeByEntityType(type); 
                                    WSDataContext db = null;
                                    if (dbList.Any(x => x.Key == DCType)) { db = dbList.FirstOrDefault(x => x.Key == DCType).Value; }
                                    else
                                    {
                                        db = GetServerContext(DCType, null, $"{typeof(WSServerMeta).Name}.LoadMetaSources() => [{type.FullName}:{DCType.Name}");
                                        dbList.Add(DCType, db);
                                    }
                                    
                                    if (db != null)
                                    {
                                        string DBName = db.GetType().CustomAttribute<DatabaseAttribute>(true).Name;
                                        WSTableSource tSrc = new WSTableSource(
                                            type,
                                            SecurityMap.FirstOrDefault(m => m.Key.Equals(DBName)).Value.Zone,
                                            type.Name,
                                            ServerFunctions,
                                            WSConstants.ACCESS_LEVEL.READ
                                        );

                                        #region READ PROPERTIES
                                        List<MetaDataMember> eProps = db.ReadProperties(type, ref LoadStatusStatic);
                                        if (eProps != null && eProps.Any())
                                        {
                                            List<WSTableParam> _params = new List<WSTableParam>();
                                            foreach (MetaDataMember prop in eProps)
                                            {
                                                try
                                                {
                                                    WSTableParam tParam = new WSTableParam(type, next_code, prop.Name, new WSColumnRef(prop.Name), prop.Type, ServerFunctions);

                                                    object[] CustomAttributes = prop.Member.GetCustomAttributes(true);

                                                    IEnumerable<AssociationAttribute> assAttributes = CustomAttributes.OfType<AssociationAttribute>();
                                                    if (assAttributes != null && assAttributes.Any())
                                                    {
                                                        tParam.IsAssociation = true;
                                                    }

                                                    IEnumerable<ColumnAttribute> cAttributes = CustomAttributes.OfType<ColumnAttribute>();
                                                    if (cAttributes != null && cAttributes.Any())
                                                    {
                                                        tParam.IsColumn = true;
                                                        if (cAttributes.FirstOrDefault().IsPrimaryKey)
                                                        {
                                                            tParam.WRITE_ACCESS_MODE = new WSAccessMode(WSConstants.ACCESS_LEVEL.LOCK, false);
                                                            if (!tSrc.Params.Any(p => p.Match(WSConstants.PARAMS.RECORD_ID.NAME)))
                                                            {
                                                                tParam.DISPLAY_NAME = WSConstants.PARAMS.RECORD_ID.NAME;
                                                                if (!tParam.ALIACES.Any(a => a.Equals(WSConstants.PARAMS.RECORD_ID.NAME))) { tParam.ALIACES.Add(WSConstants.PARAMS.RECORD_ID.NAME); }
                                                            }
                                                        }
                                                    }
                                                    _params.Add(tParam);
                                                }
                                                catch (Exception) { }
                                            }
                                            tSrc.AddParams(_params);
                                            tSrc.ClearDublicatedAliaces();
                                        }
                                        #endregion

                                        if (!ORG_SOURCES.Any(x => x.Match(tSrc))) { ORG_SOURCES.Add(tSrc); }
                                    }
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            finally {
                foreach (Type t in dbList.Keys) {
                    try {
                        if (dbList[t] != null) {
                            dbList[t].Dispose();
                        }
                    } catch (Exception e) { }
                }
            }
            return ORG_SOURCES;
        }
        private static bool resetSchema(FileInfo _SRC_CONFIG, WSSources<WSTableSource> _SOURCES)
        {
            WSLogRecord Log = new WSLogRecord("RESET SCHEMA");
            Log.Add($"RESET SCHEMA START");
            Log.Add($"Original schema:[{(_SRC_CONFIG != null ? ("Path:" + _SRC_CONFIG.FullName + ", Exists:" + _SRC_CONFIG.Exists) : "null")}]");
            Log.Add($"WSSources:[{((_SOURCES != null && _SOURCES.Any()) ? _SOURCES.Select(s => s.NAME).Aggregate((a, b) => a + "," + b) : "none")}]");
            bool done = false;
            try
            {
                if (_SOURCES != null && _SRC_CONFIG != null)
                {
                    if (!File.Exists(_SRC_CONFIG.FullName) || saveArchive(_SRC_CONFIG, ref Log))
                    {
                        if (File.Exists(_SRC_CONFIG.FullName))
                        {
                            _SRC_CONFIG.IsReadOnly = false;
                            Log.Add($"Original schema:[{_SRC_CONFIG.FullName}] set 'ReadOnly' attribute to:[{_SRC_CONFIG.IsReadOnly}]");
                        }
                        string orgPath = _SRC_CONFIG.FullName;
                        string tempPath = orgPath + ".temp";
                        using (TextWriter writer = new StreamWriter(tempPath))
                        {
                            new XmlSerializer(typeof(WSSources<WSTableSource>)).Serialize(writer, _SOURCES);

                            Log.Add($"Temp schema:[{tempPath}] created");

                            File.Delete(orgPath);

                            Log.Add($"Original schema:[{orgPath}] deleted");

                            File.Copy(tempPath, orgPath);

                            Log.Add($"Temp schema:[{tempPath}] copied to [{orgPath}]");

                            _SRC_CONFIG = new FileInfo(orgPath);

                            Log.Add($"Original schema {(_SRC_CONFIG.Exists ? "recreated" : "FAILED recreate")}");
                        }

                        if (_SRC_CONFIG != null && File.Exists(_SRC_CONFIG.FullName))
                        {
                            File.Delete(tempPath);
                            Log.Add($"Temp schema {(!File.Exists(tempPath) ? "deleted" : "FAILED to delete")}");
                            done = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Add($"\n-----------------------Exception ------------------------");
                Log.Add($"Message:\n{e.Message}");
                Log.Add($"StackTrace:\n{e.StackTrace}");
                Log.Add($"-----------------------Exception END -------------------\n");
                WSStatus status = WSStatus.NONE.clone();
                LogError(typeof(WSServerMeta), e, ref status);
            }
            ReloadXMLSet[_SRC_CONFIG.Name] = (_SRC_CONFIG != null && File.Exists(_SRC_CONFIG.FullName)) ? _SRC_CONFIG.LastWriteTime.Ticks : ReloadXMLSet[_SRC_CONFIG.Name];
            Log.Add($"Schema 'LastModified' set to : [{ReloadXMLSet[_SRC_CONFIG.Name].ToString(WSConstants.DATE_FORMAT)}]");

            Log.Add($"RESET SCHEMA DONE");
            Log.Save();
            return done;
        }

        protected static void CleanUp()
        {
            if (DBContextPool != null)
            {
                try
                {
                    long _1Sec = 10000;
                    int connPoolTimeout = WSConstants.CONFIG.ConnectionTimeout;
                    for (int i = 0; i < DBContextPool.Count; i++) { if ((DateTime.Now.Ticks - DBContextPool[i].Created.Ticks) > (WSConstants.CONFIG.ConnectionTimeout * _1Sec)) { DBContextPool[i].Dispose(); } }
                    for (int i = 0; i < DBContextPool.Count; i++) { if (DBContextPool[i].IsDisposed) { DBContextPool[i] = null; } }
                    DBContextPool = DBContextPool.Where(x => x != null).ToList();
                }
                catch (Exception) { }
            }
        }

        #endregion

        #region WSDataContext
        private static WSServerCache DBCache = new WSServerCache();

        #region (USED BY CLIENT ONLY)
        internal static WSDataContext GetInternalContext(string dbName, WSRequestID RequestID, string _Caller)
        {
            return GetClientContext(dbName, RequestID, _Caller);
        }
        private static WSDataContext GetClientContext(string dbName, WSRequestID RequestID, string _Caller)
        {
            WSDataContext dc = null;
            if (CoreSources == null) { /*LoadStatus.AddNote("CS is NULL : [{LoadStatusStatic:" + LoadStatusStatic + "}]");*/ }
            else if (!CoreSources.ContainsKey(dbName)) { /*LoadStatus.AddNote("IOSB.GDBC : database '" + dbName + "' not found");*/ }
            else if (CoreSources[dbName].FirstOrDefault() == null) { /*LoadStatus.AddNote("IOSB.GDBC : empty");*/ }
            else { dc = GetServerContext(CoreSources[dbName][0].ReturnType, RequestID, _Caller); }
            return dc;
        }
        #endregion

        #region  (USED BY SERVER)

        public static Type GetDCTypeByDBName(string DBName) { return CoreSources != null && CoreSources.ContainsKey(DBName) && CoreSources[DBName].Any() ? GetDCTypeByEntityType(CoreSources[DBName][0].ReturnType) : null; }
        public static Type GetDCTypeByEntityType(Type _Type) { return _Type.Assembly.GetTypes().Where(t => t.IsClass && t.Namespace == _Type.Namespace && t.BaseType == typeof(WSDataContext)).FirstOrDefault(); }
        internal static WSDataContext GetServerContext(Type _Type, WSRequestID _RequestID, string _Caller)
        {
            WSDataContext DBContext = null;
            try
            {
                _RequestID = _RequestID == null ? new WSRequestID() : _RequestID;

                _Type = _Type.IsSameOrSubclassOf(typeof(WSDynamicEntity)) ? GetDCTypeByEntityType(_Type) : _Type.IsSameOrSubclassOf(typeof(WSDataContext)) ? _Type : null;

                InitConnection(_Type, out DBContext, _Caller);
            }
            catch (Exception e)
            {
                DBContext = null;
                LoadStatusStatic.AddNote("Failed create DataContext for [" + _Type.FullName + "]", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);
            }
            return DBContext;
        }
        internal static List<WSDataContext> DBContextPool { get; private set; } = new List<WSDataContext>();
        private static void InitConnection(Type _Type, out WSDataContext DBContext, string _Caller)
        {
            DBContext = null;
            if (_Type != null)
            {
                CleanUp();

                DBContext = (WSDataContext)Activator.CreateInstance(_Type);
                DBContext.Caller = _Caller;

                if (DBContext != null && DBContext.Connection.State == System.Data.ConnectionState.Closed) { DBContext.Connection.Open(); }

                DBContextPool.Add(DBContext);
            }
        }
        #endregion

        #endregion

        #region RoleSet
        internal static WSRoleSet RoleSet
        {
            get
            {
                lock (_RoleSet_LOCK)
                {
                    if (_RoleSet == null && CoreSources != null)
                    {
                        LoadStatusStatic.AddNote($"Start set RoleSet");

                        _RoleSet = new WSRoleSet(CoreSources, ServerFunctions);

                        _RoleSet = _RoleSet.Load() ? _RoleSet : new WSRoleSet(new WSDBSet(WSConstants.ACCESS_LEVEL.LOCK), ServerFunctions);

                        LoadStatusStatic.AddNote($"DBSET loaded : [{(_RoleSet.Any() ? _RoleSet.Select(x => "{" + x.Key + ":[" + (x.Value.Any() ? x.Value.Select(s => s.Key + "." + s.Value.Count()).Aggregate((s1, s2) => s1 + "," + s2) : "") + "]}").Aggregate((c1, c2) => c1 + "," + c2) : "")}]");
                    }
                    return _RoleSet;
                }
            }
            set { _RoleSet = value; }
        }
        private static WSRoleSet _RoleSet = null;
        private static object _RoleSet_LOCK = new object();
        #endregion

        internal static WSUserSet SYSTEM_SOURCES
        {
            get
            {
                if (_SYSTEM_SOURCES == null)
                {
                    WSLogRecord Log = new WSLogRecord("LOAD DYNAMIC SCHEMA");

                    _SYSTEM_SOURCES = new WSUserSet();

                    foreach (string db in RoleSet.Last().Value.Keys)
                    {
                        _SYSTEM_SOURCES.Add(db, new WSUserDBSet(RoleSet.Last().Value[db]));
                    }

                    Log.Save();
                }
                return _SYSTEM_SOURCES;
            }
        }
        private static WSUserSet _SYSTEM_SOURCES = null;
        internal static void ClearCache(string SessionID = null) { Cache.Clear(SessionID); }
        private static bool saveArchive(FileInfo serverConfigFile, ref WSLogRecord log)
        {
            bool ok = false;
            log.Add($"");
            log.Add($"ARCHIVE SCHEMA START");
            try
            {
                DirectoryInfo dir = serverConfigFile.Directory;
                string extentionLessName = serverConfigFile.Name.Replace(serverConfigFile.Extension, string.Empty);
                string newName = extentionLessName + "_(" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss") + ")" + serverConfigFile.Extension;
                DirectoryInfo dirTo = new DirectoryInfo(dir.FullName + "\\archive\\" + extentionLessName + "\\");
                if (!Directory.Exists(dirTo.FullName))
                {
                    Directory.CreateDirectory(dirTo.FullName);
                    log.Add($"New directory {(Directory.Exists(dirTo.FullName) ? "created" : "FAILED to create")} :[{dirTo.FullName}]");
                }
                if (Directory.Exists(dirTo.FullName))
                {
                    string pathTo = dirTo + "\\" + newName;
                    FileInfo destFile = serverConfigFile.CopyTo(pathTo);
                    log.Add($"Schema {(destFile.Exists ? "saved" : "FAILED to save")} to :[{pathTo}]");
                    ok = destFile.Exists;
                }
            }
            catch (Exception) { }
            log.Add($"ARCHIVE SCHEMA END");
            log.Add($"");
            return ok;
        }
        public static string MapPath(string path = null)
        {
            string root = System.Web.HttpRuntime.AppDomainAppPath;
            root = !string.IsNullOrEmpty(root) && root.EndsWith("\\") ? root.Substring(0, root.LastIndexOf("\\")) : root;
            return root + (string.IsNullOrEmpty(path) ? "" : path.Replace("~", string.Empty).Replace('/', '\\'));
        }
        //public static ObjectState Recycle()
        //{
        //    using (ServerManager iisManager = new ServerManager())
        //    {
        //        Site site = iisManager.Sites.FirstOrDefault(s => s.Name == HostingEnvironment.ApplicationHost.GetSiteName());
        //        string AppPoolName = site.Applications["/"].ApplicationPoolName;
        //        ApplicationPool appPool = iisManager.ApplicationPools[AppPoolName];


        //        //PropertyBag applicationPoolBag = new PropertyBag();
        //        //applicationPoolBag[ServerManagerDemoGlobals.ApplicationPoolArray] = applicationPool;
        //        //arrayOfApplicationBags.Add(applicationPoolBag);
        //        //// If the applicationPool is stopped, restart it.
        //        //if (applicationPool.State == ObjectState.Stopped)
        //        //{
        //        //    applicationPool.Start();
        //        //}

        //        //appPool.Recycle();

        //        string attrKey = null;
        //        string attrValue = null;
        //        List<string> sb = new List<string>();
        //        ObjectState state = ObjectState.Unknown;
        //        foreach(ConfigurationAttribute appAttr in appPool.Attributes)
        //        {
        //            try
        //            {
        //                attrKey = appAttr.Name;
        //                if (appAttr.Value == null) { attrValue = "NULL"; }
        //                else
        //                {
        //                    attrValue = appAttr.Value.ToString();
        //                    if (attrKey.Equals("state")) { state = (ObjectState)appAttr.Value; }
        //                }                       
        //            }
        //            catch (Exception e)
        //            {
        //                if (attrKey.Equals("state")) {
        //                    state = ObjectState.Started;
        //                    attrValue = state.ToString();
        //                } else {
        //                    attrValue = e.Message;
        //                }
        //            }
        //            sb.Add($"{attrKey}:{attrValue}");
        //        }
        //        string appAttributes = $"{{{(sb.Any()? sb.Aggregate((a,b)=>a+","+b) : "")}}}";

        //        return state;
        //    }
        //}
        #endregion

        private static bool IsAccessible(byte AccessLevel) { return WSConstants.ACCESS_LEVEL.LEVELS.Contains(AccessLevel) && AccessLevel < WSConstants.ACCESS_LEVEL.LOCK; }
        private static WSSource GetSource(string SRCName, string DBName = null) { return SYSTEM_SOURCES[DBName].Single(x => x.Match(SRCName, DBName)); }
        private static Type GetSourceType(string SRCName, string DBName)
        {
            try { return SYSTEM_SOURCES[DBName].Single(x => x.Match(SRCName, DBName)).ReturnType; } catch (Exception) { }
            return null;
        }
        private static WSSource GetSourceByType(Type type, string _1MinTicket = null)
        {
            try { return SYSTEM_SOURCES.GetSourceByType(type); } catch (Exception) { }
            return null;
        }

        public static void LogError(Type caller, Exception e, ref WSStatus statusLines, string errorMsg = null)
        {
            WSLogRecord Log = new WSLogRecord("Exception", true)
            {
                $"{{Exception.Line:[{e.LineNumber()}]}}",
                $"{{Exception.Message:[{e.Message}]}}",
                $"{{Exception.StackTrace:[{e.StackTrace}]}}",
                $"{{InnerException.Message:[{(e.InnerException==null?"":e.InnerException.Message)}]}}",
                $"{{InnerException.StackTrace:[{(e.InnerException==null?"":e.InnerException.StackTrace)}]}}"
            };
            Log.Save();
        }

        private static ServerFunctions ServerFunctions = new ServerFunctions(IsAccessible, GetSource, GetSourceType, GetSourceByType, LogError, string.Empty);
    }
    public class ClientFunctions : ServerFunctions
    {
        internal ClientFunctions(DelIsAccessible _DelIsAccessible, DelGetSource _DelGetSource, DelGetSourceType _DelGetSourceType, DelGetSourceByType _DelGetSourceByType, DelRegError _RegError, string _SrcBinaryKey) :
            base(_DelIsAccessible, _DelGetSource, _DelGetSourceType, _DelGetSourceByType, _RegError, _SrcBinaryKey) { }
    }
    public class ServerFunctions : MetaFunctions
    {
        internal ServerFunctions(DelIsAccessible _DelIsAccessible, DelGetSource _DelGetSource, DelGetSourceType _DelGetSourceType, DelGetSourceByType _DelGetSourceByType, DelRegError _RegError, string _SrcBinaryKey) :
            base(_DelIsAccessible, _DelGetSource, _DelGetSourceType, _DelGetSourceByType, _RegError, _SrcBinaryKey) { }
    }
    public class MetaFunctions
    {
        public delegate bool DelIsAccessible(byte ACCESS_LEVEL);
        public delegate WSSource DelGetSource(string SRCName, string DBName = null);
        public delegate Type DelGetSourceType(string SRCName, string DBName);
        public delegate WSSource DelGetSourceByType(Type type, string _1MinTicket = null);
        public delegate void DelRegError(Type caller, Exception e, ref WSStatus statusLines, string errorMsg = null);

        public string SrcBinaryKey = string.Empty;
        public DelIsAccessible IsAccessible = null;
        public DelGetSource GetSource = null;
        public DelGetSourceType GetSourceType = null;
        public DelGetSourceByType GetSourceByType = null;
        public DelRegError RegError = null;

        internal MetaFunctions(DelIsAccessible _DelIsAccessible, DelGetSource _DelGetSource, DelGetSourceType _DelGetSourceType, DelGetSourceByType _DelGetSourceByType, DelRegError _RegError, string _SrcBinaryKey)
        {
            IsAccessible = _DelIsAccessible;
            GetSource = _DelGetSource;
            GetSourceType = _DelGetSourceType;
            GetSourceByType = _DelGetSourceByType;
            RegError = _RegError == null ? WSServerMeta.LogError : _RegError;
            SrcBinaryKey = _SrcBinaryKey;
        }
    }
}