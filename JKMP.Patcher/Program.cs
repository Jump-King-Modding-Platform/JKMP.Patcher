using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using JKMP.Patcher.Patches.JumpKing;
using Mono.Cecil;

namespace JKMP.Patcher
{
    internal static class Program
    {
        private class Arguments
        {
            [Option('g', "gamepath", HelpText = "The path to the game. If it's not defined the current directory will be used.")]
            public string? GamePath { get; set; }
            
            [Option("ci", HelpText = "Only used for CI. If you don't know what this is then you don't need it", Required = false, Default = false)]
            public bool CI { get; set; }
        }

        private static Dictionary<string, ICollection<IPatch>> FilePatches { get; } = new()
        {
            {
                "JumpKing.exe",
                new IPatch[]
                {
                    new CoreInitPatch(),
                    new MakeInternalTypesPublic()
                }
            }
        };
        
        static Task Main(string[] stringArgs)
        {
            return Parser.Default.ParseArguments<Arguments>(stringArgs)
                .WithParsedAsync(async args =>
                {
                    args.GamePath ??= Directory.GetCurrentDirectory();

                    if (!args.CI && !File.Exists("JKMP.Core.dll"))
                    {
                        Console.WriteLine("JKMP.Core.dll not found, make sure you extracted all files.");
                        Environment.ExitCode = 1;
                        return;
                    }

                    ModuleDefinition? coreModule = null;

                    if (!args.CI)
                    {
                        coreModule = ModuleDefinition.ReadModule("JKMP.Core.dll");
                    }

                    DefaultAssemblyResolver assemblyResolver = new();
                    assemblyResolver.AddSearchDirectory(args.GamePath);

                    // Create backup directory
                    Directory.CreateDirectory("backup");

                    Console.WriteLine($"Patching game files in '{args.GamePath}'...");

                    if (args.CI)
                    {
                        Console.WriteLine("Patching game files using CI mode");
                    }

                    foreach (var kv in FilePatches)
                    {
                        string filePath = Path.Combine(args.GamePath, kv.Key);

                        if (!File.Exists(filePath))
                        {
                            Console.WriteLine($"Could not find file: '{filePath}'! Skipping...");
                            continue;
                        }

                        // Backup original file
                        string backupFilePath = Path.Combine("backup", kv.Key);

                        if (!File.Exists(backupFilePath))
                        {
                            File.Copy(filePath, backupFilePath);
                            Console.WriteLine($"Created a backup of {kv.Key}");
                        }

                        Console.Write($"Patching {kv.Key}...");

                        using var module = ModuleDefinition.ReadModule(filePath, new ReaderParameters
                        {
                            ReadWrite = true,
                            AssemblyResolver = assemblyResolver
                        });

                        Console.WriteLine($" loaded module {module.Assembly.FullName}");

                        bool hasErrored = false;

                        foreach (IPatch patch in kv.Value)
                        {
                            if (patch.RequiresJKMPCore && args.CI)
                            {
                                Console.WriteLine($"Skipping JKMP dependant patch '{patch.Name}' due to CI flag being set");
                                continue;
                            }

                            if (patch.CheckIsPatched(module, coreModule))
                            {
                                Console.WriteLine($"Skipping previously applied patch '{patch.Name}'");
                            }
                            else
                            {
                                Console.WriteLine($"Applying patch '{patch.Name}'...");

                                try
                                {
                                    patch.Patch(module, coreModule);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($"Failed to apply patch:\n{e}");
                                    hasErrored = true;
                                }
                            }
                        }

                        if (!hasErrored)
                        {
                            Console.Write("Writing changes to original file...");
                            module.Write();
                            Console.WriteLine(" done.");
                            Console.WriteLine("All patches applied successfully!");
                        }
                        else
                        {
                            Environment.ExitCode = 1;
                            Console.WriteLine("One or more errors occured during patching.");
                        }
                    }

                    if (!args.CI)
                    {
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                    }
                });
        }
    }
}