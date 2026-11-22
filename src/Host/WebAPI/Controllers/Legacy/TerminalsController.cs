using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Domain.Legacy;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Common.Constants;

namespace Retailer.Host.Controllers.Legacy;

public class TerminalRegisterRequest
{
    public string MachineId { get; set; } = default!;
    public string? MachineName { get; set; }
    public string? RequestedCode { get; set; }
}

public class TerminalRegisterResponse
{
    public string TerminalCode { get; set; } = default!;
    public bool IsNewlyAssigned { get; set; }
}

public class TerminalsController : VersionNeutralApiController
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TenantLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly IRepository<PosTerminal> _terminalRepository;
    private readonly ICurrentTenant _currentTenant;

    public TerminalsController(
        IRepository<PosTerminal> terminalRepository,
        ICurrentTenant currentTenant)
    {
        _terminalRepository = terminalRepository;
        _currentTenant = currentTenant;
    }

    [HttpPost("register")]
    [OpenApiOperation("Register a desktop POS terminal and assign a guaranteed-unique sequential Terminal ID (T01, T02, ...).", "")]
    public async Task<HttpResponseDto<TerminalRegisterResponse>> RegisterAsync(
        TerminalRegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MachineId))
            throw new CustomException("MachineId is required.");

        var sanitizedMachineId = new string(request.MachineId.Trim()
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .Take(64)
            .ToArray());

        if (string.IsNullOrWhiteSpace(sanitizedMachineId))
            throw new CustomException("Invalid MachineId.");

        var tenantKey = _currentTenant.Id ?? "default";
        var gate = TenantLocks.GetOrAdd(tenantKey, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken);
        try
        {
            var allTerminals = await _terminalRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .ToListAsync(cancellationToken);

            var existing = allTerminals.FirstOrDefault(x => string.Equals(x.MachineId, sanitizedMachineId, StringComparison.OrdinalIgnoreCase));

            // Optional custom code requested (e.g., from Settings -> Configuration)
            if (!string.IsNullOrWhiteSpace(request.RequestedCode))
            {
                var requested = new string(request.RequestedCode.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).Take(6).ToArray());
                if (requested.Length < 2)
                    throw new CustomException("Terminal code must be 2 to 6 alphanumeric characters (e.g. T01, POS1).");

                var conflict = allTerminals.Any(x =>
                    !string.Equals(x.MachineId, sanitizedMachineId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.TerminalCode, requested, StringComparison.OrdinalIgnoreCase));

                if (conflict)
                    throw new CustomException($"Terminal code '{requested}' is already assigned to another machine.");

                if (existing != null)
                {
                    existing.TerminalCode = requested;
                    existing.MachineName = Truncate(request.MachineName, 250);
                    existing.LastSeenOn = DateTime.UtcNow;
                    existing.IsActive = true;
                    await _terminalRepository.UpdateAsync(existing);
                    await _terminalRepository.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    await _terminalRepository.AddAsync(new PosTerminal
                    {
                        MachineId = sanitizedMachineId,
                        TerminalCode = requested,
                        MachineName = Truncate(request.MachineName, 250),
                        IsActive = true,
                        LastSeenOn = DateTime.UtcNow
                    });
                }

                return new TerminalRegisterResponse
                {
                    TerminalCode = requested,
                    IsNewlyAssigned = existing == null
                }.ToInformationResponse("Terminal registered.");
            }

            // Already registered -> update LastSeenOn and return its guaranteed-unique Terminal ID
            if (existing != null && !string.IsNullOrWhiteSpace(existing.TerminalCode))
            {
                existing.LastSeenOn = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(request.MachineName))
                    existing.MachineName = Truncate(request.MachineName, 250);
                await _terminalRepository.UpdateAsync(existing);
                await _terminalRepository.SaveChangesAsync(cancellationToken);

                return new TerminalRegisterResponse
                {
                    TerminalCode = existing.TerminalCode,
                    IsNewlyAssigned = false
                }.ToInformationResponse("Terminal verified.");
            }

            // Assign next sequential T01, T02, T03, ... guaranteed unique within this tenant
            var maxSeq = 0;
            foreach (var term in allTerminals)
            {
                var val = term.TerminalCode?.Trim();
                if (!string.IsNullOrEmpty(val) && val.StartsWith("T", StringComparison.OrdinalIgnoreCase) && int.TryParse(val.Substring(1), out var n) && n > maxSeq)
                {
                    maxSeq = n;
                }
            }

            var assignedCode = $"T{maxSeq + 1:D2}";
            await _terminalRepository.AddAsync(new PosTerminal
            {
                MachineId = sanitizedMachineId,
                TerminalCode = assignedCode,
                MachineName = Truncate(request.MachineName, 250),
                IsActive = true,
                LastSeenOn = DateTime.UtcNow
            });

            return new TerminalRegisterResponse
            {
                TerminalCode = assignedCode,
                IsNewlyAssigned = true
            }.ToInformationResponse("Terminal registered.");
        }
        finally
        {
            gate.Release();
        }
    }

    private static string? Truncate(string? value, int maxLen)
        => string.IsNullOrWhiteSpace(value) ? null : (value.Length <= maxLen ? value : value[..maxLen]);
}
