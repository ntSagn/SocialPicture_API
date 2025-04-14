// SocialPicture.Persistence/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using SocialPicture.Domain.Entities;

namespace SocialPicture.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentLike> CommentLikes { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ImageTag> ImageTags { get; set; }
        public DbSet<SavedImage> SavedImages { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Password).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Fullname).HasMaxLength(100);
                entity.Property(e => e.ProfilePicture).HasMaxLength(255);
            });

            // Image entity configuration
            modelBuilder.Entity<Image>(entity =>
            {
                entity.HasKey(e => e.ImageId);
                entity.Property(e => e.ImageUrl).HasMaxLength(255).IsRequired();
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Images)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Like entity configuration
            modelBuilder.Entity<Like>(entity =>
            {
                entity.HasKey(e => e.LikeId);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Likes)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Image)
                      .WithMany(i => i.Likes)
                      .HasForeignKey(e => e.ImageId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Comment entity configuration
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.CommentId);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Comments)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Image)
                      .WithMany(i => i.Comments)
                      .HasForeignKey(e => e.ImageId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ParentComment)
                      .WithMany(c => c.Replies)
                      .HasForeignKey(e => e.ParentCommentId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // CommentLike entity configuration
            modelBuilder.Entity<CommentLike>(entity =>
            {
                entity.HasKey(e => e.CommentLikeId);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.CommentLikes)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Comment)
                      .WithMany(c => c.CommentLikes)
                      .HasForeignKey(e => e.CommentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Follow entity configuration
            modelBuilder.Entity<Follow>(entity =>
            {
                entity.HasKey(e => e.FollowId);
                entity.HasOne(e => e.Follower)
                      .WithMany(u => u.Following)
                      .HasForeignKey(e => e.FollowerId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Following)
                      .WithMany(u => u.Followers)
                      .HasForeignKey(e => e.FollowingId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Tag entity configuration
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.TagId);
                entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // ImageTag entity configuration
            modelBuilder.Entity<ImageTag>(entity =>
            {
                entity.HasKey(e => e.ImageTagId);
                entity.HasOne(e => e.Image)
                      .WithMany(i => i.ImageTags)
                      .HasForeignKey(e => e.ImageId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Tag)
                      .WithMany(t => t.ImageTags)
                      .HasForeignKey(e => e.TagId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<SavedImage>()
        .HasOne(s => s.User)
        .WithMany(u => u.SavedImages)
        .HasForeignKey(s => s.UserId)
        .OnDelete(DeleteBehavior.Restrict); // Change from Cascade to Restrict

            modelBuilder.Entity<SavedImage>()
                .HasOne(s => s.Image)
                .WithMany(i => i.SavedByUsers)
                .HasForeignKey(s => s.ImageId)
                .OnDelete(DeleteBehavior.Restrict);

            // Report entity configuration
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.ReportId);
                entity.HasOne(e => e.Reporter)
                      .WithMany(u => u.Reports)
                      .HasForeignKey(e => e.ReporterId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Image)
                      .WithMany(i => i.Reports)
                      .HasForeignKey(e => e.ImageId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ResolvedBy)
                      .WithMany(u => u.ResolvedReports)
                      .HasForeignKey(e => e.ResolvedById)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Notification entity configuration
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Follow>()
        .HasOne(f => f.Follower)
        .WithMany(u => u.Following)
        .HasForeignKey(f => f.FollowerId)
        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
