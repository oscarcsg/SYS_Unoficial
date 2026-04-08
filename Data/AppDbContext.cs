using Microsoft.EntityFrameworkCore;
using StoreYourStuffAPI.Models;

namespace StoreYourStuffAPI.Data
{
    public class AppDbContext : DbContext
    {
        #region Attributes
        // These are SQLServer DDBB' tables representation
        public DbSet<User> Users { get; set; }
        public DbSet<Link> Links { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<LinkCategory> LinkCategories { get; set; }
        public DbSet<SharedLink> SharedLinks { get; set; }
        #endregion

        #region Constructors
        // This constructor will recieve the configuration (like the conection string)
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        #endregion

        #region Methods
        // These are the "translation" instructions
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure compounds PK of the LinkCategories and SharedLinks tables
            modelBuilder.Entity<LinkCategory>()
                .HasKey(lc => new { lc.LinkId, lc.CategoryId });
            modelBuilder.Entity<SharedLink>()
                .HasKey(sl => new { sl.LinkId, sl.UserId });

            modelBuilder.Entity<Friendship>()
                .HasKey(f => new { f.RequesterId, f.AddresseeId });

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Requester)
                .WithMany(u => u.RequestedFriendships)
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Addressee)
                .WithMany(u => u.ReceivedFriendships)
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Map the "Id" properties of the classes to the real column names of the DDBB
            modelBuilder.Entity<User>().Property(u => u.Id).HasColumnName("userId");
            modelBuilder.Entity<Link>().Property(l => l.Id).HasColumnName("linkId");
            modelBuilder.Entity<Category>().Property(c => c.Id).HasColumnName("categoryId");

            // Ensure to map the exact table names
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Link>().ToTable("Links");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<Friendship>().ToTable("Friendships");
        }
        #endregion
    }
}
