using System.Windows;

namespace JaahdLogistics.Services
{
    public class LanguageService
    {
        public void SetLanguage(string langCode)
        {
            var dict = new ResourceDictionary();
            switch (langCode)
            {
                case "ar":
                    dict.Source = new Uri("Resources/Strings.ar.xaml", UriKind.Relative);
                    break;
                default:
                    dict.Source = new Uri("Resources/Strings.en.xaml", UriKind.Relative);
                    break;
            }

            var oldDict = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Strings."));

            if (oldDict != null)
                Application.Current.Resources.MergedDictionaries.Remove(oldDict);

            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
