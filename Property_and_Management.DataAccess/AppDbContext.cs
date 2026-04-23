using Microsoft.EntityFrameworkCore;
using Property_and_Management.Src.Model;

namespace Property_and_Management.DataAccess
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Game> Games => Set<Game>();
        public DbSet<Rental> Rentals => Set<Rental>();
        public DbSet<Request> Requests => Set<Request>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(u => u.DisplayName)
                      .HasColumnName("display_name")
                      .HasMaxLength(50)
                      .IsRequired()
                      .HasDefaultValue("Unknown User");
            });

            modelBuilder.Entity<Game>(entity =>
            {
                entity.ToTable("Games");
                entity.HasKey(g => g.Id);
                entity.Property(g => g.Id).HasColumnName("game_id").ValueGeneratedOnAdd();
                entity.Property(g => g.Name).HasColumnName("name").HasMaxLength(30).IsRequired();
                entity.Property(g => g.Price).HasColumnName("price").HasColumnType("decimal(5,2)");
                entity.Property(g => g.MinimumPlayerNumber).HasColumnName("minimum_player_number");
                entity.Property(g => g.MaximumPlayerNumber).HasColumnName("maximum_player_number");
                entity.Property(g => g.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
                entity.Property(g => g.Image).HasColumnName("image").HasColumnType("varbinary(max)");
                entity.Property(g => g.IsActive)
                      .HasColumnName("is_active")
                      .HasConversion(v => v ? 1 : 0, v => v != 0)
                      .HasDefaultValue(true);
                entity.HasOne(g => g.Owner)
                      .WithMany()
                      .HasForeignKey("owner_id")
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Rental>(entity =>
            {
                entity.ToTable("Rentals");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasColumnName("rental_id").ValueGeneratedOnAdd();
                entity.Property(r => r.StartDate).HasColumnName("start_date").HasColumnType("datetime");
                entity.Property(r => r.EndDate).HasColumnName("end_date").HasColumnType("datetime");
                entity.HasOne(r => r.Game).WithMany().HasForeignKey("game_id").OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(r => r.Renter).WithMany().HasForeignKey("renter_id").OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(r => r.Owner).WithMany().HasForeignKey("owner_id").OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Request>(entity =>
            {
                entity.ToTable("Requests");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasColumnName("request_id").ValueGeneratedOnAdd();
                entity.Property(r => r.StartDate).HasColumnName("start_date").HasColumnType("datetime");
                entity.Property(r => r.EndDate).HasColumnName("end_date").HasColumnType("datetime");
                entity.Property(r => r.Status)
                      .HasColumnName("status")
                      .HasConversion<int>()
                      .HasDefaultValue(RequestStatus.Open);
                entity.HasOne(r => r.Game).WithMany().HasForeignKey("game_id").OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(r => r.Renter).WithMany().HasForeignKey("renter_id").OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(r => r.Owner).WithMany().HasForeignKey("owner_id").OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(r => r.OfferingUser)
                      .WithMany()
                      .HasForeignKey("offering_user_id")
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Id).HasColumnName("notification_id").ValueGeneratedOnAdd();
                entity.Property(n => n.Timestamp).HasColumnName("timestamp").HasColumnType("datetime");
                entity.Property(n => n.Title).HasColumnName("title").HasMaxLength(30).IsRequired();
                entity.Property(n => n.Body).HasColumnName("body").HasMaxLength(500).IsRequired();
                entity.Property(n => n.Type)
                      .HasColumnName("type")
                      .HasConversion<int>()
                      .HasDefaultValue(NotificationType.Informational);
                entity.Property(n => n.RelatedRequestId).HasColumnName("related_request_id");
                entity.HasOne(n => n.User).WithMany().HasForeignKey("user_id").OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
