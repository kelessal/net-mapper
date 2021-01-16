using Net.Proxy;
using Net.Reflection;
using System;
using System.Linq.Expressions;

namespace Net.Mapper
{
    public class Mapper
    {

        readonly TypePair TypePair;
        public readonly LambdaExpression LambdaExpression;
        private Delegate CompiledDelegate;
        public object Map(object instance)
        {
            if (!this.CanMappable) throw new NotSupportedException($"{this.TypePair} is not mappable");
            if(this.CompiledDelegate == null)
            {
                this.CompiledDelegate = this.LambdaExpression.Compile();
            }
            return this.CompiledDelegate.DynamicInvoke(instance);
        }
        public bool CanMappable { get; private set; }
        internal Mapper(TypePair pair)
        {

            this.TypePair = new TypePair(pair.SrcType, pair.DestType.IsInterface ?
                InterfaceType.GetProxyType(pair.DestType): pair.DestType);
            this.CanMappable = pair.SrcType.IsMappableOf(this.TypePair.DestType);
            if (!this.CanMappable)  return;
            this.LambdaExpression = CreateExpression();
        }
        
       
        internal Mapper(TypePair pair,LambdaExpression expression)
        {
            this.TypePair = pair;
            this.CanMappable = true;
            this.LambdaExpression = expression;
        }

        private LambdaExpression CreateExpression()
        {
            var typeKind = this.TypePair.SrcType.GetTypeKind();
            if (this.TypePair.IsSameTypes)
               return PrimitiveMapExpressionBuilder.Create(this.TypePair);
            switch (typeKind)
            {
                case TypeKind.Primitive:
                   return PrimitiveMapExpressionBuilder.Create(this.TypePair);
                case TypeKind.Complex:
                   return ComplexMapExpressionBuilder.Create(this.TypePair);
                case TypeKind.Collection:
                  return CollectionMapExpressionBuilder.Create(this.TypePair);
                default:
                    return null;
            }
        }
        

    }
}
