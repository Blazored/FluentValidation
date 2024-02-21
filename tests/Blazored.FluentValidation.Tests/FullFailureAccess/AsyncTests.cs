using System;
using Blazored.FluentValidation.Tests.Model;

namespace Blazored.FluentValidation.Tests.FullFailureAccess;

public class AsyncTests : TestContext
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void GetFailuresFromLastValidation_PersonValid_ResultIsValid()
    {
        // Arrange
        var person = _fixture.ValidPerson();
        var cut = RenderComponent<AsyncComponent>();
        
        // Act
        FillForm(cut, person);
        cut.Find("button").Click();
        cut.WaitForState(() => cut.Instance.Result is not null);
        
        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Valid);
    }
    
    [Fact]
    public void GetFailuresFromLastValidation_EmailInvalid_ResultIsError()
    {
        // Arrange
        var person = _fixture.ValidPerson() with { EmailAddress = "invalid-email" };
        var cut = RenderComponent<AsyncComponent>();
        
        // Act
        FillForm(cut, person);
        cut.Find("button").Click();
        cut.WaitForState(() => cut.Instance.Result is not null);
        
        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Error);
    }
    
    [Fact]
    public void GetFailuresFromLastValidation_AgeSuspect_ResultIsWarning()
    {
        // Arrange
        var person = _fixture.ValidPerson() with { Age = 69 };
        var cut = RenderComponent<AsyncComponent>();
        
        // Act
        FillForm(cut, person);
        cut.Find("button").Click();
        cut.WaitForState(() => cut.Instance.Result is not null);
        
        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Warning);
    }
   

    private static void FillForm(IRenderedComponent<AsyncComponent> cut, Person person)
    {
        cut.Find($"input[name={nameof(Person.FirstName)}]").Change(person.FirstName);
        cut.Find($"input[name={nameof(Person.LastName)}]").Change(person.LastName);
        cut.Find($"input[name={nameof(Person.EmailAddress)}]").Change(person.EmailAddress);
        cut.Find($"input[name={nameof(Person.Age)}]").Change(person.Age.ToString());
    }

    private class Fixture
    {
        public Person ValidPerson() => new()
        {
            FirstName = "John",
            LastName = "Doe",
            EmailAddress = "john.doe@blazored.com",
            Age = 30
        };
    }
}