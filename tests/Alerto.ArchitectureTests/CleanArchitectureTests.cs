using System.Reflection;
using FluentAssertions;

namespace Alerto.ArchitectureTests;

public sealed class CleanArchitectureTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Outer_Layers()
    {
        var references = typeof(Alerto.Domain.Common.BaseEntity).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        references.Should().NotContain(["Alerto.Application", "Alerto.Infrastructure", "Alerto.Api"]);
    }

    [Fact]
    public void Application_Should_Depends_Only_On_Domain_And_Common_Libraries()
    {
        var references = typeof(Alerto.Application.DependencyInjection).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        references.Should().Contain("Alerto.Domain");
        references.Should().NotContain(["Alerto.Infrastructure", "Alerto.Api"]);
    }

    [Fact]
    public void Infrastructure_Should_Implement_Application_Ports()
    {
        var references = typeof(Alerto.Infrastructure.DependencyInjection).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        references.Should().Contain(new[] { "Alerto.Application", "Alerto.Domain" });
    }

    [Fact]
    public void Api_Should_Be_The_Only_Project_Exposing_Controllers()
    {
        var apiControllers = typeof(Program).Assembly
            .GetTypes()
            .Where(type => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .ToArray();

        apiControllers.Should().NotBeEmpty();

        typeof(Alerto.Application.DependencyInjection).Assembly
            .GetTypes()
            .Should()
            .NotContain(type => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type));

        typeof(Alerto.Infrastructure.DependencyInjection).Assembly
            .GetTypes()
            .Should()
            .NotContain(type => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type));
    }
}
