using Microsoft.EntityFrameworkCore;
using WarehouseApi.Models;

namespace WarehouseApi.Data;

public class WarehouseDbContext : DbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

    // Your tables
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<PriceHistory> PriceHistories { get; set; } = null!;
    public DbSet<ProductCategory> ProductCategories { get; set; } = null!;

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<UserPackage> UserPackages => Set<UserPackage>();
    public DbSet<UserPackageAssignment> UserPackageAssignments => Set<UserPackageAssignment>();
    public DbSet<PackageStatus> PackageStatuses => Set<PackageStatus>();
    public DbSet<PackageBatch> PackageBatches => Set<PackageBatch>();
    public DbSet<PackageBatchItem> PackageBatchItems => Set<PackageBatchItem>();
    public DbSet<GlobalSetting> GlobalSettings => Set<GlobalSetting>();
    public DbSet<SupplierCollection> SupplierCollections => Set<SupplierCollection>();
    public DbSet<SupplierCollectionItem> SupplierCollectionItems => Set<SupplierCollectionItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.HasIndex(user => new { user.ClientId, user.Role });

            entity.HasOne(user => user.Client)
                .WithMany(client => client.Users)
                .HasForeignKey(user => user.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserPackage>(entity =>
        {
            entity.Property(package => package.PackageId).ValueGeneratedOnAdd();
            entity.HasOne(package => package.Assignment)
                .WithOne(assignment => assignment.Package)
                .HasForeignKey<UserPackageAssignment>(assignment => assignment.PackageId);
        });

        modelBuilder.Entity<UserPackageAssignment>(entity =>
        {
            entity.HasIndex(assignment => assignment.ClientId);
            entity.HasIndex(assignment => assignment.UserId);
            entity.HasOne(assignment => assignment.Client)
                .WithMany()
                .HasForeignKey(assignment => assignment.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(assignment => assignment.User)
                .WithMany()
                .HasForeignKey(assignment => assignment.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(assignment => assignment.AssignedByUser)
                .WithMany()
                .HasForeignKey(assignment => assignment.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PackageStatus>(entity =>
        {
            entity.HasIndex(status => status.Name).IsUnique();
            entity.HasIndex(status => new { status.IsActive, status.DisplayOrder });
        });

        modelBuilder.Entity<PackageBatch>(entity =>
        {
            entity.HasIndex(batch => batch.BatchNumber).IsUnique();
            entity.HasIndex(batch => new { batch.ClientId, batch.Status });
            entity.HasOne(batch => batch.Client)
                .WithMany(client => client.PackageBatches)
                .HasForeignKey(batch => batch.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(batch => batch.CreatedByUser)
                .WithMany()
                .HasForeignKey(batch => batch.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PackageBatchItem>(entity =>
        {
            entity.HasKey(item => new { item.BatchId, item.PackageId });
            entity.HasIndex(item => item.PackageId).IsUnique();
            entity.HasOne(item => item.Batch)
                .WithMany(batch => batch.Items)
                .HasForeignKey(item => item.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Package)
                .WithOne(package => package.BatchItem)
                .HasForeignKey<PackageBatchItem>(item => item.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GlobalSetting>(entity =>
        {
            entity.Property(setting => setting.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<SupplierCollection>(entity =>
        {
            entity.HasIndex(collection => collection.CollectionNumber).IsUnique();
            entity.HasOne(collection => collection.CreatedByUser).WithMany()
                .HasForeignKey(collection => collection.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(collection => collection.BearerUser).WithMany()
                .HasForeignKey(collection => collection.BearerUserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SupplierCollectionItem>(entity =>
        {
            entity.HasKey(item => new { item.CollectionId, item.PackageId });
            entity.HasIndex(item => item.PackageId).IsUnique();
            entity.HasOne(item => item.Collection).WithMany(collection => collection.Items)
                .HasForeignKey(item => item.CollectionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Package).WithOne(package => package.SupplierCollectionItem)
                .HasForeignKey<SupplierCollectionItem>(item => item.PackageId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
