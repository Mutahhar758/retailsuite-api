using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Application.Common.Models;

public class LookupPaginatedResponse<T>
{
    public LookupPaginatedResponse(List<T> data, int count, int page, int pageSize)
    {
        Data = data;
        Metadata = new LookupPaginationMetadata
        {
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize),
            TotalCount = count
        };
    }

    public List<T> Data { get; set; }
    public LookupPaginationMetadata Metadata { get; set; }

}

public class LookupPaginationMetadata
{
    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public int PageSize { get; set; }
}