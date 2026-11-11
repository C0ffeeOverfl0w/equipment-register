namespace Equipment.Infrastructure.Commands;

/// <inheritdoc />
internal sealed class EfEquipmentWriteStore : IEquipmentWriteStore
{
    private readonly EquipmentDbContext _context;

    public EfEquipmentWriteStore(
        EquipmentDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentDto>> CreateAsync(
        EquipmentItem item,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);

        await _context.Database.ExecuteSqlRawAsync(DatabaseLocks.CreateEquipment, cancellationToken);
        if (await _context.Equipment.CountAsync(cancellationToken) >= maximumCount)
        {
            return Result.Failure<EquipmentDto>(EquipmentErrors.LimitReached);
        }

        string? typeName = await GetTypeNameAsync(item.EquipmentTypeId, cancellationToken);
        if (typeName is null)
        {
            return Result.Failure<EquipmentDto>(EquipmentErrors.UnknownType);
        }

        _context.Equipment.Add(item);
        Result result = await SaveChangesAsync(cancellationToken);
        if (result.IsFailure)
        {
            _context.Entry(item).State = EntityState.Detached;
            return Result.Failure<EquipmentDto>(result.Error);
        }

        await transaction.CommitAsync(cancellationToken);
        return ToDto(item, typeName);
    }

    /// <inheritdoc />
    public Task<EquipmentItem?> FindForUpdateAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return _context.Equipment
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentDto>> SaveUpdatedAsync(
        EquipmentItem item,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        string? typeName = await GetTypeNameAsync(item.EquipmentTypeId, cancellationToken);
        if (typeName is null)
        {
            _context.Entry(item).State = EntityState.Detached;
            return Result.Failure<EquipmentDto>(EquipmentErrors.UnknownType);
        }

        _context.Entry(item).Property(entity => entity.Version).OriginalValue = expectedVersion;
        Result result = await SaveChangesAsync(cancellationToken);
        if (result.IsFailure)
        {
            _context.Entry(item).State = EntityState.Detached;
        }

        return result.IsSuccess ? ToDto(item, typeName) : Result.Failure<EquipmentDto>(result.Error);
    }

    private Task<string?> GetTypeNameAsync(
        int typeId,
        CancellationToken cancellationToken)
    {
        return _context.EquipmentTypes
            .Where(type => type.Id == typeId)
            .Select(type => type.Name)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<Result> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(EquipmentErrors.VersionConflict);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: DatabaseConstraints.EquipmentInventoryNumberUnique })
        {
            return Result.Failure(EquipmentErrors.InventoryNumberConflict);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.ForeignKeyViolation, ConstraintName: DatabaseConstraints.EquipmentTypeForeignKey })
        {
            return Result.Failure(EquipmentErrors.UnknownType);
        }
    }

    private static EquipmentDto ToDto(
        EquipmentItem item,
        string typeName)
    {
        return new EquipmentDto(
            item.Id,
            item.InventoryNumber,
            item.Name,
            item.EquipmentTypeId,
            typeName,
            item.Room,
            item.Version);
    }
}
