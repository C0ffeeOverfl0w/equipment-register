namespace Equipment.UnitTests;

public sealed class EquipmentRulesTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public void CreatePreservesNumberAndArbitraryName(
        int room)
    {
        EquipmentItem item = EquipmentItem.Create("000Pc12", "ПК <script> & 'кавычки' 🖥", 1, room).Value;
        Assert.Equal("000Pc12", item.InventoryNumber);
        Assert.Equal("ПК <script> & 'кавычки' 🖥", item.Name);
        Assert.Equal(room, item.Room);
        Assert.Equal(1, item.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PC-1")]
    [InlineData("ПК1")]
    [InlineData("PC 1")]
    [InlineData("PC1\n")]
    public void InvalidNumberIsRejected(
        string? number)
    {
        Result<EquipmentItem> result = EquipmentItem.Create(number!, "Техника", 1, 1);
        Assert.Equal(EquipmentErrors.InvalidInventoryNumber, result.Error);
    }

    [Fact]
    public void NumberLengthBoundaryIsEnforced()
    {
        Assert.True(EquipmentItem.Create(new string('A', 32), "Техника", 1, 1).IsSuccess);
        Assert.True(EquipmentItem.Create(new string('A', 33), "Техника", 1, 1).IsFailure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void InvalidRoomIsRejected(
        int room)
    {
        Assert.Equal(EquipmentErrors.InvalidRoom, EquipmentItem.Create("PC1", "Техника", 1, room).Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\n")]
    [InlineData("Техника\0")]
    public void InvalidNameIsRejected(
        string? name)
    {
        Assert.True(EquipmentItem.Create("PC1", name!, 1, 1).IsFailure);
    }

    [Fact]
    public void NameLengthCountsUnicodeCharacters()
    {
        Assert.True(EquipmentItem.Create("PC1", string.Concat(Enumerable.Repeat("🖥", 256)), 1, 1).IsSuccess);
        Assert.True(EquipmentItem.Create("PC1", new string('А', 257), 1, 1).IsFailure);
    }

    [Fact]
    public void TypeNameHasItsOwnLengthBoundary()
    {
        Assert.True(EquipmentType.Create(new string('А', 128)).IsSuccess);
        Assert.True(EquipmentType.Create(new string('А', 129)).IsFailure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MissingTypeIdIsRejected(
        int typeId)
    {
        Assert.Equal(EquipmentErrors.InvalidType, EquipmentItem.Create("PC1", "Техника", typeId, 1).Error);
    }
}
