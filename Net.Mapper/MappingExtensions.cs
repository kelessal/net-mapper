using Net.Extensions;
using Net.Reflection;
using System;
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
        public static bool ObjectAssign(this object obj1,object obj2,params string[] exceptions)
        {
            if (!IsValidObjectAssignObject(obj2)) return false;
            if (!IsValidObjectAssignObject(obj1)) return false;
            var obj1IsDic = obj1 is IDictionary<string, object>; 
            var obj2IsDic = obj2 is IDictionary<string, object>;
            var exceptionSet = obj2IsDic ? exceptions.AsSafeEnumerable()
                .Select(p => p.ToLowerFirstLetter()).ToHashSet()
                :exceptions.AsSafeEnumerable().ToHashSet();
            var isAssignedAny = false;
            if(obj1IsDic && obj2IsDic)
            {
                var obj1Dic=obj1 as IDictionary<string, object>;
                var obj2Dic=obj2 as IDictionary<string, object>;
                foreach (var key in obj2Dic.Keys)
                {
                    var lowKey = key.ToLowerFirstLetter();
                    if (exceptionSet.Contains(lowKey)) continue;
                    if (obj1Dic.GetSafeValue(lowKey) == obj2Dic[key]) continue;
                    obj1Dic[lowKey] = obj2Dic[key];
                    isAssignedAny = true;
                }
            } 
            else if(!obj1IsDic && obj2IsDic)
            {
                var obj2Dic = obj2 as IDictionary<string, object>;
                var typeInfo1 = obj1.GetType().GetInfo();
                foreach (var key in obj2Dic.Keys)
                {
                    var upKey = key.ToUpperFirstLetter();
                    if (exceptionSet.Contains(key) || exceptions.Contains(upKey)) continue;
                    var prop1 = typeInfo1[upKey] ?? typeInfo1[key];
                    if (prop1.IsNull()) continue;
                    if (!prop1.Raw.CanWrite) continue;
                    if (prop1.HasAttribute<IgnoreAssignAttribute>()) continue;
                    var prop2Value=obj2Dic.GetSafeValue(key).As(prop1.Type);
                    var prop1Value=prop1.GetValue(obj1);
                    if(prop1Value.IsLogicalEqual(prop2Value)) continue;
                    prop1.SetValue(obj1,prop2Value);
                    isAssignedAny=true;
                }
                var props = obj2.GetType().GetInfo().GetAllProperties();

            }
            else if(obj1IsDic && !obj2IsDic)
            {
                var obj1Dic = obj1 as IDictionary<string, object>;
                var typeInfo2 = obj2.GetType().GetInfo();
                foreach (var prop2 in typeInfo2.GetAllProperties())
                {
                    var lowKey = prop2.Name.ToUpperFirstLetter();
                    if (exceptionSet.Contains(lowKey) || exceptions.Contains(prop2.Name)) continue;
                    if (prop2.HasAttribute<IgnoreAssignAttribute>()) continue;
                    var prop1Value = obj1Dic.GetSafeValue(lowKey) ?? obj1Dic.GetSafeValue(prop2.Name);
                    var prop2Value = prop2.GetValue(obj2);
                    if(prop1Value == prop2Value) continue;
                    obj1Dic[lowKey]=prop2Value;
                    isAssignedAny = true;
                }

            } else
            {
                var typeInfo1 = obj1.GetType().GetInfo();   
                var typeInfo2 = obj2.GetType().GetInfo();
                foreach (var prop2 in typeInfo2.GetAllProperties())
                {
                    if (exceptionSet.Contains(prop2.Name)) continue;
                    if (prop2.HasAttribute<IgnoreAssignAttribute>()) continue;
                    if (!typeInfo1.HasProperty(prop2.Name)) continue;
                    var propInfo1 = typeInfo1[prop2.Name];
                    if (propInfo1.HasAttribute<IgnoreAssignAttribute>()) continue;
                    var prop1Value = propInfo1.GetValue(obj1);
                    var prop2Value = prop2.GetValue(obj2).As(propInfo1.Type);
                    if (prop1Value.IsLogicalEqual(prop2Value)) continue;
                    propInfo1.SetValue(obj1, prop2Value);
                    isAssignedAny = true;
                }
            }
            return isAssignedAny;

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
