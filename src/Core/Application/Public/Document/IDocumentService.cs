
namespace Retailer.Application.Public.Document;
public interface IDocumentService : IScopedService
{
    Task<DocumentResponse> AddDocumentAsync(DocumentRequest request, bool compress = false, int? height = null, int? width = null);
    Task<DocumentResponse> AddDocumentAsync(DocumentFromStreamRequest request);
    Task<List<DocumentResponse>> AddMultipleDocumentsAsync(MultipleDocumentRequest request);
    Task<bool> DeleteDocumentAsync(int documentId);
    Task<List<FileUploadInitResponse>> UploadInitiateMultipleAsync(List<FileUploadInitRequest> requests);
    Task<FileUploadInitResponse> UploadInitiateAsync(FileUploadInitRequest request);
}
