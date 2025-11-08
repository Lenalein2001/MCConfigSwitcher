using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace MCConfigSwitcher.Validators
{
    public class IPv4ValidationRule : ValidationRule
    {
        private static readonly Regex IPv4Regex = new(
            @"^(?:(?:25[0-5]|2[0-4][0-9]|1?[0-9]{1,2})\.){3}(?:25[0-5]|2[0-4][0-9]|1?[0-9]{1,2})$",
            RegexOptions.Compiled);

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var text = value?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return new ValidationResult(false, "IP required");
            if (!IPv4Regex.IsMatch(text))
                return new ValidationResult(false, "Invalid IPv4");
            return ValidationResult.ValidResult;
        }
    }
}
