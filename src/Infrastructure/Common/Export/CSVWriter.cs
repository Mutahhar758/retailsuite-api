using Retailer.Application.Common.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Infrastructure.Common.Export;
public class CSVWriter : ICSVWriter
{
    private void CreateHeader<T>(StreamWriter sw)
    {
        PropertyInfo[] properties = typeof(T).GetProperties();
        for (int i = 0; i < properties.Length - 1; i++)
        {
            sw.Write(properties[i].Name + ",");
        }

        string lastProp = properties[properties.Length - 1].Name;
        sw.Write(lastProp + sw.NewLine);
    }

    private void CreateRows<T>(List<T> list, StreamWriter sw)
    {
        foreach (var item in list)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            for (int i = 0; i < properties.Length - 1; i++)
            {
                var prop = properties[i];
                sw.Write(prop.GetValue(item) + ",");
            }

            var lastProp = properties[properties.Length - 1];
            sw.Write(lastProp.GetValue(item) + sw.NewLine);
        }
    }

    public byte[] WriteCSV<T>(List<T> data)
    {
        var ms = new MemoryStream();
        var sw = new StreamWriter(stream: ms, encoding: new UTF8Encoding(true));
        CreateHeader<T>(sw);
        CreateRows(data, sw);
        sw.Flush();
        return ms.ToArray();
    }
}
