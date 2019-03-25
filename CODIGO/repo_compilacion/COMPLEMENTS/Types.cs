using System;

namespace Complements
{
    public abstract class TigerType
    {
        public static readonly TigerType Int = new IntType();
        public static readonly TigerType String = new StringType();
        public static readonly TigerType Nil = new NilType();
        public static readonly TigerType Error = new ErrorType();
        public static readonly TigerType Void = new VoidType();

        public readonly string Name;

        public abstract bool IsNullable { get; }
        public abstract Type Type { get; set; }
        
        protected TigerType(string name)
        {
            Name = name;
        }

        public virtual bool Assignable(TigerType tigerType)
        {
            if (tigerType is SimpleType)
                tigerType = ((SimpleType) tigerType).ActualType;

            return tigerType == this || IsNullable && tigerType is NilType;
        }

        public static bool AreCompatible(TigerType t1, TigerType t2)
        {
            return t1 != null && t2 != null && (t1.Assignable(t2) || t2.Assignable(t1));
        }

        public static bool AreOfSameType(TigerType t1, TigerType t2)
        {
            return t1 != null && t2 != null && t1 == t2;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class IntType : TigerType
    {
        public override bool IsNullable => false;

        public override Type Type
        {
            set { throw new NotImplementedException();  }
            get { return typeof (int); }
        }

        public IntType() : base("int") { }
    }

    public class StringType : TigerType
    {
        public override bool IsNullable => true;

        public override Type Type
        {
            set { throw new NotImplementedException(); }
            get { return typeof(string); }
        }

        public StringType() : base("string") { }
    }

    public class NilType : TigerType
    {
        public override bool IsNullable => false;

        public override Type Type
        {
            set { throw new NotImplementedException();  }
            get { return null; }
        }

        public NilType() : base("nil") { }
    }

    public class VoidType : TigerType
    {
        public override bool IsNullable => false;

        public override Type Type
        {
            set { throw new NotImplementedException(); }
            get { return typeof(void); }
        }

        public VoidType() : base("void") { }
    }

    public class ErrorType : TigerType
    {
        public override bool IsNullable => false;

        public override Type Type
        {
            set { throw new NotImplementedException(); }
            get { return null; }
        }

        public ErrorType() : base("error") { }
    }

    public class ArrayType : TigerType
    {
        public TigerType ContentType { get; set; }

        public override bool IsNullable => true;

        public override Type Type
        {
            set {   throw new NotImplementedException();  }
            get {   return ContentType.Type.MakeArrayType(); }
        }

        public ArrayType(string name, TigerType contentType) : base(name)
        {
            ContentType = contentType;
        }

        public override bool Assignable(TigerType tigerType)
        {
            return tigerType.Name == Name || tigerType is NilType;
        }
    }

    public class RecordType : TigerType
    {
        public RecordField[] RecordFields;
        public readonly string StaticName;

        public override Type Type { get; set; }
        public override bool IsNullable => true;

        public RecordType(string name, string scopeName, RecordField[] recordFields) : base(name)
        {
            RecordFields = recordFields;
            StaticName = scopeName + "_" + name;
        }

        public override bool Assignable(TigerType tigerType)
        {
            return Name == tigerType.Name || tigerType is NilType;
        }
    }

    public class SimpleType : TigerType
    {
        public TigerType ActualType { get; set; }

        public override bool IsNullable => ActualType.IsNullable;

        public override Type Type
        {
            set { throw new NotImplementedException(); }
            get { return ActualType.Type; }
        }

        public SimpleType(string name, TigerType actualType) : base(name)
        {
            ActualType = actualType;
        }

        public override bool Assignable(TigerType tigerType)
        {
            return ActualType.Assignable(tigerType);
        }

        public override string ToString()
        {
            return ActualType.ToString();
        }
    }

    public class RecordField
    {
        public readonly string Name;
        public readonly TigerType TigerType;

        public RecordField(string name, TigerType tigerType)
        {
            Name = name;
            TigerType = tigerType;
        }
    }
}
