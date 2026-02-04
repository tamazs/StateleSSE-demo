using Microsoft.EntityFrameworkCore;

namespace server;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) {}

    public DbSet<User> Users => Set<User>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
    public DbSet<UserChatRoom> UserChatRooms => Set<UserChatRoom>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW()");
        });

        // ChatRoom
        modelBuilder.Entity<ChatRoom>(e =>
        {
            e.HasKey(x => x.Id);
        });

        // Message
        modelBuilder.Entity<Message>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW()");

            e.HasOne(x => x.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(x => x.UserId);

            e.HasOne(x => x.ChatRoom)
                .WithMany(r => r.Messages)
                .HasForeignKey(x => x.ChatRoomId);
        });

        // UserChatRoom
        modelBuilder.Entity<UserChatRoom>(e =>
        {
            e.HasKey(x => new { x.UserId, x.ChatRoomId });

            e.HasOne(x => x.User)
                .WithMany(u => u.ChatRooms)
                .HasForeignKey(x => x.UserId);

            e.HasOne(x => x.ChatRoom)
                .WithMany(r => r.Members)
                .HasForeignKey(x => x.ChatRoomId);
        });
    }
}