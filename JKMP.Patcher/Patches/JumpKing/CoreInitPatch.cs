using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JKMP.Patcher.Patches.JumpKing
{
    public class CoreInitPatch : IPatch
    {
        public string Name => "Initialize JKMP.Core";

        public bool CheckIsPatched(ModuleDefinition module, ModuleDefinition coreModule)
        {
            var gameType = module.GetType("JumpKing.Program") ?? throw new PatchException("JumpKing.Program type could not be found");
            var initializeMethod = gameType.FindMethod("Run", "JumpKing.Program.Run method could not be found");

            return initializeMethod.ContainsMethodCall("JKMP.Core.JKMP", "Initialize");
        }

        public void Patch(ModuleDefinition module, ModuleDefinition coreModule)
        {
            var gameType = module.GetType("JumpKing.Program") ?? throw new PatchException("JumpKing.Program type could not be found");
            var initializeMethod = gameType.FindMethod("Run", "JumpKing.Program.Run method could not be found");
            var coreInitializeMethod = coreModule.GetType("JKMP.Core.JKMP").FindMethod("Initialize", "JKMP.Initialize could not be found");

            var il = initializeMethod.Body.GetILProcessor();
            var call = il.Create(OpCodes.Call, initializeMethod.Module.ImportReference(coreInitializeMethod));

            il.InsertBefore(initializeMethod.Body.Instructions.First(), call);
        }
    }
}