using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OelTicketsBackend.Auth;

namespace OelTicketsBackend.Data;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMembership> ProjectMemberships => Set<ProjectMembership>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Status> Statuses => Set<Status>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Project>(e =>
        {
            e.ToTable("projects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.CreatedAtUtc).IsRequired();
        });

        b.Entity<ProjectMembership>(e =>
        {
            e.ToTable("project_memberships");
            e.HasKey(x => new { x.ProjectId, x.UserId });
            e.Property(x => x.CreatedAtUtc).IsRequired();

            e.HasOne(x => x.Project).WithMany(p => p.Memberships).HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<Status>(e =>
        {
            e.ToTable("statuses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.CreatedAtUtc).IsRequired();
        });

        b.Entity<Ticket>(e =>
        {
            e.ToTable("tickets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.CreatedAtUtc).IsRequired();

            e.HasOne(x => x.Project).WithMany(p => p.Tickets).HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId);

            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Comment>(e =>
        {
            e.ToTable("comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Content).IsRequired();
            e.Property(x => x.CreatedAtUtc).IsRequired();

            e.HasOne(x => x.Ticket).WithMany(t => t.Comments).HasForeignKey(x => x.TicketId);

            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool Archived { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    public List<ProjectMembership> Memberships { get; set; } = new();
    public List<Ticket> Tickets { get; set; } = new();
}

public sealed class ProjectMembership
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }
}

public sealed class Status
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }
}

public sealed class Ticket
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;

    public string CreatedByUserId { get; set; } = "";
    public ApplicationUser CreatedByUser { get; set; } = null!;

    public string Title { get; set; } = "";
    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    public List<Comment> Comments { get; set; } = new();
}

public sealed class Comment
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public string CreatedByUserId { get; set; } = "";
    public ApplicationUser CreatedByUser { get; set; } = null!;

    public string Content { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }
}