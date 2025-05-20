using System.Runtime.InteropServices;

namespace ConsoleApp1
{
    public class Structures
    {
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
    }
}
