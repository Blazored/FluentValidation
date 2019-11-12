using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;

namespace Blazored.FluentValidation
{
    public class FluentValidationValidator : ComponentBase
    {
        [Inject] IServiceProvider ServiceProvider { get; set; }

        [CascadingParameter] EditContext CurrentEditContext { get; set; }

        [Parameter] public IValidator Validator { get; set; }
        
        /// <summary>
        /// When set to true, a field change will cause the whole model will be re-validated.
        /// When set to false, only the field that changed will be re-validated.
        /// </summary>
        [Parameter] public bool AlwaysValidateFullModel { get; set; }


        protected override void OnInitialized()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{nameof(FluentValidationValidator)} requires a cascading " +
                    $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(FluentValidationValidator)} " +
                    $"inside an {nameof(EditForm)}.");
            }

            CurrentEditContext.AddFluentValidation(ServiceProvider, Validator, AlwaysValidateFullModel);
        }
    }
}
