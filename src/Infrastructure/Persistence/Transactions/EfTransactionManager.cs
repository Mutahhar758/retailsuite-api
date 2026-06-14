using Retailer.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Retailer.Infrastructure.Persistence.Transactions;

public class EfTransactionManager
{
    public IDbContextTransaction? Transaction { get; set; }
}
