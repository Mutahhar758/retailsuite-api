using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Application.Public.Document;

public class FileUploadInitResponse : IDto
{
    public int DocumentId { get; set; } = default!;
    public string SASUrl { get; set; } = default!;
}
