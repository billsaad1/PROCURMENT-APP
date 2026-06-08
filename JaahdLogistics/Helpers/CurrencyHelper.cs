using System;
using System.Collections.Generic;

namespace JaahdLogistics.Helpers
{
    public static class CurrencyHelper
    {
        public static string ToWords(decimal number, string currency)
        {
            string en = ToWordsEn(number) + (currency == "USD" ? " US Dollars" : " Yemeni Rials");
            string ar = ToWordsAr(number) + (currency == "USD" ? " دولار أمريكي" : " ريال يمني");
            return $"{en} / {ar}";
        }

        private static string ToWordsEn(decimal number)
        {
            try
            {
                if (number == 0) return "Zero";
                if (number < 0) return "Minus " + ToWordsEn(Math.Abs(number));

                long intPart = (long)Math.Truncate(number);
                int decPart = (int)((number - intPart) * 100);

                string words = NumberToWordsEn(intPart);
                if (decPart > 0)
                {
                    words += " and " + NumberToWordsEn(decPart) + " Cents";
                }
                return words;
            }
            catch { return number.ToString("N2"); }
        }

        private static string ToWordsAr(decimal number)
        {
            try
            {
                if (number == 0) return "صفر";

                long intPart = (long)Math.Truncate(number);
                int decPart = (int)((number - intPart) * 100);

                string words = NumberToWordsAr(intPart);
                if (decPart > 0)
                {
                    words += " و " + NumberToWordsAr(decPart) + " هللة";
                }
                return words;
            }
            catch { return number.ToString("N0"); }
        }

        private static string NumberToWordsAr(long number)
        {
            if (number == 0) return "صفر";
            if (number < 0) return "سالب " + NumberToWordsAr(Math.Abs(number));

            string words = "";

            if ((number / 1000000) > 0)
            {
                long millions = number / 1000000;
                if (millions == 1) words += "مليون ";
                else if (millions == 2) words += "مليونان ";
                else if (millions >= 3 && millions <= 10) words += NumberToWordsAr(millions) + " ملايين ";
                else words += NumberToWordsAr(millions) + " مليون ";
                number %= 1000000;
            }

            if ((number / 1000) > 0)
            {
                if (words != "") words += "و ";
                long thousands = number / 1000;
                if (thousands == 1) words += "ألف ";
                else if (thousands == 2) words += "ألفان ";
                else if (thousands >= 3 && thousands <= 10) words += NumberToWordsAr(thousands) + " آلاف ";
                else words += NumberToWordsAr(thousands) + " ألف ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                if (words != "") words += "و ";
                long hundreds = number / 100;
                if (hundreds == 1) words += "مائة ";
                else if (hundreds == 2) words += "مائتان ";
                else
                {
                    var hundredsMap = new[] { "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة" };
                    words += hundredsMap[hundreds] + " ";
                }
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "") words += "و ";

                var unitsMap = new[] { "صفر", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة", "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };
                var tensMap = new[] { "صفر", "عشرة", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    if (number % 10 > 0)
                    {
                        words += unitsMap[number % 10] + " و ";
                    }
                    words += tensMap[number / 10];
                }
            }

            return words.Trim();
        }

        private static string NumberToWordsEn(long number)
        {
            if (number == 0) return "Zero";
            if (number < 0) return "Minus " + NumberToWordsEn(Math.Abs(number));

            string words = "";

            if ((number / 1000000) > 0)
            {
                words += NumberToWordsEn(number / 1000000) + " Million ";
                number %= 1000000;
            }

            if ((number / 1000) > 0)
            {
                words += NumberToWordsEn(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += NumberToWordsEn(number / 100) + " Hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "") words += "and ";

                var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
                var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += "-" + unitsMap[number % 10];
                }
            }

            return words.Trim();
        }
    }
}
