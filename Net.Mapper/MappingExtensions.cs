using Net.Extensions;
using Net.Reflection;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Net.Mapper
{
    public static class MappingExtensions
    {
        readonly static ConcurrentDictionary<TypePair, Mapper> Mappers = new ConcurrentDictionary<TypePair, Mapper>();
        readonly static ConcurrentDictionary<TypePair, object> Locks = new ConcurrentDictionary<TypePair, object>();
        internal static bool HasLock(TypePair pair) => Locks.ContainsKey(pair);
        public static Mapper GetMapper(this Type srcType, Type destType)
            => GetMapper(new TypePair(srcType, destType));
        public static Mapper GetMapper(this TypePair pair)
        {
            if (Mappers.ContainsKey(pair)) return Mappers[pair];
            var locker = Locks.GetOrAdd(pair, new object());
            lock (locker)
            {
                if (Mappers.ContainsKey(pair)) return Mappers[pair];
                var mapper = new Mapper(pair);
                Mappers[pair] = mapper;
                Locks.TryRemove(pair, out object existing);
                return mapper;
            }
        }
        public static void RegisterMapper<TSource, TDestination>(Expression<Func<TSource, TDestination>> mapper)
        {
            var typePair = new TypePair(typeof(TSource), typeof(TDestination));
            Mappers[typePair] = new Mapper(typePair, mapper);
        }
        
        static bool IsValidObjectAssignObject(object obj)
        {
            if (obj == null) return false;
            if (obj is IDictionary<string, object>) return true;
            if (obj.GetType().GetInfo().Kind != TypeKind.Complex)
                throw new Exception("Only complex object can be assigned");
            return true;
        }
        public static bool IsLogicalEqual(this object obj1, object obj2, HashSet<string> exceptionSet = default)
        {

            exceptionSet = exceptionSet ?? new HashSet<string>();
            if (obj1 is string str)
            {
                if (str.IsEmpty()) obj1 = null;
            } else if(obj1 is IEnumerable e1)
            {
                if(e1.Cast<object>().IsEmpty())
                    obj1 = null;
            }
            if (obj2 is string str2)
            {
                if (str2.IsEmpty()) obj2 = null;
            }
            else if (obj2 is IEnumerable e2)
            {
                if (e2.Cast<object>().IsEmpty())
                    obj2 = null;
            }
            if (obj1 == null && obj2 == null) return true;
            if (obj1 == null || obj2 == null) return false;
            if (obj1 is ILogicalEquatable lobj1 && obj2 is ILogicalEquatable lobj2) return lobj1.LogicalEquals(obj2) && lobj2.LogicalEquals(obj1);
             if(obj1 is ILogicalEquatable lobj11) return lobj11.LogicalEquals(obj2);
            if (obj2 is ILogicalEquatable lob22) return lob22.LogicalEquals(obj1);
            var info1 = obj1.GetType().GetInfo();
            var info2 = obj2.GetType().GetInfo();
            if (info1.Kind != info2.Kind) return false;
            if (info1.Kind == TypeKind.Unknown || info1.Kind == TypeKind.Primitive)
                return obj1.Equals(obj2);
            if (info1.Kind == TypeKind.Collection)
            {
                var list1 = (obj1 as IEnumerable).Cast<object>().ToArray();
                var list2 = (obj2 as IEnumerable).Cast<object>().ToArray();
                if (list1.Length != list2.Length) return false;
                for (int i = 0; i < list1.Length; i++)
                    if (!list1[i].IsLogicalEqual(list2[i]))
                        return false;
                return true;
            }
            var obj1IsDic = obj1 is IDictionary<string, object>;
            var obj2IsDic = obj2 is IDictionary<string, object>;
            if (obj1IsDic && obj2IsDic)
            {
                var obj1Dic = obj1 as IDictionary<string, object>;
                var obj2Dic = obj2 as IDictionary<string, object>;
                foreach (var key in obj2Dic.Keys)
                {
                    var upKey = key.ToUpperFirstLetter();
                    if (exceptionSet.Contains(key) || exceptionSet.Contains(upKey)) continue;
                    if (!obj1Dic.GetSafeValue(key).IsLogicalEqual(obj2Dic[key])) return false;
                }
                return true;
            }
            else if (obj1IsDic && !obj2IsDic)
            {
                var obj1Dic = obj1 as IDictionary<string, object>;
                foreach (var propInfo2 in info2.GetAllProperties())
                {
                    if (propInfo2.HasAttribute<ObsoleteAttribute>()) continue;
                    if (propInfo2.HasAttribute<IgnoreCompareAttribute>()) continue;
                    var lowKey = propInfo2.Name.ToLowerFirstLetter();
                    if (exceptionSet.Contains(propInfo2.Name) || exceptionSet.Contains(lowKey)) continue;
                    var obj1Value = obj1Dic.GetSafeValue(propInfo2.Name) ?? obj1Dic.GetSafeValue(lowKey);
                    if (!obj1Value.IsLogicalEqual(propInfo2.GetValue(obj2))) return false;

                }
                return true;
            } 
            else if (obj1 is IDictionary customDic1 && obj2 is IDictionary customDic2)
            {
                if(customDic1.Count!=customDic2.Count) return false;
                foreach(var key in customDic2.Keys)
                {
                    if (!customDic1.Contains(key)) return false;
                    if (!IsLogicalEqual(customDic1[key], customDic2[key])) return false;
                }
                return true;
            }
            else 
            {
                foreach (var propInfo2 in info2.GetAllProperties())
                {
                    if (!info1.HasProperty(propInfo2.Name)) continue;
                    var propInfo1 = info1[propInfo2.Name];
                    if (propInfo2.HasAttribute<ObsoleteAttribute>()) continue;
                    if (propInfo2.HasAttribute<IgnoreCompareAttribute>()) continue;
                    if (propInfo1.HasAttribute<ObsoleteAttribute>()) continue;
                    if (propInfo1.HasAttribute<IgnoreCompareAttribute>()) continue;
                    var lowKey = propInfo2.Name.ToLowerFirstLetter();
                    if (exceptionSet.Contains(propInfo2.Name) || exceptionSet.Contains(lowKey)) continue;
                    var obj1Value = propInfo1.GetValue(obj1);
                    if (!obj1Value.IsLogicalEqual(propInfo2.GetValue(obj2))) return false;

                }
                return true;
            }
        }
        public static void ObjectAssign(this object obj1,object obj2,HashSet<string> exceptionSet=default)
        {
            exceptionSet=exceptionSet?? new HashSet<string>();
            if (!IsValidObjectAssignObject(obj2)) return;
            if (!IsValidObjectAssignObject(obj1)) return;
            var obj1IsDic = obj1 is IDictionary<string, object>; 
            var obj2IsDic = obj2 is IDictionary<string, object>;
            if(obj1IsDic && obj2IsDic)
            {
                var obj1Dic=obj1 as IDictionary<string, object>;
                var obj2Dic=obj2 as IDictionary<string, object>;
                foreach (var key in obj2Dic.Keys)
                {
                    var lowKey = key.ToLowerFirstLetter();
                    if (exceptionSet.Contains(lowKey) || exceptionSet.Contains(key)) continue;
                    if (obj1Dic.GetSafeValue(lowKey) == obj2Dic[key]) continue;
                    obj1Dic[lowKey] = obj2Dic[key];
                }
            } 
            else if(!obj1IsDic && obj2IsDic)
            {
                var obj2Dic = obj2 as IDictionary<string, object>;
                var typeInfo1 = obj1.GetType().GetInfo();
                foreach (var key in obj2Dic.Keys)
                {
                    var upKey = key.ToUpperFirstLetter();
                    if (exceptionSet.Contains(key) || exceptionSet.Contains(upKey)) continue;
                    var prop1 = typeInfo1[upKey] ?? typeInfo1[key];
                    if (prop1.IsNull()) continue;
                    if (!prop1.Raw.CanWrite) continue;
                    if (prop1.HasAttribute<ObsoleteAttribute>()) continue;
                    if (prop1.HasAttribute<IgnoreAssignAttribute>()) continue;
                    var prop2Value=obj2Dic.GetSafeValue(key).As(prop1.Type);
                    prop1.SetValue(obj1,prop2Value);
                }

            }
            else if(obj1IsDic && !obj2IsDic)
            {
                var obj1Dic = obj1 as IDictionary<string, object>;
                var typeInfo2 = obj2.GetType().GetInfo();
                foreach (var prop2 in typeInfo2.GetAllProperties())
                {
                    var lowKey = prop2.Name.ToUpperFirstLetter();
                    if (exceptionSet.Contains(lowKey) || exceptionSet.Contains(prop2.Name)) continue;
                    if (prop2.HasAttribute<ObsoleteAttribute>()) continue;
                    if (prop2.HasAttribute<IgnoreAssignAttribute>()) continue;
                    var prop2Value = prop2.GetValue(obj2);
                    obj1Dic[lowKey]=prop2Value;
                }

            } else
            {
                var typeInfo1 = obj1.GetType().GetInfo();   
                var typeInfo2 = obj2.GetType().GetInfo();
                foreach (var prop2 in typeInfo2.GetAllProperties())
                {
                    if (exceptionSet.Contains(prop2.Name)) continue;
                    if (prop2.HasAttribute<ObsoleteAttribute>()) continue;
                    if (prop2.HasAttribute<IgnoreAssignAttribute>()) continue;
                    if (!typeInfo1.HasProperty(prop2.Name)) continue;
                    var propInfo1 = typeInfo1[prop2.Name];
                    if (!propInfo1.Raw.CanWrite) continue;
                    if (propInfo1.HasAttribute<ObsoleteAttribute>()) continue;
                    if (propInfo1.HasAttribute<IgnoreAssignAttribute>()) continue;
                    var prop1Value = propInfo1.GetValue(obj1);
                    var prop2Value = prop2.GetValue(obj2).As(propInfo1.Type);
                    if (prop1Value == prop2Value) continue;
                    propInfo1.SetValue(obj1, prop2Value);
                }
            }

        }
        private static bool isQueryeableSelectFn(MethodInfo mi)
        {
            var parameters = mi.GetParameters();
            if (parameters.Length != 2) return false;
            var secondParameter = parameters[1];
            var genericExpressionArgs = secondParameter.ParameterType.GetGenericArguments();
            if (genericExpressionArgs.Length != 1) return false;
            return genericExpressionArgs[0].GetGenericArguments().Length == 2;
        }

        public static IQueryable MapTo<T>(this IQueryable<T> queryable, Type mappingType)
        {
            var mapper = new TypePair(typeof(T), mappingType).GetMapper();
            var mappingExpression = mapper.LambdaExpression;
            var method = typeof(Queryable)
                .FindMethod("Select", isQueryeableSelectFn, typeof(T), mappingType);
            return method.Invoke(queryable, new object[] { queryable, mappingExpression }) as IQueryable;

        }

        public static object ObjectMap(this object obj,Type mappingType)
        {
            if (obj is null) return null;
            var mapper = new TypePair(obj.GetType(), mappingType).GetMapper();
            return mapper.Map(obj);

        }

        public static bool IsMappableOf(this Type source, Type dest)
        {
            if (source == dest) return true;
            var srcInfo = source.GetInfo();
            var destInfo = source.GetInfo();
            if (srcInfo.Kind != destInfo.Kind) return false;
            return srcInfo.Kind != TypeKind.Unknown;

        }


        public static void DicMapping<TKey, T, TItem, TVal>(this Dictionary<TKey, T> dic,
            IEnumerable<TItem> items,
            Func<TItem, TKey> keyExp,
            Func<T, TVal> valExp,
            Action<TItem, TVal> action)
        {
            foreach (var item in items.Where(p => p != null))
            {
                var key = keyExp(item);
                if (key == null) continue;
                if (!dic.ContainsKey(key)) continue;
                var val = valExp(dic[key]);
                action(item, val);
            }
        }
        public static void DicMapping<TKey, T, TItem, TVal>(this Dictionary<TKey, T> dic,
           IEnumerable<TItem> items,
           Func<TItem, IEnumerable<TKey>> keyExp,
           Func<T, TVal> valExp,
           Action<TItem, IEnumerable<TVal>> action)
        {
            foreach (var item in items.Where(p => p != null))
            {
                var keyList = keyExp(item);
                if (keyList == null) continue;
                List<TVal> result = new List<TVal>();
                foreach (var key in keyList)
                {
                    if (!dic.ContainsKey(key)) continue;
                    var val = valExp(dic[key]);
                    result.Add(val);
                }
                action(item, result);

            }
        }
       
    }
}
