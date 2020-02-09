using FluentValidation;
using FluentValidation.Internal;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Linq;

namespace Blazored.FluentValidation
{
    public static class EditContextFluentValidationExtensions
    {
        public static EditContext AddFluentValidation(this EditContext editContext, IValidatorFactory validatorFactory)
        {
            return AddFluentValidation(editContext, validatorFactory, null);
        }

        public static EditContext AddFluentValidation(this EditContext editContext, IValidatorFactory validatorFactory, IValidator validator)
        {
            if (editContext == null)
            {
                throw new ArgumentNullException(nameof(editContext));
            }

            if (validatorFactory == null)
            {
                throw new ArgumentNullException(nameof(validatorFactory));
            }

            var messages = new ValidationMessageStore(editContext);

            editContext.OnValidationRequested +=
                (sender, eventArgs) => ValidateModel((EditContext)sender, messages, validatorFactory, validator);

            editContext.OnFieldChanged +=
                (sender, eventArgs) => ValidateField(editContext, messages, eventArgs.FieldIdentifier, validatorFactory, validator);

            return editContext;
        }

        private static async void ValidateModel(EditContext editContext, ValidationMessageStore messages, IValidatorFactory validatorFactory, IValidator validator = null)
        {
            if (validator == null)
            {
                validator = validatorFactory.GetValidator(editContext.Model.GetType());
            }

            if (validator == null)
            {
                throw new TypeLoadException($"Unable to locate a validator of type {typeof(IValidator<>).MakeGenericType(editContext.Model.GetType()).GetGenericTypeName()}");
            }

            var validationResults = await validator.ValidateAsync(editContext.Model);

            messages.Clear();
            foreach (var validationResult in validationResults.Errors)
            {
                messages.Add(editContext.Field(validationResult.PropertyName), validationResult.ErrorMessage);
            }

            editContext.NotifyValidationStateChanged();
        }

        private static async void ValidateField(EditContext editContext, ValidationMessageStore messages, FieldIdentifier fieldIdentifier, IValidatorFactory validatorFactory, IValidator validator = null)
        {
            if (validator == null)
            {
                validator = validatorFactory.GetValidator(fieldIdentifier.Model.GetType());
            }

            if (validator != null)
            {
                var descriptor = validator.CreateDescriptor(); 
                var fieldValidators = descriptor.GetValidatorsForMember(fieldIdentifier.FieldName);
                if (!fieldValidators.Any())
                {
                    return;
                }

                var properties = new[] { fieldIdentifier.FieldName };
                var context = new ValidationContext(fieldIdentifier.Model, new PropertyChain(), new MemberNameValidatorSelector(properties));

                var validationResults = await validator.ValidateAsync(context);

                messages.Clear(fieldIdentifier);
                messages.Add(fieldIdentifier, validationResults.Errors.Select(error => error.ErrorMessage));

                editContext.NotifyValidationStateChanged();
            }
        }
    }
}
