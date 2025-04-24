using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct IntegrationParameters
{
    public double LowerBound;
    public double UpperBound;
    public int NumberOfPoints;
}

public delegate double DoubleDoubleDelegate(double x);

public delegate double sin_function_c(double x);

public delegate double cos_function_c(double x);

public delegate double sin_function_cpp(double x);

public delegate double cos_function_cpp(double x);

public delegate double monte_carlo_integration_c(ref IntegrationParameters params_c,
    [MarshalAs(UnmanagedType.FunctionPtr)] DoubleDoubleDelegate func);

public delegate double monte_carlo_integration_cpp(ref IntegrationParameters params_cpp,
    [MarshalAs(UnmanagedType.FunctionPtr)] DoubleDoubleDelegate func);

public delegate void test_cpp(int times);

public static class NativeConfig
{
    public static readonly Dictionary<string, (string library, Type delegateType)> FunctionMap = new()
    {
        { "sin_function_c", ("C_Library", typeof(sin_function_c)) },
        { "cos_function_c", ("C_Library", typeof(cos_function_c)) },
        { "monte_carlo_integration_c", ("C_Library", typeof(monte_carlo_integration_c)) },
        { "sin_function_cpp", ("C++_Library", typeof(sin_function_cpp)) },
        { "cos_function_cpp", ("C++_Library", typeof(cos_function_cpp)) },
        { "monte_carlo_integration_cpp", ("C++_Library", typeof(monte_carlo_integration_cpp)) },
        { "test_cpp", ("C++_Library", typeof(test_cpp)) }
    };
}
public static class NativeLoader
{
    private static readonly Dictionary<string, IntPtr> _libraryHandles = new();
    private static readonly Dictionary<string, Delegate> _delegates = new();

    public static void Init()
    {
        // Загружаем все библиотеки, указанные в конфиге
        var requiredLibs = new HashSet<string>();
        foreach (var (_, (lib, _)) in NativeConfig.FunctionMap)
            requiredLibs.Add(lib);

        foreach (string lib in requiredLibs)
        {
            string libName = GetPlatformSpecificLibName(lib);
            _libraryHandles[lib] = NativeLibrary.Load(libName);
        }

        // Загружаем функции
        foreach (var (funcName, (lib, delegateType)) in NativeConfig.FunctionMap)
        {
            IntPtr libHandle = _libraryHandles[lib];
            IntPtr funcPtr = NativeLibrary.GetExport(libHandle, funcName);
            var del = Marshal.GetDelegateForFunctionPointer(funcPtr, delegateType);
            _delegates[funcName] = del;
        }
    }

    public static T GetFunction<T>(string name) where T : Delegate
    {
        if (_delegates.TryGetValue(name, out var del))
            return (T)del;
        throw new InvalidOperationException($"Function '{name}' not found.");
    }

    public static void Cleanup()
    {
        foreach (var handle in _libraryHandles.Values)
            NativeLibrary.Free(handle);
        _libraryHandles.Clear();
        _delegates.Clear();
    }

    private static string GetPlatformSpecificLibName(string shortName)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"{shortName}.dll"
            : $"lib{shortName}.so";
    }
}
class Program
{
 

    // Реализация метода Монте-Карло на C#
    static double MonteCarloIntegrationCS(ref IntegrationParameters parameters, Func<double, double> func)
    {
        Random rand = new Random();
        double sum = 0;
        for (int i = 0; i < parameters.NumberOfPoints; i++)
        {
            double x = parameters.LowerBound + (parameters.UpperBound - parameters.LowerBound) * rand.NextDouble();
            sum += func(x);
        }
        return (parameters.UpperBound - parameters.LowerBound) * sum / parameters.NumberOfPoints;
    }

    // Пример функции для интегрирования на C#
    static double SinFunctionCS(double x)
    {
        return Math.Sin(x);
    }

    // Пример еще одной функции для интегрирования на C#
    static double CosFunctionCS(double x)
    {
        return Math.Cos(x);
    }

    static (double, long) MeasureTime(Func<double> func)
    {
        Stopwatch sw = Stopwatch.StartNew();
        double result = func();
        sw.Stop();
        return (result, sw.ElapsedMilliseconds);
    }

    static void Main()
    {
        IntegrationParameters parameters = new IntegrationParameters
        {
            LowerBound = 0.0,
            UpperBound = Math.PI,
            NumberOfPoints = 10000000 // Большое количество точек
        };

        NativeLoader.Init();

        var sin_function_cppp = NativeLoader.GetFunction<sin_function_cpp>("sin_function_cpp");
        var test_cpp = NativeLoader.GetFunction<test_cpp>("test_cpp");

        Console.WriteLine("Замер времени выполнения метода Монте-Карло:");
        Console.WriteLine($"Интегрирование от {parameters.LowerBound} до {parameters.UpperBound} с {parameters.NumberOfPoints} точками.");
        Console.WriteLine();

        // 1. C# - интегрирование Sin
        var (resultCS_Sin, timeCS_Sin) = MeasureTime(
            () => MonteCarloIntegrationCS(ref parameters, SinFunctionCS));
        Console.WriteLine($"C# - Sin(x): Result = {resultCS_Sin:F8}, Time = {timeCS_Sin} ms");

        // 2. C# - интегрирование Cos
        var (resultCS_Cos, timeCS_Cos) = MeasureTime(
            () => MonteCarloIntegrationCS(ref parameters, CosFunctionCS));
        Console.WriteLine($"C# - Cos(x): Result = {resultCS_Cos:F8}, Time = {timeCS_Cos} ms");

        var monteCarloIntegrationCpp = NativeLoader.GetFunction<monte_carlo_integration_cpp>("monte_carlo_integration_cpp");

        // 3. C++ - интегрирование Sin
        var sinFunctionCpp = NativeLoader.GetFunction<sin_function_cpp>("sin_function_cpp");
        var (resultCpp_Sin, timeCpp_Sin) = MeasureTime(
            () => monteCarloIntegrationCpp(ref parameters, new DoubleDoubleDelegate(sinFunctionCpp)));
        Console.WriteLine($"C++ - Sin(x): Result = {resultCpp_Sin:F8}, Time = {timeCpp_Sin} ms");

        // 4. C++ - интегрирование Cos
        var cosFunctionCpp = NativeLoader.GetFunction<cos_function_cpp>("cos_function_cpp");
        var (resultCpp_Cos, timeCpp_Cos) = MeasureTime(
            () => monteCarloIntegrationCpp(ref parameters, new DoubleDoubleDelegate(cosFunctionCpp)));
        Console.WriteLine($"C++ - Cos(x): Result = {resultCpp_Cos:F8}, Time = {timeCpp_Cos} ms");

        var monteCarloIntegrationC = NativeLoader.GetFunction<monte_carlo_integration_c>("monte_carlo_integration_c");

        // 5. C - интегрирование Sin
        var sinFunctionC = NativeLoader.GetFunction<sin_function_c>("sin_function_c");
        var (resultC_Sin, timeC_Sin) = MeasureTime(
            () => monteCarloIntegrationC(ref parameters, new DoubleDoubleDelegate(sinFunctionC)));
        Console.WriteLine($"C - Sin(x): Result = {resultC_Sin:F8}, Time = {timeC_Sin} ms");

        // 6. C - интегрирование Cos
        var cosFunctionC = NativeLoader.GetFunction<cos_function_c>("cos_function_c");
        var (resultC_Cos, timeC_Cos) = MeasureTime(
            () => monteCarloIntegrationC(ref parameters, new DoubleDoubleDelegate(cosFunctionC)));
        Console.WriteLine($"C - Cos(x): Result = {resultC_Cos:F8}, Time = {timeC_Cos} ms");
    }
}