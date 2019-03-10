using System;

namespace Net.Mapper
{
    public struct TypePair : IEquatable<TypePair>
    {
        public TypePair(Type srcType, Type destType)
        {
            this.SrcType = srcType;
            this.DestType = destType;
        }
        public Type SrcType { get; private set; }
        public Type DestType { get; private set; }
        public bool IsSameTypes => this.SrcType == this.DestType;

        public bool Equals(TypePair other)
            => this.SrcType == other.SrcType && this.DestType == other.DestType;
        public override bool Equals(object obj)
        {
            if (!(obj is TypePair)) return false;
            return this.Equals((TypePair)obj);
        }
        public override int GetHashCode()
          => this.SrcType.GetHashCode() ^ this.DestType.GetHashCode();

        public override string ToString()
        {
            return $"TypePair({this.SrcType.Name},{this.DestType.Name}";
        }
    }
}
