using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Retailer.Domain.Common.Enums;

namespace Retailer.Application.Public.Document;

public class FileUploadInitRequest
{
    public string Name { get; set; } = default!;
    public string Extension { get; set; } = default!;
    public long? Size { get; set; }
    public bool IsPrivate { get; set; } = false;

    [EnumDataType(typeof(FileType))]
    public FileType FileType { get; set; }
}