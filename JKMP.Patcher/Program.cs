﻿using System;
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
        }

        private static Dictionary<string, ICollection<IPatch>> FilePatches { get; set; } = new()
        {
            {
                "JumpKing.exe",
                new[]
                {
                    new CoreInitPatch()
                }
            }
        };
        
        static Task Main(string[] stringArgs)
        {
            return Parser.Default.ParseArguments<Arguments>(stringArgs)
                .WithParsedAsync(async args =>
                {
                    args.GamePath ??= Directory.GetCurrentDirectory();

                    if (!File.Exists("JKMP.Core.dll"))
                    {
                        Console.WriteLine("JKMP.Core.dll not found, make sure you extracted all files.");
                        return;
                    }

                    var coreModule = ModuleDefinition.ReadModule("JKMP.Core.dll");
                    
                    DefaultAssemblyResolver assemblyResolver = new();
                    assemblyResolver.AddSearchDirectory(args.GamePath);
                    
                    // Create backup directory
                    Directory.CreateDirectory("backup");

                    Console.WriteLine($"Patching game files in '{args.GamePath}'...");

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

                        foreach (IPatch patch in kv.Value)
                        {
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
                                }
                            }
                        }

                        Console.Write("Writing changes to original file...");
                        module.Write();
                        Console.WriteLine(" done.");
                    }

                    Console.WriteLine("All patches applied successfully! Exiting in 5 seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                });
        }
    }
}