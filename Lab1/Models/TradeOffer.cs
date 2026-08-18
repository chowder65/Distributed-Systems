namespace RetroGameExchange.Models;

public enum TradeOfferStatus
{
    Pending,
    Accepted,
    Rejected
}

public class TradeOffer
{
    public int Id { get; set; }
    public int OfferedGameId { get; set; }
    public Game OfferedGame { get; set; } = null!;
    public int RequestedGameId { get; set; }
    public Game RequestedGame { get; set; } = null!;
    public int OfferingUserId { get; set; }
    public User OfferingUser { get; set; } = null!;
    public TradeOfferStatus Status { get; set; } = TradeOfferStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
