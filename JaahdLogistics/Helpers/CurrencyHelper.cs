using System;
using System.Collections.Generic;

namespace JaahdLogistics.Helpers
{
    public static class CurrencyHelper
    {
        public static string ToWords(decimal number, string currency)
        {
            if (currency == "USD")
                return ToWordsEn(number) + " US Dollars";
            else
                return ToWordsAr(number) + " ريال يمني";
        }

        private static string ToWordsEn(decimal number)
        {
            // Simplified version for demo
            return number.ToString("N2");
        }

        private static string ToWordsAr(decimal number)
        {
            // Simplified version for demo
            return number.ToString("N0");
        }
    }
}
