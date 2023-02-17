using System.CommandLine.Invocation;
using System.CommandLine;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Core.Infrastructure.ConsoleApp.Extensions
{
    public static class CommandExtensions
    {
        public static void SetHandler(this Command command, 
            Func<InvocationContext, CancellationToken, Task> handle, 
            Action<InvocationContext> handleCancellation)
        {
            command.SetHandler(async context =>
            {
                var cancellationToken = context.GetCancellationToken();
                try
                {
                    await handle(context, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    handleCancellation(context);
                }
            });
        }
    }
}
