using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using JaahdLogistics.Helpers.Export;

namespace JaahdLogistics.Views
{
    public partial class PrintPreviewWindow : Window
    {
        private string _currentTemplateName;

        public PrintPreviewWindow(object dataContext, string templateName)
        {
            InitializeComponent();
            DataContext = dataContext;
            _currentTemplateName = templateName;
            
            // Set the template based on the form type
            var template = Application.Current.TryFindResource(templateName) as ControlTemplate;
            if (template != null)
            {
                PreviewContent.Template = template;

                // Set landscape for Bid Analysis and Reports
                if (templateName == "BidAnalysisPrintTemplate" || templateName == "ReportPrintTemplate")
                {
                    PreviewContent.Width = 1080;
                    PreviewContent.Height = 750;
                    PrintBorder.Width = 1080;
                    PrintBorder.Height = 750;
                    this.Width = 1200;
                }
            }
        }

        private void SavePDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFile = new SaveFileDialog
                {
                    Filter = "PDF Document (*.pdf)|*.pdf",
                    FileName = "Logistics_Doc_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                };

                if (saveFile.ShowDialog() == true)
                {
                    // Use the Direct PDF Export helper
                    PDFExportHelper.ExportToPDF(DataContext, _currentTemplateName, saveFile.FileName);

                    // Show success but do NOT open system print dialog
                    MessageBox.Show("Document saved as PDF successfully.", "PDF Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("PDF Saving failed: " + ex.Message);
            }
        }

        private void SaveExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFile = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = "Logistics_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                };

                if (saveFile.ShowDialog() == true)
                {
                    ExcelExportHelper.ExportToExcel(DataContext, _currentTemplateName, saveFile.FileName);
                    MessageBox.Show("Document saved as Excel successfully.", "Excel Export");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Excel Saving failed: " + ex.Message);
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Temporarily reset zoom to 1.0 for printing to avoid "half page" issues
                var originalTransform = PrintBorder.LayoutTransform;
                PrintBorder.LayoutTransform = null;
                PrintBorder.UpdateLayout();

                // Set Page Orientation based on content dimensions
                if (PreviewContent.Width > PreviewContent.Height)
                    printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                else
                    printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Portrait;

                // Scale to fit page if content is slightly larger than printable area
                double scale = Math.Min(printDialog.PrintableAreaWidth / PrintBorder.ActualWidth,
                                        printDialog.PrintableAreaHeight / PrintBorder.ActualHeight);

                if (scale < 1.0)
                {
                    PrintBorder.LayoutTransform = new System.Windows.Media.ScaleTransform(scale, scale);
                    PrintBorder.UpdateLayout();
                }

                printDialog.PrintVisual(PrintBorder, "Jaahd Logistics Document");

                // Restore original UI zoom
                PrintBorder.LayoutTransform = originalTransform;
                PrintBorder.UpdateLayout();
            }
        }
    }
}
