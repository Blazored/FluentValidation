using Blazored.FluentValidation.Tests.Model;

namespace Blazored.FluentValidation.Tests.AssemblyScanning;

public class Tests : TestContext
{
    private readonly Fixture _fixture = new();
    
    [Fact]
    public void DisableAssemblyScanning_SetToTrue_NoValidationHappens()
    {
        // Arrange
        var cut = RenderComponent<Component>(p => p.Add(c => c.DisableAssemblyScanning, true));
        var person = _fixture.InvalidPerson();

        // Act
        cut.Find($"input[name={nameof(Person.FirstName)}]").Change(person.FirstName);
        cut.Find("button").Click();

        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Valid);
    }

    [Fact]
    public void DisableAssemblyScanning_SetToFalse_ValidationHappens()
    {
        // Arrange
        var cut = RenderComponent<Component>(p => p.Add(c => c.DisableAssemblyScanning, false));
        var person = _fixture.InvalidPerson();

        // Act
        cut.Find($"input[name={nameof(Person.FirstName)}]").Change(person.FirstName);
        cut.Find("button").Click();

        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Error);
    }

    [Fact]
    public void DisableAssemblyScanning_NotSet_ValidationHappens()
    {
        // Arrange
        var cut = RenderComponent<Component>(p => p.Add(c => c.DisableAssemblyScanning, null));
        var person = _fixture.InvalidPerson();

        // Act
        cut.Find($"input[name={nameof(Person.FirstName)}]").Change(person.FirstName);
        cut.Find("button").Click();

        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Error);
    }
    
    private class Fixture
    {
        public Person InvalidPerson() => new()
        {
            FirstName = "",
            LastName = "Doe",
            EmailAddress = "john.doe@blazored.org",
            Age = 30
        };
    }
}