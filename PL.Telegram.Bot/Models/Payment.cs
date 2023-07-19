namespace PL.Telegram.Bot.Models;

public class Payment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.Now;
    public DateTime? DateConfirmed { get; set; }
    public PaymentStatusEnum Status { get; set; }
    
    public User User { get; set; }

    public void Confirm()
    {
        Status = PaymentStatusEnum.Confirmed;
        DateConfirmed = DateTime.Now;
    }
}

public enum PaymentStatusEnum
{
    Initialized,
    Confirmed,
    Denied
}