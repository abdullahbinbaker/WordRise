using System.Reflection;
using NetArchTest.Rules;
using Wordrise.Modules.ContentAuthoring.Api;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Extensions;
using Wordrise.Modules.Identity.Contracts;
using Wordrise.Modules.Identity.Core;
using Wordrise.Modules.Learning.Api;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain;
using Wordrise.Modules.Learning.Infrastructure.Extensions;
using Wordrise.Shared.Abstractions;
using Wordrise.Shared.Kernel;

namespace Wordrise.ArchitectureTests;

/// <summary>
/// The rules that keep the monolith modular. A module may use another module's Contracts,
/// never its internals. Register each new module in <see cref="Modules"/>.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private sealed record Module(string Name, Assembly EntryPoint, Assembly[] Assemblies, string[] InternalNamespaces);

    private static readonly Module[] Modules =
    [
        new(
            "Identity",
            typeof(IdentityModuleStartup).Assembly,
            [typeof(IdentityModuleStartup).Assembly],
            ["Wordrise.Modules.Identity.Core"]),
        new(
            "Learning",
            typeof(LearningModuleStartup).Assembly,
            [
                typeof(LearningErrors).Assembly,
                typeof(ICurrentLearner).Assembly,
                typeof(LearningInfrastructureExtensions).Assembly,
                typeof(LearningModuleStartup).Assembly,
            ],
            [
                "Wordrise.Modules.Learning.Domain",
                "Wordrise.Modules.Learning.Application",
                "Wordrise.Modules.Learning.Infrastructure",
                "Wordrise.Modules.Learning.Api",
            ]),
        new(
            "ContentAuthoring",
            typeof(ContentAuthoringModuleStartup).Assembly,
            [
                typeof(AuthoringErrors).Assembly,
                typeof(IDraftRepository).Assembly,
                typeof(AuthoringInfrastructureExtensions).Assembly,
                typeof(ContentAuthoringModuleStartup).Assembly,
            ],
            [
                "Wordrise.Modules.ContentAuthoring.Domain",
                "Wordrise.Modules.ContentAuthoring.Application",
                "Wordrise.Modules.ContentAuthoring.Infrastructure",
                "Wordrise.Modules.ContentAuthoring.Api",
            ]),
    ];

    public static TheoryData<string> ModuleNames() => [.. Modules.Select(m => m.Name)];

    private static Module Find(string name) => Modules.Single(m => m.Name == name);

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void A_module_does_not_use_another_modules_internals(string moduleName)
    {
        var module = Find(moduleName);
        var otherInternals = Modules.Where(m => m != module).SelectMany(m => m.InternalNamespaces).ToArray();

        var result = Types.InAssemblies(module.Assemblies).ShouldNot().HaveDependencyOnAny(otherInternals).GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void A_module_entry_assembly_exposes_only_its_startup(string moduleName)
    {
        var result = Types.InAssembly(Find(moduleName).EntryPoint)
            .That()
            .DoNotInherit(typeof(ModuleStartup))
            .And()
            .DoNotHaveNameMatching("^<")
            .And()
            .DoNotResideInNamespaceMatching(@"\.Migrations$")
            .ShouldNot()
            .BePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Learning_domain_depends_on_nothing_but_the_shared_kernel()
    {
        var result = Types.InAssembly(typeof(LearningErrors).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Wordrise.Modules.Learning.Application",
                "Wordrise.Modules.Learning.Infrastructure",
                "Wordrise.Modules.Learning.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Mediator")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Learning_application_does_not_know_the_database_or_the_web()
    {
        var result = Types.InAssembly(typeof(ICurrentLearner).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Wordrise.Modules.Learning.Infrastructure",
                "Wordrise.Modules.Learning.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Authoring_domain_depends_on_nothing_but_the_shared_kernel()
    {
        var result = Types.InAssembly(typeof(AuthoringErrors).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Wordrise.Modules.ContentAuthoring.Application",
                "Wordrise.Modules.ContentAuthoring.Infrastructure",
                "Wordrise.Modules.ContentAuthoring.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Anthropic",
                "Mediator")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Authoring_application_does_not_know_the_database_the_web_or_the_ai_provider()
    {
        var result = Types.InAssembly(typeof(IDraftRepository).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Wordrise.Modules.ContentAuthoring.Infrastructure",
                "Wordrise.Modules.ContentAuthoring.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Anthropic")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Only_authoring_infrastructure_talks_to_the_ai_provider()
    {
        var others = Modules
            .SelectMany(m => m.Assemblies)
            .Where(a => a != typeof(AuthoringInfrastructureExtensions).Assembly)
            .ToArray();

        var result = Types.InAssemblies(others).ShouldNot().HaveDependencyOn("Anthropic").GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Contracts_do_not_depend_on_any_module_internals()
    {
        var internals = Modules.SelectMany(m => m.InternalNamespaces).ToArray();

        var result = Types
            .InAssemblies([typeof(Roles).Assembly, typeof(Wordrise.Modules.Learning.Contracts.ILessonPublisher).Assembly])
            .ShouldNot()
            .HaveDependencyOnAny(internals)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Shared_projects_do_not_depend_on_modules_or_the_host()
    {
        var result = Types.InAssemblies([typeof(IModuleStartup).Assembly, typeof(Result).Assembly])
            .ShouldNot()
            .HaveDependencyOnAny("Wordrise.Modules", "Wordrise.Host")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(TestResult result) =>
        "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
