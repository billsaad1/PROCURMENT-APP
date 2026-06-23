using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace JaahdLogistics.Views
{
    public partial class PrintPreviewWindow : Window
    {
        public PrintPreviewWindow(object dataContext, string templateName)
        {
            InitializeComponent();
            DataContext = dataContext;
            
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

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Find any DataGrid in the DataContext or Content to export its data
                DataGrid? grid = null;

                // Case 1: ReportViewModel has the grid in ReportContent
                var property = DataContext.GetType().GetProperty("ReportContent");
                if (property != null)
                {
                    grid = property.GetValue(DataContext) as DataGrid;
                }

                if (grid == null)
                {
                    MessageBox.Show("Export is only available for Report data at this time.", "Export Info");
                    return;
                }

                SaveFileDialog saveFile = new SaveFileDialog
                {
                    Filter = "Excel CSV (*.csv)|*.csv",
                    FileName = "Logistics_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                };

                if (saveFile.ShowDialog() == true)
                {
                    StringBuilder sb = new StringBuilder();

                    // Headers (Handle Arabic characters with UTF8 encoding)
                    var headers = grid.Columns.Select(c => "\"" + c.Header?.ToString()?.Replace("\"", "\"\"") + "\"");
                    sb.AppendLine(string.Join(",", headers));

                    // Data
                    foreach (var item in grid.ItemsSource)
                    {
                        var row = new List<string>();
                        foreach (var col in grid.Columns)
                        {
                            if (col is DataGridTextColumn textCol && textCol.Binding is System.Windows.Data.Binding binding)
                            {
                                var propName = binding.Path.Path;
                                var val = item.GetType().GetProperty(propName)?.GetValue(item, null);
                                row.Add("\"" + val?.ToString()?.Replace("\"", "\"\"") + "\"");
                            }
                            else
                            {
                                row.Add("");
                            }
                        }
                        sb.AppendLine(string.Join(",", row));
                    }

                    File.WriteAllText(saveFile.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Report exported successfully to:\n" + saveFile.FileName, "Export Success");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message);
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
