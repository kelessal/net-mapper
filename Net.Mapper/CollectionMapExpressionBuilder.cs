using System.Linq;
using System.Linq.Expressions;
using Net.Reflection;

namespace Net.Mapper
{
    static class CollectionMapExpressionBuilder
    {


        public static LambdaExpression Create(TypePair pair)
        {
            var parameter = Expression.Parameter(pair.SrcType,pair.SrcType.Name.ToLowerInvariant());
            var srcInfo = pair.SrcType.GetInfo();
            var destInfo = pair.DestType.GetInfo();
            var elementPair = new TypePair(srcInfo.ElementTypeInfo.Type, destInfo.ElementTypeInfo.Type);
            if (MappingExtensions.HasLock(elementPair)) return null;
            var mapper = elementPair.GetMapper();
            if (!mapper.CanMappable) return null;
            var miSelect = typeof(Enumerable).FindMethod("Select", p =>
             {
                 var expParam = p.GetParameters()[1];
                 return expParam.ParameterType.GetGenericArguments().Length == 2;
             }, srcInfo.ElementTypeInfo.Type, destInfo.ElementTypeInfo.Type);
            Expression selectExpression = Expression.Call(miSelect,parameter, mapper.LambdaExpression);
            return Expression.Lambda(selectExpression, parameter);
        
        }
    }
}
