namespace Equipment.Domain;

public static class EquipmentRules
{
    public const int MaximumEquipmentCount = 2000;
    public const int InventoryNumberMaxLength = 32;
    public const int NameMaxLength = 256;
    public const int TypeNameMaxLength = 128;
    public const int MinimumRoom = 1;
    public const int MaximumRoom = 1000;

    /// <summary>
    /// Проверяет алфавит и длину учётного номера
    /// </summary>
    public static bool IsValidInventoryNumber(
        string? value)
    {
        return !string.IsNullOrEmpty(value)
            && value.Length <= InventoryNumberMaxLength
            && value.All(char.IsAsciiLetterOrDigit);
    }

    /// <summary>
    /// Проверяет непустое имя и длину в Unicode-символах по правилам PostgreSQL
    /// </summary>
    public static bool IsValidName(
        string? value,
        int maximumLength)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.EnumerateRunes().Count() <= maximumLength
            && !value.Contains('\0');
    }

    internal static Result ValidateName(
        string? value,
        int maximumLength,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure(new Error("Equipment.NameRequired", "Укажите наименование.", ErrorType.Validation, field));
        }

        if (!IsValidName(value, maximumLength))
        {
            return Result.Failure(new Error("Equipment.InvalidName", $"Допустимо не более {maximumLength} символов без нулевого символа.", ErrorType.Validation, field));
        }

        return Result.Success();
    }
}
