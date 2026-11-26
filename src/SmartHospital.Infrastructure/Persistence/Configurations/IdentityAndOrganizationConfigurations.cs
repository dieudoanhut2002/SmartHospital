using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHospital.Domain.Identity;
using SmartHospital.Domain.Organization;
using SmartHospital.Infrastructure.Identity;

namespace SmartHospital.Infrastructure.Persistence.Configurations;

public class IdentityAndOrganizationConfigurations :
    IEntityTypeConfiguration<ApplicationUser>,
    IEntityTypeConfiguration<ApplicationRole>,
    IEntityTypeConfiguration<Permission>,
    IEntityTypeConfiguration<RolePermission>,
    IEntityTypeConfiguration<UserFacilityScope>,
    IEntityTypeConfiguration<UserDepartmentScope>,
    IEntityTypeConfiguration<Hospital>,
    IEntityTypeConfiguration<Facility>,
    IEntityTypeConfiguration<Department>,
    IEntityTypeConfiguration<Specialty>,
    IEntityTypeConfiguration<Room>,
    IEntityTypeConfiguration<Clinic>,
    IEntityTypeConfiguration<Bed>,
    IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EmployeeCode).HasMaxLength(50);
    }

    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
    }

    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.Property(x => x.Code).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }

    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        builder.HasOne(x => x.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<UserFacilityScope> builder)
    {
        builder.ToTable("UserFacilityScopes");
        builder.HasIndex(x => new { x.UserId, x.FacilityId }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<UserDepartmentScope> builder)
    {
        builder.ToTable("UserDepartmentScopes");
        builder.HasIndex(x => new { x.UserId, x.FacilityId, x.DepartmentId }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Hospital> builder)
    {
        builder.ToTable("Hospitals");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.ToTable("Facilities");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
        builder.HasIndex(x => new { x.HospitalId, x.Code }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
        builder.HasIndex(x => new { x.FacilityId, x.Code }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("Specialties");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.FacilityId, x.Code }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("Clinics");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.QueuePrefix).HasMaxLength(10).IsRequired();
        builder.HasIndex(x => new { x.FacilityId, x.Code }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Bed> builder)
    {
        builder.ToTable("Beds");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.FacilityId, x.Code }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.EmployeeCode).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
