namespace Rehably.Domain.Entities.Billing;

public class SessionPackageOffer
{
    public Guid Id { get; set; }
    public Guid DiscountId { get; set; }
    public int SessionsToPurchase { get; set; }
    public int SessionsFree { get; set; }
    public string? ValidForServiceType { get; set; }

    public Discount Discount { get; set; } = null!;
}
