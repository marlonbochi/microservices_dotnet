using System.Reflection;
using NetArchTest.Rules;

namespace Architecture.Tests;

/// <summary>
/// Guards the Clean Architecture dependency rule (constitution principle II) and service autonomy
/// (principle III) for every layered microservice.
/// </summary>
public sealed class LayerDependencyTests
{
    private const string EntityFramework = "Microsoft.EntityFrameworkCore";
    private const string MassTransit = "MassTransit";
    private const string AspNetCore = "Microsoft.AspNetCore";

    private static readonly string[] ServiceNames = ["Catalog", "Inventory", "Ordering"];

    public static TheoryData<string> Services => [.. ServiceNames];

    private static Assembly Layer(string service, string layer) => Assembly.Load($"{service}.{layer}");

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_DoesNotDependOnFrameworksOrOuterLayers(string service)
    {
        var result = Types.InAssembly(Layer(service, "Domain"))
            .ShouldNot()
            .HaveDependencyOnAny(EntityFramework, MassTransit, AspNetCore,
                $"{service}.Application", $"{service}.Infrastructure", $"{service}.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_DoesNotDependOnInfrastructureConcerns(string service)
    {
        var result = Types.InAssembly(Layer(service, "Application"))
            .ShouldNot()
            .HaveDependencyOnAny(EntityFramework, MassTransit, AspNetCore,
                $"{service}.Infrastructure", $"{service}.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Infrastructure_DoesNotDependOnApi(string service)
    {
        var result = Types.InAssembly(Layer(service, "Infrastructure"))
            .ShouldNot()
            .HaveDependencyOn($"{service}.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Service_DoesNotDependOnOtherServices(string service)
    {
        var others = ServiceNames.Where(other => other != service).ToArray();

        foreach (var layer in new[] { "Domain", "Application", "Infrastructure", "Api" })
        {
            var result = Types.InAssembly(Layer(service, layer))
                .ShouldNot()
                .HaveDependencyOnAny(others)
                .GetResult();

            result.IsSuccessful.ShouldBeTrue(Describe(result));
        }
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void DomainAndApplicationClasses_AreSealed(string service)
    {
        foreach (var layer in new[] { "Domain", "Application" })
        {
            var result = Types.InAssembly(Layer(service, layer))
                .That().AreClasses().And().AreNotAbstract().And().AreNotStatic().And().AreNotNested()
                .Should().BeSealed()
                .GetResult();

            result.IsSuccessful.ShouldBeTrue(Describe(result));
        }
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        $"Violations: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
