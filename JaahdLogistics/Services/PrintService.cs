using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JaahdLogistics.Views;

namespace JaahdLogistics.Services
{
    public class PrintService
    {
        public void ShowPreview(object dataContext, string templateName)
        {
            var preview = new PrintPreviewWindow(dataContext, templateName);
            preview.ShowDialog();
        }

        public void DirectPrint(object dataContext, string templateName)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                var template = Application.Current.TryFindResource(templateName) as ControlTemplate;
                if (template == null) return;

                var control = new Control { Template = template, DataContext = dataContext };

                // Set default A4 dimensions or Landscape if needed
                if (templateName == "BidAnalysisPrintTemplate" || templateName == "ReportPrintTemplate")
                {
                    control.Width = 1080;
                    control.Height = 750;
                    printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                }
                else
                {
                    control.Width = 794;
                    control.Height = 1123;
                    printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Portrait;
                }

                var border = new Border { Child = control, Background = Brushes.White };
                border.Measure(new Size(control.Width, control.Height));
                border.Arrange(new Rect(0, 0, control.Width, control.Height));
                border.UpdateLayout();

                // Scale to fit
                double scale = Math.Min(printDialog.PrintableAreaWidth / control.Width,
                                        printDialog.PrintableAreaHeight / control.Height);

                if (scale < 1.0)
                {
                    border.LayoutTransform = new ScaleTransform(scale, scale);
                    border.Measure(new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight));
                    border.Arrange(new Rect(0, 0, printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight));
                }

                printDialog.PrintVisual(border, "Jaahd Logistics Document");
            }
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
