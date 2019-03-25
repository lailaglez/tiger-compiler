using AST;
using Complements;
using Errors;

namespace Semantics
{
    static class SemanticErrors
    {
        //Variable, function or type access errors
        public static Error FunctionNameAlreadyInUse(TigerNode tigerNode, string name)
        {
            return NameAlreadyInUse(tigerNode, name, "function");
        }
        public static Error VariableNameAlreadyInUse(TigerNode tigerNode, string name)
        {
            return NameAlreadyInUse(tigerNode, name, "variable");
        }
        public static Error TypeNameAlreadyInUse(TigerNode tigerNode, string name)
        {
            return NameAlreadyInUse(tigerNode, name, "Type");
        }
        private static Error NameAlreadyInUse(TigerNode tigerNode, string name, string nameType)
        {
            nameType = nameType == "Type" ? nameType : "Variable or function";
            return new Error(tigerNode.Line, tigerNode.Column, $"{nameType} name {name} already in use");
        }
        public static Error NonExistentTypeReference(TigerNode tigerNode, string name)
        {
            return UndefinedReference(tigerNode, name, "type");
        }
        public static Error NonExistentFunctionReference(TigerNode tigerNode, string name)
        {
            return UndefinedReference(tigerNode, name, "function");
        }
        public static Error NonExistentVariableReference(TigerNode tigerNode, string name)
        {
            return UndefinedReference(tigerNode, name, "variable");
        }
        private static Error UndefinedReference(TigerNode tigerNode, string name, string referenceType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"The {referenceType} {name} does not exist in the current context");
        }
        public static Error InvalidImplicitVariableDeclaration(TigerNode tigerNode, TigerType tigerType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Implicit variable declaration cannot assign {tigerType.Name} value");
        }
        public static Error NonAssignableVariable(TigerNode tigerNode, string variableName)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"The variable {variableName} may not be assigned to");
        }

        //Function Errors
        public static Error WrongNumberOfParameters(TigerNode tigerNode, string functionName, int numberOfParameters, int numberOfArguments)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Method {functionName} has {numberOfParameters} parameter(s) but is invoked with {numberOfArguments} argument(s)");
        }
        public static Error RepeatedParameterName(TigerNode tigerNode, string name)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"The parameter {name} is a duplicate");
        }
        public static Error InvalidArgumentType(TigerNode tigerNode, TigerType argumentType, TigerType parameterType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Invalid argument type. Argument type {argumentType} is not assignable to {parameterType}");
        }
        public static Error IncompatibleFunctionReturnTypeBody(TigerNode tigerNode, TigerType returnType, TigerType bodyType)
        {
            return new Error(tigerNode.Line, tigerNode.Column,
                $"Function body and return types are incompatible. Cannot assign {bodyType} to {returnType}");
        }

        //Record Errors
        public static Error WrongNumberOfFields(TigerNode tigerNode, string recordName, int numberOfRequiredFields, int numberOfFields)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Record {recordName} has {numberOfRequiredFields} fields(s) but only {numberOfFields} are provided");
        }
        public static Error InvalidField(TigerNode tigerNode, string fieldName, string recordName)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Invalid field. Record {recordName} does not contain field {fieldName}");
        }
        public static Error InvalidFieldType(TigerNode tigerNode, TigerType expectedType, TigerType actualType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Invalid field type. Expecting {expectedType} type and obtained {actualType} type");
        }
        public static Error WrongFieldPosition(TigerNode tigerNode, string expectedRecordField, string actualRecordField)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Record field init must respect declaration order. Expecting {expectedRecordField} field and " +
                                                              $"obtained {actualRecordField}");
        }
        public static Error DuplicateFieldDeclaration(TigerNode tigerNode, string recordName, string fieldName)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"The record {recordName} already contains a field named {fieldName}");
        }
        public static Error NonRecordType(TigerNode tigerNode, TigerType tigerType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Record type expected. Cannot convert type {tigerType.Name} to record");
        }

        //Array errors
        public static Error NonIntegerArrayLength(TigerNode tigerNode, TigerType tigerType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Array length must be int. Cannot convert type {tigerType.Name} to int");
        }
        public static Error ArrayInitialValueInvalidType(TigerNode tigerNode, TigerType initType, TigerType arrayType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Array initial value and type mismatch. Cannot convert type {initType.Name} to {arrayType}");
        }
        public static Error ArrayIndexerInvalidType(TigerNode tigerNode, TigerType indexerType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Array indexer must be int. Cannot convert type {indexerType.Name} to int");
        }
        public static Error NonArrayType(TigerNode tigerNode, TigerType tigerType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Array type expected. Cannot convert type {tigerType.Name} to array");
        }

        //BinaryNode errors
        public static Error ArithmeticOperandInvalidType(TigerNode tigerNode, TigerType operandType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Arithmetic operand must be int. Cannot convert type {operandType.Name} to int");
        }
        public static Error LogicalOperandInvalidType(TigerNode tigerNode, TigerType operandType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Logical operand must be int. Cannot convert type {operandType.Name} to int");
        }
        public static Error InvalidOrderComparison(TigerNode tigerNode, TigerType t1, TigerType t2)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Invalid order comparison. Both operands must be int or string. Cannot compare {t1} and {t2}");
        }
        public static Error InvalidIdentityComparison(TigerNode tigerNode, TigerType t1, TigerType t2)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Invalid identity comparison. Both operands must be of the same type. Cannot compare {t1} and {t2}");
        }

        //If errors
        public static Error IncompatibleIfElseReturnType(TigerNode tigerNode, TigerType ifReturnType, TigerType elseReturnType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"If and else return types must be the same. {ifReturnType} and {elseReturnType} are not compatible");
        }
        public static Error InvalidIfBodyType(TigerNode tigerNode)
        {
            return InvalidBodyType(tigerNode, "If");
        }
        public static Error InvalidConditionType(TigerNode tigerNode, TigerType conditionType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Condition type must be int. Cannot convert type {conditionType.Name} to int");
        }

        //Loop errors
        public static Error InvalidForBoundType(TigerNode tigerNode, TigerType boundType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"For bound type must be int. Cannot convert type {boundType.Name} to int");
        }
        public static Error InvalidWhileBodyType(TigerNode tigerNode)
        {
            return InvalidBodyType(tigerNode, "While");
        }
        public static Error InvalidForBodyType(TigerNode tigerNode)
        {
            return InvalidBodyType(tigerNode, "For");
        }
        public static Error BreakInIncorrectPostion(TigerNode tigerNode)
        {
            return new Error(tigerNode.Line, tigerNode.Column, "No enclosing loop out of which to break");
        }

        //Other errors
        public static Error InvalidReturnType(TigerNode tigerNode, TigerType t1)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Return type {t1.Name} is not defined in parent scope");
        }
        public static Error InvalidIntegerConstant(TigerNode tigerNode, string text)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"{text} is an invalid integer constant");
        }
        public static Error InvalidAssignType(TigerNode tigerNode, TigerType t1, TigerType t2)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Invalid assignation. Cannot assign type {t1} to {t2}");
        }
        private static Error InvalidBodyType(TigerNode tigerNode, string nodeType)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"{nodeType} body must be void");
        }
        public static Error CircularTypeDefinition(TigerNode tigerNode, string name)
        {
            return new Error(tigerNode.Line, tigerNode.Column, $"Cannot define type {name}. Its definition is circular");
        }
    }
}

