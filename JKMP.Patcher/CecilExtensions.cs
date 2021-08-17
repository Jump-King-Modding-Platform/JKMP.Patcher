using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JKMP.Patcher
{
    internal static class CecilExtensions
    {
        public static MethodDefinition? FindMethod(this TypeDefinition typeDefinition, string methodName)
        {
            return typeDefinition.Methods.FirstOrDefault(method => method.Name == methodName);
        }

        public static MethodDefinition FindMethod(this TypeDefinition typeDefinition, string methodName, string notFoundMessage)
        {
            return FindMethod(typeDefinition, methodName) ?? throw new PatchException(notFoundMessage);
        }

        public static bool ContainsMethodCall(this ILProcessor processor, string fullTypeName, string methodName)
        {
            return processor.Body.Instructions.Any(instr =>
                instr.OpCode == OpCodes.Call &&
                instr.Operand is MethodReference mRef &&
                mRef.DeclaringType.Name == fullTypeName &&
                mRef.Name == methodName
            );
        }

        public static bool ContainsMethodCall(this MethodDefinition method, string fullTypeName, string methodName)
        {
            return method.Body.GetILProcessor().ContainsMethodCall(fullTypeName, methodName);
        }
    }
}