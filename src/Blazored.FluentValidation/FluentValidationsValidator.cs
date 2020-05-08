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
        [Parameter] public bool DisableAssemblyScanning { get; set; }


        protected override void OnInitialized()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{nameof(FluentValidationValidator)} requires a cascading " +
                    $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(FluentValidationValidator)} " +
                    $"inside an {nameof(EditForm)}.");
            }

            CurrentEditContext.AddFluentValidation(ServiceProvider, DisableAssemblyScanning, Validator);
        }
    }
}
