using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
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

    // Подключение функций из C
    [DllImport("C_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double newton_method_c(IntegrationData data);

    [DllImport("C_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double simpson_method_c(ref IntegrationData data);

    [DllImport("C_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void gauss_elimination_c(ref LinearSystem system, double[] result);

    // Подключение функций из C++
    [DllImport("C++_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double newton_method_cpp(IntegrationData data);

    [DllImport("C++_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double simpson_method_cpp(ref IntegrationData data);

    [DllImport("C++_Library.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void gauss_elimination_cpp(ref LinearSystem system, double[] result);

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

 

    // Функция для замера времени выполнения
    static (double, long) MeasureTime(Func<double> func)
    {
        Stopwatch sw = Stopwatch.StartNew();
        double result = func();
        sw.Stop();
        return (result, sw.ElapsedMilliseconds);
    }

    static void Main()
    {
        IntegrationData data = new IntegrationData { a = 10.0, b = 20.0, n = 10000000 };
        LinearSystem system = new LinearSystem
        {
            matrix = new double[12]
        };

        // Заполнение матрицы
        system[0, 0] = 2; system[0, 1] = -1; system[0, 2] = 1; system[0, 3] = 3;
        system[1, 0] = 1; system[1, 1] = 3; system[1, 2] = 2; system[1, 3] = 1;
        system[2, 0] = 1; system[2, 1] = -1; system[2, 2] = 2; system[2, 3] = 0;

        double[] result = new double[3];

        Console.WriteLine("Сравнение выполнения функций на C#, C и C++:");

        // Метод Ньютона
        var (csNewton, csTimeNewton) = MeasureTime(() => NewtonMethodCS(data));
        var (cNewton, cTimeNewton) = MeasureTime(() => newton_method_c(data));
        var (cppNewton, cppTimeNewton) = MeasureTime(() => newton_method_cpp(data));

        Console.WriteLine($"Newton C#: {csNewton}, Time: {csTimeNewton} ms");
        Console.WriteLine($"Newton C:  {cNewton}, Time: {cTimeNewton} ms");
        Console.WriteLine($"Newton C++: {cppNewton}, Time: {cppTimeNewton} ms");
        Console.WriteLine();

        // Метод Симпсона
        var (csSimpson, csTimeSimpson) = MeasureTime(() => SimpsonMethodCS(ref data));
        var (cSimpson, cTimeSimpson) = MeasureTime(() => simpson_method_c(ref data));
        var (cppSimpson, cppTimeSimpson) = MeasureTime(() => simpson_method_cpp(ref data));

        Console.WriteLine($"Simpson C#: {csSimpson}, Time: {csTimeSimpson} ms");
        Console.WriteLine($"Simpson C:  {cSimpson}, Time: {cTimeSimpson} ms");
        Console.WriteLine($"Simpson C++: {cppSimpson}, Time: {cppTimeSimpson} ms");
        Console.WriteLine();

        // Метод Гаусса
        Stopwatch sw = Stopwatch.StartNew();
        GaussEliminationCS(ref system, result);
        sw.Stop();
        long csTimeGauss = sw.ElapsedMilliseconds;
        Console.WriteLine($"Gauss C#: [{string.Join(", ", result)}], Time: {csTimeGauss} ms");

        sw.Restart();
        gauss_elimination_c(ref system, result);
        sw.Stop();
        long cTimeGauss = sw.ElapsedMilliseconds;
        Console.WriteLine($"Gauss C:  [{string.Join(", ", result)}], Time: {cTimeGauss} ms");

        sw.Restart();
        gauss_elimination_cpp(ref system, result);
        sw.Stop();
        long cppTimeGauss = sw.ElapsedMilliseconds;
        Console.WriteLine($"Gauss C++: [{string.Join(", ", result)}], Time: {cppTimeGauss} ms");

       
    }
}
