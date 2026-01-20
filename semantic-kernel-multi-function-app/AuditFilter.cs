using Microsoft.SemanticKernel;

namespace test
{
    public sealed class AuditFilter() : IAutoFunctionInvocationFilter
    {
        public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
        {
            Console.WriteLine($"FILTER INVOKED : {context.Function.Name}");

            // Execution the function
            await next(context);

        }
    }
}