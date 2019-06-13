using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;

namespace Blazored.FluentValidation
{
    public class FluentValidationValidator : ComponentBase
    {
        [CascadingParameter] EditContext CurrentEditContext { get; set; }

        [Parameter] IValidator Validator { get; set; }

        protected override void OnInit()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{nameof(FluentValidationValidator)} requires a cascading " +
                    $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(FluentValidationValidator)} " +
                    $"inside an {nameof(EditForm)}.");
            }

            Console.WriteLine(Validator == null);
            CurrentEditContext.AddFluentValidation(Validator);
        }
    }
}
