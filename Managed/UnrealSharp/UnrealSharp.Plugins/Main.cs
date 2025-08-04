using LanguageExt.UnitsOfMeasure;
using Microsoft.Build.Locator;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using UnrealSharp.Binds;
using UnrealSharp.Core;
using UnrealSharp.Shared;
using UnrealSharp.Tools;

namespace UnrealSharp.Plugins;

public static class Main
{
    internal static DllImportResolver _dllImportResolver = null!;

    public static readonly AssemblyLoadContext MainLoadContext =
        AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) ??
        AssemblyLoadContext.Default;

    private static readonly int _apiVersion = 1;

    private static ToolLogger? _logger;


    [UnmanagedCallersOnly(EntryPoint = "Start")]
    public static int Start()
    {
        _logger = new ToolLogger("unreal-sharp-plugin");
        return _apiVersion;
    }

    [UnmanagedCallersOnly(EntryPoint = "Shutdown")]
    public static void Shutdown()
    {
        if(_logger != null)
        {
            _logger.Info("UnrealSharp plugin is shutting down.");
            _logger.Dispose();
        }   
    }


    public unsafe static int GetStringLength(nint stringPtr)
    {
        var ptr = (byte*)stringPtr;

        // Find the null terminator
        var length = 0;
        while (*(ptr + length) != 0)
        {
            length++;
        }

        return length;
    }
    [UnmanagedCallersOnly(EntryPoint = "InitializeUnrealSharp")]
    private static unsafe NativeBool InitializeUnrealSharp(nint workingDirectoryPathPtr, nint assemblyPathPtr, PluginsCallbacks* pluginCallbacks, nint bindsCallbacks, nint managedCallbacks)
    {
       
        try
        {
            _logger ??= new ToolLogger("unreal-sharp-plugin");
            

#if !PACKAGE
            string dotnetSdk = DotNetUtilities.GetLatestDotNetSdkPath();
            _logger.Info($"Using .NET SDK: '{dotnetSdk}'");
            MSBuildLocator.RegisterMSBuildPath(dotnetSdk);
#endif

            var serializedData = new Span<byte>((void*)workingDirectoryPathPtr, GetStringLength(workingDirectoryPathPtr));
            var workingDirectoryPath = Encoding.ASCII.GetString(serializedData);
            
            
            _logger.Info($"Working directory: '{workingDirectoryPath}'");
            AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", workingDirectoryPath);

            _logger.Info($"Creating plugin callbacks...");

            // Initialize plugin callbacks
            try
            {
                *pluginCallbacks = PluginsCallbacks.Create(_logger);
                if (pluginCallbacks == null)
                {
                    _logger.Error("Failed to create plugin callbacks.");
                    return NativeBool.False;
                }                
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating plugin callbacks: {ex.Message}");
                return NativeBool.False;
            }
            _logger.Info($"Successfully created plugin callbacks");

            // Initialize native binds callbacks
            _logger.Info($"Initializing native binds...");
            try
            {
                NativeBinds.InitializeNativeBinds(bindsCallbacks);
                
            }
            catch (Exception ex)
            {
                _logger.Error($"Error initializing native binds: {ex.Message}");
                return NativeBool.False;
            }

            _logger.Info($"Initializing managed callbacks...");
            try
            {
                ManagedCallbacks.Initialize(managedCallbacks);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error initializing native binds: {ex.Message}");
                return NativeBool.False;
            }
            _logger.Info($"UnrealSharp successfully setup!");
            LogUnrealSharpPlugins.Log("UnrealSharp successfully setup!");
            return NativeBool.True;
        }
        catch
        {            
            return NativeBool.False;
        }
    }
}
