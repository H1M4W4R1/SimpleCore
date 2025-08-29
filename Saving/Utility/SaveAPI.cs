using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Systems.SimpleCore.Saving.Data;
using Systems.SimpleCore.Saving.Data.Enums;
using Systems.SimpleCore.Saving.Data.Markers;
using Systems.SimpleCore.Saving.Data.Transitions;
using Systems.SimpleCore.Saving.Objects.Markers;
using UnityEngine.Assertions;

namespace Systems.SimpleCore.Saving.Utility
{
    /// <summary>
    ///     API Handling data saving
    /// </summary>
    public static class SaveAPI
    {
#region Save/Load

        /// <summary>
        ///     Save file as default file if possible, otherwise as first supported file type.
        /// </summary>
        /// <param name="saveable">Object to save</param>
        /// <param name="instanceFactory">Factory for creating instances of save files</param>
        /// <returns>Save file</returns>
        /// <exception cref="InvalidOperationException">Thrown if no default save file type is provided and no supported file types are declared.</exception>
        [NotNull] public static SaveFileBase Save(
            [NotNull] ISaveable saveable,
            [CanBeNull] Func<Type, object> instanceFactory = null)
        {
            Assert.IsNotNull(saveable, "Saveable cannot be null.");

            Type targetType;
            if (saveable is IHasDefaultSaveFile {DefaultSaveFileType: not null} provider)
                targetType = provider.DefaultSaveFileType;
            else
            {
                IReadOnlyList<Type> supported = saveable.GetAllSupportedFileTypes();
                if (supported == null || supported.Count == 0)
                    throw new InvalidOperationException(
                        "Saveable does not declare any supported save file types and no default is provided.");

                targetType = supported[0];
            }

            return SaveAs(saveable, targetType, instanceFactory);
        }

        /// <summary>
        ///     Save object as specific file type.
        /// </summary>
        /// <param name="saveable">Object to save</param>
        /// <param name="instanceFactory">Factory for creating instances of save files</param>
        /// <typeparam name="TSaveFile">Type of save file</typeparam>
        /// <returns>Save file</returns>
        /// <exception cref="InvalidOperationException">Thrown if the saveable cannot be saved as the given type.</exception>
        [NotNull] public static SaveFileBase SaveAs<TSaveFile>(
            [NotNull] ISaveable saveable,
            [CanBeNull] Func<Type, object> instanceFactory = null)
            where TSaveFile : SaveFileBase => SaveAs(saveable, typeof(TSaveFile), instanceFactory);

        /// <summary>
        ///     Save object as specific file type.
        /// </summary>
        /// <param name="saveable">Object to save</param>
        /// <param name="targetSaveFileType">Type of save file</param>
        /// <param name="instanceFactory">Factory for creating instances of save files</param>
        /// <returns>Save file</returns>
        /// <exception cref="InvalidOperationException">Thrown if the saveable cannot be saved as the given type.</exception>
        [NotNull] public static SaveFileBase SaveAs(
            [NotNull] ISaveable saveable,
            [NotNull] Type targetSaveFileType,
            [CanBeNull] Func<Type, object> instanceFactory = null)
        {
            Assert.IsNotNull(saveable, "Saveable cannot be null.");
            Assert.IsNotNull(targetSaveFileType, "Target save file type cannot be null.");
            Assert.IsTrue(typeof(SaveFileBase).IsAssignableFrom(targetSaveFileType),
                "Target save file type must derive from SaveFileBase.");

            //  If object can be saved exactly as requested, call its Save implementation for that file type.
            IReadOnlyList<Type> supportedTypes = saveable.GetAllSupportedFileTypes();
            if (supportedTypes.Contains(targetSaveFileType))
                return (SaveFileBase) InvokeInterfaceSave(saveable, targetSaveFileType);

            // Otherwise choose best supported type which can be converted to target (shortest path).
            TransitionInfo? bestPath = null;
            Type bestStart = null;
            for (int supportedTypeIndex = 0; supportedTypeIndex < supportedTypes.Count; supportedTypeIndex++)
            {
                Type start = supportedTypes[supportedTypeIndex];
                TransitionInfo path = ComputeTransitionPath(start, targetSaveFileType);
                if (!path.IsPossible) continue;
                if (bestPath != null && path.Steps.Count >= bestPath.Value.Steps.Count) continue;
                bestPath = path;
                bestStart = start;
            }

            // If no path found, throw exception
            if (bestPath == null)
                throw new InvalidOperationException(
                    $"No conversion path found from any supported save-file types [{string.Join(", ", supportedTypes.Select(t => t.Name))}] to requested {targetSaveFileType.Name}.");

            // Create initial save using the start type
            SaveFileBase current = (SaveFileBase) InvokeInterfaceSave(saveable, bestStart);

            // Apply conversion chain
            current = ApplyConversionChain(current, bestPath.Value.Steps, instanceFactory);

            if (!targetSaveFileType.IsInstanceOfType(current))
                throw new InvalidOperationException("Conversion chain did not produce the requested target type.");

            return current;
        }

        /// <summary>
        ///     Load file into saveable object.
        /// </summary>
        /// <param name="saveable">Object to load into</param>
        /// <param name="file">File to load</param>
        /// <param name="fileType">Type of file</param>
        /// <param name="instanceFactory">Factory for creating instances of save files</param>
        /// <exception cref="InvalidOperationException">Thrown if the file cannot be loaded into the object.</exception>
        public static void Load(
            [NotNull] ISaveable saveable,
            [NotNull] SaveFileBase file,
            [NotNull] Type fileType,
            [CanBeNull] Func<Type, object> instanceFactory = null)
        {
            Assert.IsNotNull(saveable, "Saveable object cannot be null");
            Assert.IsNotNull(file, "File object cannot be null");
            Assert.IsNotNull(fileType, "File type cannot be null");
            Assert.IsTrue(typeof(SaveFileBase).IsAssignableFrom(fileType),
                "File type must derive from SaveFileBase.");

            // If the provided runtime file doesn't match the provided fileType, prefer file.GetType() but still allow fileType parameter.
            if (!fileType.IsInstanceOfType(file)) fileType = file.GetType(); // Use actual file type if mismatch


            IReadOnlyList<Type> supportedTypes = saveable.GetAllSupportedFileTypes();

            // If saveable supports file type directly
            if (supportedTypes.Contains(fileType))
            {
                InvokeInterfaceLoad(saveable, fileType, file);
                return;
            }

            // Otherwise find conversion path from incoming fileType to one of supported types
            TransitionInfo? bestPath = null;
            Type bestTargetType = null;
            foreach (Type desired in supportedTypes)
            {
                TransitionInfo path = ComputeTransitionPath(fileType, desired);
                if (!path.IsPossible) continue;
                if (bestPath != null && path.Steps.Count >= bestPath.Value.Steps.Count) continue;
                bestPath = path;
                bestTargetType = desired;
            }

            if (bestPath == null)
                throw new InvalidOperationException(
                    $"No conversion path found from incoming type {fileType.Name} to any of the object's supported file types [{string.Join(", ", supportedTypes.Select(t => t.Name))}].");

            // Apply conversion chain to transform 'file' into desired supported type
            SaveFileBase transformed = ApplyConversionChain(file, bestPath.Value.Steps, instanceFactory);

            // Finally call Load on the object's interface for the resulting type
            InvokeInterfaceLoad(saveable, bestTargetType, transformed);
        }

        /// <summary>
        ///     Load file into saveable object as specific file type.
        /// </summary>
        /// <param name="saveable">Object to load into</param>
        /// <param name="file">File to load</param>
        /// <param name="instanceFactory">Factory for creating instances of save files</param>
        /// <typeparam name="TFile">Type of file</typeparam>
        /// <exception cref="InvalidOperationException">Thrown if the file cannot be loaded into the object.</exception>
        public static void Load<TFile>(
            [NotNull] ISaveable saveable,
            [NotNull] SaveFileBase file,
            [CanBeNull] Func<Type, object> instanceFactory = null)
            where TFile : SaveFileBase
        {
            Load(saveable, file, typeof(TFile), instanceFactory);
        }

        /// <summary>
        ///     Load file into saveable object as default file type.
        /// </summary>
        /// <param name="saveable">Object to load into</param>
        /// <param name="file">File to load</param>
        /// <param name="instanceFactory">Factory for creating instances of save files</param>
        /// <exception cref="InvalidOperationException">Thrown if the file cannot be loaded into the object.</exception>
        public static void Load(
            [NotNull] ISaveable saveable,
            [NotNull] SaveFileBase file,
            [CanBeNull] Func<Type, object> instanceFactory = null)
        {
            Assert.IsNotNull(saveable, "Saveable cannot be null.");
            Assert.IsNotNull(file, "File cannot be null.");

            // Determine desired target type: default provided by object, otherwise first supported type.
            Type desiredTarget;
            if (saveable is IHasDefaultSaveFile {DefaultSaveFileType: not null} provider)
            {
                desiredTarget = provider.DefaultSaveFileType;
            }
            else
            {
                IReadOnlyList<Type> supported = saveable.GetAllSupportedFileTypes();
                if (supported == null || supported.Count == 0)
                    throw new InvalidOperationException(
                        "Saveable does not declare any supported save file types and no default is provided.");

                desiredTarget = supported[0];
            }

            // Perform load operation
            Load(saveable, file, desiredTarget, instanceFactory);
        }

#endregion

#region Conversion and execution of save/load

        /// <summary>
        ///     Applies a conversion chain described by ordered steps. Returns final SaveFileBase instance.
        /// </summary>
        private static SaveFileBase ApplyConversionChain(
            [NotNull] SaveFileBase startingFile,
            [NotNull] IReadOnlyList<SaveFileTransitionStep> steps,
            Func<Type, object> instanceFactory)
        {
            if (startingFile == null) throw new ArgumentNullException(nameof(startingFile));
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            SaveFileBase current = startingFile;

            foreach (SaveFileTransitionStep step in steps)
            {
                if (!step.From.IsInstanceOfType(current))
                    throw new InvalidOperationException(
                        $"Expected a file of type {step.From.FullName} but got {current.GetType().FullName} at step {step}.");

                switch (step.Kind)
                {
                    case SaveFileTransitionKind.Upgrade:
                        current = InvokeUpgrade(step, current, instanceFactory); break;
                    case SaveFileTransitionKind.Downgrade:
                        current = InvokeDowngrade(step, current, instanceFactory); break;
                    default: throw new InvalidOperationException($"Unhandled transition kind {step.Kind}.");
                }
            }

            return current;
        }

        private static SaveFileBase InvokeUpgrade(
            SaveFileTransitionStep step,
            SaveFileBase current,
            Func<Type, object> instanceFactory)
        {
            Type foundInterface = typeof(IUpgradeableSaveFile<,>).MakeGenericType(step.To, step.From);
            Type impl = FindFirstImplementationOfInterface(foundInterface);
            if (impl == null)
                throw new InvalidOperationException(
                    $"No implementation of {foundInterface.FullName} found to perform upgrade {step}.");

            object converter = CreateInstance(impl, instanceFactory);
            // Call GetUpgradedVersion(TFrom)
            MethodInfo method =
                foundInterface.GetMethod(
                    nameof(IUpgradeableSaveFile<SaveFileBase, SaveFileBase>.GetUpgradedVersion));
            if (method == null) return null;

            object result = InvokeInterfaceMethod(converter, foundInterface, method, current);
            return (SaveFileBase) result;
        }

        private static SaveFileBase InvokeDowngrade(
            SaveFileTransitionStep step,
            SaveFileBase current,
            Func<Type, object> instanceFactory)
        {
            Type foundInterface = typeof(IDowngradableSaveFile<,>).MakeGenericType(step.To, step.From);
            Type impl = FindFirstImplementationOfInterface(foundInterface);
            if (impl == null)
                throw new InvalidOperationException(
                    $"No implementation of {foundInterface.FullName} found to perform downgrade {step}.");

            object converter = CreateInstance(impl, instanceFactory);
            MethodInfo method =
                foundInterface.GetMethod(nameof(IDowngradableSaveFile<SaveFileBase, SaveFileBase>
                    .GetDowngradedVersion));
            if (method == null) return null;

            object result = InvokeInterfaceMethod(converter, foundInterface, method, current);
            return (SaveFileBase) result;
        }

        private static object InvokeInterfaceSave([NotNull] ISaveable saveable, Type fileType)
        {
            Type foundInterface = typeof(ISaveable<>).MakeGenericType(fileType);
            MethodInfo method = foundInterface.GetMethod("Save",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException($"ISaveable<{fileType.Name}> does not expose Save() method.");

            return InvokeInterfaceMethod(saveable, foundInterface, method, Array.Empty<object>());
        }

        private static void InvokeInterfaceLoad([NotNull] ISaveable saveable, Type fileType, SaveFileBase file)
        {
            Type foundInterface = typeof(ISaveable<>).MakeGenericType(fileType);
            MethodInfo method = foundInterface.GetMethod("Load",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException(
                    $"ISaveable<{fileType.Name}> does not expose Load(...) method.");

            InvokeInterfaceMethod(saveable, foundInterface, method, new object[] {file});
        }

        private static object InvokeInterfaceMethod(
            [NotNull] object target,
            [NotNull] Type interfaceType,
            [NotNull] MethodInfo interfaceMethod,
            params object[] args)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (interfaceMethod == null) throw new ArgumentNullException(nameof(interfaceMethod));

            // Try to get interface mapping to find the actual target method
            // (handles explicit implementations & non-public)
            InterfaceMapping map = target.GetType().GetInterfaceMap(interfaceType);
            int idx = Array.FindIndex(map.InterfaceMethods, m => MethodsEqual(m, interfaceMethod));
            MethodInfo targetMethod;
            if (idx >= 0 && idx < map.TargetMethods.Length)
            {
                targetMethod = map.TargetMethods[idx];
            }
            else
            {
                // Fallback: try to invoke the interface method info directly (may succeed for public methods)
                targetMethod = interfaceMethod;
            }

            return targetMethod.Invoke(target, args);
        }

        private static bool MethodsEqual([CanBeNull] MethodInfo a, [CanBeNull] MethodInfo b)
        {
            if (a == null || b == null) return false;
            if (a.MetadataToken == b.MetadataToken && a.Module == b.Module) return true;
            if (a.Name != b.Name) return false;
            ParameterInfo[] aParams = a.GetParameters();
            ParameterInfo[] bParams = b.GetParameters();
            if (aParams.Length != bParams.Length) return false;
            for (int i = 0; i < aParams.Length; i++)
            {
                if (aParams[i].ParameterType != bParams[i].ParameterType) return false;
            }

            return true;
        }

        [CanBeNull] private static Type FindFirstImplementationOfInterface(Type constructedInterface)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly asm in assemblies)
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type == null || type.IsAbstract || type.IsInterface) continue;
                    Type[] interfaces = type.GetInterfaces();
                    if (interfaces.Any(i => i == constructedInterface)) return type;
                }
            }

            return null;
        }

        [NotNull] private static object CreateInstance(
            [NotNull] Type implType,
            [CanBeNull] Func<Type, object> instanceFactory)
        {
            Assert.IsNotNull(implType, "Interface implementation was not found.");

            // ReSharper disable once InvertIf
            // Custom instance factory
            if (instanceFactory != null)
            {
                object inst = instanceFactory(implType);
                if (inst == null)
                    throw new InvalidOperationException("Instance factory returned null for converter " +
                                                        implType.FullName);
                return inst;
            }

            // Default: Activator
            return Activator.CreateInstance(implType) ??
                   throw new InvalidOperationException("Unable to create instance of converter " +
                                                       implType.FullName);
        }

#endregion

#region Transition Path Calculation

        /// <summary>
        ///     Adjacency map for all save files, cached locally
        /// </summary>
        [NotNull]
        private static readonly Dictionary<Type, List<(Type To, SaveFileTransitionKind Kind)>> _adjacencyMap =
            new();

        /// <summary>
        ///     Computes the transition path from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>
        ///     save-file types (must derive from <see cref="SaveFileBase"/>)
        /// </summary>
        public static TransitionInfo ComputeTransitionPath<TFrom, TTo>()
            where TFrom : SaveFileBase
            where TTo : SaveFileBase => ComputeTransitionPath(typeof(TFrom), typeof(TTo));

        /// <summary>
        ///     Computes the transition path from <paramref name="fromType"/> to <paramref name="toType"/>.
        ///     Scans loaded assemblies for types deriving from <see cref="SaveFileBase"/> that declare the upgrade/downgrade interfaces.
        /// </summary>
        /// <param name="fromType">Source save-file type (must derive from <see cref="SaveFileBase"/>)</param>
        /// <param name="toType">Target save-file type (must derive from <see cref="SaveFileBase"/>)</param>
        /// <returns>TransitionInfo describing the path, or IsPossible == false if no path exists.</returns>
        public static TransitionInfo ComputeTransitionPath([NotNull] Type fromType, [NotNull] Type toType)
        {
            // Perform necessary checks
            Assert.IsNotNull(fromType, "Source file type is null");
            Assert.IsNotNull(toType, "Target file type is null");

            Assert.IsTrue(typeof(SaveFileBase).IsAssignableFrom(fromType),
                $"{fromType.FullName} does derive from SaveFileBase.");
            Assert.IsTrue(typeof(SaveFileBase).IsAssignableFrom(toType),
                $"{toType.FullName} does derive from SaveFileBase.");

            // We are the same type
            if (fromType == toType)
                return new TransitionInfo(fromType, toType, true, Array.Empty<SaveFileTransitionStep>());

            // Build adjacency graph from discovered interfaces:
            // edges: TFrom -> TTo with kind (Upgrade or Downgrade)
            Dictionary<Type, List<(Type To, SaveFileTransitionKind Kind)>> adjacency =
                GetOrBuildAdjacencyMap();

            // BFS
            Queue<Type> queue = new();
            queue.Enqueue(fromType);

            // Predecessor map: node -> (prevNode, step from prevNode -> node)
            Dictionary<Type, (Type Prev, SaveFileTransitionStep Step)> predecessor = new()
            {
                [fromType] = (null, default)
            };

            bool found = false;

            // Perform BFS
            while (queue.Count > 0 && !found)
            {
                // Dequeue the next node
                Type current = queue.Dequeue();
                if (!adjacency.TryGetValue(current, out List<(Type To, SaveFileTransitionKind Kind)> neighbors))
                    continue;

                // Process neighbors
                for (int neighbourIndex = 0; neighbourIndex < neighbors.Count; neighbourIndex++)
                {
                    (Type To, SaveFileTransitionKind Kind) edge = neighbors[neighbourIndex];
                    Type neighbor = edge.To;
                    if (predecessor.ContainsKey(neighbor)) continue;

                    // Create step and add to predecessor map
                    SaveFileTransitionStep step = new(current, neighbor, edge.Kind);
                    predecessor[neighbor] = (current, step);

                    // Found target type
                    if (neighbor == toType)
                    {
                        found = true;
                        break;
                    }

                    // Enqueue the neighbor to the queue
                    queue.Enqueue(neighbor);
                }
            }

            // Could not find a path
            if (!found) return new TransitionInfo(fromType, toType, false, Array.Empty<SaveFileTransitionStep>());

            // Reconstruct path
            List<SaveFileTransitionStep> reversedSteps = new();
            Type cursor = toType;
            while (predecessor.TryGetValue(cursor, out (Type Prev, SaveFileTransitionStep Step) info) &&
                   info.Prev != null)
            {
                reversedSteps.Add(info.Step);
                cursor = info.Prev;
            }

            // Reverse the steps to get the correct order and return
            reversedSteps.Reverse();
            return new TransitionInfo(fromType, toType, true, reversedSteps);
        }

        /// <summary>
        ///     Gets current or builds new adjacency map.
        /// </summary>
        [NotNull] private static Dictionary<Type, List<(Type To, SaveFileTransitionKind Kind)>>
            GetOrBuildAdjacencyMap()
        {
            // If we have already built the adjacency map, return it
            if (_adjacencyMap is {Count: > 0}) return _adjacencyMap;

            // We must examine loaded assemblies for types deriving from SaveFileBase
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Scan all assemblies in the app domain
            foreach (Assembly assembly in assemblies)
            {
                // Load all types in the assembly
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    continue; // ignore problematic assemblies
                }

                // Handle all types
                for (int index = 0; index < types.Length; index++)
                {
                    // Get the type
                    Type type = types[index];
                    if (type == null) continue;

                    // We only need to look at types implementing the marker interfaces
                    // we do not check for SaveFileBase as we might add converter support in the future
                    Type[] interfaces = type.GetInterfaces();
                    foreach (Type interfaceType in interfaces)
                    {
                        // We only care about generic interfaces
                        if (!interfaceType.IsGenericType) continue;

                        // Get the generic definition and handle the upgrade/downgrade interfaces
                        Type genDef = interfaceType.GetGenericTypeDefinition();
                        if (genDef == typeof(IUpgradeableSaveFile<,>))
                        {
                            Type[] args = interfaceType.GetGenericArguments();
                            Type to = args[0];
                            Type from = args[1];

                            // Add the edge to the adjacency map
                            AddEdge(_adjacencyMap, from, to, SaveFileTransitionKind.Upgrade);
                        }
                        else if (genDef == typeof(IDowngradableSaveFile<,>))
                        {
                            Type[] args = interfaceType.GetGenericArguments();
                            Type to = args[0];
                            Type from = args[1];

                            // Add the edge to the adjacency map
                            AddEdge(_adjacencyMap, from, to, SaveFileTransitionKind.Downgrade);
                        }
                    }
                }
            }

            return _adjacencyMap;
        }

        /// <summary>
        ///     Adds an edge to the adjacency map.
        /// </summary>
        private static void AddEdge(
            [NotNull] Dictionary<Type, List<(Type To, SaveFileTransitionKind Kind)>> adjacency,
            [CanBeNull] Type from,
            [CanBeNull] Type to,
            SaveFileTransitionKind kind)
        {
            // Ignore null types and invalid ones
            if (from == null || to == null) return;

            // Handles checking for SaveFileBase, 
            // we might add converter support in the future, so this will be modified
            if (!typeof(SaveFileBase).IsAssignableFrom(from)) return;
            if (!typeof(SaveFileBase).IsAssignableFrom(to)) return;

            // Add the edge to the adjacency map
            if (!adjacency.TryGetValue(from, out List<(Type To, SaveFileTransitionKind Kind)> list))
            {
                list = new List<(Type To, SaveFileTransitionKind Kind)>();
                adjacency[from] = list;
            }

            // Avoid duplicate edges
            if (!list.Any(e => e.To == to && e.Kind == kind)) list.Add((to, kind));
        }

#endregion
    }
}