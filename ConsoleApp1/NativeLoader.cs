using System.Runtime.InteropServices;

namespace ConsoleApp1
{
    public static class NativeLoader
    {
        private static readonly Dictionary<string, IntPtr> _libraryHandles = new();
        private static readonly Dictionary<string, Delegate> _delegates = new();
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;

            // Загружаем все библиотеки, указанные в конфиге
            var requiredLibs = new HashSet<string>();
            foreach (var (_, (lib, _)) in NativeConfig.FunctionMap)
                requiredLibs.Add(lib);

            foreach (string lib in requiredLibs)
            {
                LoadLibraryWithRetry(lib);
            }

            // Загружаем функции
            foreach (var (funcName, (lib, delegateType)) in NativeConfig.FunctionMap)
            {
                if (!_libraryHandles.ContainsKey(lib)) continue;

                try
                {
                    IntPtr funcPtr = NativeLibrary.GetExport(_libraryHandles[lib], funcName);
                    var del = Marshal.GetDelegateForFunctionPointer(funcPtr, delegateType);
                    _delegates[funcName] = del;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load function '{funcName}' from library '{lib}': {ex.Message}");
                }
            }

            _initialized = true;
        }

        private static void LoadLibraryWithRetry(string lib)
        {
            while (true)
            {
                try
                {
                    string libName = GetPlatformSpecificLibName(lib);
                    Console.WriteLine($"Attempting to load library: {libName}");
                    _libraryHandles[lib] = NativeLibrary.Load(libName);
                    Console.WriteLine($"Successfully loaded: {libName}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load library '{lib}': {ex.Message}");
                    Console.WriteLine($"Please ensure '{GetPlatformSpecificLibName(lib)}' exists in:");
                    Console.WriteLine($"   - Application directory: {AppDomain.CurrentDomain.BaseDirectory}");
                    Console.WriteLine("Possible solutions:");
                    Console.WriteLine("   1. Check file exists and has correct permissions");
                    Console.WriteLine("   2. Verify architecture (x64/x86) matches");
                    Console.WriteLine("   3. Install required runtime dependencies");
                    Console.WriteLine("\nPress any key to retry or 'Q' to skip this library...");

                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine($"⏩ Skipping library '{lib}'");
                        return;
                    }
                }
            }
        }

        public static T GetFunction<T>(string name) where T : Delegate
        {
            if (_delegates.TryGetValue(name, out var del))
                return (T)del;

            throw new InvalidOperationException($"Function '{name}' not loaded. Reason: " +
                (_initialized ? "function not found in loaded libraries" : "libraries not initialized"));
        }

        private static string GetPlatformSpecificLibName(string shortName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return $"{shortName}.dll";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return $"lib{shortName}.so";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return $"lib{shortName}.dylib";

            throw new PlatformNotSupportedException("Unsupported operating system");
        }

        public static void Cleanup()
        {
            foreach (var handle in _libraryHandles.Values)
                NativeLibrary.Free(handle);
            _libraryHandles.Clear();
            _delegates.Clear();
        }
    }
}
