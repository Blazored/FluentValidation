using FluentValidation;
using FluentValidation.Internal;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using static FluentValidation.AssemblyScanner;

namespace Blazored.FluentValidation
{
    public static class EditContextFluentValidationExtensions
    {
        private static readonly char[] Separators = { '.', '[' };
        private static readonly List<string> ScannedAssembly = new List<string>();
        private static readonly List<AssemblyScanResult> AssemblyScanResults = new List<AssemblyScanResult>();

        public static EditContext AddFluentValidation(this EditContext editContext, IServiceProvider serviceProvider, bool disableAssemblyScanning, IValidator validator, FluentValidationValidator fluentValidationValidator)

        {
            if (editContext == null)
            {
                throw new ArgumentNullException(nameof(editContext));
            }

            var messages = new ValidationMessageStore(editContext);

            editContext.OnValidationRequested +=
                (sender, eventArgs) => ValidateModel((EditContext)sender, messages, serviceProvider, disableAssemblyScanning, fluentValidationValidator, validator);

            editContext.OnFieldChanged +=
                (sender, eventArgs) => ValidateField(editContext, messages, eventArgs.FieldIdentifier, serviceProvider, disableAssemblyScanning, validator);

            return editContext;
        }

        private static async void ValidateModel(EditContext editContext,
                                                ValidationMessageStore messages,
                                                IServiceProvider serviceProvider,
                                                bool disableAssemblyScanning,
                                                FluentValidationValidator fluentValidationValidator,
                                                IValidator validator = null)
        {
            validator ??= GetValidatorForModel(serviceProvider, editContext.Model, disableAssemblyScanning);

            if (validator is object)
            {
                var context = ValidationContext<object>.CreateWithOptions(editContext.Model, fluentValidationValidator.Options ?? (opt => opt.IncludeAllRuleSets()));

                var validationResults = await validator.ValidateAsync(context);

                messages.Clear();
                foreach (var validationResult in validationResults.Errors)
                {
                    var fieldIdentifier = ToFieldIdentifier(editContext, validationResult.PropertyName);
                    messages.Add(fieldIdentifier, validationResult.ErrorMessage);
                }

                editContext.NotifyValidationStateChanged();
            }
        }

        private static async void ValidateField(EditContext editContext,
                                                ValidationMessageStore messages,
                                                FieldIdentifier fieldIdentifier,
                                                IServiceProvider serviceProvider,
                                                bool disableAssemblyScanning,
                                                IValidator validator = null)
        {
            var properties = new[] { fieldIdentifier.FieldName };
            var context = new ValidationContext<object>(fieldIdentifier.Model, new PropertyChain(), new MemberNameValidatorSelector(properties));

            validator ??= GetValidatorForModel(serviceProvider, fieldIdentifier.Model, disableAssemblyScanning);

            if (validator is object)
            {
                var validationResults = await validator.ValidateAsync(context);

                messages.Clear(fieldIdentifier);
                messages.Add(fieldIdentifier, validationResults.Errors.Select(error => error.ErrorMessage));

                editContext.NotifyValidationStateChanged();
            }
        }

        private static IValidator GetValidatorForModel(IServiceProvider serviceProvider, object model, bool disableAssemblyScanning)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(model.GetType());
            if (serviceProvider != null)
            {
                try
                {
                    if (serviceProvider.GetService(validatorType) is IValidator validator)
                    {
                        return validator;
                    }
                }
                catch (Exception)
                {
                }
            }

            if (disableAssemblyScanning)
            {
                return null;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(i => !ScannedAssembly.Contains(i.FullName)))
            {
                try
                {
                    AssemblyScanResults.AddRange(FindValidatorsInAssembly(assembly));
                }
                catch (Exception)
                {
                }

                ScannedAssembly.Add(assembly.FullName);
            }


            var interfaceValidatorType = typeof(IValidator<>).MakeGenericType(model.GetType());

            Type modelValidatorType = AssemblyScanResults.FirstOrDefault(i => interfaceValidatorType.IsAssignableFrom(i.InterfaceType))?.ValidatorType;

            if (modelValidatorType == null)
            {
                return null;
            }

            return (IValidator)ActivatorUtilities.CreateInstance(serviceProvider, modelValidatorType);
        }

        private static FieldIdentifier ToFieldIdentifier(EditContext editContext, string propertyPath)
        {
            // This code is taken from an article by Steve Sanderson (https://blog.stevensanderson.com/2019/09/04/blazor-fluentvalidation/)
            // all credit goes to him for this code.

            // This method parses property paths like 'SomeProp.MyCollection[123].ChildProp'
            // and returns a FieldIdentifier which is an (instance, propName) pair. For example,
            // it would return the pair (SomeProp.MyCollection[123], "ChildProp"). It traverses
            // as far into the propertyPath as it can go until it finds any null instance.

            var obj = editContext.Model;

            while (true)
            {
                var nextTokenEnd = propertyPath.IndexOfAny(Separators);
                if (nextTokenEnd < 0)
                {
                    return new FieldIdentifier(obj, propertyPath);
                }

                var nextToken = propertyPath.Substring(0, nextTokenEnd);
                propertyPath = propertyPath.Substring(nextTokenEnd + 1);

                object newObj;
                if (nextToken.EndsWith("]"))
                {
                    // It's an indexer
                    // This code assumes C# conventions (one indexer named Item with one param)
                    nextToken = nextToken.Substring(0, nextToken.Length - 1);
                    var prop = obj.GetType().GetProperty("Item");

                    if (prop is object)
                    {
                        // we've got an Item property
                        var indexerType = prop.GetIndexParameters()[0].ParameterType;
                        var indexerValue = Convert.ChangeType(nextToken, indexerType);
                        newObj = prop.GetValue(obj, new object[] { indexerValue });
                    }
                    else
                    {
                        // If there is no Item property
                        // Try to cast the object to array
                        if (obj is object[] array)
                        {
                            var indexerValue = Convert.ToInt32(nextToken);
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
                    var prop = obj.GetType().GetProperty(nextToken);
                    if (prop == null)
                    {
                        throw new InvalidOperationException($"Could not find property named {nextToken} on object of type {obj.GetType().FullName}.");
                    }
                    newObj = prop.GetValue(obj);
                }

                if (newObj == null)
                {
                    // This is as far as we can go
                    return new FieldIdentifier(obj, nextToken);
                }

                obj = newObj;
            }
        }

        /// <summary>
        /// Runs an function that returns a value and ignores any Exceptions that occur.
        /// Returns true or falls depending on whether catch was
        /// triggered
        /// </summary>
        /// <param name="operation">parameterless lamda that returns a value of T</param>
        /// <param name="defaultValue">Default value returned if operation fails</param>
        public static T IgnoreErrors<T>(Func<T> operation, T defaultValue = default(T))
        {
            if (operation == null)
                return defaultValue;

            T result;
            try
            {
                result = operation.Invoke();
            }
            catch
            {
                result = defaultValue;
            }

            return result;
        }
    }
}
