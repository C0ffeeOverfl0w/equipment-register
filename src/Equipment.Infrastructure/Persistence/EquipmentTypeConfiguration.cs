namespace Equipment.Infrastructure.Persistence;

internal sealed class EquipmentTypeConfiguration : IEntityTypeConfiguration<EquipmentType>
{
    public void Configure(
        EntityTypeBuilder<EquipmentType> builder)
    {
        builder.ToTable("equipment_types", table =>
            table.HasCheckConstraint("ck_equipment_types_name", "name !~ '^[[:space:]]*$'"));
        builder.HasKey(type => type.Id)
            .HasName("pk_equipment_types");
        builder.Property(type => type.Id)
            .HasColumnName("id");
        builder.Property(type => type.Name)
            .HasColumnName("name")
            .HasMaxLength(EquipmentRules.TypeNameMaxLength)
            .IsRequired();
    }
}
