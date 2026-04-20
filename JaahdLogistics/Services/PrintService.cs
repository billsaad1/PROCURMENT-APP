using System.Windows;
using System.Windows.Controls;

namespace JaahdLogistics.Services
{
    public class PrintService
    {
        public void Print(FrameworkElement element)
        {
            PrintDialog printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                printDlg.PrintVisual(element, "Jaahd Logistics Document");
            }
        }
    }
}
