namespace Equipment.UnitTests;

public sealed class EquipmentUpdateTests
{
    [Fact]
    public void UpdateChangesAllDetailsAndAdvancesVersion()
    {
        EquipmentItem item = EquipmentItem.Create("000PC", "Исходная", 1, 1).Value;
        Result result = item.UpdateDetails("000PC2", "Новая 🖥", 2, 1000);
        Assert.True(result.IsSuccess);
        Assert.Equal("000PC2", item.InventoryNumber);
        Assert.Equal("Новая 🖥", item.Name);
        Assert.Equal(2, item.EquipmentTypeId);
        Assert.Equal(1000, item.Room);
        Assert.Equal(2, item.Version);
    }

    [Theory]
    [InlineData("ПК", "Новая", 2, 2)]
    [InlineData("PC2", " ", 2, 2)]
    [InlineData("PC2", "Новая", 0, 2)]
    [InlineData("PC2", "Новая", 2, 0)]
    public void InvalidUpdateDoesNotPartiallyChangeEntity(
        string number,
        string name,
        int typeId,
        int room)
    {
        EquipmentItem item = EquipmentItem.Create("PC1", "Исходная", 1, 1).Value;
        Result result = item.UpdateDetails(number, name, typeId, room);
        Assert.True(result.IsFailure);
        Assert.Equal("PC1", item.InventoryNumber);
        Assert.Equal("Исходная", item.Name);
        Assert.Equal(1, item.EquipmentTypeId);
        Assert.Equal(1, item.Room);
        Assert.Equal(1, item.Version);
    }
}
