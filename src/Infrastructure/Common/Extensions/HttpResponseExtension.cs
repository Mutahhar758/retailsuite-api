using Retailer.Application.Common.Enums;
using Retailer.Application.Common.Models;
using System.Net;

namespace Retailer.Infrastructure.Common.Extensions;

public static class HttpResponseExtension
{
    public static HttpResponseDto<T> ToInformationResponse<T>(this T data, string? message = null, HttpStatusCode httpStatus = HttpStatusCode.OK)
        where T : class
    {
        return new HttpResponseDto<T>
        {
            Body = data,
            Metadata = new HttpResponseMetadata
            {
                Type = HttpResponseType.Information.ToString(),
                StatusCode = (int)httpStatus,
                Message = message,
            }
        };
    }

    public static HttpResponseDto<string> InformationResponse(string message, HttpStatusCode httpStatus = HttpStatusCode.OK)
        => ToInformationResponse(message, message, httpStatus);

    public static HttpResponseDto<T> ToWarningResponse<T>(this T data, string? message = null, HttpStatusCode httpStatus = HttpStatusCode.OK)
        where T : class
    {
        return new HttpResponseDto<T>
        {
            Body = data,
            Metadata = new HttpResponseMetadata
            {
                Type = HttpResponseType.Warning.ToString(),
                StatusCode = (int)httpStatus,
                Message = message,
            }
        };
    }

    public static HttpResponseDto<T> ToSuccessResponse<T>(this T data, string? message = null, HttpStatusCode httpStatus = HttpStatusCode.OK)
        where T : class
    {
        return new HttpResponseDto<T>
        {
            Body = data,
            Metadata = new HttpResponseMetadata
            {
                Type = HttpResponseType.Success.ToString(),
                StatusCode = (int)httpStatus,
                Message = message,
            }
        };
    }

    public static HttpResponseDto<string> SuccessResponse(string message, HttpStatusCode httpStatus = HttpStatusCode.OK)
        => ToSuccessResponse(message, message, httpStatus);

    public static HttpResponseDto<T> ToErrorResponse<T>(this T data, string? message = null, HttpStatusCode httpStatus = HttpStatusCode.BadRequest)
        where T : class
    {
        return new HttpResponseDto<T>
        {
            Body = data,
            Metadata = new HttpResponseMetadata
            {
                Type = HttpResponseType.Error.ToString(),
                StatusCode = (int)httpStatus,
                Message = message,
            }
        };
    }
}
