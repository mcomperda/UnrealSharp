using Mono.Cecil;
using Mono.Cecil.Pdb;
using System.IO;
using System.Text.Json;
using UnrealSharp.Tools;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver
{
    public class Weaver(WeaverContext context)
    {

        private static readonly JsonSerializerOptions MetadataSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
        };


        private readonly WeaverContext _context = context;
               
        public void Run()
        {
            LoadBindingsAssembly();
            ProcessUserAssemblies();
        }

        private void LoadBindingsAssembly()
        {
            
            var searchPaths = new HashSet<string>();
            foreach (string assemblyPath in _context.Options.AssemblyPaths)
            {
                var file = new FileInfo(PathUtils.StripQuotes(assemblyPath));   
                
                if(!file.Exists)
                {
                    throw new FileNotFoundException($"Could not find assembly at: {file.FullName}");
                }

                if(file.Directory == null)
                {
                    throw new InvalidOperationException($"Assembly path does not have a valid directory: {file.FullName}");
                }

                if(!file.Directory.Exists)
                {
                    throw new DirectoryNotFoundException($"Assembly directory does not exist: {file.Directory.FullName}");
                }
                
                var directory = file.Directory.FullName;

                _context.Importer.AssemblyResolver.AddSearchDirectory(file.Directory.FullName);
                searchPaths.Add(directory);
                _context.Logger.Info($"Added assembly search path: '{directory}'");
            }

        }

        private void ProcessUserAssemblies()
        {
            var outputDirInfo = new DirectoryInfo(PathUtils.StripQuotes(_context.Options.OutputDirectory));
            
            if (!outputDirInfo.Exists)
            {
                _context.Logger.Info($"Creating output directory for weaved assemblies: '{outputDirInfo.FullName}'");
                outputDirInfo.Create();
            }
            else
            {
                _context.Logger.Info($"Processing weaved assemblies into: '{outputDirInfo.FullName}'");
            }

            var userAssemblies = LoadUserAssemblies(_context.Importer.AssemblyResolver);

            _context.Logger.Info($"Found {userAssemblies.Count} user assemblies to process:");
            foreach (var assembly in userAssemblies)
            {
                _context.Logger.Info($" - {assembly.Name.FullName} ({assembly.MainModule.FileName})");
            }

            var orderedUserAssemblies = OrderUserAssembliesByReferences(userAssemblies);

            WriteUnrealSharpMetadataFile(orderedUserAssemblies, outputDirInfo);
            ProcessOrderedUserAssemblies(orderedUserAssemblies, outputDirInfo);
        }

        private void WriteUnrealSharpMetadataFile(ICollection<AssemblyDefinition> orderedAssemblies, DirectoryInfo outputDirectory)
        {
            var unrealSharpMetadata = new UnrealSharpMetadata
            {
                AssemblyLoadingOrder = orderedAssemblies
                    .Select(x => Path.GetFileNameWithoutExtension(x.MainModule.FileName)).ToList(),
            };

            var metaDataContent = JsonSerializer.Serialize(unrealSharpMetadata, MetadataSerializerOptions);
            var fileName = Path.Combine(outputDirectory.FullName, "UnrealSharp.assemblyloadorder.json");
            File.WriteAllText(fileName, metaDataContent);

            _context.Logger.Info($"Wrote UnrealSharp assembly load order metadata to: '{fileName}'");
        }

        private void ProcessOrderedUserAssemblies(ICollection<AssemblyDefinition> assemblies, DirectoryInfo outputDirectory)
        {
            Exception? exception = null;

            foreach (AssemblyDefinition assembly in assemblies)
            {
                if (assembly.Name.FullName == _context.Importer.ProjectGlueAssembly.FullName)
                {
                    _context.Logger.Info($"Skipping assembly '{assembly.Name.FullName}' as it is the project glue assembly.");
                    continue;
                }

                try
                {
                    string outputPath = Path.Combine(outputDirectory.FullName, Path.GetFileName(assembly.MainModule.FileName));
                    StartWeavingAssembly(assembly, outputPath);
                    _context.Importer.WeavedAssemblies.Add(assembly);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    break;
                }
            }

            foreach (AssemblyDefinition assembly in assemblies)
            {
                assembly.Dispose();
            }

            if (exception != null)
            {
                throw new AggregateException("Assembly processing failed", exception);
            }
        }

        private static ICollection<AssemblyDefinition> OrderUserAssembliesByReferences(ICollection<AssemblyDefinition> assemblies)
        {
            HashSet<string> assemblyNames = new HashSet<string>();

            foreach (AssemblyDefinition assembly in assemblies)
            {
                assemblyNames.Add(assembly.FullName);
            }

            List<AssemblyDefinition> result = new List<AssemblyDefinition>(assemblies.Count);
            HashSet<AssemblyDefinition> remaining = new HashSet<AssemblyDefinition>(assemblies);

            // Add assemblies with no references first between the user assemblies.
            foreach (AssemblyDefinition assembly in assemblies)
            {
                bool hasReferenceToUserAssembly = false;
                foreach (AssemblyNameReference? reference in assembly.MainModule.AssemblyReferences)
                {
                    if (!assemblyNames.Contains(reference.FullName))
                    {
                        continue;
                    }

                    hasReferenceToUserAssembly = true;
                    break;
                }

                if (hasReferenceToUserAssembly)
                {
                    continue;
                }

                result.Add(assembly);
                remaining.Remove(assembly);
            }

            do
            {
                bool added = false;

                foreach (AssemblyDefinition assembly in assemblies)
                {
                    if (!remaining.Contains(assembly))
                    {
                        continue;
                    }

                    bool allResolved = true;
                    foreach (AssemblyNameReference? reference in assembly.MainModule.AssemblyReferences)
                    {
                        if (assemblyNames.Contains(reference.FullName))
                        {
                            bool found = false;
                            foreach (AssemblyDefinition addedAssembly in result)
                            {
                                if (addedAssembly.FullName != reference.FullName)
                                {
                                    continue;
                                }

                                found = true;
                                break;
                            }

                            if (found)
                            {
                                continue;
                            }

                            allResolved = false;
                            break;
                        }
                    }

                    if (!allResolved)
                    {
                        continue;
                    }

                    result.Add(assembly);
                    remaining.Remove(assembly);
                    added = true;
                }

                if (added || remaining.Count <= 0)
                {
                    continue;
                }

                foreach (AssemblyDefinition asm in remaining)
                {
                    result.Add(asm);
                }

                break;

            } while (remaining.Count > 0);

            return result;
        }

        private List<AssemblyDefinition> LoadUserAssemblies(IAssemblyResolver resolver)
        {
            var readerParams = new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadSymbols = true,
                SymbolReaderProvider = new PdbReaderProvider(),
            };

            var result = new List<AssemblyDefinition>();

            foreach (var assemblyPath in _context.Options.AssemblyPaths.Select(PathUtils.StripQuotes))
            {
                if (!File.Exists(assemblyPath))
                {
                    throw new FileNotFoundException($"Could not find assembly at: {assemblyPath}");
                }

                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParams);
                result.Add(assembly);
            }

            return result;
        }
    
        private void StartWeavingAssembly(AssemblyDefinition assembly, string assemblyOutputPath)
        {
            _context.Logger.Info($"Weaving assembly '{assembly.Name.FullName}' into folder '{assemblyOutputPath}'");

            void CleanOldFilesAndMoveExistingFiles()
            {
                var pdbOutputFile = new FileInfo(Path.ChangeExtension(assemblyOutputPath, ".pdb"));

                if (!pdbOutputFile.Exists)
                {
                    return;
                }

                var tmpDirectory = Path.Join(Path.GetTempPath(), assembly.Name.Name);
                if (Path.GetPathRoot(tmpDirectory) != Path.GetPathRoot(pdbOutputFile.FullName)) //if the temp directory is on a different drive, move will not work as desired if file is locked since it does a copy for drive boundaries
                {
                    tmpDirectory = Path.Join(Path.GetDirectoryName(assemblyOutputPath), "..", "_Temporary", assembly.Name.Name);
                }

                try
                {
                    if (Directory.Exists(tmpDirectory))
                    {
                        foreach (var file in Directory.GetFiles(tmpDirectory))
                        {
                            File.Delete(file);
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(tmpDirectory);
                    }
                }
                catch
                {
                    //no action needed
                }

                //move the file to an temp folder to prevent write locks in case a debugger is attached to UE which locks the pdb for writes (common strategy). 
                var tmpDestFileName = Path.Join(tmpDirectory, Path.GetFileName(Path.ChangeExtension(Path.GetTempFileName(), ".pdb")));
                File.Move(pdbOutputFile.FullName, tmpDestFileName);
            }

            var cleanupTask = Task.Run(CleanOldFilesAndMoveExistingFiles);
            _context.Importer.ImportCommonTypes(assembly);

            var assemblyMetaData = new ApiMetaData(assembly.Name.Name);
            StartProcessingAssembly(assembly, assemblyMetaData);

            string sourcePath = Path.GetDirectoryName(assembly.MainModule.FileName)!;
            CopyAssemblyDependencies(assemblyOutputPath, sourcePath);

            Task.WaitAll(cleanupTask);

            _context.Logger.Info($"Writing weaved assembly '{assembly.Name.FullName}' to '{assemblyOutputPath}'");


            assembly.Write(assemblyOutputPath, new WriterParameters
            {
                SymbolWriterProvider = new PdbWriterProvider(),
            });

            WriteAssemblyMetaDataFile(assemblyMetaData, assemblyOutputPath);
        }

        private void WriteAssemblyMetaDataFile(ApiMetaData metadata, string outputPath)
        {
            string metaDataContent = JsonSerializer.Serialize(metadata, MetadataSerializerOptions);
            
            string metadataFilePath = Path.ChangeExtension(outputPath, "metadata.json");
            File.WriteAllText(metadataFilePath, metaDataContent);
            _context.Logger.Info($"Writing metadata file for '{metadata.AssemblyName}' to '{metadataFilePath}'");
        }

        private void StartProcessingAssembly(AssemblyDefinition userAssembly, ApiMetaData metadata)
        {
            try
            {
                List<TypeDefinition> classes = [];
                List<TypeDefinition> structs = [];
                List<TypeDefinition> enums = [];
                List<TypeDefinition> interfaces = [];
                List<TypeDefinition> multicastDelegates = [];
                List<TypeDefinition> delegates = [];

                try
                {
                    void RegisterType(List<TypeDefinition> typeDefinitions, TypeDefinition typeDefinition)
                    {
                        typeDefinitions.Add(typeDefinition);
                        _context.Importer.AddGeneratedTypeAttribute(typeDefinition);
                    }

                    foreach (ModuleDefinition? module in userAssembly.Modules)
                    {
                        _context.Logger.Info($"Processing module: {module.Name}");

                        foreach (TypeDefinition? type in module.Types)
                        {
                            _context.Logger.Info($"Processing type: {type.Name}");

                            if (type.IsUClass())
                            {
                                RegisterType(classes, type);
                            }
                            else if (type.IsUEnum())
                            {
                                RegisterType(enums, type);
                            }
                            else if (type.IsUStruct())
                            {
                                RegisterType(structs, type);
                            }
                            else if (type.IsUInterface())
                            {
                                RegisterType(interfaces, type);
                            }
                            else if (type.BaseType != null && type.BaseType.FullName.Contains("UnrealSharp.MulticastDelegate"))
                            {
                                RegisterType(multicastDelegates, type);
                            }
                            else if (type.BaseType != null && type.BaseType.FullName.Contains("UnrealSharp.Delegate"))
                            {
                                RegisterType(delegates, type);
                            }
                            else
                            {
                                _context.Logger.Warning($"Skipping type: {type.Name}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error enumerating types: {ex.Message}");
                    throw;
                }

                // Enums
                if(enums.Count > 0)
                {                    
                    _context.Importer.UnrealEnumProcessor.ProcessEnums(enums, metadata);
                }
                else
                {
                    _context.Logger.Info("No enums found in the assembly.");
                }

                // Interfaces
                if (interfaces.Count > 0)
                {                    
                    _context.Importer.UnrealInterfaceProcessor.ProcessInterfaces(interfaces, metadata);
                }
                else
                {
                    _context.Logger.Info("No interfaces found in the assembly.");
                }

                // Structs
                if (structs.Count > 0)
                {
                    _context.Importer.UnrealStructProcessor.ProcessStructs(structs, metadata, userAssembly);
                }
                else
                {
                    _context.Logger.Info("No structs found in the assembly.");
                }

                // Classes
                if(classes.Count > 0)
                {
                    _context.Importer.UnrealClassProcessor.ProcessClasses(classes, metadata);
                }
                else
                {
                    _context.Logger.Info("No classes found in the assembly.");
                }


                if(multicastDelegates.Count > 0)
                {
                    _context.Importer.UnrealDelegateProcessor.ProcessDelegates(delegates, multicastDelegates, userAssembly, metadata.DelegateMetaData);                    
                }
                else
                {
                    _context.Logger.Info("No multicast delegates found in the assembly.");
                }                                
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error during assembly processing: {ex.Message}");
                throw;
            }
        }

        
        private static void CopyAssemblyDependencies(string destinationPath, string sourcePath)
        {
            var directoryName = Path.GetDirectoryName(destinationPath) ?? throw new InvalidOperationException("Assembly path does not have a valid directory.");

            try
            {
                var destinationDirectory = new DirectoryInfo(directoryName);
                var sourceDirectory = new DirectoryInfo(sourcePath);

                FileUtils.RecursiveFileCopy(sourceDirectory, destinationDirectory);
            }
            catch (Exception ex)
            {
                ErrorEmitter.Error("WeaverError", sourcePath, 0, "Failed to copy dependencies: " + ex.Message);
            }
        }
    }
}
