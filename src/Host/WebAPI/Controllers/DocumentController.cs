using Retailer.Application.Public.Document;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers;

//[Authorize]
public class DocumentController : VersionNeutralApiController
{
    private readonly IDocumentService _documentService;

    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost]
    [OpenApiOperation("Upload a document.", "")]
    public async Task<HttpResponseDto<DocumentResponse>> AddOrUpdateAdminImageAsync([FromForm] DocumentRequest request)
    {
        var response = await _documentService.AddDocumentAsync(request);
        return response.ToInformationResponse();
    }

    [HttpPost("documents")]
    [OpenApiOperation("Upload multiple documents", "")]

    public async Task<HttpResponseDto<List<DocumentResponse>>> UploadMultipleDocumentsAsync([FromForm] MultipleDocumentRequest request)
    {
        var response = await _documentService.AddMultipleDocumentsAsync(request);
        return response.ToInformationResponse();
    }

    [HttpPost("initialize-upload")]
    [OpenApiOperation("Initialize Upload.", "")]
    public async Task<HttpResponseDto<FileUploadInitResponse>> UploadInitiateAsync(FileUploadInitRequest request)
    {
        var response = await _documentService.UploadInitiateAsync(request);
        return response.ToInformationResponse();
    }

    [HttpPost("initialize-bulk-upload")]
    [OpenApiOperation("Initialize Bulk Upload.", "")]
    public async Task<HttpResponseDto<List<FileUploadInitResponse>>> UploadInitiateMultipleAsync(List<FileUploadInitRequest> request)
    {
        var response = await _documentService.UploadInitiateMultipleAsync(request);
        return response.ToInformationResponse();
    }
}
