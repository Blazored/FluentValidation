using FluentValidation;
using FluentValidation.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;

namespace Blazored.FluentValidation
{
    public class FluentValidationValidator : ComponentBase
    {
        [Inject] private IServiceProvider ServiceProvider { get; set; }

        [CascadingParameter] private EditContext CurrentEditContext { get; set; }

        [Parameter] public IValidator Validator { get; set; }
        [Parameter] public bool DisableAssemblyScanning { get; set; }

        internal Action<ValidationStrategy<object>> Options;

        public bool Validate(Action<ValidationStrategy<object>> options)
        {
            Options = options;

            try
            {
                return CurrentEditContext.Validate();
            }
            finally
            {
                Options = null;
            }
        }

        protected override void OnInitialized()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{nameof(FluentValidationValidator)} requires a cascading " +
                    $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(FluentValidationValidator)} " +
                    $"inside an {nameof(EditForm)}.");
            }

            CurrentEditContext.AddFluentValidation(ServiceProvider, DisableAssemblyScanning, Validator, this);
        }
    }
}
