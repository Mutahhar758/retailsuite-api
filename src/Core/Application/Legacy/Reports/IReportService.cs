namespace Retailer.Application.Legacy.Reports;

public interface IReportService : ITransientService
{
    Task<List<AccountStatementLineResponse>> GetAccountStatementAsync(AccountStatementFilter filter, CancellationToken cancellationToken);
    Task<byte[]> GetAccountStatementPdfAsync(AccountStatementFilter filter, CancellationToken cancellationToken);
    Task<List<AccountStatementWithDueLineResponse>> GetAccountStatementWithDueAsync(AccountStatementFilter filter, CancellationToken cancellationToken);
    Task<byte[]> GetAccountStatementWithDuePdfAsync(AccountStatementFilter filter, CancellationToken cancellationToken);
    Task<List<BalanceDetailLineResponse>> GetBalanceDetailAsync(BalanceDetailFilter filter, CancellationToken cancellationToken);
    Task<byte[]> GetBalanceDetailPdfAsync(BalanceDetailFilter filter, CancellationToken cancellationToken);
    Task<List<TrialBalanceLineResponse>> GetTrialBalanceAsync(TrialBalanceFilter filter, CancellationToken cancellationToken);
    Task<byte[]> GetTrialBalancePdfAsync(TrialBalanceFilter filter, CancellationToken cancellationToken);
    Task<List<StockLedgerLineResponse>> GetStockLedgerAsync(StockLedgerFilter filter, CancellationToken cancellationToken);
    Task<byte[]> GetStockLedgerPdfAsync(StockLedgerFilter filter, CancellationToken cancellationToken);
    Task<List<StockBalanceLineResponse>> GetStockBalanceAsync(StockBalanceFilter filter, CancellationToken cancellationToken);
    Task<byte[]> GetStockBalancePdfAsync(StockBalanceFilter filter, CancellationToken cancellationToken);
    Task<List<BalanceSheetLineResponse>> GetBalanceSheetAsync(BalanceSheetFilter filter, CancellationToken cancellationToken);
    Task<byte[]> GetBalanceSheetPdfAsync(BalanceSheetFilter filter, CancellationToken cancellationToken);
    Task<List<IncomeSummaryLineResponse>> GetIncomeSummaryAsync(IncomeSummaryFilter filter, CancellationToken cancellationToken);
    Task<byte[]> GetIncomeSummaryPdfAsync(IncomeSummaryFilter filter, CancellationToken cancellationToken);
    Task<CustomerBillResponse> GetCustomerBillAsync(CustomerBillFilter filter, CancellationToken cancellationToken);
    Task<byte[]> GetCustomerBillPdfAsync(CustomerBillFilter filter, CancellationToken cancellationToken);
    Task<List<EnvelopeLineResponse>> GetEnvelopeAsync(EnvelopeFilter filter, CancellationToken cancellationToken);
    Task<SaleBillResponse> GetSaleBillAsync(SaleBillFilter filter, CancellationToken cancellationToken);
    Task<PurchaseBillResponse> GetPurchaseBillAsync(PurchaseBillFilter filter, CancellationToken cancellationToken);
    Task<PurchaseBillResponse> GetPurchaseRetBillAsync(PurchaseBillFilter filter, CancellationToken cancellationToken);
    Task<SaleRetBillResponse> GetSaleRetBillAsync(SaleRetBillFilter filter, CancellationToken cancellationToken);
    Task<PurchaseSupplyComparisonResponse> GetPurchaseSupplyComparisonAsync(PurchaseSupplyComparisonFilter filter, CancellationToken cancellationToken);
    Task<CustomerBalanceRecoveryResponse> GetCustomerBalanceRecoveryAsync(CustomerBalanceRecoveryFilter filter, CancellationToken cancellationToken);
    Task<byte[]> GetCustomerBalanceRecoveryPdfAsync(CustomerBalanceRecoveryFilter filter, CancellationToken cancellationToken);
}
