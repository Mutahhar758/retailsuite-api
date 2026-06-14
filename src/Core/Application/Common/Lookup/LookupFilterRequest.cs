using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Application.Common.Lookup;

public class LookupFilterRequest
{
    public string? Keyword { get; set; } = default!;

    public int PageNumber { get; set; } = default!;

    public int PageSize { get; set; } = int.MaxValue;
}
