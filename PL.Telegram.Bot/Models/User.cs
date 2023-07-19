using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models;

public class User
{
    public int Id { get; set; }
    public long TelegramId { get; set; }

    public LanguageEnum Language { get; set; } = LanguageEnum.Eng;
    
    public bool IsPayed
    {
        get
        {
            if (ConfirmedPayment?.DateConfirmed is null) return false;
            return ConfirmedPayment.DateConfirmed.Value.AddDays(28) > DateTime.Now;
        }
    }

    public Payment? ConfirmedPayment => Payments.FirstOrDefault(p => p.Status == PaymentStatusEnum.Confirmed);
    public Payment? InitializedPayment => Payments.FirstOrDefault(p => p.Status == PaymentStatusEnum.Initialized);
    public IEnumerable<Payment> Payments { get; set; }
}