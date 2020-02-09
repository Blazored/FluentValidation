using FluentValidation;
using FluentValidation.Internal;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Blazored.FluentValidation
{
    public static class EditContextFluentValidationExtensions
    {
        private readonly static char[] separators = new[] { '.', '[' };

        public static EditContext AddFluentValidation(this EditContext editContext, IServiceProvider serviceProvider)
        {
            return AddFluentValidation(editContext, serviceProvider, null);
        }

        public static EditContext AddFluentValidation(this EditContext editContext, IServiceProvider serviceProvider, IValidator validator)
        {
            if (editContext == null)
            {
                throw new ArgumentNullException(nameof(editContext));
            }

            var messages = new ValidationMessageStore(editContext);

            editContext.OnValidationRequested +=
                (sender, eventArgs) => ValidateModel((EditContext)sender, messages, serviceProvider, validator);

            editContext.OnFieldChanged +=
                (sender, eventArgs) => ValidateField(editContext, messages, eventArgs.FieldIdentifier, serviceProvider, validator);

            return editContext;
        }

        private static async void ValidateModel(EditContext editContext, ValidationMessageStore messages, IServiceProvider serviceProvider, IValidator validator = null)
        {
            if (validator == null)
            {
                validator = GetValidatorForModel(serviceProvider, editContext.Model);
            }

            var validationResults = await validator.ValidateAsync(editContext.Model);

            messages.Clear();
            foreach (var validationResult in validationResults.Errors)
            {
                var fieldIdentifier = ToFieldIdentifier(editContext, validationResult.PropertyName);
                messages.Add(fieldIdentifier, validationResult.ErrorMessage);
            }

            editContext.NotifyValidationStateChanged();
        }

        private static async void ValidateField(EditContext editContext, ValidationMessageStore messages, FieldIdentifier fieldIdentifier, IServiceProvider serviceProvider, IValidator validator = null)
        {
            var properties = new[] { fieldIdentifier.FieldName };
            var context = new ValidationContext(fieldIdentifier.Model, new PropertyChain(), new MemberNameValidatorSelector(properties));

            validator = validator ?? GetValidatorForModel(serviceProvider, fieldIdentifier.Model);

            if (validator != null)
            {
                var validationResults = await validator.ValidateAsync(context);

                messages.Clear(fieldIdentifier);
                messages.Add(fieldIdentifier, validationResults.Errors.Select(error => error.ErrorMessage));

                editContext.NotifyValidationStateChanged();
            }
        }

        private static IValidator GetValidatorForModel(IServiceProvider serviceProvider, object model)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(model.GetType());
            if (serviceProvider != null)
            {
                if (serviceProvider.GetService(validatorType) is IValidator validator)
                {
                    return validator;
                }
            }

            var abstractValidatorType = typeof(AbstractValidator<>).MakeGenericType(model.GetType());

            Type modelValidatorType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                modelValidatorType = assembly.GetTypes().FirstOrDefault(t => t.IsSubclassOf(abstractValidatorType));

                if (modelValidatorType != null)
                {
                    break;
                }
            }

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
                var nextTokenEnd = propertyPath.IndexOfAny(separators);
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
                    var indexerType = prop.GetIndexParameters()[0].ParameterType;
                    var indexerValue = Convert.ChangeType(nextToken, indexerType);
                    newObj = prop.GetValue(obj, new object[] { indexerValue });
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
    }
}
