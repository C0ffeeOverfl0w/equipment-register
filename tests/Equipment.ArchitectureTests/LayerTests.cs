namespace Equipment.ArchitectureTests;

public sealed class LayerTests
{
    [Theory]
    [InlineData("Equipment.Domain", new string[0])]
    [InlineData("Equipment.Application", new[] { "Equipment.Domain" })]
    [InlineData("Equipment.Infrastructure", new[] { "Equipment.Application", "Equipment.Domain" })]
    [InlineData("Equipment.Web", new[] { "Equipment.Application", "Equipment.Infrastructure" })]
    public void ProjectReferencesFollowDependencyRule(
        string project,
        string[] allowedReferences)
    {
        XDocument document = ReadProject(project);
        string[] references = [.. document.Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension(element.Attribute("Include")!.Value))
            .Order()];
        Assert.Equal(allowedReferences.Order(), references);

        if (project is "Equipment.Domain" or "Equipment.Application")
        {
            Assert.Empty(document.Descendants("FrameworkReference"));
            Assert.All(document.Descendants("PackageReference"), element =>
                Assert.Contains(element.Attribute("Include")!.Value, new[]
                {
                    "Microsoft.Extensions.DependencyInjection.Abstractions",
                    "Microsoft.Extensions.Logging.Abstractions",
                    "MediatR",
                    "FluentValidation",
                    "FluentValidation.DependencyInjectionExtensions"
                }));
        }

        if (project == "Equipment.Domain")
        {
            Assert.Empty(document.Descendants("PackageReference"));
        }
    }

    [Theory]
    [InlineData(typeof(EquipmentItem))]
    [InlineData(typeof(IEquipmentReadStore))]
    public void CoreAssembliesHaveNoWebOrPersistenceDependency(
        Type marker)
    {
        Assert.All(marker.Assembly.GetReferencedAssemblies(), reference =>
        {
            string name = reference.Name!;
            Assert.False(name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || name.StartsWith("Npgsql", StringComparison.Ordinal)
                || name is "System.Net.Http" or "System.ComponentModel.Annotations"
                || name is "Equipment.Infrastructure" or "Equipment.Web", name);
        });
    }

    [Fact]
    public void InfrastructureDoesNotReferenceWeb()
    {
        Assert.DoesNotContain(typeof(EquipmentDbContext).Assembly.GetReferencedAssemblies(), reference => reference.Name == "Equipment.Web");
    }

    [Theory]
    [InlineData(typeof(EquipmentItem))]
    [InlineData(typeof(EquipmentType))]
    public void EntitiesEnforceEncapsulationWithoutOrmAttributes(
        Type entity)
    {
        ConstructorInfo[] constructors = entity.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(entity.IsSealed);
        Assert.All(constructors, constructor => Assert.True(constructor.IsPrivate));
        Assert.Contains(constructors, constructor => constructor.GetParameters().Length == 0);
        Assert.DoesNotContain(entity.GetCustomAttributesData(), attribute =>
            attribute.AttributeType.Namespace?.StartsWith("System.ComponentModel.DataAnnotations", StringComparison.Ordinal) == true);
        Assert.All(entity.GetProperties(), property =>
        {
            Assert.True(property.SetMethod is null || property.SetMethod.IsPrivate, property.Name);
            Assert.DoesNotContain(property.GetCustomAttributesData(), attribute =>
                attribute.AttributeType.Namespace?.StartsWith("System.ComponentModel.DataAnnotations", StringComparison.Ordinal) == true);
        });
    }

    [Fact]
    public void ApplicationPortsDoNotExposeInfrastructureOrQueryable()
    {
        Type[] ports = [typeof(IEquipmentReadStore), typeof(IEquipmentWriteStore)];
        foreach (MethodInfo method in ports.SelectMany(port => port.GetMethods()))
        {
            IEnumerable<Type> types = method.GetParameters().Select(parameter => parameter.ParameterType)
                .Append(method.ReturnType);
            foreach (Type type in types.SelectMany(Flatten))
            {
                Assert.DoesNotContain(type.Assembly.GetName().Name!, new[] { "Equipment.Infrastructure", "Equipment.Web" });
                Assert.False(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>));
                Assert.False(type.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true);
            }
        }
    }

    [Fact]
    public void HandlersAreInternalSealedAndUseConstructorInjection()
    {
        Type[] handlers = [.. typeof(IEquipmentReadStore).Assembly.GetTypes().Where(type => type.GetInterfaces().Any(IsHandler))];
        Assert.NotEmpty(handlers);
        foreach (Type handler in handlers)
        {
            Assert.False(handler.IsPublic, handler.Name);
            Assert.True(handler.IsSealed, handler.Name);
            Assert.EndsWith("Handler", handler.Name);
            Assert.Single(handler.GetConstructors());
            Assert.All(handler.GetFields(BindingFlags.NonPublic | BindingFlags.Instance), field =>
            {
                Assert.True(field.IsInitOnly, field.Name);
                Assert.StartsWith("_", field.Name);
                Assert.True(field.FieldType.IsInterface, field.Name);
            });
        }
    }

    [Theory]
    [InlineData(typeof(EquipmentController))]
    [InlineData(typeof(EquipmentTypesController))]
    public void ControllersDependOnUseCasesAndExposeNoEntities(
        Type controller)
    {
        Assert.All(controller.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
            parameter => Assert.Equal(typeof(ISender), parameter.ParameterType));
        Assert.All(controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), method =>
        {
            Assert.DoesNotContain(Flatten(method.ReturnType), type => type == typeof(EquipmentItem) || type == typeof(EquipmentType));
        });
    }

    [Fact]
    public void ValidatorsAreInternalSealed()
    {
        Type[] validators = [.. typeof(IEquipmentReadStore).Assembly.GetTypes()
            .Where(type => type.GetInterfaces().Any(contract => contract.IsGenericType
                && contract.GetGenericTypeDefinition() == typeof(IValidator<>)))];
        Assert.NotEmpty(validators);
        Assert.All(validators, validator =>
        {
            Assert.False(validator.IsPublic);
            Assert.True(validator.IsSealed);
            Assert.EndsWith("Validator", validator.Name);
        });
    }

    [Fact]
    public void CqrsHandlersImplementMediatrContracts()
    {
        Type[] handlers = [.. typeof(IEquipmentReadStore).Assembly.GetTypes().Where(type => type.GetInterfaces().Any(IsHandler))];
        Assert.NotEmpty(handlers);
        Assert.All(handlers, handler => Assert.Contains(handler.GetInterfaces(), contract =>
            contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));
    }

    private static bool IsHandler(
        Type type)
    {
        return type.IsGenericType
        && (type.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) || type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));
    }

    private static IEnumerable<Type> Flatten(
        Type type)
    {
        yield return type;
        foreach (Type argument in type.GetGenericArguments().SelectMany(Flatten))
        {
            yield return argument;
        }
    }

    private static XDocument ReadProject(
        string name)
    {
        return XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ProjectFiles", name + ".csproj"));
    }
}
