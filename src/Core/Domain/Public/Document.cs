using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Domain.Public;

public class Document : AuditableEntity, IAggregateRoot
{
    public string FileType { get; set; } = default!;

    public string ConvertedFileName { get; set; } = default!;

    public string OriginalFileName { get; set; } = default!;

    public string? Path { get; set; }

    public string? AccessURL { get; set; }
}