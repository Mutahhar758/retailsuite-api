using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Combined multi-vendor batch document for compiling all selected Wanda vendor bills together.
/// </summary>
public class WandaVendorBillBatchDocument : IDocument
{
    private readonly List<(VendorBillHeader Header, List<WandaVendorBillLineResponse> Lines)> _batch;

    public WandaVendorBillBatchDocument(List<(VendorBillHeader Header, List<WandaVendorBillLineResponse> Lines)> batch)
    {
        _batch = batch ?? new();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        if (_batch.Count == 0)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40, Unit.Point);
                page.Content().AlignCenter().PaddingTop(60).Column(c =>
                {
                    c.Item().AlignCenter().Text("No billing transactions or balances found for the selected vendors in this period.")
                        .FontSize(12).Italic().FontColor(Colors.Grey.Darken1);
                });
            });
            return;
        }

        for (int i = 0; i < _batch.Count; i++)
        {
            var item = _batch[i];
            var singleDoc = new WandaVendorBillDocument(item.Header, item.Lines);
            singleDoc.Compose(container);
        }
    }
}
