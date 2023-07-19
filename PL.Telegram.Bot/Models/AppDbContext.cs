using Microsoft.EntityFrameworkCore;

namespace PL.Telegram.Bot.Models;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<ClientConfig> ClientConfigs { get; set; }
    public DbSet<ClientActionsLog> ClientActionsLogs { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}