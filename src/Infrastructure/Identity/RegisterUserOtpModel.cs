using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Infrastructure.Identity;
public class RegisterUserOtpModel
{
    public string? UserName { get; set; }
    public string? Otp { get; set; }
}
