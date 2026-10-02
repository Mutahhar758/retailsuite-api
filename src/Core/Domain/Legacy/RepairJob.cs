using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("RepairJob")]
public class RepairJob : AuditableEntity<string>, IAggregateRoot, ISoftDelete
{
    public DateOnly JobDate { get; set; }
    public string CustomerAcc { get; set; } = default!;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    public string? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public string DeviceModel { get; set; } = default!;
    public string? Imei { get; set; }
    public string? PasscodeOrPattern { get; set; }
    public string FaultDescription { get; set; } = default!;
    public string? PhysicalCondition { get; set; }

    public decimal? EstimatedCost { get; set; }
    public decimal? AdvancePaid { get; set; }
    public string Status { get; set; } = "Received";

    public string? AssignedTechnicianId { get; set; }
    public HrInfo? AssignedTechnician { get; set; }

    public DateTime? ExpectedDelivery { get; set; }
    public DateTime? DeliveredOn { get; set; }
    public string? SaleVNo { get; set; }
    public string? Remarks { get; set; }

    public ChartOfAccount? CustomerAccount { get; set; }
    public ICollection<RepairJobPart> Parts { get; set; } = new List<RepairJobPart>();
    public ICollection<RepairJobServiceItem> Services { get; set; } = new List<RepairJobServiceItem>();

    public DateTime? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
}
