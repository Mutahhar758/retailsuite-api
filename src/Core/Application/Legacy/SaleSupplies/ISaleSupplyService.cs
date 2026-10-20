using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.SaleSupplies;

public interface ISaleSupplyService : ITransientService
{
    Task<List<SaleSupplyResponse>> GetListAsync(SaleSupplyListFilter filter, CancellationToken cancellationToken);
    Task<List<SaleSupplyLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken);
    Task<string> CreateAsync(SaleSupplyCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, SaleSupplyUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
    Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken);
    Task<List<SaleSupplyLineResponse>> GetCustomerLinesAsync(string customerId, DateOnly? fromDate, DateOnly? toDate, string? itemId, CancellationToken cancellationToken);
    Task UpdateLineAsync(string voucherNo, int seq, SaleSupplyLineRequest request, CancellationToken cancellationToken);
    Task UpdateCustomerLinesAsync(List<SaleSupplyCustomerLineUpdateRequest> requests, CancellationToken cancellationToken);
    Task<PresignedUploadUrlResponse?> GetAttachmentUploadUrlAsync(string fileName, CancellationToken cancellationToken);
    Task<List<SaleSupplyAttachmentDto>> GetAttachmentsAsync(string voucherNo, CancellationToken cancellationToken);
    Task<SaleSupplyAttachmentDto> AddAttachmentAsync(string voucherNo, string mediaId, string? fileName, string? description, CancellationToken cancellationToken);
    Task DeleteAttachmentAsync(string voucherNo, int attachmentId, CancellationToken cancellationToken);
}
