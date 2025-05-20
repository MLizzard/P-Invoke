
namespace ConsoleApp1
{
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
}
