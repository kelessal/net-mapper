using Net.Expressions;
using Net.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Net.Mapper
{
    static class ComplexMapExpressionBuilder
    {
        public static LambdaExpression Create(TypePair pair)
        {
            var parameter = Expression.Parameter(pair.SrcType, pair.SrcType.Name.ToLowerInvariant());
            var srcInfo = pair.SrcType.GetInfo();
            var destInfo = pair.DestType.GetInfo();
            var mappableSourceProperties = srcInfo.GetAllProperties();
            var writableDestinationProperties = destInfo.GetAllProperties().Where(p => p.Raw.CanWrite);
            List<MemberBinding> bindings = new List<MemberBinding>();
            foreach (var destProp in writableDestinationProperties)
            {
                if (destProp.HasAttribute<NoMapAttribute>()) continue;
                var mapPropName = destProp.GetAttribute<PropertyMapAttribute>()?.MappedPropertyName ?? destProp.Name;
                if (!srcInfo.HasProperty(mapPropName)) continue;
                var srcProp = srcInfo[mapPropName];
                var propPair = new TypePair(srcProp.Type, destProp.Type);
                if (MappingExtensions.HasLock(propPair)) continue;
                var mapper = propPair.GetMapper();
                if (mapper == null) continue;
                if (!mapper.CanMappable) continue;
                var srcPropExp = Expression.Property(parameter, srcProp.Raw);
                var lambda = mapper.LambdaExpression.ReplaceParameter(mapper.LambdaExpression.Parameters[0], srcPropExp) as LambdaExpression;
                var newBinding = Expression.Bind(destProp.Raw, lambda.Body);
                bindings.Add(newBinding);
            }
            var newExp = Expression.New(destInfo.Type);
            var memInitExp = Expression.MemberInit(newExp, bindings.ToArray());
            return Expression.Lambda(memInitExp, parameter);
        }
    }
}
