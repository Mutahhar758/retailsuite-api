using System;
using System.Collections.Generic;
using Retailer.Application.Legacy.Reports;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public enum SaleReceiptPrintLayout
{
    Thermal80mm,
    A5Sheet,
    A4Sheet
}

/// <summary>
/// Header information for a QuestPDF Sale Receipt / Bill.
/// </summary>
public class SaleReceiptHeader
{
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string CompanyName { get; set; } = "Retail Suite Enterprise";
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string CustomerName { get; set; } = "Walk-in Customer";
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public string? Narration { get; set; }
    public string? Description { get; set; }
    public string? Cashier { get; set; }
    public string? Counter { get; set; }
    public string? ThankyouLine { get; set; }

    public decimal GrossTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal CashReceipt { get; set; }
    public decimal CashBack { get; set; }
    public decimal Balance { get; set; }
    public bool IsDuplicate { get; set; }

    public SaleReceiptPrintLayout Layout { get; set; } = SaleReceiptPrintLayout.Thermal80mm;
    public QrPaymentInfo? QrPayment { get; set; }
    public bool ShowQrPayment => QrPayment != null && QrPayment.IsEnabled && !string.IsNullOrWhiteSpace(QrPayment.AccountNumber) && (Balance > 0 || NetAmount > 0);
}

/// <summary>
/// Individual line item on a QuestPDF Sale Receipt / Bill.
/// </summary>
public class SaleReceiptLine
{
    public int Seq { get; set; }
    public string Item { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal GrossRate { get; set; }
    public decimal Discount { get; set; }
    public decimal Carriage { get; set; }
    public decimal Amount { get; set; }
    public string? Imei { get; set; }

    public string FormattedQty => Qty.ToString("#,##0.##");
    public string FormattedQtyWithUnit => !string.IsNullOrWhiteSpace(Unit)
        ? $"{Qty:#,##0.##} {Unit}"
        : Qty.ToString("#,##0.##");
    public string FormattedRate => Rate.ToString("#,##0");
    public string FormattedDiscount => Discount > 0 ? Discount.ToString("#,##0") : "-";
    public string FormattedAmount => Amount.ToString("#,##0");
}
