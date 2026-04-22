using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JaahdLogistics.Views;

namespace JaahdLogistics.Services
{
    public class PrintService
    {
        public void ShowPreview(FrameworkElement element)
        {
            // Create a clone or deep copy of the element to avoid visual tree issues
            // For simplicity in this demo, we use the element directly but ideally, 
            // you should render it to a fixed document or a visual.
            
            var preview = new PrintPreviewWindow(element);
            preview.ShowDialog();
        }

        public void Print(FrameworkElement element)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(element, "Jaahd Logistics Print");
            }
        }
    }
}
