namespace Equipment.Infrastructure.Persistence;

internal sealed class EquipmentConfiguration : IEntityTypeConfiguration<EquipmentItem>
{
    public void Configure(
        EntityTypeBuilder<EquipmentItem> builder)
    {
        builder.ToTable("equipment", table =>
        {
            table.HasCheckConstraint("ck_equipment_inventory_number", "inventory_number ~ '^[A-Za-z0-9]{1,32}$'");
            table.HasCheckConstraint("ck_equipment_name", "name !~ '^[[:space:]]*$'");
            table.HasCheckConstraint("ck_equipment_room", "room BETWEEN 1 AND 1000");
            table.HasCheckConstraint("ck_equipment_version", "version > 0");
        });
        builder.HasKey(item => item.Id)
            .HasName("pk_equipment");
        builder.Property(item => item.Id)
            .HasColumnName("id");
        builder.Property(item => item.InventoryNumber)
            .HasColumnName("inventory_number")
            .HasMaxLength(EquipmentRules.InventoryNumberMaxLength)
            .IsRequired();
        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(EquipmentRules.NameMaxLength)
            .IsRequired();
        builder.Property(item => item.EquipmentTypeId)
            .HasColumnName("equipment_type_id");
        builder.Property(item => item.Room)
            .HasColumnName("room");
        builder.Property(item => item.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .HasDefaultValue(1L);
        builder.HasIndex(item => item.InventoryNumber)
            .IsUnique()
            .HasDatabaseName("uq_equipment_inventory_number");
        builder.HasIndex(item => item.EquipmentTypeId)
            .HasDatabaseName("ix_equipment_type_id");
        builder.HasOne(item => item.Type)
            .WithMany()
            .HasForeignKey(item => item.EquipmentTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_equipment_type");
    }
}
