using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Retailer.Domain.Common.Enums;

namespace Retailer.Infrastructure.Common.Extensions;
public static class CurrencyExtensions
{
    public static long ToLowestCurrencyUnit(this decimal value, Currency currency)
    {
        if (new[] { Currency.mro, Currency.mga, Currency.kmf }.Contains(currency))
        {
            return Convert.ToInt64(value * 5);
        }
        else if (new[] { Currency.kwd, Currency.omr }.Contains(currency))
        {
            return Convert.ToInt64(value * 1000);
        }
        else
        {
            return Convert.ToInt64(value * 100);
        }

    }

    public static decimal ToBaseCurrencyUnit(this long value, Currency currency)
    {
        if (new[] { Currency.mro, Currency.mga, Currency.kmf }.Contains(currency))
        {
            return Convert.ToDecimal(value / 5M);
        }
        else if (new[] { Currency.kwd, Currency.omr }.Contains(currency))
        {
            return Convert.ToDecimal(value / 1000M);
        }
        else
        {
            return Convert.ToDecimal(value / 100M);
        }

    }
}
