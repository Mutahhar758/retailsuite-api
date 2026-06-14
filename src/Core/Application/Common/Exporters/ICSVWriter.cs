
namespace Retailer.Application.Common.Export;

public interface ICSVWriter : ITransientService
{
    byte[] WriteCSV<T>(List<T> data);
}