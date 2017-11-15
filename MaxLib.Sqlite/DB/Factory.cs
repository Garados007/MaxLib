using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Data.SQLite;

namespace MaxLib.DB
{
    namespace Helper
    {
        using System.Linq.Expressions;

        public class FastInvoke
        {
            public static Func<T, TReturn> BuildTypedGetter<T, TReturn>(PropertyInfo propertyInfo)
            {
                return (Func<T, TReturn>)Delegate.CreateDelegate(typeof(Func<T, TReturn>), propertyInfo.GetGetMethod());
            }

            public static Action<T, TProperty> BuildTypedSetter<T, TProperty>(PropertyInfo propertyInfo)
            {
                return (Action<T, TProperty>)Delegate.CreateDelegate(typeof(Action<T, TProperty>), propertyInfo.GetSetMethod());
            }

            public static Action<T, object> BuildUntypedSetter<T>(PropertyInfo propertyInfo)
            {
                var targetType = propertyInfo.DeclaringType;
                var methodInfo = propertyInfo.GetSetMethod();
                var exTarget = Expression.Parameter(typeof(T), "t");
                var exTarget2 = Expression.Convert(exTarget, targetType);
                var exValue = Expression.Parameter(typeof(object), "p");
                var exBody = Expression.Call(exTarget2, methodInfo,
                    Expression.Convert(exValue, propertyInfo.PropertyType));
                var lambda = Expression.Lambda<Action<T, object>>(exBody, exTarget, exValue);
                return lambda.Compile();
            }

            public static Func<T, object> BuildUntypedGetter<T>(PropertyInfo propertyInfo)
            {
                var targetType = propertyInfo.DeclaringType;
                var methodInfo = propertyInfo.GetGetMethod();
                var returnType = methodInfo.ReturnType;
                var exTarget = Expression.Parameter(typeof(T), "t");
                var exTarget2 = Expression.Convert(exTarget, targetType);
                var exBody = Expression.Call(exTarget2, methodInfo);
                var exBody2 = Expression.Convert(exBody, typeof(object));
                var lambda = Expression.Lambda<Func<T, object>>(exBody2, exTarget);
                return lambda.Compile();
            }
        }
    }

    public class DbFactory : IDisposable
    {
        public static DbFactory Current { get; private set; }

        public static void SetCurrent(Database db)
        {
            if (db == null) Current = null;
            else Current = new DbFactory(db);
        }

        class InfoContainer
        {
            public Type ClassType;
            public DbClassAttribute ClassAttribute;
            public Dictionary<string, PropertyInfo> Properties = new Dictionary<string, PropertyInfo>();
            public Dictionary<string, DbPropAttribute> PropAttributes = new Dictionary<string, DbPropAttribute>();
            public Dictionary<string, string> PropertyType = new Dictionary<string, string>();
            public string[] NameTable = new string[0];
            public string[] PrimaryKeys = new string[0];

            public Dictionary<string, Action<object, object>> SetMethods = new Dictionary<string, Action<object, object>>();
            public Dictionary<string, Func<object, object>> GetMethods = new Dictionary<string, Func<object, object>>();

            public string selectAllQuery, updateQuery, addQuery;

            public long LoadedCount = 0, LoadSingle = 0, LoadAll = 0, LoadMatch = 0, Count = 0, Update = 0, Add = 0, Delete = 0;
            public Dictionary<string, Query> QueryBuffer = new Dictionary<string, Query>();
        }

        class lcComp : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return string.Equals(x?.ToLower(), y?.ToLower());
            }

            public int GetHashCode(string obj)
            {
                return obj.ToLower().GetHashCode();
            }
        }
        
        public struct TypeStatistic
        {
            public long LoadedCount { get; set; }
            public long LoadSingle { get; set; }
            public long LoadAll { get; set; }
            public long LoadMatch { get; set; }
            public long Count { get; set; }
            public long Update { get; set; }
            public long Add { get; set; }
            public long Delete { get; set; }
        }



        Dictionary<Type, InfoContainer> storedTypes = new Dictionary<Type, InfoContainer>();
        Database db;
        lcComp lccomp = new lcComp();

        public Database Database => db;

        public bool ValidPropertyCheck { get; set; }
        public bool CacheQuerys { get; set; }

        public DbFactory(Database db)
        {
            this.db = db ?? throw new ArgumentNullException("db");
            ValidPropertyCheck = true;
            CacheQuerys = true;
        }

        public IEnumerable<KeyValuePair<Type, TypeStatistic>> GetStatisticsByTypes()
        {
            foreach (var t in storedTypes)
            {
                yield return new KeyValuePair<Type, TypeStatistic>(t.Key, new TypeStatistic()
                {
                    LoadedCount = t.Value.LoadedCount,
                    LoadSingle = t.Value.LoadSingle,
                    LoadAll = t.Value.LoadAll,
                    LoadMatch = t.Value.LoadMatch,
                    Count = t.Value.Count,
                    Update = t.Value.Update,
                    Add = t.Value.Add,
                    Delete = t.Value.Delete
                });
            }
        }

        private void LoadQuerys(InfoContainer cont)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            for (int i = 0; i < cont.NameTable.Length; ++i)
            {
                if (i != 0) sb.Append(", ");
                sb.Append(cont.NameTable[i]);
            }
            sb.Append(" FROM ");
            sb.Append(cont.ClassAttribute.TableName);
            cont.selectAllQuery = sb.ToString();

            sb.Clear();
            sb.Append("UPDATE ");
            sb.Append(cont.ClassAttribute.TableName);
            sb.Append(" SET ");
            int valCount = 0;
            for (int i = 0; i < cont.NameTable.Length; ++i)
                if (!cont.PrimaryKeys.Contains(cont.NameTable[i]))
                {
                    if (valCount++ != 0) sb.Append(", ");
                    sb.Append(cont.NameTable[i]);
                    sb.Append("=?");
                }
            sb.Append(" WHERE ");
            for (int i = 0; i < cont.PrimaryKeys.Length; ++i)
            {
                if (i != 0) sb.Append(" AND ");
                sb.Append(cont.PrimaryKeys[i]);
                sb.Append("=?");
            }
            cont.updateQuery = sb.ToString();

            sb.Clear();
            sb.Append("INSERT INTO ");
            sb.Append(cont.ClassAttribute.TableName);
            sb.Append(" (");
            valCount = 0;
            for (int i = 0; i < cont.NameTable.Length; ++i)
                if (!cont.PropAttributes[cont.NameTable[i]].AutoGenerated)
                {
                    if (valCount++ > 0) sb.Append(',');
                    sb.Append(cont.NameTable[i]);
                }
            sb.Append(") VALUES (");
            for (int i = 0; i < valCount; ++i)
            {
                if (i != 0) sb.Append(',');
                sb.Append('?');
            }
            sb.Append(")");
            cont.addQuery = sb.ToString();
        }

        private string getPropType(PropertyInfo p)
        {
            if (p.PropertyType.IsAssignableFrom(typeof(bool?))) return "bo?";
            if (p.PropertyType.IsAssignableFrom(typeof(short?))) return "sh?";
            if (p.PropertyType.IsAssignableFrom(typeof(int?))) return "in?";
            if (p.PropertyType.IsAssignableFrom(typeof(long?))) return "lo?";
            if (p.PropertyType.IsAssignableFrom(typeof(float?))) return "fl?";
            if (p.PropertyType.IsAssignableFrom(typeof(double?))) return "do?";
            if (p.PropertyType.IsAssignableFrom(typeof(char?))) return "ch?";
            if (p.PropertyType.IsAssignableFrom(typeof(DateTime?))) return "Da?";
            if (p.PropertyType.IsAssignableFrom(typeof(Guid?))) return "Gu?";
            if (p.PropertyType.IsAssignableFrom(typeof(string))) return "str";
            if (p.PropertyType.IsAssignableFrom(typeof(bool))) return "boo";
            if (p.PropertyType.IsAssignableFrom(typeof(short))) return "sho";
            if (p.PropertyType.IsAssignableFrom(typeof(int))) return "int";
            if (p.PropertyType.IsAssignableFrom(typeof(long))) return "lon";
            if (p.PropertyType.IsAssignableFrom(typeof(float))) return "flo";
            if (p.PropertyType.IsAssignableFrom(typeof(double))) return "dou";
            if (p.PropertyType.IsAssignableFrom(typeof(char))) return "cha";
            if (p.PropertyType.IsAssignableFrom(typeof(DateTime))) return "Dat";
            if (p.PropertyType.IsAssignableFrom(typeof(Guid))) return "Gui";
            if (Nullable.GetUnderlyingType(p.PropertyType)?.IsEnum ?? false) return "en?";
            if (p.PropertyType.IsEnum) return "enu";
            return null;
        }

        private void LoadInfo(Type type)
        {
            var cont = new InfoContainer();
            cont.ClassType = type;
            cont.ClassAttribute = type.GetCustomAttribute<DbClassAttribute>();
            if (cont.ClassAttribute == null) throw new TypeAccessException("type doesn't contains DbClassAttribute");
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = prop.GetCustomAttribute<DbPropAttribute>();
                if (attr == null) continue;
                if (attr.DbPropName == null) attr.DbPropName = prop.Name;
                cont.Properties[attr.DbPropName] = prop;
                cont.PropAttributes[attr.DbPropName] = attr;
                cont.PropertyType[attr.DbPropName] = getPropType(prop);
                cont.SetMethods[attr.DbPropName] = Helper.FastInvoke.BuildUntypedSetter<object>(prop);
                cont.GetMethods[attr.DbPropName] = Helper.FastInvoke.BuildUntypedGetter<object>(prop);
            }
            List<string> nl = new List<string>(cont.PropAttributes.Count), pl = new List<string>(cont.PropAttributes.Count);
            foreach (var e in cont.PropAttributes)
            {
                nl.Add(e.Value.DbPropName);
                if (e.Value.PrimaryKey) pl.Add(e.Value.DbPropName);
            }
            cont.NameTable = nl.ToArray();
            cont.PrimaryKeys = pl.ToArray();
            storedTypes.Add(type, cont);
            LoadQuerys(cont);
        }

        private bool MatchTransfer<Check>(object target, PropertyInfo prop, Func<Check> getValue)
        {
            if (prop.PropertyType.IsAssignableFrom(typeof(Check)))
            {
                prop.SetValue(target, getValue());
                return true;
            }
            else return false;
        }

        private bool MatchTransfer<Check>(object target, PropertyInfo prop, bool resultIsNull, object nullValue, 
            Check defaultNullValue, Func<Check> getValue)
        {
            return MatchTransfer(target, prop, () => resultIsNull ? nullValue is Check ? (Check)nullValue : defaultNullValue : getValue());
        }

        private void FillKnowProp<T>(InfoContainer cont, string name, object target, PropertyInfo prop, bool resultIsNull, object nullValue, T defaultNullValue, Func<T> getValue)
        {
            cont.SetMethods[name](target, resultIsNull ? nullValue is T ? nullValue : defaultNullValue : getValue());
            //prop.SetValue(target, resultIsNull ? nullValue is T ? nullValue : defaultNullValue : getValue());
        }

        Query lastQuery = null;

        private Query GetQuery(InfoContainer cont, string method, DbValue[] keys, Func<string> creator)
        {
            if (CacheQuerys)
            {
                var sb = new StringBuilder();
                sb.Append(method);
                if (keys != null)
                    foreach (var k in keys)
                    {
                        sb.Append(GetMod(k.Comp));
                        sb.Append(k.Key);
                    }
                var key = sb.ToString();
                if (cont.QueryBuffer.ContainsKey(key))
                    return cont.QueryBuffer[key];
                var q = db.Create(creator());
                cont.QueryBuffer.Add(key, q);
                return q;
            }
            else
            {
                lastQuery?.Dispose();
                return lastQuery = db.Create(creator());
            }
        }

        Query getLastInsertKey = null;

        public int LastInsertedId()
        {
            if (getLastInsertKey == null)
                getLastInsertKey = db.Create("SELECT last_insert_rowid()");
            using (var r = getLastInsertKey.ExecuteReader(false))
            {
                r.Read();
                return r.GetInt32(0);
            }
        }

        public T ReadFromDbReader<T>(SQLiteDataReader reader, string prefix = "") where T:new()
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (prefix == null) prefix = "";
            var type = typeof(T);
            if (!storedTypes.ContainsKey(type)) LoadInfo(type);
            var cont = storedTypes[type];
            cont.LoadedCount++;

            var r = Activator.CreateInstance<T>();
            if (r is IDbLoader)
            {
                ((IDbLoader)r).Load(reader, prefix);
                return r;
            }

            int[] cols = new int[cont.NameTable.Length];
            for (int i = 0; i < cols.Length; ++i)
                cols[i] = reader.GetOrdinal(prefix + cont.NameTable[i]);

            for (int i = 0; i<cols.Length; ++i)
            {
                var name = cont.NameTable[i];
                var prop = cont.Properties[name];
                var attr = cont.PropAttributes[name];
                var ind = cols[i];
                
                
                switch (cont.PropertyType[name])
                {
                    case "bo?": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => (bool?)reader.GetBoolean(ind)); break;
                    case "sh?": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => (short?)reader.GetInt16(ind)); break;
                    case "in?": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => (int?)reader.GetInt32(ind)); break;
                    case "lo?": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => (long?)reader.GetInt64(ind)); break;
                    case "fl?": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => (float?)reader.GetFloat(ind)); break;
                    case "do?": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => (double?)reader.GetDouble(ind)); break;
                    case "ch?": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => (char?)reader.GetChar(ind)); break;
                    case "Da?": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => (DateTime?)reader.GetDateTime(ind)); break;
                    case "Gu?": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => (Guid?)reader.GetGuid(ind)); break;
                    case "str": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => reader.GetString(ind)); break;
                    case "boo": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, false, () => reader.GetBoolean(ind)); break;
                    case "sho": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetInt16(ind)); break;
                    case "int": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetInt32(ind)); break;
                    case "lon": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetInt64(ind)); break;
                    case "flo": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetFloat(ind)); break;
                    case "dou": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetDouble(ind)); break;
                    case "cha": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetChar(ind)); break;
                    case "Dat": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, new DateTime(), () => reader.GetDateTime(ind)); break;
                    case "Gui": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, new Guid(), () => reader.GetGuid(ind)); break;
                    case "en?": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => (int?)reader.GetInt32(ind)); break;
                    case "enu": FillKnowProp(cont, name, r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetInt32(ind)); break;

                    default:
                        {
                            if (!MatchTransfer(r, prop, () => reader.IsDBNull(ind) ? null : (bool?)reader.GetBoolean(ind)) &
                                !MatchTransfer(r, prop, () => reader.IsDBNull(ind) ? null : (short?)reader.GetInt16(ind)) &
                                !MatchTransfer(r, prop, () => reader.IsDBNull(ind) ? null : (int?)reader.GetInt32(ind)) &
                                !MatchTransfer(r, prop, () => reader.IsDBNull(ind) ? null : (long?)reader.GetInt64(ind)) &
                                !MatchTransfer(r, prop, () => reader.IsDBNull(ind) ? null : (float?)reader.GetFloat(ind)) &
                                !MatchTransfer(r, prop, () => reader.IsDBNull(ind) ? null : (double?)reader.GetDouble(ind)) &
                                !MatchTransfer(r, prop, () => reader.IsDBNull(ind) ? null : (char?)reader.GetChar(ind)) &
                                !MatchTransfer(r, prop, () => reader.IsDBNull(ind) ? null : (DateTime?)reader.GetDateTime(ind)) &
                                !MatchTransfer(r, prop, () => reader.IsDBNull(ind) ? null : (Guid?)reader.GetGuid(ind)) &
                                !MatchTransfer(r, prop, reader.IsDBNull(ind), attr.NullValue, null, () => reader.GetString(ind)) &
                                !MatchTransfer(r, prop, reader.IsDBNull(ind), attr.NullValue, false, () => reader.GetBoolean(ind)) &
                                !MatchTransfer(r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetInt16(ind)) &
                                !MatchTransfer(r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetInt32(ind)) &
                                !MatchTransfer(r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetInt64(ind)) &
                                !MatchTransfer(r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetFloat(ind)) &
                                !MatchTransfer(r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetDouble(ind)) &
                                !MatchTransfer(r, prop, reader.IsDBNull(ind), attr.NullValue, 0, () => reader.GetChar(ind)) &
                                !MatchTransfer(r, prop, reader.IsDBNull(ind), attr.NullValue, DateTime.Now, () => reader.GetDateTime(ind)) &
                                !MatchTransfer(r, prop, reader.IsDBNull(ind), attr.NullValue, new Guid(), () => reader.GetGuid(ind)))
                                throw new ArgumentException("cannot found matching local type for " + prefix + name);
                        } break;
                }

            }

            return r;
        }

        public T LoadSingle<T>(params DbValue[] keys) where T : new()
        {
            if (keys == null) throw new ArgumentNullException("keys");
            var type = typeof(T);
            if (!storedTypes.ContainsKey(type)) LoadInfo(type);
            var cont = storedTypes[type];
            if (!cont.ClassAttribute.EnableSingleInstances)
                throw new NotSupportedException("the fetch of single instances is not allowed");
            cont.LoadSingle++;

            if (ValidPropertyCheck)
            {
                var kn = new List<string>();
                for (int i = 0; i < keys.Length; ++i)
                {
                    if (!cont.NameTable.Contains(keys[i].Key, lccomp))
                        throw new ArgumentException("key " + keys[i].Key + " doesn't exists in table");
                    kn.Add(keys[i].Key);
                }
                for (int i = 0; i < cont.PrimaryKeys.Length; ++i)
                {
                    if (!kn.Contains(cont.PrimaryKeys[i]))
                        throw new ArgumentException("primary key " + cont.PrimaryKeys[i] + " not setted");
                }
            }

            var q = GetQuery(cont, "LoadSingle", keys, () =>
            {
                var sb = new StringBuilder();
                sb.Append("SELECT ");
                for (int i = 0; i < cont.NameTable.Length; ++i)
                {
                    if (i != 0) sb.Append(", ");
                    sb.Append(cont.NameTable[i]);
                }
                sb.Append(" FROM ");
                sb.Append(cont.ClassAttribute.TableName);
                sb.Append(" WHERE ");
                for (int i = 0; i < keys.Length; ++i)
                {
                    if (i != 0) sb.Append(" AND ");
                    sb.Append(keys[i].Key);
                    sb.Append(GetMod(keys[i].Comp));
                    sb.Append("?");
                }
                if (keys.Length == 0) sb.Append('1');
                return sb.ToString();
            });

            var val = new object[keys.Length];
            for (int i = 0; i < keys.Length; ++i)
            {
                if (keys[i].Value != null && keys[i].Value.GetType().IsEnum)
                    val[i] = (int)keys[i].Value;
                else val[i] = keys[i].Value;
            }

            q.SetValues(val);
            using (var r = q.ExecuteReader(false))
            {
                if (!r.Read()) throw new KeyNotFoundException("entry not found");
                return ReadFromDbReader<T>(r);
            }
        }

        public IEnumerable<T> LoadAll<T>() where T:new()
        {
            var type = typeof(T);
            if (!storedTypes.ContainsKey(type)) LoadInfo(type);
            var cont = storedTypes[type];
            cont.LoadAll++;
            
            using (var q = db.Create(cont.selectAllQuery))
            {
                using (var r = q.ExecuteReader(false))
                {
                    while (r.Read())
                        yield return ReadFromDbReader<T>(r);
                }
            }
        }

        public IEnumerable<T> LoadMatch<T>(params DbValue[] keys) where T : new()
        {
            if (keys == null) throw new ArgumentNullException("keys");
            var type = typeof(T);
            if (!storedTypes.ContainsKey(type)) LoadInfo(type);
            var cont = storedTypes[type];
            cont.LoadMatch++;

            if (ValidPropertyCheck)
            {
                var kn = new List<string>();
                for (int i = 0; i < keys.Length; ++i)
                {
                    if (!cont.NameTable.Contains(keys[i].Key, lccomp))
                        throw new ArgumentException("key " + keys[i].Key + " doesn't exists in table");
                    kn.Add(keys[i].Key);
                }
            }

            var q = GetQuery(cont, "LoadMatch", keys, () =>
            {
                var sb = new StringBuilder();
                sb.Append("SELECT ");
                for (int i = 0; i < cont.NameTable.Length; ++i)
                {
                    if (i != 0) sb.Append(", ");
                    sb.Append(cont.NameTable[i]);
                }
                sb.Append(" FROM ");
                sb.Append(cont.ClassAttribute.TableName);
                sb.Append(" WHERE ");
                for (int i = 0; i < keys.Length; ++i)
                {
                    if (i != 0) sb.Append(" AND ");
                    sb.Append(keys[i].Key);
                    sb.Append(GetMod(keys[i].Comp));
                    sb.Append("?");
                }
                if (keys.Length == 0) sb.Append('1');
                return sb.ToString();
            });

            var val = new object[keys.Length];
            for (int i = 0; i < keys.Length; ++i)
            {
                if (keys[i].Value != null && keys[i].Value.GetType().IsEnum)
                    val[i] = (int)keys[i].Value;
                else val[i] = keys[i].Value;
            }

            q.SetValues(val);
            using (var r = q.ExecuteReader(false))
            {
                while (r.Read())
                    yield return ReadFromDbReader<T>(r);
            }
        }
        
        public int Count<T>(params DbValue[] keys)
        {
            if (keys == null) throw new ArgumentNullException("keys");
            var type = typeof(T);
            if (!storedTypes.ContainsKey(type)) LoadInfo(type);
            var cont = storedTypes[type];
            cont.Count++;

            if (ValidPropertyCheck)
            {
                var kn = new List<string>();
                for (int i = 0; i < keys.Length; ++i)
                {
                    if (!cont.NameTable.Contains(keys[i].Key, lccomp))
                        throw new ArgumentException("key " + keys[i].Key + " doesn't exists in table");
                    kn.Add(keys[i].Key);
                }
            }

            var sb = new StringBuilder();
            sb.Append("SELECT COUNT(*)  FROM ");
            sb.Append(cont.ClassAttribute.TableName);
            sb.Append(" WHERE ");
            var val = new object[keys.Length];
            for (int i = 0; i < keys.Length; ++i)
            {
                if (i != 0) sb.Append(" AND ");
                sb.Append(keys[i].Key);
                sb.Append(GetMod(keys[i].Comp));
                sb.Append("?");
                if (keys[i].Value != null && keys[i].Value.GetType().IsEnum)
                    val[i] = (int)keys[i].Value;
                else val[i] = keys[i].Value;
            }
            if (keys.Length == 0) sb.Append('1');

            using (var q = db.Create(sb.ToString()))
            {
                q.SetValues(val);
                using (var r = q.ExecuteReader(false))
                {
                    r.Read();
                    return r.GetInt32(0);
                }
            }
        }

        public int Update<T>(T value)
        {
            if (value == null) throw new ArgumentNullException("value");
            var type = typeof(T);
            if (!storedTypes.ContainsKey(type)) LoadInfo(type);
            var cont = storedTypes[type];
            cont.Update++;

            var val = new object[cont.NameTable.Length];
            var ind = 0;
            for (int i = 0; i < cont.NameTable.Length; ++i)
                if (!cont.PrimaryKeys.Contains(cont.NameTable[i]))
                {
                    val[ind] = cont.GetMethods[cont.NameTable[i]](value);
                    if (val[ind] != null && val[ind].GetType().IsEnum)
                        val[ind] = (int)val[ind];
                    ind++;
                }
            //val[ind++] = cont.Properties[cont.NameTable[i]].GetValue(value); 
            for (int i = 0; i < cont.PrimaryKeys.Length; ++i)
            {
                val[ind] = cont.GetMethods[cont.PrimaryKeys[i]](value);
                if (val[ind] != null && val[ind].GetType().IsEnum)
                    val[ind] = (int)val[ind];
                ind++;
            }
            //val[ind++] = cont.Properties[cont.PrimaryKeys[i]].GetValue(value);

            using (var q = db.Create(cont.updateQuery))
            {
                q.SetValues(val);
                return q.ExecuteNonQuery();
            }
        }

        public void Add<T>(T value)
        {
            if (value == null) throw new ArgumentNullException("value");
            var type = typeof(T);
            if (!storedTypes.ContainsKey(type)) LoadInfo(type);
            var cont = storedTypes[type];
            cont.Add++;

            var val = new List<object>(cont.NameTable.Length);
            for (int i = 0; i < cont.NameTable.Length; ++i)
                if (!cont.PropAttributes[cont.NameTable[i]].AutoGenerated)
                {
                    var v = cont.GetMethods[cont.NameTable[i]](value);
                    if (v != null && v.GetType().IsEnum)
                        v = (int)v;
                    val.Add(v);
                }
                    //val.Add(cont.Properties[cont.NameTable[i]].GetValue(value));

            using (var q = db.Create(cont.addQuery))
            {
                q.SetValues(val.ToArray());
                q.ExecuteNonQuery();
            }
        }

        public int Delete<T>(params DbValue[] keys)
        {
            if (keys == null) throw new ArgumentNullException("keys");
            var type = typeof(T);
            if (!storedTypes.ContainsKey(type)) LoadInfo(type);
            var cont = storedTypes[type];
            cont.Delete++;

            if (ValidPropertyCheck)
            {
                var kn = new List<string>();
                for (int i = 0; i < keys.Length; ++i)
                {
                    if (!cont.NameTable.Contains(keys[i].Key, lccomp))
                        throw new ArgumentException("key " + keys[i].Key + " doesn't exists in table");
                    kn.Add(keys[i].Key);
                }
            }

            var sb = new StringBuilder();
            sb.Append("DELETE FROM ");
            sb.Append(cont.ClassAttribute.TableName);
            sb.Append(" WHERE ");
            var val = new object[keys.Length];
            for (int i = 0; i < keys.Length; ++i)
            {
                if (i != 0) sb.Append(" AND ");
                sb.Append(keys[i].Key);
                sb.Append(GetMod(keys[i].Comp));
                sb.Append("?");
                if (keys[i].Value != null && keys[i].Value.GetType().IsEnum)
                    val[i] = (int)keys[i].Value;
                else val[i] = keys[i].Value;
            }
            if (keys.Length == 0) sb.Append('1');

            using (var q = db.Create(sb.ToString()))
            {
                q.SetValues(val);
                return q.ExecuteNonQuery();
            }
        }

        string GetMod(DbComp comp)
        {
            switch(comp)
            {
                case DbComp.eq: return "=";
                case DbComp.gt: return ">";
                case DbComp.gteq: return ">=";
                case DbComp.lt: return "<";
                case DbComp.lteq: return "<=";
                case DbComp.neq: return "<>";
                default: return null;
            }
        }

        public void Dispose()
        {
            getLastInsertKey?.Dispose();
            getLastInsertKey = null;
            foreach (var t in storedTypes)
            {
                foreach (var q in t.Value.QueryBuffer)
                    q.Value.Dispose();
                t.Value.QueryBuffer.Clear();
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class DbPropAttribute : Attribute
    {
        public string DbPropName { get; set; }

        public object NullValue { get; private set; }

        public bool PrimaryKey { get; private set; }

        public bool AutoGenerated { get; private set; }

        public DbPropAttribute(string dbPropName = null, bool primaryKey = false, object nullValue = null, bool autoGenerated = false)
        {
            DbPropName = dbPropName;
            NullValue = nullValue;
            PrimaryKey = primaryKey;
            AutoGenerated = autoGenerated;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = true)]
    public sealed class DbClassAttribute : Attribute
    {
        public string TableName { get; private set; }

        public bool EnableSingleInstances { get; private set; }

        public DbClassAttribute(string tableName, bool enableSingleInstances = true)
        {
            TableName = tableName;
            EnableSingleInstances = enableSingleInstances;
        }
    }

    public class DbValue
    {
        public string Key { get; private set; }

        public object Value { get; private set; }

        public DbComp Comp { get; private set; }

        public DbValue(string key, object value, DbComp comp = DbComp.eq)
        {
            Key = key;
            Value = value;
            Comp = comp;
        }

        public static implicit operator DbValue(KeyValuePair<string, object> kvp)
        {
            return new DbValue(kvp.Key, kvp.Value);
        }

        public static implicit operator KeyValuePair<string, object>(DbValue dbValue)
        {
            return new KeyValuePair<string, object>(dbValue.Key, dbValue.Value);
        }
    }

    public interface IDbLoader
    {
        void Load(SQLiteDataReader reader, string prefix);
    }

    public enum DbComp
    {
        eq,
        gt,
        gteq,
        lt,
        lteq,
        neq
    }
}
