using Mono.Cecil;

namespace JKMP.Patcher
{
    public interface IPatch
    {
        string Name { get; }
        
        bool RequiresJKMPCore { get; }
        
        bool CheckIsPatched(ModuleDefinition module, ModuleDefinition? coreModule);
        void Patch(ModuleDefinition module, ModuleDefinition? coreModule);
    }
}