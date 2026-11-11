namespace Equipment.Web.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class EquipmentWriteRequest
{
    [Required(ErrorMessage = "Укажите учётный номер.")]
    public string? InventoryNumber { get; init; }

    [Required(ErrorMessage = "Укажите наименование.")]
    public string? Name { get; init; }

    [Required(ErrorMessage = "Выберите тип техники.")]
    public int? EquipmentTypeId { get; init; }

    [Required(ErrorMessage = "Укажите комнату размещения.")]
    public int? Room { get; init; }

    public EquipmentDetails ToDetails()
    {
        return new(InventoryNumber!, Name!, EquipmentTypeId!.Value, Room!.Value);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class EquipmentUpdateRequest : EquipmentWriteRequest
{
    [Required(ErrorMessage = "Передайте исходную версию карточки.")]
    public long? Version { get; init; }
}
