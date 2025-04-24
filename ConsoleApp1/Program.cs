using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public delegate double DoubleDoubleDelegate(double x);

class Program
{
    public struct IntegrationParameters
    {
        public double LowerBound;
        public double UpperBound;
        public int NumberOfPoints;
    }

    [DllImport("C_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double monte_carlo_integration_c(ref IntegrationParameters params_c, [MarshalAs(UnmanagedType.FunctionPtr)] DoubleDoubleDelegate func);

    [DllImport("C_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double sin_function_c(double x);
    [DllImport("C_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double cos_function_c(double x);

    [DllImport("C++_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double monte_carlo_integration_cpp(ref IntegrationParameters params_cpp, [MarshalAs(UnmanagedType.FunctionPtr)] DoubleDoubleDelegate func);

    [DllImport("C++_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double sin_function_cpp(double x);
    [DllImport("C++_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double cos_function_cpp(double x);

    [DllImport("C++_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void test_cpp(int times);

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

        test_cpp(5);

        Console.WriteLine("Замер времени выполнения метода Монте-Карло:");
        Console.WriteLine($"Интегрирование от {parameters.LowerBound} до {parameters.UpperBound} с {parameters.NumberOfPoints} точками.");
        Console.WriteLine();

        // 1. C# - интегрирование Sin
        var (resultCS_Sin, timeCS_Sin) = MeasureTime(() => MonteCarloIntegrationCS(ref parameters, SinFunctionCS));
        Console.WriteLine($"C# - Sin(x): Result = {resultCS_Sin:F8}, Time = {timeCS_Sin} ms");

        // 2. C# - интегрирование Cos
        var (resultCS_Cos, timeCS_Cos) = MeasureTime(() => MonteCarloIntegrationCS(ref parameters, CosFunctionCS));
        Console.WriteLine($"C# - Cos(x): Result = {resultCS_Cos:F8}, Time = {timeCS_Cos} ms");

        // 3. C++ - интегрирование Sin
        var (resultCpp_Sin, timeCpp_Sin) = MeasureTime(() => monte_carlo_integration_cpp(ref parameters, new DoubleDoubleDelegate(sin_function_cpp)));
        Console.WriteLine($"C++ - Sin(x): Result = {resultCpp_Sin:F8}, Time = {timeCpp_Sin} ms");

        // 4. C++ - интегрирование Cos
        var (resultCpp_Cos, timeCpp_Cos) = MeasureTime(() => monte_carlo_integration_cpp(ref parameters, new DoubleDoubleDelegate(cos_function_cpp)));
        Console.WriteLine($"C++ - Cos(x): Result = {resultCpp_Cos:F8}, Time = {timeCpp_Cos} ms");

        // 5. C - интегрирование Sin
        var (resultC_Sin, timeC_Sin) = MeasureTime(() => monte_carlo_integration_c(ref parameters, new DoubleDoubleDelegate(sin_function_c)));
        Console.WriteLine($"C - Sin(x): Result = {resultC_Sin:F8}, Time = {timeC_Sin} ms");

        // 6. C - интегрирование Cos
        var (resultC_Cos, timeC_Cos) = MeasureTime(() => monte_carlo_integration_c(ref parameters, new DoubleDoubleDelegate(cos_function_c)));
        Console.WriteLine($"C - Cos(x): Result = {resultC_Cos:F8}, Time = {timeC_Cos} ms");
    }
}