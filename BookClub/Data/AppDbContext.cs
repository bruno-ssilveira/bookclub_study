using System.Security.Claims;
using BookClub.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookClub.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<UserClub> UserClubs => Set<UserClub>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<ClubBook> ClubBooks => Set<ClubBook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user");
            ConfigureAuditFields(entity, "user");

            entity.Property(u => u.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Surname)
                .HasColumnName("surname")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255)
                .IsRequired();

            entity.Ignore(u => u.FullName);

            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("ix_user_email");
        });

        modelBuilder.Entity<Club>(entity =>
        {
            entity.ToTable("club");
            ConfigureAuditFields(entity, "club");

            entity.Property(c => c.Name)
                .HasColumnName("name")
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(c => c.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            entity.Property(c => c.IsPrivate)
                .HasColumnName("is_private")
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(c => c.InviteCode)
                .HasColumnName("invite_code")
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(c => c.InviteCode)
                .IsUnique()
                .HasDatabaseName("ix_club_invite_code");
        });

        modelBuilder.Entity<UserClub>(entity =>
        {
            entity.ToTable("user_club");
            ConfigureAuditFields(entity, "user_club");

            entity.Property(uc => uc.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(uc => uc.ClubId)
                .HasColumnName("club_id")
                .IsRequired();

            entity.Property(uc => uc.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(uc => uc.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(BookClub.Models.Enums.MembershipStatus.Active)
                .HasSentinel((BookClub.Models.Enums.MembershipStatus)0)
                .IsRequired();

            entity.HasIndex(uc => new { uc.ClubId, uc.Status })
                .HasDatabaseName("ix_user_club_club_id_status");

            entity.HasIndex(uc => new { uc.UserId, uc.ClubId })
                .IsUnique()
                .HasDatabaseName("ix_user_club_user_id_club_id");

            entity.HasOne(uc => uc.User)
                .WithMany(u => u.UserClubs)
                .HasForeignKey(uc => uc.UserId)
                .HasConstraintName("fk_user_club_user_user_id")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uc => uc.Club)
                .WithMany(c => c.UserClubs)
                .HasForeignKey(uc => uc.ClubId)
                .HasConstraintName("fk_user_club_club_club_id")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("book");
            ConfigureAuditFields(entity, "book");

            entity.Property(b => b.ExternalId)
                .HasColumnName("external_id")
                .HasMaxLength(100);

            entity.Property(b => b.Isbn)
                .HasColumnName("isbn")
                .HasMaxLength(20);

            entity.Property(b => b.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(b => b.Author)
                .HasColumnName("author")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(b => b.Synopsis)
                .HasColumnName("synopsis")
                .HasColumnType("text");

            entity.Property(b => b.Pages)
                .HasColumnName("pages");

            entity.Property(b => b.CoverUrl)
                .HasColumnName("cover_url")
                .HasMaxLength(500);

            entity.Property(b => b.PublishedYear)
                .HasColumnName("published_year");

            entity.HasIndex(b => b.ExternalId)
                .HasDatabaseName("ix_book_external_id");

            entity.HasIndex(b => b.Isbn)
                .HasDatabaseName("ix_book_isbn");
        });

        modelBuilder.Entity<ClubBook>(entity =>
        {
            entity.ToTable("club_book");
            ConfigureAuditFields(entity, "club_book");

            entity.Property(cb => cb.ClubId)
                .HasColumnName("club_id")
                .IsRequired();

            entity.Property(cb => cb.BookId)
                .HasColumnName("book_id")
                .IsRequired();

            entity.Property(cb => cb.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(cb => cb.StartDate)
                .HasColumnName("start_date")
                .HasColumnType("date");

            entity.Property(cb => cb.TargetFinishDate)
                .HasColumnName("target_finish_date")
                .HasColumnType("date");

            entity.Property(cb => cb.DisplayOrder)
                .HasColumnName("display_order")
                .HasDefaultValue(0)
                .IsRequired();

            entity.HasIndex(cb => new { cb.ClubId, cb.Status })
                .HasDatabaseName("ix_club_book_club_id_status");

            entity.HasOne(cb => cb.Club)
                .WithMany(c => c.ClubBooks)
                .HasForeignKey(cb => cb.ClubId)
                .HasConstraintName("fk_club_book_club_club_id")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cb => cb.Book)
                .WithMany(b => b.ClubBooks)
                .HasForeignKey(cb => cb.BookId)
                .HasConstraintName("fk_club_book_book_book_id")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuditFields<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName)
        where TEntity : BaseEntity
    {
        entity.HasKey(e => e.Id)
            .HasName($"pk_{tableName}");

        entity.Property(e => e.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        entity.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

        entity.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.UpdatedBy)
            .HasColumnName("updated_by");

        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .HasConstraintName($"fk_{tableName}_user_created_by")
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .HasConstraintName($"fk_{tableName}_user_updated_by")
            .OnDelete(DeleteBehavior.Restrict);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditInformation()
    {
        var utcNow = DateTime.UtcNow;
        var currentUserId = GetCurrentUserId();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                if (entry.Entity.CreatedBy is null && currentUserId.HasValue)
                {
                    entry.Entity.CreatedBy = currentUserId.Value;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(e => e.CreatedAt).IsModified = false;
                entry.Property(e => e.CreatedBy).IsModified = false;

                entry.Entity.UpdatedAt = utcNow;
                if (currentUserId.HasValue)
                {
                    entry.Entity.UpdatedBy = currentUserId.Value;
                }
            }
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
