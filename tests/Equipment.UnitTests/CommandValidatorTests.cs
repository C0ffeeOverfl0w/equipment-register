namespace Equipment.UnitTests;

public sealed class CommandValidatorTests
{
    [Fact]
    public async Task InvalidDetailsProduceFlatClientFieldNames()
    {
        var validator = new CreateEquipmentCommandValidator();
        ValidationResult result = await validator.ValidateAsync(new CreateEquipmentCommand(new EquipmentDetails("ПК", " ", 0, 1001)));
        Assert.Equal(["equipmentTypeId", "inventoryNumber", "name", "room"], result.Errors.Select(error => error.PropertyName)
            .Order());
    }

    [Fact]
    public async Task NullDetailsAreRejectedWithoutAccessingNestedProperties()
    {
        var validator = new CreateEquipmentCommandValidator();
        ValidationResult result = await validator.ValidateAsync(new CreateEquipmentCommand(null!));
        Assert.Equal("details", Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public async Task UnicodeBoundaryAndLeadingZerosAreAccepted()
    {
        var validator = new CreateEquipmentCommandValidator();
        var command = new CreateEquipmentCommand(new EquipmentDetails("000PC", string.Concat(Enumerable.Repeat("🖥", 256)), 1, 1000));
        Assert.True((await validator.ValidateAsync(command)).IsValid);
    }

    [Theory]
    [InlineData("Device\0")]
    [InlineData(" ")]
    public async Task InvalidNamesAreRejectedBeforeHandler(
        string name)
    {
        var validator = new CreateEquipmentCommandValidator();
        ValidationResult result = await validator.ValidateAsync(new CreateEquipmentCommand(new EquipmentDetails("PC1", name, 1, 1)));
        Assert.Equal("name", Assert.Single(result.Errors).PropertyName);
    }
}
