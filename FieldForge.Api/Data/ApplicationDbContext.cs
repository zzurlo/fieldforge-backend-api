using Microsoft.EntityFrameworkCore;
using FieldForge.Api.Models;

namespace FieldForge.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies { get; set; }
    public DbSet<EmployeeInvite> EmployeeInvites { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<ServiceOrder> ServiceOrders { get; set; }
    public DbSet<ServiceOrderTechnician> ServiceOrderTechnicians { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role 
            { 
                Id = 1, 
                Name = "OrganizationAdmin",
                NormalizedName = "ORGANIZATIONADMIN"
            },
            new Role 
            { 
                Id = 2, 
                Name = "Technician",
                NormalizedName = "TECHNICIAN"
            },
            new Role 
            { 
                Id = 3, 
                Name = "Biller",
                NormalizedName = "BILLER"
            }
        );
    }
}