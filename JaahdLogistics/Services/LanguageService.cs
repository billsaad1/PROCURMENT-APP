using System;
using System.Linq;
using System.Windows;

namespace JaahdLogistics.Services
{
    public class LanguageService
    {
        public void SetLanguage(string langCode)
        {
            var dict = new ResourceDictionary();
            string source = langCode == "ar" ? "Resources/Strings.ar.xaml" : "Resources/Strings.en.xaml";
            dict.Source = new Uri(source, UriKind.RelativeOrAbsolute);

            var mergedDicts = Application.Current.Resources.MergedDictionaries;
            var oldDict = mergedDicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Strings."));

            if (oldDict != null)
                mergedDicts.Remove(oldDict);

            mergedDicts.Add(dict);
        }
    }
}
