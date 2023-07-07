using FluentValidation;
using FluentValidation.Internal;
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

    public static IDisposable AddFluentValidation(this EditContext editContext, IServiceProvider serviceProvider, bool disableAssemblyScanning, IValidator? validator, FluentValidationValidator fluentValidationValidator)
    {
        ArgumentNullException.ThrowIfNull(editContext, nameof(editContext));

        return new FluentValidationEventSubscriptions(editContext, serviceProvider, disableAssemblyScanning, validator, fluentValidationValidator);
    }



    private sealed class FluentValidationEventSubscriptions : IDisposable
    {
        private readonly EditContext _editContext;
        private readonly IServiceProvider? _serviceProvider;
        private readonly ValidationMessageStore _messages;
        private readonly bool _disableAssemblyScanning;
        private readonly FluentValidationValidator _fluentValidationValidator;
        private IValidator? _validator;

        public FluentValidationEventSubscriptions(EditContext editContext, IServiceProvider serviceProvider, bool disableAssemblyScanning, IValidator? validator, FluentValidationValidator fluentValidationValidator)
        {
            _editContext = editContext ?? throw new ArgumentNullException(nameof(editContext));
            _serviceProvider = serviceProvider;
            _messages = new ValidationMessageStore(_editContext);
            _disableAssemblyScanning = disableAssemblyScanning;
            _validator = validator;
            _fluentValidationValidator = fluentValidationValidator;

            editContext.OnValidationRequested += OnValidationRequestedHandler;
            editContext.OnFieldChanged += OnFieldChangedHandler;
        }

        private async void OnFieldChangedHandler(object? sender, FieldChangedEventArgs e)
        {
            await ValidateField(sender, e);
        }

        private async void OnValidationRequestedHandler(object? sender, ValidationRequestedEventArgs e)
        {
            await ValidateModel(sender, e);
        }

        private async Task ValidateModel(object? sender, ValidationRequestedEventArgs e)
        {
            _validator ??= GetValidatorForModel(_serviceProvider, _editContext.Model, _disableAssemblyScanning);

            if (_validator is not null)
            {
                ValidationContext<object> context;

                if (_fluentValidationValidator.ValidateOptions is not null)
                {
                    context = ValidationContext<object>.CreateWithOptions(_editContext.Model, _fluentValidationValidator.ValidateOptions);
                }
                else if (_fluentValidationValidator.Options is not null)
                {
                    context = ValidationContext<object>.CreateWithOptions(_editContext.Model, _fluentValidationValidator.Options);
                }
                else
                {
                    context = new ValidationContext<object>(_editContext.Model);
                }

                var asyncValidationTask = _validator.ValidateAsync(context);
                _editContext.Properties[PendingAsyncValidation] = asyncValidationTask;
                var validationResults = await asyncValidationTask;

                _messages.Clear();
                foreach (var validationResult in validationResults.Errors)
                {
                    var fieldIdentifier = ToFieldIdentifier(_editContext, validationResult.PropertyName);
                    _messages.Add(fieldIdentifier, validationResult.ErrorMessage);
                }

                _editContext.NotifyValidationStateChanged();
            }
        }

        private async Task ValidateField(object? sender, FieldChangedEventArgs e)
        {
            var properties = new[] { e.FieldIdentifier.FieldName };
            var context = new ValidationContext<object>(e.FieldIdentifier.Model, new PropertyChain(), new MemberNameValidatorSelector(properties));

            _validator ??= GetValidatorForModel(_serviceProvider, e.FieldIdentifier.Model, _disableAssemblyScanning);

            if (_validator is not null)
            {
                var validationResults = await _validator.ValidateAsync(context);

                _messages.Clear(e.FieldIdentifier);
                _messages.Add(e.FieldIdentifier, validationResults.Errors.Select(error => error.ErrorMessage));

                _editContext.NotifyValidationStateChanged();
            }
        }

        private IValidator? GetValidatorForModel(IServiceProvider serviceProvider, object model, bool disableAssemblyScanning)
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

        private FieldIdentifier ToFieldIdentifier(in EditContext editContext, in string propertyPath)
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

                        newObj = prop.GetValue(obj, new[] { indexerValue });
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

        public void Dispose()
        {
            _messages.Clear();
            _editContext.OnFieldChanged -= OnFieldChangedHandler;
            _editContext.OnValidationRequested -= OnValidationRequestedHandler;
            _editContext.NotifyValidationStateChanged();
        }
    }
}