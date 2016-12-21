using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
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
    public static class WSExtention
    {
        public static int LineNumber(this Exception e)
        {
            int linenum = 0;
            try { linenum = new StackTrace(e, true).GetFrame(0).GetFileLineNumber(); } catch { }
            try { linenum = linenum > 0 ? linenum : Convert.ToInt32(e.StackTrace.Substring(e.StackTrace.LastIndexOf(' '))); } catch { }
            return linenum;
        }
        public static object DeepCopy(object obj)
        {
            if (obj == null)
                return null;
            Type type = obj.GetType();

            if (type.IsValueType || type == typeof(string))
            {
                return obj;
            }
            else if (type.IsArray)
            {
                Type elementType = Type.GetType(
                     type.FullName.Replace("[]", string.Empty));
                var array = obj as Array;
                Array copied = Array.CreateInstance(elementType, array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    copied.SetValue(DeepCopy(array.GetValue(i)), i);
                }
                return Convert.ChangeType(copied, obj.GetType());
            }
            else if (type.IsClass)
            {

                object toret = Activator.CreateInstance(obj.GetType());
                FieldInfo[] fields = type.GetFields(BindingFlags.Public |
                            BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    object fieldValue = field.GetValue(obj);
                    if (fieldValue == null)
                        continue;
                    field.SetValue(toret, DeepCopy(fieldValue));
                }
                return toret;
            }
            else
                throw new ArgumentException("Unknown type");
        }
        #region GetGenericTypeArguments
        public static Type[] GetGenericTypeArguments(this Type t)
        {
            lock (GetGenericTypeArgumentsLock)
            {
                try
                {
#if NET45
                    return t.GenericTypeArguments;
#else
                    return t.IsGenericType && !t.IsGenericTypeDefinition ? t.GetGenericArguments() : Type.EmptyTypes;
#endif
                }
                catch (Exception) { }
                return null;
            }
        }
        private static object GetGenericTypeArgumentsLock = new object();
        #endregion

        #region CustomAttributesData
        public static IEnumerable<CustomAttributeData> CustomAttributesData(this MemberInfo member)
        {
            lock (CustomAttributesDataLock)
            {
                try
                {
#if NET45
                    return member.CustomAttributes;
#else
                    return member.GetCustomAttributesData();
#endif
                }
                catch (Exception) { }
                return null;
            }
        }
        private static object CustomAttributesDataLock = new object();
        #endregion

        #region CustomAttributesData
        public static T CustomAttribute<T>(this Type eType, bool inherit = false)
        {
            lock (CustomAttributeLock)
            {
                T t = default(T);
                try
                {
#if NET45
                    t = eType.GetCustomAttribute<T>();
#else
                    object[] tObj = eType.GetCustomAttributes(typeof(T), inherit);
                    if (tObj != null && tObj.FirstOrDefault() != null) { t = (T)tObj.FirstOrDefault(); }
#endif

                }
                catch (Exception) { }
                return t;
            }
        }
        private static object CustomAttributeLock = new object();
        #endregion

        #region CustomAttributesData
        public static T CustomAttribute<T>(this MemberInfo member, bool inherit = true)
        {
            lock (CustomAttributeByMemberLock)
            {
                T t = default(T);
                try
                {
                    IEnumerable<T> cAttributes = member.GetCustomAttributes(typeof(T), inherit).OfType<T>();
                    if (cAttributes.Any())
                    {
                        t = cAttributes.FirstOrDefault();
                    }
                }
                catch (Exception) { }
                return t;
            }
        }
        private static object CustomAttributeByMemberLock = new object();
        #endregion

        public static void SetPropertyValue(this PropertyInfo prop, WSDynamicEntity entity, object val)
        {
            try
            {
                bool? is45 = null;
#if NET45
                    prop.SetValue(entity, val);
                    is45=true;
#endif

#if NET4
                    is45 = false;
                    prop.SetValue(entity, val, null);
#endif

                if (is45 == null)
                {
                    //try {
                    //    prop.SetValue(entity, val);
                    //} catch {
                    prop.SetValue(entity, val, null);
                    //}
                }
            }
            catch (Exception e) { }
        }

        internal static WSDCState GetState(this Type _ContextType, IEnumerable<WSDCItem> Items)
        {
            IEnumerable<WSDCItem> contexts = Items.Where(x => x.Context.GetType().Equals(_ContextType));
            
            if (contexts == null || !contexts.Any()) return WSDCState.NotExists;
            
            else if (!contexts.Any(x => !x.Context.IsDisposed)) return WSDCState.Disposed;
            else if (!contexts.Any(x => x.Context.Connection.State != System.Data.ConnectionState.Broken)) return WSDCState.Broken;
            
            else if (!contexts.Any(x => x.Context.Connection.State != System.Data.ConnectionState.Connecting)) return WSDCState.Connecting;
            else if (!contexts.Any(x => x.Context.Connection.State != System.Data.ConnectionState.Executing)) return WSDCState.Executing;
            else if (!contexts.Any(x => x.Context.Connection.State != System.Data.ConnectionState.Fetching)) return WSDCState.Fetching;

            else if (!contexts.Any(x => x.Context.Connection.State != System.Data.ConnectionState.Closed)) return WSDCState.Closed;
            else return WSDCState.Open;
        }

        #region Validation

        public static bool ContainsParam(this Dictionary<string, string> dict, WSParam param)
        {
            if (dict != null && dict.Any() && param != null)
            {
                try
                {
                    return dict.ContainsKey(param.NAME) || (param.ALIACES != null && dict.Keys.Any(key => param.ALLOWED_VALUES.Any(value => value.Match(key))));
                }
                catch (Exception) { }
            }
            return false;
        }

        public static bool IsBase64String(this string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
        }
        public static bool IsNumber(this string s)
        {
            s = s.Trim();
            return s.IsShort() || s.IsInteger() || s.IsDouble() || s.IsFloat() || s.IsDecimal();
        }
        public static bool IsShort(this string s)
        {
            s = s.Trim();
            short i = 0;
            return short.TryParse(s, out i);
        }
        public static bool IsInteger(this string s)
        {
            s = s.Trim();
            int i = 0;
            return int.TryParse(s, out i);
        }
        public static bool IsDouble(this string s)
        {
            s = s.Trim();
            double d = 0;
            return double.TryParse(s, out d);
        }
        public static bool IsFloat(this string s)
        {
            s = s.Trim();
            float f = 0;
            return float.TryParse(s, out f);
        }
        public static bool IsDecimal(this string s)
        {
            s = s.Trim();
            decimal dec = 0;
            return decimal.TryParse(s, out dec);
        }
        public static bool IsSimple(this Type t)
        {
            return WSConstants.SIMPLE_DATATYPES.Any(s => t.IsAssignableFrom(s));
        }
        public static bool IsSimpleCollection(this Type t)
        {
            return t.IsCollection() && t.GetEntityType().IsSimple();
        }
        public static bool IsNumeric(this Type t)
        {
            return t != null && WSConstants.NUMERIC_TYPES.Any(x => t.IsAssignableFrom(x));
        }
        public static bool IsShort(this Type t)
        {
            return t != null && t.IsAssignableFrom(typeof(short));
        }
        public static bool IsInteger(this Type t)
        {
            return t != null && t.IsAssignableFrom(typeof(int));
        }
        public static bool IsDouble(this Type t)
        {
            return t != null && t.IsAssignableFrom(typeof(double));
        }
        public static bool IsFloat(this Type t)
        {
            return t != null && t.IsAssignableFrom(typeof(float));
        }
        public static bool IsDecimal(this Type t)
        {
            return t != null && t.IsAssignableFrom(typeof(decimal));
        }
        public static bool IsByte(this Type t)
        {
            return t != null && t.IsAssignableFrom(typeof(byte));
        }
        public static bool IsBoolean(this Type t)
        {
            return t != null && t.IsAssignableFrom(typeof(bool));
        }
        public static bool IsIEnumerable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        public static bool IsNullable(this Type type)
        {
            return type == null || !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        public static Type[] SORTABLE_DATATYPES = new Type[] {
            typeof(int),
            typeof(short),
            typeof(float),
            typeof(double),
            typeof(long),
            typeof(decimal),
            typeof(byte),
            typeof(bool),
            typeof(string),
            typeof(TimeSpan),
            typeof(DateTime),
            typeof(Guid)
        };
        public static bool IsSameOrSubclassOf(this Type descendantType, Type baseType)
        {
            return descendantType.IsSubclassOf(baseType) || descendantType == baseType;
        }

        public static bool IsNullOrEmptyValue(this string s)
        {
            return string.IsNullOrEmpty(s) || WSConstants.ALIACES.EMPTY.Match(s);
        }

        public static bool IsTrue(this string s) { return !string.IsNullOrEmpty(s) && WSConstants.ALIACES.TRUE.Match(s); }
        public static bool IsFalse(this string s) { return !string.IsNullOrEmpty(s) && WSConstants.ALIACES.FALSE.Match(s); }

        public static bool IsKeyValueCollection(this Type type) { return type.GetInterface(typeof(IEnumerable<>).FullName) != null; }
        public static bool IsCollection(this Object obj) { return obj != null && obj.GetType().IsCollection(); }
        public static bool IsCollectionOf<T>(this Object obj) { return obj != null && obj.GetType().IsCollectionOf<T>(); }
        public static bool IsCollection(this Type type)
        {
            try
            {
                string IEnum = typeof(IEnumerable<>).FullName;
                return type.GetInterface(IEnum) != null;
            }
            catch (Exception) { }
            return false;
        }
        public static bool IsCollectionOf<T>(this Type type)
        {
            try
            {
                if (type.GetInterface(typeof(IEnumerable<>).FullName) != null)
                {
                    bool isTypOfT = false;
                    isTypOfT = type.GetGenericTypeArguments() != null && type.GetGenericTypeArguments().Any(t => t.IsSameOrSubclassOf(typeof(T)));

                    if (isTypOfT) return true;
                    else
                    {
                        Type eType = type.GetElementType();
                        return eType != null && eType.IsSameOrSubclassOf(typeof(T));
                    }
                }
            }
            catch (Exception) { }
            return false;
        }

        public enum ImageFormat { bmp, jpeg, gif, tiff, png, unknown }
        public static ImageFormat GetImageFormat(this byte[] bytes)
        {
            var bmp = Encoding.ASCII.GetBytes("BM");     // BMP
            if (bmp.SequenceEqual(bytes.Take(bmp.Length))) return ImageFormat.bmp;
            var gif = Encoding.ASCII.GetBytes("GIF");    // GIF
            if (gif.SequenceEqual(bytes.Take(gif.Length))) return ImageFormat.gif;
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            if (png.SequenceEqual(bytes.Take(png.Length))) return ImageFormat.png;
            var tiff = new byte[] { 73, 73, 42 };         // TIFF
            if (tiff.SequenceEqual(bytes.Take(tiff.Length))) return ImageFormat.tiff;
            var tiff2 = new byte[] { 77, 77, 42 };         // TIFF
            if (tiff2.SequenceEqual(bytes.Take(tiff2.Length))) return ImageFormat.tiff;
            var jpeg = new byte[] { 255, 216, 255, 224 }; // jpeg
            if (jpeg.SequenceEqual(bytes.Take(jpeg.Length))) return ImageFormat.jpeg;
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon
            if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length))) return ImageFormat.jpeg;
            return ImageFormat.unknown;
        }

        #endregion

        #region Data Read&Write

        #region IQueryable<X>

        public static object SkipAndTakeLock = new object();
        public static IQueryable<X> SkipAndTake<X>(this IQueryable<X> list, int OFFSET, int COUNT) where X : class
        {
            lock (SkipAndTakeLock)
            {
                IQueryable<X> temp = list;
                if (temp != null)
                {
                    try
                    {
                        temp = OFFSET <= 0 ? temp : temp.Skip(OFFSET);
                        temp = COUNT < 0 ? temp : temp.Take(COUNT);
                    }
                    catch (Exception) { }
                }
                return temp;
            }
        }

        #endregion IQueryable<X> EXTENTIONS

        #region IEnumerable<X>

        public static object SelectDistinctLock = new object();
        public static IEnumerable<X> SelectDistinct<X>(this IQueryable<X> items, IEnumerable<string> fields) where X : class
        {
            lock (SelectDistinctLock)
            {
                return items.SelectDynamic(fields).Distinct(new DynamicEqualityComparer<X>(fields));
            }
        }

        public static object SelectDynamicLock = new object();
        public static IEnumerable<X> SelectDynamic<X>(this IEnumerable<X> items, IEnumerable<string> fields)
        {
            lock (SelectDynamicLock)
            {
                return items.Select(CreateDynamicStatement<X>(fields));
            }
        }

        #endregion IEnumerable<X> EXTENTIONS

        #region Dictionary<string, string>

        public static object SaveLock = new object();
        public static void Save(this Dictionary<string, string> dict, string key, string value, bool replaceAllowed = true)
        {
            lock (SaveLock)
            {
                if (dict != null)
                {
                    try
                    {
                        key = key == null ? null : key.ToLower();
                        if (dict.ContainsKey(key)) { if (replaceAllowed) { dict[key] = value; } }
                        else { dict.Add(key, value); }
                    }
                    catch (Exception) { }
                }
            }
        }

        public static object ReadDictionaryLock = new object();
        public static bool ReadValue(this Dictionary<string, string> dict, WSParam key, out string value)
        {
            lock (ReadDictionaryLock)
            {
                value = null;
                try
                {
                    if (dict != null && key != null)
                    {
                        if (dict.Any(d => key.Match(d.Key)))
                        {
                            value = dict.FirstOrDefault(d => key.Match(d.Key)).Value;
                            return true;
                        }
                    }
                }
                catch (Exception) { }
                return false;
            }
        }
        public static object ReadDictionaryByVWSValueLock = new object();
        public static bool ReadValue(this Dictionary<string, string> dict, WSValue key, out string value)
        {
            lock (ReadDictionaryByVWSValueLock)
            {
                value = null;
                try
                {
                    if (dict != null && key != null)
                    {
                        if (dict.Any(d => key.Match(d.Key)))
                        {
                            value = dict.FirstOrDefault(d => key.Match(d.Key)).Value;
                            return true;
                        }
                    }
                }
                catch (Exception) { }
                return false;
            }
        }

        #endregion Dictionary<string, string> EXTENTIONS

        #region Data&Context

        private static object ReadPropertiesLock = new object();
        public static List<MetaDataMember> ReadProperties(this DataContext dc, Type type, ref WSStatus statusLines)
        {
            lock (ReadPropertiesLock)
            {
                try
                {
                    AttributeMappingSource ams = new System.Data.Linq.Mapping.AttributeMappingSource();
                    MetaModel model = ams.GetModel(dc.GetType());
                    var meta = model.GetTable(type);

                    return meta.RowType.DataMembers.Where(x => x.IsPersistent).ToList();//TODO@ANDVO : avoid using '.ToList()' to speed up execution
                }
                catch (Exception) { }
                return null;
            }
        }

        private static object PrimitivePropertiesLock = new object();
        public static List<MetaDataMember> PrimitiveProperties(this DataContext dc, Type type, ref WSStatus statusLines)
        {
            lock (PrimitivePropertiesLock)
            {
                try
                {
                    return dc.ReadProperties(type, ref statusLines).Where(x => !x.IsAssociation).ToList();//TODO@ANDVO : avoid using '.ToList()' to speed up execution
                }
                catch (Exception) { }
                return null;
            }
        }

        private static object AssociationPropertiesLock = new object();
        public static List<MetaDataMember> AssociationProperties(this DataContext dc, Type type, ref WSStatus statusLines)
        {
            lock (AssociationPropertiesLock)
            {
                try
                {
                    return dc.ReadProperties(type, ref statusLines).Where(x => x.IsAssociation).ToList();//TODO@ANDVO : avoid using '.ToList()' to speed up execution
                }
                catch (Exception) { }
                return null;
            }
        }

        private static object getMetaTypeLock = new object();
        public static MetaType getMetaType(this WSDataContext db, Type EntityType)
        {
            lock (getMetaTypeLock)
            {
                try
                {
                    return db.Mapping.GetTable(EntityType).RowType;
                }
                catch (Exception) { }
                return null;
            }
        }

        public static object GetTableNameLock = new object();
        public static string GetTableName<A>(this Table<A> table) where A : WSEntity
        {
            lock (GetTableNameLock)
            {
                try
                {
                    var rowType = table.GetEntityType();
                    return table.Context.Mapping.GetTable(rowType).TableName;
                }
                catch (Exception) { }
                return null;
            }
        }

        public static object TruncateLock = new object();
        public static bool Truncate<A>(this Table<A> table) where A : WSEntity
        {
            lock (TruncateLock)
            {
                try
                {
                    var sqlCommand = String.Format("TRUNCATE TABLE {0}", table.GetTableName());
                    table.Context.ExecuteCommand(sqlCommand);
                    return true;
                }
                catch (Exception) { }
                return false;
            }
        }
        
        #endregion Table<A> EXTENTIONS

        #region Type
        public static bool HasProperty(this Type type, string colName, bool IgnoreCase = true)
        {
            try
            {
                if (!string.IsNullOrEmpty(colName))
                {
                    Func<PropertyInfo, bool> func = (p => colName.Equals(p.Name));
                    if (IgnoreCase) { func = (p => colName.ToLower().Equals(p.Name.ToLower())); }
                    return type.GetProperties().Any(func);
                }
            }
            catch (Exception) { }
            return false;
        }

        private static object IsCompositeByMetaIDLock = new object();
        public static bool IsCompositeID(this MetaType meta)
        {
            lock (IsCompositeByMetaIDLock)
            {
                try
                {
                    return meta != null && meta.IdentityMembers.Count > 1;
                }
                catch (Exception) { }
                return false;
            }
        }
        private static object GetGenericMethodLock = new object();
        public static MethodBase GetGenericMethod(this Type type, string name, Type[] GenericArguments, Type[] argTypes, BindingFlags flags)
        {
            lock (GetGenericMethodLock)
            {
                var methods = type.GetMethods()
                    .Where(m => m.Name == name && m.GetGenericArguments().Length == GenericArguments.Length)
                    .Select(m => m.MakeGenericMethod(GenericArguments));

                return Type.DefaultBinder.SelectMethod(flags, methods.ToArray(), argTypes, null);
            }
        }
        #endregion

        #region IList
        private static object GetListEntityTypeLock = new object();
        public static Type GetEntityType(this IEnumerable list)
        {
            lock (GetListEntityTypeLock)
            {
                return list.GetType().GetEntityType();
            }
        }
        private static object GetEntityTypeLock = new object();
        public static Type GetEntityType(this Type type)
        {
            lock (GetEntityTypeLock)
            {
                Type entityType = null;
                try
                {
                    if (type != null)
                    {
                        if (type.IsCollection())
                        {
                            Type[] GTA = type.GetGenericTypeArguments();
                            //if Generic Collection 
                            if (GTA != null && GTA.Any()) { entityType = GTA[0]; }
                            //if Array
                            if (entityType == null)
                            {
                                Type ifType = type.GetInterface(typeof(IEnumerable<>).FullName);
                                if (ifType != null)
                                {
                                    entityType = ifType.GetGenericTypeArguments().FirstOrDefault();
                                }
                            }
                        }
                        else entityType = type;
                    }
                }
                catch (Exception) { }


                return (entityType == null && !type.IsSimple()) ? type : entityType;
            }
        }

        private static object IsEntityLock = new object();
        public static bool IsValidDynamicEntity(this Type eType)/*(10) : [VERIFY FOR 'INotifyPropertyChanging' present, since it must have a primary key]*/
        {
            lock (IsEntityLock)
            {
                if (eType != null && eType.IsClass && eType.IsSameOrSubclassOf(typeof(WSDynamicEntity)))
                {
                    return eType.CustomAttribute<TableAttribute>() != null;
                }
                return false;
            }
        }
        #endregion

        private static object CreateDynamicStatementLock = new object();
        private static Func<X, X> CreateDynamicStatement<X>(IEnumerable<string> fields)
        {
            lock (CreateDynamicStatementLock)
            {
                try
                {
                    var eParameter = Expression.Parameter(typeof(X), "o");
                    var newX = Expression.New(typeof(X));

                    IEnumerable<PropertyInfo> dprops = fields.Select(o => typeof(X).GetProperty(o.Trim()));

                    List<MemberBinding> bindings = new List<MemberBinding>();
                    foreach (PropertyInfo p in dprops) { bindings.Add(Expression.Bind(p, Expression.Property(eParameter, p))); }

                    var xInit = Expression.MemberInit(newX, bindings);// initialization "new X { Field1 = o.Field1, Field2 = o.Field2 }"
                    var lambda = Expression.Lambda<Func<X, X>>(xInit, eParameter);// expression "o => new X { Field1 = o.Field1, Field2 = o.Field2 }"
                    return lambda.Compile();
                }
                catch (Exception) { }
                return null;
            }
        }
        
        public static string GetRAMSize(this object o)
        {
            long size = 0;
            try
            {
                using (Stream s = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(s, o);
                    size = s.Length;
                }
            }
            catch (Exception) { }
            return SizeToString(size);
        }
        private const long _kb = 1024;
        private const long _mb = _kb * _kb;
        private const long _gb = _kb * _mb;
        private static string SizeToString(long total)
        {
            try
            {
                if (total >= 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    char? space = null;

                    long gb_ = total / _gb;
                    if (gb_ > 0) { sb.Append(gb_ + "GB"); space = ' '; }

                    long mb_ = (total % _gb) / _mb;
                    if (mb_ > 0) { sb.Append(space + mb_ + "MB"); space = ' '; }

                    long kb_ = (total / _mb) / _kb;
                    if (kb_ > 0) { sb.Append(space + kb_ + "KB"); space = ' '; }

                    long b_ = total / _kb;
                    if (b_ > 0) { sb.Append(space + b_ + "B"); }

                    return sb.Length > 0 ? sb.ToString() : "0Bytes";
                }
            }
            catch (Exception) { }
            return "undefined";
        }
        #endregion

        #region Convertion

        public static Expression<Func<T, T>> FuncToExpression<T>(Func<T, T> f) { return x => f(x); }

        public static Expression<Func<T, bool>> FuncToExpression<T>(Func<T, bool> f) { return x => f(x); }

        public static Func<T, bool> ExpressionToFunc<T>(Expression<Func<T, bool>> e) { return e.Compile(); }

        public static void Add(this List<WSStatus> list, string text)
        {
            try
            {
                if (list != null) { WSStatus status = WSStatus.NONE_Copy(); status.AddNote(text); list.Add(status); }
            }
            catch (Exception) { }
        }

        public static string ToHex(this int number) { return number.ToString("X"); }

        public static Dictionary<string, string> ToDictionary(this Hashtable inputValues)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            try { if (inputValues != null) { foreach (string o in inputValues.Keys) { try { if (o != null) { dict.Save(o, inputValues[o].ToString()); } } catch (Exception) { } } } } catch (Exception) { }
            return dict;
        }
        public static Dictionary<string, string> ToDictionary(this IDictionary inputValues)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            try
            {
                if (inputValues != null) { foreach (string o in inputValues.Keys) { try { if (o != null) { dict.Save(o, inputValues[o].ToString()); } } catch (Exception) { } } }
            }
            catch (Exception) { }
            return dict;
        }
        public static string[] ToWords(this string text)
        {
            lock (ToWordsLock)
            {
                string[] words = null;
                try { words = string.IsNullOrEmpty(text) ? new string[] { } : text.ToSingleLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); }
                catch (Exception) { }
                return words;
            }
        }
        public static object ToWordsLock = new object();
        public static string ToSingleLine(this string text)
        {
            lock (ToSingleLineLock)
            {
                return string.IsNullOrEmpty(text) ? text : text.Replace("\n", "").Replace("\r", "").Replace("\t", "");
            }
        }
        public static object ToSingleLineLock = new object();
        public static WSJson ToJson(this string jText)
        {
            lock (ToJsonLock)
            {
                WSJson json = null;
                try
                {
                    jText = jText == null ? null : jText.Trim();
                    if (!string.IsNullOrEmpty(jText))
                    {
                        //WSJObject
                        if ('{' == jText.FirstOrDefault() && '}' == jText.LastOrDefault())
                        {
                            json = JsonConvert.DeserializeObject<WSJson>(jText, new WSFilterConverter());
                        }
                        //WSJArray
                        else if ('[' == jText.FirstOrDefault() && ']' == jText.LastOrDefault())
                        {
                            WSJObject jObj = (WSJObject)JsonConvert.DeserializeObject<WSJson>("{json:" + jText + "}", new WSFilterConverter());
                            json = jObj.Value[0].Value;
                        }
                        //WSJValue
                        else
                        {
                            json = new WSJValue(jText);
                        }
                    }
                }
                catch (Exception) { }
                return json;
            }
        }
        private static object ToJsonLock = new object();
        public static bool TryGetJson(this string jText, out WSJson json)
        {
            lock (TryGetJsonLock)
            {
                json = jText.ToJson();
                return json != null;
            }
        }
        private static object TryGetJsonLock = new object();

        public static bool Read(this Type type, object value, out dynamic output, char[] trimChars = null, char[] listSeparators = null, string columnName = null)
        {
            lock (ConvertLock)
            {
                output = null;
                trimChars = trimChars == null ? WSConstants.TRIM_CHARS : trimChars;
                listSeparators =
                    listSeparators != null ? listSeparators :
                    typeof(DateTime?).IsAssignableFrom(type) ? WSConstants.DATE_LIST_SEPARATORS :
                    type.IsNumeric() ? WSConstants.NUMERIC_SEPARATORS :
                    WSConstants.LIST_SEPARATORS;
                try
                {
                    if (type == null) { output = ConvertWithNoType(value); }
                    else
                    {
                        bool isNullable = type.IsNullable();
                        if (value == null || string.IsNullOrEmpty(value.ToString())) { return isNullable; }
                        else
                        {
                            string sVal = value.ToString();
                            string[] values = new string[] { sVal };
                            if (trimChars.Any()) { sVal = sVal.Trim(trimChars); }
                            if (listSeparators.Any() && !type.IsAssignableFrom(typeof(string))) { values = sVal.Split(listSeparators, StringSplitOptions.RemoveEmptyEntries); }
                            List<dynamic> dList = new List<dynamic>();

                            Dictionary<string, bool> converted = new Dictionary<string, bool> { };

                            #region FILL VALUES
                            foreach (string val in values)
                            {
                                if (typeof(DateTime?).IsAssignableFrom(type))
                                {
                                    if (type.IsNullable())
                                    {
                                        DateTime? date = null;
                                        converted.Add(val == null ? "" : val, new WSConverter().ToDate(val, out date));
                                        if (converted[val == null ? "" : val]) { dList.Add(date); }
                                    }
                                    else
                                    {
                                        DateTime date = DateTime.MinValue;
                                        converted.Add(val == null ? "" : val, new WSConverter().ToDate(val, out date));
                                        if (converted[val == null ? "" : val]) { dList.Add(date); }
                                    }
                                }
                                else if (typeof(TimeSpan?).IsAssignableFrom(type))
                                {
                                    //TODO:ANDVO@2015-10-05 :  TEST REQUIRED !!!

                                    if (type.IsNullable())
                                    {
                                        TimeSpan? time = null;
                                        converted.Add(val == null ? "" : val, new WSConverter().ToTime(val, out time));
                                        if (converted[val == null ? "" : val]) { dList.Add(time); }
                                    }
                                    else
                                    {
                                        TimeSpan time = TimeSpan.MinValue;
                                        converted.Add(val == null ? "" : val, new WSConverter().ToTime(val, out time));
                                        if (converted[val == null ? "" : val]) { dList.Add(time); }
                                    }
                                }
                                else
                                {
                                    if (typeof(int?).IsAssignableFrom(type))
                                    {
                                        dList.Add(int.Parse(val));
                                    }
                                    else if (typeof(short?).IsAssignableFrom(type))
                                    {
                                        dList.Add(short.Parse(val));
                                    }
                                    else if (typeof(decimal?).IsAssignableFrom(type))
                                    {
                                        dList.Add(decimal.Parse(val, System.Globalization.CultureInfo.InvariantCulture));
                                    }
                                    else if (typeof(bool?).IsAssignableFrom(type))
                                    {
                                        if (WSConstants.ALIACES.TRUE.Match(val)) { dList.Add(true); }
                                        else if (WSConstants.ALIACES.FALSE.Match(val)) { dList.Add(false); }
                                    }
                                    else if (typeof(byte?).IsAssignableFrom(type))
                                    {
                                        dList.Add(byte.Parse(val));
                                    }
                                    else if (type.IsAssignableFrom(typeof(String)))
                                    {
                                        dList.Add(val);
                                    }
                                    else if (typeof(Guid?).IsAssignableFrom(type))
                                    {
                                        dList.Add(new Guid(val));
                                    }
                                    converted.Add(val, dList.Any());
                                }
                            }
                            #endregion

                            Type entityType = null;
                            if (dList.Any() && !converted.Any(x => !x.Value))
                            {
                                output = dList.Count == 1 ? dList[0] : dList;
                                entityType = dList[0] == null ? type.IsNullable() ? type : null : dList[0].GetType();
                                return dList.Any() && entityType != null && type.IsAssignableFrom(entityType);
                            }
                        }
                    }
                }
                catch (Exception) { }
                return false;
            }
        }
        public static object ConvertLock = new object();

        private static object ConvertWithNoType(object value)
        {
            lock (ConvertWithNoTypeLock)
            {
                object newObj = null;
                try
                {
                    if (value == null || string.IsNullOrEmpty(value.ToString())) { return null; }
                    else
                    {
                        int i = 0;
                        short sh = 0;
                        decimal dec = 0;
                        byte b = 0;
                        Guid g = Guid.Empty;
                        DateTime date = DateTime.MinValue;

                        if (int.TryParse(value.ToString(), out i)) { newObj = i; }
                        else if (short.TryParse(value.ToString(), out sh)) { newObj = sh; }
                        else if (decimal.TryParse(value.ToString(), out dec)) { newObj = dec; }
                        else if (byte.TryParse(value.ToString(), out b)) { newObj = b; }
                        else if (Guid.TryParse(value.ToString(), out g)) { newObj = g; }
                        else if (new WSConverter().ToDate(value.ToString(), out date)) { newObj = date; }
                        else if (WSConstants.ALIACES.TRUE.Match(value.ToString())) { newObj = true; }
                        else if (WSConstants.ALIACES.FALSE.Match(value.ToString())) { newObj = false; }
                        else { newObj = value.ToString(); }
                    }
                }
                catch (Exception) { }
                return newObj;
            }
        }
        private static object ConvertWithNoTypeLock = new object();

        public static string ClearControllChars(this string text)
        {
            try
            {
                text = !string.IsNullOrEmpty(text) ? text.Replace('\x2' + "", "").Replace('\x1E' + "", "") : text;
            }
            catch (Exception) { }
            return text;
        }
        
        public static string EscapedUrl(this Uri uri)
        {
            return uri == null ?
                null :
                uri
                .AbsoluteUri
                .ToLower()
                .Replace("%3a", ":")
                .Replace("%2c", ",")
                .Replace("%5b", "[")
                .Replace("%7b", "{")
                .Replace("%5d", "]")
                .Replace("%7d", "}")
                .Replace("%22", "\"")
                .Replace("%27", "'");
        }
        #endregion
    }

    public class DynamicEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        private readonly IEnumerable<string> dynamicFields;

        public DynamicEqualityComparer(IEnumerable<string> _dynamicFields)
        {
            dynamicFields = _dynamicFields;
        }

        public bool Equals(T x, T y)
        {
            if (x != null && y != null && x.GetType() == y.GetType())
            {
                var type = x.GetType();

                var relevantProperties = type.GetProperties().Where(propertyInfo => dynamicFields.Contains(propertyInfo.Name));

                foreach (var propertyInfo in relevantProperties)
                {
                    var xPropertyValue = type.GetProperty(propertyInfo.Name).GetValue(x, null);

                    var yPropertyValue = type.GetProperty(propertyInfo.Name).GetValue(y, null);

                    if (xPropertyValue != yPropertyValue && (xPropertyValue == null || !xPropertyValue.Equals(yPropertyValue)))
                    {
                        return false;
                    }
                }
                return true;
            }
            return x == y;
        }

        public int GetHashCode(T obj)
        {
            var type = typeof(T);

            var objectProperties = type.GetProperties();

            return objectProperties.Sum(property => property.GetHashCode());
        }
    }
}
