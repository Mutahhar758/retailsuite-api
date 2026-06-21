using Mapster;
using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Legacy.HumanResources;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.HumanResources;

public class HRInfoService : IHRInfoService
{
    private readonly IRepository<HrInfo> _hrInfoRepository;
    private readonly IMediaServiceClient _mediaServiceClient;

    public HRInfoService(IRepository<HrInfo> hrInfoRepository, IMediaServiceClient mediaServiceClient)
    {
        _hrInfoRepository = hrInfoRepository;
        _mediaServiceClient = mediaServiceClient;
    }

    public async Task<List<HRInfoResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var employees = await _hrInfoRepository.GetAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var responses = employees.Adapt<List<HRInfoResponse>>();
        await PopulateMediaUrlsAsync(responses, cancellationToken);
        return responses;
    }

    public async Task<HRInfoResponse?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var employee = await _hrInfoRepository.GetByIdAsync(id, cancellationToken);
        if (employee == null) return null;

        var response = employee.Adapt<HRInfoResponse>();
        if (!string.IsNullOrEmpty(response.MediaId))
        {
            await PopulateMediaUrlsAsync(new List<HRInfoResponse> { response }, cancellationToken);
        }
        return response;
    }

    public async Task CreateAsync(HRInfoUpsertRequest request, CancellationToken cancellationToken)
    {
        var maxCode = await _hrInfoRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .MaxAsync(x => (long?)Convert.ToInt64(x.Id), cancellationToken) ?? 0;

        var nextCode = (maxCode + 1).ToString("D3");

        var newEmployee = request.Adapt<HrInfo>();
        newEmployee.Id = nextCode;

        await _hrInfoRepository.AddAsync(newEmployee);
    }

    public async Task UpdateAsync(string id, HRInfoUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new BadRequestException("ID is required.");

        var employee = await _hrInfoRepository.GetByIdAsync(id, cancellationToken);

        if (employee == null)
            throw new NotFoundException($"HR Info with ID {id} not found.");

        request.Adapt(employee);
        await _hrInfoRepository.UpdateAsync(employee);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var employee = await _hrInfoRepository.GetByIdAsync(id, cancellationToken);

        if (employee == null)
            throw new Exception($"HR Info with ID {id} not found.");

        await _hrInfoRepository.DeleteAsync(employee);
    }

    public async Task<PresignedUploadUrlResponse?> GetPresignedUploadUrlAsync(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new BadRequestException("File name is required.");

        var cleanFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName) || cleanFileName != fileName || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            throw new BadRequestException("Invalid file name. Only plain file names without paths are allowed.");
        }

        return await _mediaServiceClient.GetUploadUrlAsync(cleanFileName, "employee", cancellationToken);
    }

    private async Task PopulateMediaUrlsAsync(List<HRInfoResponse> items, CancellationToken cancellationToken)
    {
        var tasks = items
            .Where(x => !string.IsNullOrEmpty(x.MediaId))
            .Select(async item =>
            {
                try
                {
                    var sasResponse = await _mediaServiceClient.GetViewTokenAsync(item.MediaId!, 24, cancellationToken);
                    if (sasResponse != null)
                    {
                        item.MediaUrl = sasResponse.ViewUrl;
                    }
                }
                catch
                {
                    // Fail-safe: ignore media service exceptions to keep main application running
                }
            });
        await Task.WhenAll(tasks);
    }
}
