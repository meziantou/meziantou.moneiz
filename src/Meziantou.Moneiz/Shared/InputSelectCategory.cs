using System;
using System.Globalization;
using System.Linq;
using System.Transactions;
using Meziantou.Framework;
using Meziantou.Moneiz.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Meziantou.Moneiz.Shared
{
    public sealed class InputSelectCategory : InputSelect<Category>
    {
        [Parameter]
        public Database Database { get; set; }

        [Parameter]
        public bool IsOptional { get; set; }

        protected override string FormatValueAsString(Category value)
        {
            return value?.Id.ToStringInvariant();
        }

        protected override bool TryParseValueFromString(string value, out Category result, out string validationErrorMessage)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resultInt))
            {
                result = Database.GetCategoryById(resultInt);
                validationErrorMessage = null;
                return true;
            }
            else
            {
                if (IsOptional)
                {
                    result = null;
                    validationErrorMessage = null;
                    return true;
                }

                result = default;
                validationErrorMessage = "The chosen value is not a valid number.";
                return false;
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "select");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "class", CssClass);
            builder.AddAttribute(3, "value", BindConverter.FormatValue(CurrentValueAsString));
            builder.AddAttribute(4, "onchange", EventCallback.Factory.CreateBinder<string>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));

            if (Database != null)
            {
                var i = 5;
                if (IsOptional)
                {
                    builder.OpenElement(i++, "option");
                    builder.CloseElement();
                }

                foreach (var group in Database.Categories.GroupBy(c => c.GroupName).OrderBy(g => g.Key))
                {
                    if (group.Key != null)
                    {
                        builder.OpenElement(i++, "optgroup");
                        builder.AddAttribute(i++, "label", group.Key);
                    }

                    foreach (var category in group.OrderBy(c => c.Name))
                    {
                        builder.OpenElement(i++, "option");
                        builder.AddAttribute(i++, "value", category.Id.ToStringInvariant());
                        builder.AddContent(i++, category.Name);
                        builder.CloseElement();
                    }

                    if (group.Key != null)
                    {
                        builder.CloseElement();
                    }
                }
            }

            builder.CloseElement();
        }
    }
}
