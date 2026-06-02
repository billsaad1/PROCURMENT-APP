using System;
using System.Globalization;
using System.Windows.Data;

namespace JaahdLogistics.Helpers
{
    public class CurrencyWordsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                string currency = parameter as string ?? "YER";
                return CurrencyHelper.ToWords(amount, currency);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
