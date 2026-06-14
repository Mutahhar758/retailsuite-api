using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Retailer.Infrastructure.Persistence.Transactions;

namespace Retailer.Host.Middlewares;

internal class EfTransactionMiddleware : IMiddleware
{

    public EfTransactionMiddleware()
    {
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var transactionManager = context.RequestServices.GetService(typeof(EfTransactionManager)) as EfTransactionManager;
        try
        {
            await next(context);
            // Commit transaction if started
            if (transactionManager?.Transaction != null)
            {
                await transactionManager.Transaction.CommitAsync();
                await transactionManager.Transaction.DisposeAsync();
                transactionManager.Transaction = null;
            }
        }
        catch
        {
            // Rollback transaction if started
            if (transactionManager?.Transaction != null)
            {
                await transactionManager.Transaction.RollbackAsync();
                await transactionManager.Transaction.DisposeAsync();
                transactionManager.Transaction = null;
            }

            throw;
        }
    }
}
