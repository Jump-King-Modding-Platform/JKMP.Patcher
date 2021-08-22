using System;
using Mono.Cecil;

namespace JKMP.Patcher.Patches.JumpKing
{
    public abstract class MakeTypePublicPatch : IPatch
    {
        public virtual string Name => $"Change visibility for type '{fullTypeName}' to public";

        private readonly string fullTypeName;

        protected MakeTypePublicPatch(string fullTypeName)
        {
            this.fullTypeName = fullTypeName ?? throw new ArgumentNullException(nameof(fullTypeName));
        }

        public bool CheckIsPatched(ModuleDefinition module, ModuleDefinition coreModule)
        {
            var type = module.GetType(fullTypeName);

            if (type == null)
                throw new InvalidOperationException($"Could not find a type with the name '{fullTypeName}'");

            return type.IsPublic && !type.IsNotPublic;
        }

        public void Patch(ModuleDefinition module, ModuleDefinition coreModule)
        {
            var type = module.GetType(fullTypeName);
            type.IsPublic = true;
            type.IsNotPublic = false;
        }
    }

    public class MakeGameLoopPublicPatch : MakeTypePublicPatch
    {
        public MakeGameLoopPublicPatch() : base("JumpKing.GameManager.GameLoop") { }
    }

    public class MakeIntroStatePublicPatch : MakeTypePublicPatch
    {
        public MakeIntroStatePublicPatch() : base("JumpKing.GameManager.IntroState") { }
    }

    public class MakeGameTitleScreenPublicPatch : MakeTypePublicPatch
    {
        public MakeGameTitleScreenPublicPatch() : base("JumpKing.GameManager.TitleScreen.GameTitleScreen") { }
    }
}