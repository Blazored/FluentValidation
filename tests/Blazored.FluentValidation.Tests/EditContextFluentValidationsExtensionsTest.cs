using System;
using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Blazored.FluentValidation.Tests
{
    public class EditContextFluentValidationsExtensionsTest
    {
        internal class TopLevelModelValidator : AbstractValidator<TopLevelModel>
        {
            public TopLevelModelValidator()
            {
                RuleFor(p => p.Name).NotEmpty().Length(3, 50);
                RuleFor(p => p.TestModel)
                    .SetValidator(new TestModelValidator());
            }
        }

        internal class TestModelValidator : AbstractValidator<TestModel>
        {
            public TestModelValidator()
            {
                RuleFor(p => p.RequiredString).NotEmpty().WithMessage("RequiredString:required");
                RuleFor(p => p.IntFrom1To100).InclusiveBetween(1, 100).WithMessage("IntFrom1To100:range");
            }
        }

        internal class TopLevelModel
        {
            public string Name { get; set; }
            public TestModel TestModel { get; set; }
        }

        internal class TestModel
        {
            public string RequiredString { get; set; }
            public int IntFrom1To100 { get; set; }
#pragma warning disable 649
            public string ThisWillNotBeValidatedBecauseItIsAField;
            private string ThisWillNotBeValidatedBecauseItIsPrivate { get; set; }
            internal string ThisWillNotBeValidatedBecauseItIsInternal { get; set; }
#pragma warning restore 649
        }

        [Theory]
        [InlineData(nameof(TestModel.ThisWillNotBeValidatedBecauseItIsAField))]
        [InlineData(nameof(TestModel.ThisWillNotBeValidatedBecauseItIsInternal))]
        [InlineData("ThisWillNotBeValidatedBecauseItIsPrivate")]
        [InlineData("This does not correspond to anything")]
        [InlineData("")]
        public void IgnoresFieldChangesThatDoNotCorrespondToAValidatableProperty(string fieldName)
        {
            // arrange
            var serviceProvider = new ServiceCollection()
                .AddTransient<IValidator<TestModel>, TestModelValidator>()
                .BuildServiceProvider();
            var editContext =
                new EditContext(new TestModel()).AddFluentValidation(
                    new ServiceProviderValidatorFactory(serviceProvider));
            var onValidationStateChangedCount = 0;
            editContext.OnValidationStateChanged += (sender, eventArgs) => onValidationStateChangedCount++;

            // act && assert: Ignores field changes that don't correspond to a validatable property
            editContext.NotifyFieldChanged(editContext.Field(fieldName));
            onValidationStateChangedCount.ShouldBe(0);

            // act && assert: For sanity, observe that we would have validated if it was a validatable property
            editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.RequiredString)));
            onValidationStateChangedCount.ShouldBe(1);
        }

        [Fact]
        public void CannotUseNullEditContext()
        {
            // arrange
            var editContext = (EditContext) null;
            var serviceProvider = new ServiceCollection()
                .AddTransient<IValidatorFactory, ServiceProviderValidatorFactory>().BuildServiceProvider();

            // act && assert
            var ex = Should.Throw<ArgumentNullException>(() =>
                editContext.AddFluentValidation(new ServiceProviderValidatorFactory(serviceProvider)));
            ex.ParamName.ShouldBe("editContext");
        }

        [Fact]
        public void CannotUseNullValidatorFactory()
        {
            // arrange
            var editContext = new EditContext(new object());

            // act && assert
            var ex = Should.Throw<ArgumentNullException>(() => editContext.AddFluentValidation(null));
            ex.ParamName.ShouldBe("validatorFactory");
        }

        [Fact]
        public void ClearsExistingValidationMessagesOnFurtherRuns()
        {
            // arrange
            var serviceProvider = new ServiceCollection()
                .AddTransient<IValidator<TestModel>, TestModelValidator>()
                .BuildServiceProvider();

            var model = new TestModel {IntFrom1To100 = 101};
            var editContext =
                new EditContext(model).AddFluentValidation(new ServiceProviderValidatorFactory(serviceProvider));

            // act && assert: initially invalid
            editContext.Validate().ShouldBeFalse();

            // act && assert: becomes valid
            model.RequiredString = "Hello";
            model.IntFrom1To100 = 100;

            editContext.Validate().ShouldBeTrue();
        }

        [Fact]
        public void GetsValidationMessageFromFluentValidation()
        {
            // arrange
            var serviceProvider = new ServiceCollection()
                .AddTransient<IValidator<TestModel>, TestModelValidator>()
                .BuildServiceProvider();

            var model = new TestModel {IntFrom1To100 = 101};
            var editContext =
                new EditContext(model).AddFluentValidation(new ServiceProviderValidatorFactory(serviceProvider));

            // act
            var isValid = editContext.Validate();

            // assert
            isValid.ShouldBeFalse();

            new[]
            {
                "RequiredString:required",
                "IntFrom1To100:range"
            }.ShouldBe(editContext.GetValidationMessages());

            new[]
            {
                "RequiredString:required"
            }.ShouldBe(editContext.GetValidationMessages(editContext.Field(nameof(TestModel.RequiredString))));

            new[]
            {
                "IntFrom1To100:range"
            }.ShouldBe(editContext.GetValidationMessages(editContext.Field(nameof(TestModel.IntFrom1To100))));
        }

        [Fact]
        public void NotifiesValidationStateChangedAfterObjectValidation()
        {
            // arrange
            var serviceProvider = new ServiceCollection()
                .AddTransient<IValidator<TestModel>, TestModelValidator>()
                .BuildServiceProvider();

            var model = new TestModel {IntFrom1To100 = 101};
            var editContext =
                new EditContext(model).AddFluentValidation(new ServiceProviderValidatorFactory(serviceProvider));
            var onValidationStateChangedCount = 0;
            editContext.OnValidationStateChanged += (sender, eventArgs) => onValidationStateChangedCount++;

            // act && assert: notifies after invalid results
            editContext.Validate().ShouldBeFalse();
            onValidationStateChangedCount.ShouldBe(1);

            // act && assert: notifies after valid results
            model.RequiredString = "Hello";
            model.IntFrom1To100 = 100;
            editContext.Validate().ShouldBeTrue();
            onValidationStateChangedCount.ShouldBe(2);

            // act && assert: notifies even if results haven't changed
            editContext.Validate().ShouldBeTrue();
            onValidationStateChangedCount.ShouldBe(3);
        }

        [Fact]
        public void PerformsPerPropertyValidationOnFieldChange()
        {
            // arrange
            var serviceProvider = new ServiceCollection()
                .AddTransient<IValidator<TestModel>, TestModelValidator>()
                .AddTransient<IValidator<TopLevelModel>, TopLevelModelValidator>()
                .BuildServiceProvider();

            var independentTopLevelModel = new TopLevelModel();
            var model = new TestModel {IntFrom1To100 = 101};
            var editContext =
                new EditContext(independentTopLevelModel).AddFluentValidation(
                    new ServiceProviderValidatorFactory(serviceProvider));
            var onValidationStateChangedCount = 0;
            var requiredStringIdentifier = new FieldIdentifier(model, nameof(TestModel.RequiredString));
            var intFrom1To100Identifier = new FieldIdentifier(model, nameof(TestModel.IntFrom1To100));
            editContext.OnValidationStateChanged += (sender, eventArgs) => onValidationStateChangedCount++;

            // act && assert: notify about RequiredString
            editContext.NotifyFieldChanged(requiredStringIdentifier);
            onValidationStateChangedCount.ShouldBe(1);
            new[] {"RequiredString:required"}.ShouldBe(editContext.GetValidationMessages());

            // act && assert: fix RequiredString, but only notify about IntFrom1To100
            model.RequiredString = "Hello";
            editContext.NotifyFieldChanged(intFrom1To100Identifier);
            onValidationStateChangedCount.ShouldBe(2);
            new[]
            {
                "RequiredString:required",
                "IntFrom1To100:range"
            }.ShouldBe(editContext.GetValidationMessages());

            // act && assert
            editContext.NotifyFieldChanged(requiredStringIdentifier);
            onValidationStateChangedCount.ShouldBe(3);
            new[] {"IntFrom1To100:range"}.ShouldBe(editContext.GetValidationMessages());
        }

        [Fact]
        public void ReturnsEditContextForChaining()
        {
            // arrange
            var editContext = new EditContext(new object());
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            // act
            var returnValue = editContext.AddFluentValidation(new ServiceProviderValidatorFactory(serviceProvider));

            // assert
            editContext.ShouldBeSameAs(returnValue);
        }

        [Fact]
        public void ShouldNotValidateNestedModelsIfRuleDoesNotExist()
        {
            // arrange
            var serviceProvider = new ServiceCollection()
                .AddTransient<IValidator<TopLevelModel>, TopLevelModelValidator>()
                .BuildServiceProvider();

            var model = new TopLevelModel
            {
                Name = "ab",
                TestModel = new TestModel()
            };
            var editContext =
                new EditContext(model).AddFluentValidation(new ServiceProviderValidatorFactory(serviceProvider));
            var onValidationStateChangedCount = 0;
            var requiredNameIdentifier = new FieldIdentifier(model, nameof(TopLevelModel.Name));
            var requiredStringIdentifier = new FieldIdentifier(model, nameof(TestModel.RequiredString));
            editContext.OnValidationStateChanged += (sender, eventArgs) => onValidationStateChangedCount++;

            // act && assert
            editContext.NotifyFieldChanged(requiredStringIdentifier);
            onValidationStateChangedCount.ShouldBe(0);

            // act && assert
            editContext.NotifyFieldChanged(requiredNameIdentifier);
            onValidationStateChangedCount.ShouldBe(1);
        }
    }
}