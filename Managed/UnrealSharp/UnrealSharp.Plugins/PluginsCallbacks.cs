using System.Reflection;
using System.Runtime.InteropServices;
using UnrealSharp.Core;
using UnrealSharp.Tools;

namespace UnrealSharp.Plugins;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct PluginsCallbacks
{
    public delegate* unmanaged<char*, NativeBool, nint> LoadPlugin;
    public delegate* unmanaged<char*, NativeBool> UnloadPlugin;
    private static ToolLogger? _logger;


    [UnmanagedCallersOnly]
    private static nint ManagedLoadPlugin(char* assemblyPathPtr, NativeBool isCollectible)
    {
        var assemblyPath = new string(assemblyPathPtr);
        _logger?.Info($"Loading plugin from assembly path: '{assemblyPath}'");
        Assembly? newPlugin;
        try
        {
            newPlugin = PluginLoader.LoadPlugin(assemblyPath, isCollectible.ToManagedBool());
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to load plugin from assembly path '{assemblyPath}': {ex.Message}");
            return IntPtr.Zero;
        }   

        if (newPlugin == null)
        {
            _logger?.Error($"Failed to load plugin from assembly. It is null.");
            return IntPtr.Zero;
        };

        return GCHandle.ToIntPtr(GCHandleUtilities.AllocateStrongPointer(newPlugin, newPlugin));
    }

    [UnmanagedCallersOnly]
    private static NativeBool ManagedUnloadPlugin(char* assemblyPath)
    {
        string assemblyPathStr = new(assemblyPath);
        return PluginLoader.UnloadPlugin(assemblyPathStr).ToNativeBool();
    }

   
    public static PluginsCallbacks Create(ToolLogger logger)
    {
        _logger = logger;

        try
        {
            var callback = new PluginsCallbacks
            {
                LoadPlugin = &ManagedLoadPlugin,
                UnloadPlugin = &ManagedUnloadPlugin,
            };
            return callback;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to create plugin callbacks: {ex.Message}");
            throw;
        }                   
    }
}