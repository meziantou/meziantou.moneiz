using System.Globalization;
using System.Threading.Tasks;
using Meziantou.Framework;
using Meziantou.Moneiz.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Meziantou.Moneiz.Shared
{
    public sealed class InputSelectAccount : InputBase<Account>
    {
        private Database _database;

        [Inject]
        private DatabaseProvider DatabaseProvider { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _database = await DatabaseProvider.GetDatabase();
        }

        protected override string FormatValueAsString(Account value)
        {
            return value?.Id.ToStringInvariant();
        }

        protected override bool TryParseValueFromString(string value, out Account result, out string validationErrorMessage)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resultInt))
            {
                result = _database.GetAccountById(resultInt);
                validationErrorMessage = null;
                return true;
            }
            else
            {
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
            builder.AddAttribute(4, "onchange", EventCallback.Factory.CreateBinder<string>(this, value => CurrentValueAsString = value, CurrentValueAsString));

            if (_database != null)
            {
                var i = 5;
                foreach (var account in _database.VisibleAccounts)
                {
                    builder.OpenElement(i++, "option");
                    builder.AddAttribute(i++, "value", account.Id.ToStringInvariant());
                    builder.AddContent(i++, account.ToString());
                    builder.CloseElement();
                }
            }

            builder.CloseElement();
        }
    }
}
