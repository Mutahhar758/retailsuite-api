using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Public.Document;
using Retailer.Application.Storage;
using Retailer.Domain.Common.Enums;
using Retailer.Domain.Public;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Common.External;
using Retailer.Infrastructure.FileStorage;
using Retailer.Shared.Localization;
using Mapster;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Public;
public class DocumentService : IDocumentService
{
    private readonly IRepository<Document> _documentRepository;
    private readonly IAzureStorageService _azureService;
    private readonly IStringLocalizer<DocumentService> _localizer;
    private readonly AzureStorageSettings _azureSettings;
    private readonly LinkSettings _linkSettings;

    public DocumentService(IRepository<Document> documentRepository, IAzureStorageService azureService, IStringLocalizer<DocumentService> localizer, IOptions<AzureStorageSettings> azureSettings, IOptions<LinkSettings> linkSettings)
    {
        _documentRepository = documentRepository;
        _azureService = azureService;
        _localizer = localizer;
        _azureSettings = azureSettings.Value;
        _linkSettings = linkSettings.Value;
    }

    public async Task<DocumentResponse> AddDocumentAsync(DocumentRequest request, bool compress = false, int? height = null, int? width = null)
    {
        var mediaStream = new MediaStream()
        {
            OriginalFileName = request.DocumentFile.FileName
        };

        if (request.DocumentFile.Length > _azureSettings.FileSizeLimit)
        {
            throw new ConflictException(_localizer[MessageConstants.FileSizeTooLarge, _azureSettings.FileSizeLimit / 1048576]);
        }

        string extension = Path.GetExtension(mediaStream.OriginalFileName).ToLower();

        if (compress && FileType.Image.GetDescriptionList().Any(x => x == extension))
            mediaStream.InputStream = ImageCompression.ResizeImage(request.DocumentFile.OpenReadStream(), height!.Value, width!.Value);
        else
            mediaStream.InputStream = request.DocumentFile.OpenReadStream();

        string[] allowedExtensions = _azureSettings.DocumentAllowedExtension.Split(',');

        if (!allowedExtensions.Contains(extension))
        {
            throw new ConflictException(_localizer[MessageConstants.MediaExtensionInvalid, _azureSettings.DocumentAllowedExtension]);
        }

        string convertedFileName = $"{Guid.NewGuid()}{Path.GetExtension(mediaStream.OriginalFileName).ToLower()}";

        //string tempPath = UploadFileOnTempPath(mediaStream.InputStream, convertedFileName);

        var document = new Document
        {
            ConvertedFileName = convertedFileName,
            FileType = extension,
            OriginalFileName = mediaStream.OriginalFileName,
        };

        await _documentRepository.AddAsync(document);

        var uploadResponse = await _azureService.UploadAsync(mediaStream.InputStream, request.Path, convertedFileName);

        document.AccessURL = uploadResponse.BlobUrlWithSasToken;
        document.Path = uploadResponse.BlobPath;

        await _documentRepository.UpdateAsync(document);

        return document.Adapt<DocumentResponse>();
    }

    public async Task<DocumentResponse> AddDocumentAsync(DocumentFromStreamRequest request)
    {
        var mediaStream = new MediaStream
        {
            OriginalFileName = request.DocumentName,
            InputStream = new MemoryStream(request.Document)
        };
        string convertedFileName = $"{Guid.NewGuid()}{Path.GetExtension(mediaStream.OriginalFileName).ToLower()}";

        var document = new Document
        {
            ConvertedFileName = convertedFileName,
            FileType = request.Extension,
            OriginalFileName = mediaStream.OriginalFileName,
        };

        await _documentRepository.AddAsync(document);

        var uploadResponse = await _azureService.UploadAsync(mediaStream.InputStream, request.Path, convertedFileName);

        document.AccessURL = uploadResponse.BlobUrlWithSasToken;
        document.Path = uploadResponse.BlobPath;

        await _documentRepository.UpdateAsync(document);

        return document.Adapt<DocumentResponse>();
    }

    public async Task<List<DocumentResponse>> AddMultipleDocumentsAsync(MultipleDocumentRequest request)
    {
        var requestDocuments = request.Documents.Select(rd => new
        {
            MediaStream = new MediaStream
            {
                OriginalFileName = rd.FileName,
                InputStream = rd.OpenReadStream()
            },
            Extension = Path.GetExtension(rd.FileName).ToLower(),
            ConvertedFileName = $"{Guid.NewGuid()}{Path.GetExtension(rd.FileName).ToLower()}",
            FileLength = rd.Length
        }).ToList();

        if (request.Documents.Any(r => r.Length > _azureSettings.FileSizeLimit))
            throw new ConflictException(_localizer[MessageConstants.FileSizeTooLarge, _azureSettings.FileSizeLimit / 1048576]);

        var allowedExtensions = _azureSettings.DocumentAllowedExtension.Split(',').ToHashSet();

        if (requestDocuments.Any(d => !allowedExtensions.Contains(d.Extension)))
            throw new ConflictException(_localizer[MessageConstants.MediaExtensionInvalid, _azureSettings.DocumentAllowedExtension]);

        List<Task<AzureUploadResponse>> uploadTasks = new();

        requestDocuments.ForEach(rd =>
        {
            uploadTasks.Add(_azureService.UploadAsync(
                rd.MediaStream.InputStream,
                request.Path,
                rd.ConvertedFileName));
        });

        var uploads = await Task.WhenAll(uploadTasks);

        var documents = requestDocuments.Select(rd => new Document
        {
            ConvertedFileName = rd.ConvertedFileName,
            FileType = rd.Extension,
            OriginalFileName = rd.MediaStream.OriginalFileName,
            Path = uploads.FirstOrDefault(u => u.FileName == rd.ConvertedFileName).BlobPath,
            AccessURL = uploads.FirstOrDefault(u => u.FileName == rd.ConvertedFileName).BlobUrlWithSasToken
        }).ToList();

        await _documentRepository.AddRangeAsync(documents);

        return documents.Adapt<List<DocumentResponse>>();
    }

    public async Task<bool> DeleteDocumentAsync(int documentId)
    {
        var document = await this._documentRepository.GetAll()
            .Where(x => x.Id == documentId)
            .FirstOrDefaultAsync() ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, EntityConstants.Document]);

        await this._documentRepository.DeleteAsync(document);

        return true;
    }

    public async Task<FileUploadInitResponse> UploadInitiateAsync(FileUploadInitRequest request)
    {
        var (response, document) = await PrepareFileUploadAsync(request);

        await _documentRepository.AddAsync(document, false);
        await _documentRepository.SaveChangesAsync();

        response.DocumentId = document.Id;
        return response;
    }

    public async Task<List<FileUploadInitResponse>> UploadInitiateMultipleAsync(List<FileUploadInitRequest> requests)
    {
        var responses = new List<FileUploadInitResponse>();
        var documents = new List<Document>();

        foreach (var request in requests)
        {
            var (response, document) = await PrepareFileUploadAsync(request);
            responses.Add(response);
            documents.Add(document);
            await _documentRepository.AddAsync(document, false);
        }

        await _documentRepository.SaveChangesAsync();

        for (int i = 0; i < documents.Count; i++)
        {
            responses[i].DocumentId = documents[i].Id;
        }

        return responses;
    }

    private async Task<(FileUploadInitResponse Response, Document Document)> PrepareFileUploadAsync(FileUploadInitRequest request)
    {
        var blobServiceClient = new BlobServiceClient(_azureSettings.ConnectionString);

        string containerName = request.IsPrivate
            ? _azureSettings.AzurePrivateContainerReference
            : _azureSettings.AzureContainerReference;

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var allowedExtensions = request.FileType.GetDescriptionList();
        if (!allowedExtensions.Contains(request.Extension.ToLower()))
        {
            throw new BadRequestException(_localizer[MessageConstants.MediaExtensionInvalid, _azureSettings.DocumentAllowedExtension]);
        }

        string partitionPath = Enum.GetName(typeof(FileType), request.FileType)! + "s";
        string path = $"{request.Name}_{Guid.NewGuid()}";
        string convertedFileName = $"{partitionPath}/{path}{request.Extension.Trim()}".Replace(" ", "_");

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = convertedFileName,
            ExpiresOn = DateTime.UtcNow.AddYears(15),
            ContentType = DocumentConstants.GetContentType(request.Extension)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.All);

        string sasToken = sasBuilder.ToSasQueryParameters(
            new StorageSharedKeyCredential(_azureSettings.AccountName, _azureSettings.AccountKey)
        ).ToString();

        string accessUrl = $"{_linkSettings.AzureStorageBaseUrl}{containerName}/{convertedFileName}?{sasToken}";

        var document = new Document
        {
            ConvertedFileName = convertedFileName,
            FileType = request.Extension,
            OriginalFileName = request.Name,
            AccessURL = accessUrl
        };

        var response = new FileUploadInitResponse
        {
            SASUrl = accessUrl
        };

        return (response, document);
    }
}
