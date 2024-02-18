using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using static FluentValidation.AssemblyScanner;

namespace Blazored.FluentValidation;

public static class EditContextFluentValidationExtensions
{
    private static readonly char[] Separators = { '.', '[' };
    private static readonly List<string> ScannedAssembly = new();
    private static readonly List<AssemblyScanResult> AssemblyScanResults = new();
    public const string PendingAsyncValidation = "AsyncValidationTask";

    public static void AddFluentValidation(this EditContext editContext, IServiceProvider serviceProvider, bool disableAssemblyScanning, IValidator? validator, FluentValidationValidator fluentValidationValidator)
    {
        ArgumentNullException.ThrowIfNull(editContext, nameof(editContext));

        var messages = new ValidationMessageStore(editContext);

        editContext.OnValidationRequested +=
            async (sender, _) => await ValidateModel((EditContext)sender!, messages, serviceProvider, disableAssemblyScanning, fluentValidationValidator, validator);
        
        editContext.OnFieldChanged +=
            async (_, eventArgs) => await ValidateField(editContext, messages, eventArgs.FieldIdentifier, serviceProvider, disableAssemblyScanning, fluentValidationValidator, validator);
    }

    private static async Task ValidateModel(EditContext editContext,
        ValidationMessageStore messages,
        IServiceProvider serviceProvider,
        bool disableAssemblyScanning,
        FluentValidationValidator fluentValidationValidator,
        IValidator? validator = null)
    {
        validator ??= GetValidatorForModel(serviceProvider, editContext.Model, disableAssemblyScanning);

        if (validator is not null)
        {
            var context = ConstructValidationContext(editContext, fluentValidationValidator);

            var asyncValidationTask = validator.ValidateAsync(context);
            editContext.Properties[PendingAsyncValidation] = asyncValidationTask;
            var validationResults = await asyncValidationTask;

            messages.Clear();
            fluentValidationValidator.LastValidationResult = new Dictionary<FieldIdentifier, List<ValidationFailure>>();
            
            foreach (var validationResult in validationResults.Errors)
            {
                var fieldIdentifier = ToFieldIdentifier(editContext, validationResult.PropertyName);
                messages.Add(fieldIdentifier, validationResult.ErrorMessage);
                
                if (fluentValidationValidator.LastValidationResult.TryGetValue(fieldIdentifier, out var failures))
                {
                    failures.Add(validationResult);
                }
                else
                {
                    fluentValidationValidator.LastValidationResult.Add(fieldIdentifier, new List<ValidationFailure> { validationResult });
                }
            }

            editContext.NotifyValidationStateChanged();
        }
    }

    private static async Task ValidateField(EditContext editContext,
        ValidationMessageStore messages,
        FieldIdentifier fieldIdentifier,
        IServiceProvider serviceProvider,
        bool disableAssemblyScanning,
        FluentValidationValidator fluentValidationValidator,
        IValidator? validator = null)
    {
        var propertyPath = PropertyPathHelper.ToFluentPropertyPath(editContext, fieldIdentifier);

        if (string.IsNullOrEmpty(propertyPath))
        {
            return;
        }
        
        var context = ConstructValidationContext(editContext, fluentValidationValidator);

        var fluentValidationValidatorSelector = context.Selector;
        var changedPropertySelector = ValidationContext<object>.CreateWithOptions(editContext.Model, strategy =>
        {
            strategy.IncludeProperties(propertyPath);
        }).Selector;
        
        var compositeSelector =
            new IntersectingCompositeValidatorSelector(new[] { fluentValidationValidatorSelector, changedPropertySelector });

        validator ??= GetValidatorForModel(serviceProvider, editContext.Model, disableAssemblyScanning);
        
        if (validator is not null)
        {
            var validationResults = await validator.ValidateAsync(new ValidationContext<object>(editContext.Model, new PropertyChain(), compositeSelector));
            var errorMessages = validationResults.Errors
                .Where(validationFailure => validationFailure.PropertyName == propertyPath)
                .Select(validationFailure => validationFailure.ErrorMessage)
                .Distinct();

            messages.Clear(fieldIdentifier);
            messages.Add(fieldIdentifier, errorMessages);

            editContext.NotifyValidationStateChanged();
        }
    }

    private static ValidationContext<object> ConstructValidationContext(EditContext editContext,
        FluentValidationValidator fluentValidationValidator)
    {
        ValidationContext<object> context;

        if (fluentValidationValidator.ValidateOptions is not null)
        {
            context = ValidationContext<object>.CreateWithOptions(editContext.Model, fluentValidationValidator.ValidateOptions);
        }
        else if (fluentValidationValidator.Options is not null)
        {
            context = ValidationContext<object>.CreateWithOptions(editContext.Model, fluentValidationValidator.Options);
        }
        else
        {
            context = new ValidationContext<object>(editContext.Model);
        }

        return context;
    }

    private static IValidator? GetValidatorForModel(IServiceProvider serviceProvider, object model, bool disableAssemblyScanning)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(model.GetType());
        try
        {
            if (serviceProvider.GetService(validatorType) is IValidator validator)
            {
                return validator;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        if (disableAssemblyScanning)
        {
            return null;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(i => i.FullName is not null && !ScannedAssembly.Contains(i.FullName)))
        {
            try
            {
                AssemblyScanResults.AddRange(FindValidatorsInAssembly(assembly));
            }
            catch (Exception)
            {
                // ignored
            }

            ScannedAssembly.Add(assembly.FullName!);
        }


        var interfaceValidatorType = typeof(IValidator<>).MakeGenericType(model.GetType());
        var modelValidatorType = AssemblyScanResults.FirstOrDefault(i => interfaceValidatorType.IsAssignableFrom(i.InterfaceType))?.ValidatorType;

        if (modelValidatorType is null)
        {
            return null;
        }

        return (IValidator)ActivatorUtilities.CreateInstance(serviceProvider, modelValidatorType);
    }

    private static FieldIdentifier ToFieldIdentifier(in EditContext editContext, in string propertyPath)
    {
        // This code is taken from an article by Steve Sanderson (https://blog.stevensanderson.com/2019/09/04/blazor-fluentvalidation/)
        // all credit goes to him for this code.

        // This method parses property paths like 'SomeProp.MyCollection[123].ChildProp'
        // and returns a FieldIdentifier which is an (instance, propName) pair. For example,
        // it would return the pair (SomeProp.MyCollection[123], "ChildProp"). It traverses
        // as far into the propertyPath as it can go until it finds any null instance.

        var obj = editContext.Model;
        var nextTokenEnd = propertyPath.IndexOfAny(Separators);
            
        // Optimize for a scenario when parsing isn't needed.
        if (nextTokenEnd < 0)
        {
            return new FieldIdentifier(obj, propertyPath);
        }

        ReadOnlySpan<char> propertyPathAsSpan = propertyPath;

        while (true)
        {
            var nextToken = propertyPathAsSpan.Slice(0, nextTokenEnd);
            propertyPathAsSpan = propertyPathAsSpan.Slice(nextTokenEnd + 1);

            object? newObj;
            if (nextToken.EndsWith("]"))
            {
                // It's an indexer
                // This code assumes C# conventions (one indexer named Item with one param)
                nextToken = nextToken.Slice(0, nextToken.Length - 1);
                var prop = obj.GetType().GetProperty("Item");

                if (prop is not null)
                {
                    // we've got an Item property
                    var indexerType = prop.GetIndexParameters()[0].ParameterType;
                    var indexerValue = Convert.ChangeType(nextToken.ToString(), indexerType);
                        
                    newObj = prop.GetValue(obj, new [] { indexerValue });
                }
                else
                {
                    // If there is no Item property
                    // Try to cast the object to array
                    if (obj is object[] array)
                    {
                        var indexerValue = int.Parse(nextToken);
                        newObj = array[indexerValue];
                    }
                    else
                    {
                        throw new InvalidOperationException($"Could not find indexer on object of type {obj.GetType().FullName}.");
                    }
                }
            }
            else
            {
                // It's a regular property
                var prop = obj.GetType().GetProperty(nextToken.ToString());
                if (prop == null)
                {
                    throw new InvalidOperationException($"Could not find property named {nextToken.ToString()} on object of type {obj.GetType().FullName}.");
                }
                newObj = prop.GetValue(obj);
            }

            if (newObj == null)
            {
                // This is as far as we can go
                return new FieldIdentifier(obj, nextToken.ToString());
            }

            obj = newObj;
                
            nextTokenEnd = propertyPathAsSpan.IndexOfAny(Separators);
            if (nextTokenEnd < 0)
            {
                return new FieldIdentifier(obj, propertyPathAsSpan.ToString());
            }
        }
    }
}