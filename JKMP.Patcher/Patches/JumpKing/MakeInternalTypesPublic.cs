using System;
using System.Linq;
using Mono.Cecil;

namespace JKMP.Patcher.Patches.JumpKing
{
    public class MakeInternalTypesPublic : IPatch
    {
        public string Name => "Make all internal types public";
        public bool RequiresJKMPCore => false;

        public bool CheckIsPatched(ModuleDefinition module, ModuleDefinition? coreModule)
        {
            return false;
        }

        public void Patch(ModuleDefinition module, ModuleDefinition? coreModule)
        {
            foreach (var type in module.Types)
            {
                if (type.IsPublic && !type.IsNotPublic)
                    continue;

                PatchType(type, null);
            }
        }

        private static void PatchType(TypeDefinition type, TypeDefinition? parentType)
        {
            if (type.IsWindowsRuntime || type.IsRuntimeSpecialName)
                return;

            if (type.IsSpecialName)
                return;
            
            if (type.FullName.StartsWith("<")) // Generated types seem to start with < and end with >
                return;
            
            Console.WriteLine($"Making {type.FullName} public");
            
            type.IsPublic = true;
            type.IsNotPublic = false;
            
            if (parentType != null)
                type.IsNestedPublic = true;
            
            foreach (var subType in type.NestedTypes)
            {
                PatchType(subType, type);
            }
        }
    }
}