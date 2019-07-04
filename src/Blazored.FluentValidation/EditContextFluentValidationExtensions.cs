using FluentValidation;
using FluentValidation.Internal;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Linq;
using System.Reflection;

namespace Blazored.FluentValidation
{
    public static class EditContextFluentValidationExtensions
    {
        public static EditContext AddFluentValidation(this EditContext editContext)
        {
            return AddFluentValidation(editContext, null);
        }

        public static EditContext AddFluentValidation(this EditContext editContext, IValidator validator)
        {
            if (editContext == null)
            {
                throw new ArgumentNullException(nameof(editContext));
            }

            var messages = new ValidationMessageStore(editContext);

            editContext.OnValidationRequested +=
                (sender, eventArgs) => ValidateModel((EditContext)sender, messages, validator);

            editContext.OnFieldChanged +=
                (sender, eventArgs) => ValidateField(editContext, messages, eventArgs.FieldIdentifier, validator);

            return editContext;
        }

        private static async void ValidateModel(EditContext editContext, ValidationMessageStore messages, IValidator validator = null)
        {
            if (validator == null)
            {
                validator = GetValidatorForModel(editContext.Model);
            }

            var validationResults = await validator.ValidateAsync(editContext.Model);

            messages.Clear();
            foreach (var validationResult in validationResults.Errors)
            {
                messages.Add(editContext.Field(validationResult.PropertyName), validationResult.ErrorMessage);
            }

            editContext.NotifyValidationStateChanged();
        }

        private static async void ValidateField(EditContext editContext, ValidationMessageStore messages, FieldIdentifier fieldIdentifier, IValidator validator = null)
        {
            var properties = new[] { fieldIdentifier.FieldName };
            var context = new ValidationContext(fieldIdentifier.Model, new PropertyChain(), new MemberNameValidatorSelector(properties));

            if (validator == null)
            {
                validator = GetValidatorForModel(editContext.Model);
            }

            var validationResults = await validator.ValidateAsync(context);

            messages.Clear(fieldIdentifier);
            messages.AddRange(fieldIdentifier, validationResults.Errors.Select(error => error.ErrorMessage));

            editContext.NotifyValidationStateChanged();
        }

        private static IValidator GetValidatorForModel(object model)
        {
            var abstractValidatorType = typeof(AbstractValidator<>).MakeGenericType(model.GetType());

            Type modelValidatorType = null;

            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                modelValidatorType = a.GetTypes().FirstOrDefault(t => t.IsSubclassOf(abstractValidatorType));

                if (modelValidatorType != null)
                {
                    break;
                }
            }

            if (modelValidatorType == null)
            {
                throw new TypeLoadException($"Unable to locate a validator of type {abstractValidatorType.FullName}");
            }

            var modelValidatorInstance = (IValidator)Activator.CreateInstance(modelValidatorType);

            return modelValidatorInstance;
        }
    }
}
