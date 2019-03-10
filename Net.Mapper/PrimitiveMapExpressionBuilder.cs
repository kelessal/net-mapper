using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Net.Mapper
{
    static class PrimitiveMapExpressionBuilder
    {
        static MethodInfo toStringMi = typeof(Object).GetMethod("ToString");

        public static LambdaExpression Create(TypePair pair)
        {
            var parameter = Expression.Parameter(pair.SrcType,pair.SrcType.Name.ToLowerInvariant());
            if (pair.IsSameTypes)
                return Expression.Lambda(parameter, parameter);
            else if (pair.DestType == typeof(string))
                return Expression.Lambda(Expression.Call(parameter, toStringMi), parameter);
            else
                return Expression.Lambda(Expression.Convert(parameter, pair.DestType), parameter);
        }
    }
}
