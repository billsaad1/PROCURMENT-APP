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
                // We remove our own SaveFileDialog because "Microsoft Print to PDF"
                // will show its own mandatory save dialog. This avoids the "double save" issue.

                PrintDialog printDialog = new PrintDialog();

                // Attempt to auto-select PDF printer to save user time
                try {
                    var pdfQueue = new System.Printing.LocalPrintServer().GetPrintQueues()
                        .FirstOrDefault(q => q.Name.Contains("PDF") || q.Description.Contains("PDF"));
                    if (pdfQueue != null) printDialog.PrintQueue = pdfQueue;
                } catch { }

                // Set Orientation and A4 Paper Size
                printDialog.PrintTicket.PageOrientation = (PreviewContent.Width > PreviewContent.Height)
                    ? System.Printing.PageOrientation.Landscape
                    : System.Printing.PageOrientation.Portrait;

                // Force A4
                try {
                    printDialog.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(System.Printing.PageMediaSizeName.ISOA4);
                } catch { }

                // Remove UI visual margins/borders for clean PDF output
                var originalMargin = PrintBorder.Margin;
                var originalBorder = PrintBorder.BorderThickness;
                var originalPadding = PrintBorder.Padding;
                var originalTransform = PrintBorder.LayoutTransform;

                PrintBorder.Margin = new Thickness(0);
                PrintBorder.BorderThickness = new Thickness(0);
                PrintBorder.Padding = new Thickness(0);
                PrintBorder.LayoutTransform = null;
                PrintBorder.UpdateLayout();

                // Scale to fit the ACTUAL printable area of the PDF driver (removes white margins)
                double scale = Math.Min(printDialog.PrintableAreaWidth / PrintBorder.ActualWidth,
                                        printDialog.PrintableAreaHeight / PrintBorder.ActualHeight);

                PrintBorder.LayoutTransform = new System.Windows.Media.ScaleTransform(scale, scale);
                PrintBorder.UpdateLayout();

                // Show the print dialog only if we couldn't find a PDF printer or if we want to let user confirm
                if (printDialog.PrintQueue?.Name.Contains("PDF") == true || printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(PrintBorder, "Jaahd Logistics PDF Export");
                    MessageBox.Show("Document sent to PDF driver. Please choose the save location in the next window.", "PDF Export");
                }

                // Restore UI state
                PrintBorder.Margin = originalMargin;
                PrintBorder.BorderThickness = originalBorder;
                PrintBorder.Padding = originalPadding;
                PrintBorder.LayoutTransform = originalTransform;
                PrintBorder.UpdateLayout();
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
