using Retailer.Application.Identity.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Application.Identity.Users;
public class VerifyOtpRequest : DeviceInfoRequest
{
    public string? Otp { get; set; }
    public string? Email { get; set; }
}
