using System.Collections.Generic;
using System.Linq;
using AST;
using Complements;
using Errors;
using Semantics;

namespace COMPLEMENTS
{
    public class TypeResolution
    {
        private readonly TypeDeclarationNode[] TypeDeclarationNodes;
        private readonly Dictionary<string, DisjointSet> Dictionary;
        private readonly DisjointSet[] Sets;
        private readonly TigerScope Scope;
        private readonly Report Report;

        public TypeResolution(TypeDeclarationNode[] typeDeclarationNodes, TigerScope scope, Report report)
        {
            TypeDeclarationNodes = typeDeclarationNodes;
            Dictionary = new Dictionary<string, DisjointSet>();
            Sets = new DisjointSet[TypeDeclarationNodes.Length];
            Scope = scope;
            Report = report;
        }

        public void CreateSets(int declarationIndex)
        {
            var decl = TypeDeclarationNodes[declarationIndex];

            var simpleType = decl as SimpleTypeDeclarationNode;
            if (simpleType != null)
            {
                Sets[declarationIndex] = new DisjointSet(new SimpleType(decl.IdNode.Name, TigerType.Nil));
                if (!Dictionary.ContainsKey(simpleType.TypeAccessNode.IdNode.Name))
                    Dictionary[simpleType.TypeAccessNode.IdNode.Name] = new DisjointSet(simpleType.TypeAccessNode.TigerType);
                Dictionary[decl.IdNode.Name] = Sets[declarationIndex];
            }

            var arrayType = decl as ArrayTypeDeclarationNode;
            if (arrayType != null)
            {
                Sets[declarationIndex] = new DisjointSet(new ArrayType(decl.IdNode.Name, TigerType.Nil), DependencyType.ArrayOf);
                if (!Dictionary.ContainsKey(arrayType.TypeAccessNode.IdNode.Name))
                    Dictionary[arrayType.TypeAccessNode.IdNode.Name] = new DisjointSet(arrayType.TypeAccessNode.TigerType);
                Dictionary[decl.IdNode.Name] = Sets[declarationIndex];
            }

            var recordType = decl as RecordTypeDeclarationNode;
            if (recordType != null)
            {
                Sets[declarationIndex] = new DisjointSet(recordType.RecordTigerType);
                Dictionary[decl.IdNode.Name] = Sets[declarationIndex];
            }
        }

        public void JoinSets()
        {
            foreach (var t in TypeDeclarationNodes)
            {
                var simpleDecl = t as SimpleTypeDeclarationNode;

                if (simpleDecl != null)
                    if (Dictionary.ContainsKey(simpleDecl.TypeAccessNode.IdNode.Name))
                        DisjointSet.AliasOfJoin(Dictionary[simpleDecl.IdNode.Name], Dictionary[simpleDecl.TypeAccessNode.IdNode.Name]);

                var arrayDecl = t as ArrayTypeDeclarationNode;

                if (arrayDecl != null)
                    if (Dictionary.ContainsKey(arrayDecl.TypeAccessNode.IdNode.Name))
                        DisjointSet.ArrayOfJoin(Dictionary[arrayDecl.IdNode.Name], Dictionary[arrayDecl.TypeAccessNode.IdNode.Name]);
            }
        }

        public void SolveTypes()
        {
            for (int i = 0; i < TypeDeclarationNodes.Length; i++)
            {
                if (Sets[i] == null)
                    continue;
                var parentSet = DisjointSet.SetOf(Sets[i]);
                if (parentSet.IsInvalid)
                {
                    Scope.RemoveType(Sets[i].Name);
                    Report.AddError(SemanticErrors.CircularTypeDefinition(TypeDeclarationNodes[i], TypeDeclarationNodes[i].IdNode.Name));
                }
                else
                {
                    DisjointSet.FirstArrayDependency(Sets[i]);
                    if (Sets[i].TigerType is ArrayType)
                        ((ArrayType) Sets[i].TigerType).ContentType = Sets[i].Parent.TigerType;
                    if (Sets[i].TigerType is SimpleType)
                        ((SimpleType) Sets[i].TigerType).ActualType = Sets[i].Parent.TigerType;
                    if (!(TypeDeclarationNodes[i] is RecordTypeDeclarationNode))
                        Scope.CompleteType(Sets[i].Name, Sets[i].TigerType);
                }
            }
        }

        public void SolveRecords()
        {
            for (int i = 0; i < TypeDeclarationNodes.Length; i++)
            {
                var record = TypeDeclarationNodes[i] as RecordTypeDeclarationNode;
                if (record != null)
                {
                    var recordType = Sets[i].TigerType as RecordType;

                    if (recordType != null)
                    {
                        recordType.RecordFields = ((RecordTypeDeclarationNode)TypeDeclarationNodes[i]).FieldDeclarationNodes.Select(f => 
                                                    new RecordField(f.IdNode.Name, Dictionary.ContainsKey(f.TypeNode.IdNode.Name)
                                                                    ? Dictionary[f.TypeNode.IdNode.Name].TigerType
                                                                    : f.TypeNode.TigerType)).ToArray();
                        Scope.CompleteType(recordType.Name, recordType);
                    }
                }
            }
        }
    }

    public class DisjointSet
    {
        private DependencyType DependencyType;

        public readonly TigerType TigerType;
        public DisjointSet Parent;
        public bool IsInvalid;

        public string Name => TigerType.Name;

        public DisjointSet(TigerType tigerType, DependencyType dependencyType = DependencyType.AliasOf)
        {
            TigerType = tigerType;
            Parent = this;
            DependencyType = dependencyType;
        }

        public static DisjointSet SetOf(DisjointSet ds)
        {
            return ds == ds.Parent ? ds : SetOf(ds.Parent);
        }

        public static void FirstArrayDependency(DisjointSet ds)
        {
            var current = ds.Parent;

            while (current != current.Parent)
            {
                if (current.DependencyType == DependencyType.ArrayOf)
                {
                    ds.Parent = current;
                    return;
                }
                current = current.Parent;
            }

            ds.Parent = current;
        }

        public static void AliasOfJoin(DisjointSet ds1, DisjointSet alias)
        {
            var set1 = SetOf(ds1);
            var set2 = SetOf(alias);

            if (set1 == set2)
            {
                set1.IsInvalid = true;
                return;
            }

            ds1.Parent = alias;
            FirstArrayDependency(ds1);
        }

        public static void ArrayOfJoin(DisjointSet array, DisjointSet arrayType)
        {
            var set1 = SetOf(array);
            var set2 = SetOf(arrayType);

            if (set1 == set2)
            {
                set1.IsInvalid = true;
                return;
            }

            array.Parent = arrayType;
            FirstArrayDependency(array);
            array.DependencyType = DependencyType.ArrayOf;
        }

        public override string ToString()
        {
            return Name + (DependencyType == DependencyType.ArrayOf ? " array of " : " alias of ") + Parent.Name;
        }
    }

    public enum DependencyType { AliasOf, ArrayOf }
}
