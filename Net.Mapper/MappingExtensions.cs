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

        public static void ObjectAssign(this object obj1,object obj2,params string[] exceptions)
        {
            var exceptionSet = new HashSet<string>(exceptions);
            var info1 = obj1.GetType().GetInfo();
            var info2 = obj2.GetType().GetInfo();
            if (info1.PropertySize > info2.PropertySize)
            {
                foreach (var prop2 in info2.GetAllProperties())
                {
                    if (exceptionSet.Contains(prop2.Name)) continue;
                    if (!info1.HasProperty(prop2.Name)) continue;
                    var prop1 = info1[prop2.Name];
                    if (!prop1.Raw.CanWrite) continue;
                    var prop2Value = prop2.GetValue(obj2);
                    if (prop2Value.IsNull()) continue;
                    if (prop2.Type != prop1.Type)
                        prop2Value=prop2Value.As(prop1.Type);
                    prop1.SetValue(obj1, prop2Value);
                }

            } else
            {

                foreach (var prop1 in info1.GetAllProperties())
                {
                    if (exceptionSet.Contains(prop1.Name)) continue;
                    if (!info2.HasProperty(prop1.Name)) continue;
                    var prop2 = info2[prop1.Name];
                    if (!prop1.Raw.CanWrite) continue;
                    var prop2Value = prop2.GetValue(obj2);
                    if (prop2Value.IsNull()) continue;
                    if (prop2.Type != prop1.Type)
                        prop2Value = prop2Value.As(prop1.Type);
                    prop1.SetValue(obj1, prop2Value);
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
