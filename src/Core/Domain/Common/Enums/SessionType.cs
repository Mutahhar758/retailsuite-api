using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Domain.Common.Enums;
public enum SessionType
{
    Invalid,
    Normal = 1,
    External = 2,
    Guest = 3
}
