using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct IntegrationParameters
{
    public double LowerBound;
    public double UpperBound;
    public int NumberOfPoints;
}

// Определение структур
public struct IntegrationData
{
    public double a, b;
    public int n;
}

[StructLayout(LayoutKind.Sequential)]
public struct LinearSystem
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public double[] matrix;

    public double this[int row, int col]
    {
        get => matrix[row * 4 + col];
        set => matrix[row * 4 + col] = value;
    }
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

public delegate double newton_method_c(IntegrationData data);

public delegate double simpson_method_c(ref IntegrationData data);

public delegate void gauss_elimination_c(ref LinearSystem system, double[] result);

public delegate double newton_method_cpp(IntegrationData data);

public delegate double simpson_method_cpp(ref IntegrationData data);

public delegate void gauss_elimination_cpp(ref LinearSystem system, double[] result);

public delegate void test_cpp(int times);

public static class NativeConfig
{
    public static readonly Dictionary<string, (string library, Type delegateType)> FunctionMap = new()
    {
        { "sin_function_c", ("C_Library", typeof(sin_function_c)) },
        { "cos_function_c", ("C_Library", typeof(cos_function_c)) },
        { "monte_carlo_integration_c", ("C_Library", typeof(monte_carlo_integration_c)) },
        { "newton_method_c", ("C_Library", typeof(newton_method_c)) },
        { "simpson_method_c", ("C_Library", typeof(simpson_method_c)) },
        { "gauss_elimination_c", ("C_Library", typeof(gauss_elimination_c)) },

        { "sin_function_cpp", ("C++_Library", typeof(sin_function_cpp)) },
        { "cos_function_cpp", ("C++_Library", typeof(cos_function_cpp)) },
        { "monte_carlo_integration_cpp", ("C++_Library", typeof(monte_carlo_integration_cpp)) },
        { "newton_method_cpp", ("C++_Library", typeof(newton_method_cpp)) },
        { "simpson_method_cpp", ("C++_Library", typeof(simpson_method_cpp)) },
        { "gauss_elimination_cpp", ("C++_Library", typeof(gauss_elimination_cpp)) },
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
    // Реализация функций на C#
    static double NewtonMethodCS(IntegrationData data)
    {
        double x = (data.a + data.b) / 2;
        for (int i = 0; i < data.n; i++)
        {
            x = x - (x * x - 2) / (2 * x);
        }
        return x;
    }

    static double SimpsonMethodCS(ref IntegrationData data)
    {
        double a = data.a, b = data.b;
        int n = data.n;
        double h = (b - a) / n;
        double sum = Math.Sin(a) + Math.Sin(b);

        for (int i = 1; i < n; i += 2)
            sum += 4 * Math.Sin(a + i * h);
        for (int i = 2; i < n; i += 2)
            sum += 2 * Math.Sin(a + i * h);

        return (h / 3) * sum;
    }

    static void GaussEliminationCS(ref LinearSystem system, double[] result)
    {
        int size = 3;
        for (int i = 0; i < size; i++)
        {
            for (int j = i + 1; j < size; j++)
            {
                double ratio = system[j, i] / system[i, i];
                for (int k = 0; k <= size; k++)
                    system[j, k] -= ratio * system[i, k];
            }
        }

        for (int i = size - 1; i >= 0; i--)
        {
            result[i] = system[i, size];
            for (int j = i + 1; j < size; j++)
                result[i] -= system[i, j] * result[j];
            result[i] /= system[i, i];
        }
    }


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
    static IntegrationParameters GetIntegrationParameters(IntegrationParameters defaults)
    {
        Console.Write("Хотите ввести параметры интегрирования? (y/n): ");
        string? input = Console.ReadLine();

        if (input?.ToLower() == "y")
        {
            Console.Write("Введите нижнюю границу (a): ");
            double a = double.Parse(Console.ReadLine()!);

            Console.Write("Введите верхнюю границу (b): ");
            double b = double.Parse(Console.ReadLine()!);

            Console.Write("Введите количество точек: ");
            int n = int.Parse(Console.ReadLine()!);

            return new IntegrationParameters { LowerBound = a, UpperBound = b, NumberOfPoints = n };
        }

        Console.WriteLine("Используются значения по умолчанию.");
        return defaults;
    }

    static IntegrationData GetIntegrationData(IntegrationData defaults)
    {
        Console.Write("Хотите ввести свои данные? (y/n): ");
        string? input = Console.ReadLine();

        if (input?.ToLower() == "y")
        {
            Console.Write("Введите нижнюю границу (a): ");
            double a = double.Parse(Console.ReadLine()!);

            Console.Write("Введите верхнюю границу (b): ");
            double b = double.Parse(Console.ReadLine()!);

            Console.Write("Введите количество интервалов (n): ");
            int n = int.Parse(Console.ReadLine()!);

            return new IntegrationData { a = a, b = b, n = n };
        }

        Console.WriteLine("Используются значения по умолчанию.");
        return defaults;
    }

    static LinearSystem GetLinearSystem(LinearSystem defaults)
    {
        Console.Write("Хотите ввести свою систему? (y/n): ");
        string? input = Console.ReadLine();

        if (input?.ToLower() != "y")
        {
            Console.WriteLine("Используется система по умолчанию.");
            return defaults;
        }

        double[] matrix = new double[12];
        Console.WriteLine("Введите коэффициенты системы (3 уравнения, по 4 значения в каждом: A, B, C, D):");
        for (int i = 0; i < 3; i++)
        {
            Console.Write($"Уравнение {i + 1}: ");
            string? line = Console.ReadLine();
            string[] parts = line?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            if (parts.Length != 4)
            {
                Console.WriteLine("Ошибка ввода. Ожидалось 4 значения.");
                i--; // Повтор ввода строки
                continue;
            }

            for (int j = 0; j < 4; j++)
            {
                if (!double.TryParse(parts[j], out matrix[i * 4 + j]))
                {
                    Console.WriteLine("Ошибка ввода числа.");
                    i--; // Повтор строки
                    break;
                }
            }
        }

        return new LinearSystem { matrix = matrix };
    }

    static void Main()
    {
        IntegrationParameters parameters = new IntegrationParameters
        {
            LowerBound = 0.0,
            UpperBound = Math.PI,
            NumberOfPoints = 10000000 // Большое количество точек
        };

        IntegrationData data = new IntegrationData { a = 10.0, b = 20.0, n = 10000000 };
        LinearSystem defaultSystem = new LinearSystem
        {
            matrix = new double[12]
        };

        defaultSystem[0, 0] = 2; defaultSystem[0, 1] = -1; defaultSystem[0, 2] = 1; defaultSystem[0, 3] = 3;
        defaultSystem[1, 0] = 1; defaultSystem[1, 1] = 3; defaultSystem[1, 2] = 2; defaultSystem[1, 3] = 1;
        defaultSystem[2, 0] = 1; defaultSystem[2, 1] = -1; defaultSystem[2, 2] = 2; defaultSystem[2, 3] = 0;

        double[] result = new double[3];

        NativeLoader.Init();

        while (true)
        {
            Console.WriteLine("Выберите алгоритм для тестирования:");
            Console.WriteLine("1 - Monte Carlo");
            Console.WriteLine("2 - Newton Method");
            Console.WriteLine("3 - Simpson Method");
            Console.WriteLine("4 - Gauss Elimination");
            Console.WriteLine("0 - Выход");

            Console.Write("Введите номер: ");
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "0":
                    return;

                case "1":
                    Console.WriteLine("\nВыберите функцию для интегрирования:");
                    Console.WriteLine("1 - sin(x)");
                    Console.WriteLine("2 - cos(x)");
                    Console.Write("Введите номер: ");
                    string funcChoice = Console.ReadLine();

                    IntegrationParameters mcParams = GetIntegrationParameters(parameters);

                    var monteCarloIntegrationCpp = NativeLoader.GetFunction<monte_carlo_integration_cpp>("monte_carlo_integration_cpp");
                    var monteCarloIntegrationC = NativeLoader.GetFunction<monte_carlo_integration_c>("monte_carlo_integration_c");

                    var sinCpp = NativeLoader.GetFunction<sin_function_cpp>("sin_function_cpp");
                    var cosCpp = NativeLoader.GetFunction<cos_function_cpp>("cos_function_cpp");

                    var sinC = NativeLoader.GetFunction<sin_function_c>("sin_function_c");
                    var cosC = NativeLoader.GetFunction<cos_function_c>("cos_function_c");

                    switch (funcChoice)
                    {
                        case "1": // sin(x)
                            Console.WriteLine("\n--- Monte Carlo Integration: sin(x) ---");

                            var (resCSsin, tCSsin) = MeasureTime(() => MonteCarloIntegrationCS(ref mcParams, SinFunctionCS));
                            var (resCppSin, tCppSin) = MeasureTime(() => monteCarloIntegrationCpp(ref mcParams, new DoubleDoubleDelegate(sinCpp)));
                            var (resCSin, tCSin) = MeasureTime(() => monteCarloIntegrationC(ref mcParams, new DoubleDoubleDelegate(sinC)));

                            Console.WriteLine($"C#:  {resCSsin:F8}, Time: {tCSsin} ms");
                            Console.WriteLine($"C++: {resCppSin:F8}, Time: {tCppSin} ms");
                            Console.WriteLine($"C:   {resCSin:F8}, Time: {tCSin} ms");
                            break;

                        case "2": // cos(x)
                            Console.WriteLine("\n--- Monte Carlo Integration: cos(x) ---");

                            var (resCScos, tCScos) = MeasureTime(() => MonteCarloIntegrationCS(ref mcParams, CosFunctionCS));
                            var (resCppCos, tCppCos) = MeasureTime(() => monteCarloIntegrationCpp(ref mcParams, new DoubleDoubleDelegate(cosCpp)));
                            var (resCCos, tCCos) = MeasureTime(() => monteCarloIntegrationC(ref mcParams, new DoubleDoubleDelegate(cosC)));

                            Console.WriteLine($"C#:  {resCScos:F8}, Time: {tCScos} ms");
                            Console.WriteLine($"C++: {resCppCos:F8}, Time: {tCppCos} ms");
                            Console.WriteLine($"C:   {resCCos:F8}, Time: {tCCos} ms");
                            break;

                        default:
                            Console.WriteLine("Неизвестная функция.");
                            break;
                    }
                    break;

                case "2":
                    Console.WriteLine("\n--- Newton Method ---");

                    IntegrationData newtonData = GetIntegrationData(data);

                    var NewtonC = NativeLoader.GetFunction<newton_method_c>("newton_method_c");
                    var NewtonCpp = NativeLoader.GetFunction<newton_method_cpp>("newton_method_cpp");

                    var (nCS, tNCS) = MeasureTime(() => NewtonMethodCS(newtonData));
                    var (nC, tNC) = MeasureTime(() => NewtonC(newtonData));
                    var (nCpp, tNCpp) = MeasureTime(() => NewtonCpp(newtonData));

                    Console.WriteLine($"C#:  {nCS}, Time: {tNCS} ms");
                    Console.WriteLine($"C:   {nC}, Time: {tNC} ms");
                    Console.WriteLine($"C++: {nCpp}, Time: {tNCpp} ms");
                    break;

                case "3":
                    Console.WriteLine("\n--- Simpson Method ---");

                    IntegrationData simpData = GetIntegrationData(data);

                    var SimpsonC = NativeLoader.GetFunction<simpson_method_c>("simpson_method_c");
                    var SimpsonCpp = NativeLoader.GetFunction<simpson_method_cpp>("simpson_method_cpp");

                    var (sCS, tSCS) = MeasureTime(() => SimpsonMethodCS(ref simpData));
                    var (sC, tSC) = MeasureTime(() => SimpsonC(ref simpData));
                    var (sCpp, tSCpp) = MeasureTime(() => SimpsonCpp(ref simpData));

                    Console.WriteLine($"C#:  {sCS}, Time: {tSCS} ms");
                    Console.WriteLine($"C:   {sC}, Time: {tSC} ms");
                    Console.WriteLine($"C++: {sCpp}, Time: {tSCpp} ms");
                    break;

                case "4":
                    Console.WriteLine("\n--- Gauss Elimination ---");

                    var system = GetLinearSystem(defaultSystem);

                    var GaussC = NativeLoader.GetFunction<gauss_elimination_c>("gauss_elimination_c");
                    var GaussCpp = NativeLoader.GetFunction<gauss_elimination_cpp>("gauss_elimination_cpp");

                    Stopwatch sw = Stopwatch.StartNew();
                    GaussEliminationCS(ref system, result);
                    sw.Stop();
                    Console.WriteLine($"C#: [{string.Join(", ", result)}], Time: {sw.ElapsedMilliseconds} ms");

                    sw.Restart();
                    GaussC(ref system, result);
                    sw.Stop();
                    Console.WriteLine($"C:  [{string.Join(", ", result)}], Time: {sw.ElapsedMilliseconds} ms");

                    sw.Restart();
                    GaussCpp(ref system, result);
                    sw.Stop();
                    Console.WriteLine($"C++: [{string.Join(", ", result)}], Time: {sw.ElapsedMilliseconds} ms");
                    break;

                default:
                    Console.WriteLine("Неизвестный выбор.");
                    break;
            }

        }
        NativeLoader.Cleanup();
    }
}