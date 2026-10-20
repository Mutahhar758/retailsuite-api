using Retailer.Application.Legacy.SaleSupplies;

namespace Retailer.Application.Common.Interfaces;

public interface IAiScanService : ITransientService
{
    Task<SaleSupplyScanResponse> ScanSaleSupplySheetAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);
}
