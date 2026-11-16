using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Retailer.Infrastructure.Auth.Jwt;
using Retailer.Infrastructure.FileStorage;

namespace Retailer.Host.Controllers.Common;

[Route("api/local-media")]
[ApiVersionNeutral]
public sealed class LocalMediaController : BaseApiController
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
    private readonly JwtSettings _jwtSettings;

    public LocalMediaController(IOptions<JwtSettings> jwtSettings) =>
        _jwtSettings = jwtSettings.Value;

    [HttpPost("upload")]
    [AllowAnonymous]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> UploadAsync(
        [FromQuery] string fileId,
        [FromQuery] long exp,
        [FromQuery] string sig,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileId)
            || !LocalMediaServiceClient.VerifySignature("upload", fileId, exp, sig, _jwtSettings.Key))
        {
            return Unauthorized(new { Message = "Invalid or expired upload signature." });
        }

        var file = Request.Form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { Message = "No file provided." });
        }

        var fullPath = LocalMediaServiceClient.ResolveSafeFilePath(fileId);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        return Ok(new { Success = true, FileId = fileId });
    }

    [HttpGet("view")]
    [AllowAnonymous]
    public IActionResult View(
        [FromQuery] string fileId,
        [FromQuery] long exp,
        [FromQuery] string sig)
    {
        if (string.IsNullOrWhiteSpace(fileId)
            || !LocalMediaServiceClient.VerifySignature("view", fileId, exp, sig, _jwtSettings.Key))
        {
            return Unauthorized(new { Message = "Invalid or expired view signature." });
        }

        var fullPath = LocalMediaServiceClient.ResolveSafeFilePath(fileId);
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        if (!ContentTypeProvider.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        Response.Headers.CacheControl = "private, max-age=3600";
        return PhysicalFile(fullPath, contentType);
    }
}
