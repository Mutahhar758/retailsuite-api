using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Application.Identity.Tokens;
public class BiometricTokenRequest : DeviceInfoRequest
{
    [Required]
    public string? UserId { get; set; }
    [Required]
    public string? Signature { get; set; }
}