using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using JaahdLogistics.Models;
using JaahdLogistics.ViewModels;

namespace JaahdLogistics.Helpers.Export
{
    public static class ExcelExportHelper
    {
        public static void ExportToExcel(object viewModel, string templateName, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Logistics Document");
                worksheet.RightToLeft = true; // Support Arabic layouts

                if (templateName.Contains("PR")) ExportPR(worksheet, viewModel);
                else if (templateName.Contains("PO")) ExportPO(worksheet, viewModel);
                else if (templateName.Contains("RFQ")) ExportRFQ(worksheet, viewModel);
                else if (templateName.Contains("BidAnalysis")) ExportBA(worksheet, viewModel);
                else if (templateName.Contains("GRN")) ExportGRN(worksheet, viewModel);
                else if (templateName.Contains("ThreeWayMatch")) ExportTWM(worksheet, viewModel);
                else if (templateName.Contains("Report")) ExportReport(worksheet, viewModel);
                else ExportGeneric(worksheet, viewModel);

                // Adjust column widths for professional look
                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
        }

        private static void AddHeader(IXLWorksheet ws, string title, int colSpan, Settings? settings = null)
        {
            // Set Row Heights
            ws.Row(1).Height = 60;

            if (settings?.LogoImage != null && settings.LogoImage.Length > 0)
            {
                try
                {
                    using (var ms = new MemoryStream(settings.LogoImage))
                    {
                        var picture = ws.AddPicture(ms)
                            .MoveTo(ws.Cell(1, 1))
                            .WithSize(60, 60);
                    }
                }
                catch { /* Ignore image errors */ }
            }

            ws.Cell(1, 2).Value = title;
            ws.Range(1, 2, 1, colSpan).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(18)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1B4F72"))
                .Font.SetFontColor(XLColor.White);

            if (settings != null)
            {
                ws.Row(2).Height = 25;
                ws.Cell(2, 1).Value = settings.AssociationName;
                ws.Range(2, 1, 2, colSpan).Merge().Style
                    .Font.SetBold()
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Font.SetFontSize(12);
            }
        }

        private static void ExportPR(IXLWorksheet ws, object vm)
        {
            var prVM = vm as PurchaseRequisitionViewModel;
            if (prVM?.CurrentPR == null) return;
            var pr = prVM.CurrentPR;

            AddHeader(ws, "Purchase Requisition / طلب شراء", 7, prVM.Settings);

            ws.Cell(3, 1).Value = "PR Number:"; ws.Cell(3, 2).Value = pr.PRNumber;
            ws.Cell(3, 4).Value = "Project:"; ws.Cell(3, 5).Value = pr.Project?.Name;
            ws.Cell(3, 6).Value = "Date:"; ws.Cell(3, 7).Value = pr.Date.ToShortDateString();

            ws.Cell(4, 1).Value = "Justification:";
            ws.Cell(4, 2).Value = pr.Justification;
            ws.Range(4, 2, 4, 7).Merge().Style.Alignment.SetWrapText(true);

            // Items Table
            var row = 6;
            ws.Cell(row, 1).Value = "No";
            ws.Cell(row, 2).Value = "Description / المواصفات";
            ws.Cell(row, 3).Value = "Unit";
            ws.Cell(row, 4).Value = "Qty";
            ws.Cell(row, 5).Value = "Unit Price";
            ws.Cell(row, 6).Value = "Total";
            ws.Cell(row, 7).Value = "Budget Line";
            ws.Range(row, 1, row, 7).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            int i = 1;
            foreach (var item in pr.Items)
            {
                row++;
                ws.Cell(row, 1).Value = i++;
                ws.Cell(row, 2).Value = item.Description;
                ws.Cell(row, 3).Value = item.Unit;
                ws.Cell(row, 4).Value = item.Quantity;
                ws.Cell(row, 5).Value = item.UnitPrice;
                ws.Cell(row, 6).Value = item.TotalPrice;
                ws.Cell(row, 7).Value = item.BudgetLineId;
                ws.Range(row, 1, row, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            row += 1;
            ws.Cell(row, 5).Value = "Total Amount:";
            ws.Cell(row, 6).Value = pr.TotalAmount;
            ws.Cell(row, 6).Style.Font.SetBold().NumberFormat.Format = "#,##0.00";
            ws.Range(row, 5, row, 6).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F2F4F4"));

            row += 2;
            // Signatures
            void AddSig(int r, int c, string role, string? name, string? title)
            {
                ws.Cell(r, c).Value = role;
                ws.Cell(r, c).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(r + 1, c).Value = title ?? "";
                ws.Cell(r + 2, c).Value = "(Signed)";
                ws.Cell(r + 3, c).Value = name ?? "";
                ws.Range(r, c, r + 3, c).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            AddSig(row, 1, "Requester", pr.RequesterName, pr.RequesterTitle);
            AddSig(row, 3, "Logistics", pr.LogisticsName, pr.LogisticsTitle);
            AddSig(row, 5, "Finance", pr.FinanceName, pr.FinanceTitle);
            AddSig(row, 7, "Approval", pr.FinalName, pr.FinalTitle);
        }

        private static void ExportPO(IXLWorksheet ws, object vm)
        {
            var pVM = vm as ProcurementViewModel;
            if (pVM?.CurrentPO == null) return;
            var po = pVM.CurrentPO;

            AddHeader(ws, "PURCHASE ORDER / أمر شراء", 6, pVM.Settings);

            ws.Cell(3, 1).Value = "PO Number:"; ws.Cell(3, 2).Value = po.PONumber;
            ws.Cell(3, 5).Value = "Date:"; ws.Cell(3, 6).Value = po.Date.ToShortDateString();

            ws.Cell(5, 1).Value = "VENDOR DETAILS"; ws.Range(5, 1, 5, 3).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#AED6F1"));
            ws.Cell(6, 1).Value = "Name:"; ws.Cell(6, 2).Value = po.VendorName;
            ws.Cell(7, 1).Value = "Address:"; ws.Cell(7, 2).Value = po.VendorAddress;
            ws.Cell(8, 1).Value = "Tel:"; ws.Cell(8, 2).Value = po.VendorTel;

            var row = 10;
            ws.Cell(row, 1).Value = "Description";
            ws.Cell(row, 2).Value = "Unit";
            ws.Cell(row, 3).Value = "Qty";
            ws.Cell(row, 4).Value = "Unit Price";
            ws.Cell(row, 5).Value = "Total";
            ws.Range(row, 1, row, 5).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            foreach (var item in po.Items)
            {
                row++;
                ws.Cell(row, 1).Value = item.Description;
                ws.Cell(row, 2).Value = item.Unit;
                ws.Cell(row, 3).Value = item.Quantity;
                ws.Cell(row, 4).Value = item.UnitPrice;
                ws.Cell(row, 5).Value = item.TotalPrice;
                ws.Range(row, 1, row, 5).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            row += 1;
            ws.Cell(row, 4).Value = "Grand Total:";
            ws.Cell(row, 5).Value = po.TotalAmount;
            ws.Cell(row, 5).Style.Font.SetBold().NumberFormat.Format = "#,##0.00";
            ws.Range(row, 4, row, 5).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC"));

            row += 3;
            // Signatures
            void AddSig(int r, int c, string role, string? name, string? title, XLColor color)
            {
                ws.Cell(r, c).Value = role;
                ws.Cell(r, c).Style.Font.SetBold().Fill.SetBackgroundColor(color).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(r + 1, c).Value = title ?? "";
                ws.Cell(r + 2, c).Value = name ?? "";
                ws.Range(r, c, r + 2, c).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            AddSig(row, 1, "Checked By", po.LogisticsName, po.LogisticsTitle, XLColor.FromHtml("#AED6F1"));
            AddSig(row, 3, "Reviewed By", po.FinanceName, po.FinanceTitle, XLColor.LightGray);
            AddSig(row, 5, "Final Approved", po.FinalName, po.FinalTitle, XLColor.LightGray);
        }

        private static void ExportRFQ(IXLWorksheet ws, object vm)
        {
            var pVM = vm as ProcurementViewModel;
            if (pVM?.CurrentRFQ == null) return;
            var rfq = pVM.CurrentRFQ;

            AddHeader(ws, "Request for Quotation / طلب عرض سعر", 5, pVM.Settings);
            ws.Cell(3, 1).Value = "RFQ Number:"; ws.Cell(3, 2).Value = rfq.RFQNumber;
            ws.Cell(3, 4).Value = "Closing Date:"; ws.Cell(3, 5).Value = rfq.ClosingDate?.ToShortDateString();

            var row = 6;
            ws.Cell(row, 1).Value = "Description";
            ws.Cell(row, 2).Value = "Unit";
            ws.Cell(row, 3).Value = "Qty";
            ws.Cell(row, 4).Value = "Unit Price (Vendor)";
            ws.Cell(row, 5).Value = "Total (Vendor)";
            ws.Range(row, 1, row, 5).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            if (pVM.SelectedPR != null)
            {
                foreach(var item in pVM.SelectedPR.Items)
                {
                    row++;
                    ws.Cell(row, 1).Value = item.Description;
                    ws.Cell(row, 2).Value = item.Unit;
                    ws.Cell(row, 3).Value = item.Quantity;
                    ws.Range(row, 1, row, 5).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    ws.Range(row, 4, row, 5).Style.Fill.SetBackgroundColor(XLColor.WhiteSmoke);
                }
            }

            row += 2;
            ws.Cell(row, 1).Value = "Terms & Conditions:";
            ws.Cell(row + 1, 1).Value = rfq.Terms;
            ws.Range(row + 1, 1, row + 4, 5).Merge().Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top).Alignment.SetWrapText(true).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            row += 6;
            ws.Cell(row, 1).Value = "Vendor Details & Stamp:";
            ws.Range(row, 1, row + 4, 3).Merge().Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }

        private static void ExportBA(IXLWorksheet ws, object vm)
        {
            var pVM = vm as ProcurementViewModel;
            if (pVM?.CurrentBidAnalysis == null) return;
            var ba = pVM.CurrentBidAnalysis;

            AddHeader(ws, "Bid Analysis / تحليل العروض", 5 + (pVM.Bidders.Count * 2), pVM.Settings);

            var row = 5;
            ws.Cell(row, 1).Value = "Description";
            ws.Cell(row, 2).Value = "Unit";
            ws.Cell(row, 3).Value = "Qty";
            ws.Cell(row, 4).Value = "Estimative Unit Price";
            ws.Cell(row, 5).Value = "Estimative Total";

            int col = 6;
            foreach (var bidder in pVM.Bidders)
            {
                ws.Cell(row - 2, col).Value = "BIDDER";
                ws.Cell(row - 1, col).Value = bidder.Name;
                ws.Range(row - 1, col, row - 1, col + 1).Merge().Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, col).Value = "Price";
                ws.Cell(row, col + 1).Value = "Total";
                col += 2;
            }

            ws.Range(row, 1, row, col - 1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            foreach (var matrixRow in pVM.MatrixRows)
            {
                row++;
                ws.Cell(row, 1).Value = matrixRow.Description;
                ws.Cell(row, 2).Value = matrixRow.Unit;
                ws.Cell(row, 3).Value = matrixRow.Quantity;
                ws.Cell(row, 4).Value = matrixRow.EstimativeUnitPrice;
                ws.Cell(row, 5).Value = matrixRow.EstimativeTotal;

                int itemCol = 6;
                foreach (var price in matrixRow.BidderPrices)
                {
                    ws.Cell(row, itemCol).Value = price.UnitPrice;
                    ws.Cell(row, itemCol + 1).Value = price.TotalPrice;
                    itemCol += 2;
                }
                ws.Range(row, 1, row, col - 1).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            row += 2;
            ws.Cell(row, 1).Value = "Recommended Award To:";
            ws.Cell(row, 4).Value = ba.RecommendedBidderName;
            ws.Range(row, 4, row, 7).Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.Blue);

            row += 2;
            ws.Cell(row, 1).Value = "Reasons:";
            ws.Cell(row, 2).Value = ba.RecommendationReasons;
            ws.Range(row, 2, row + 2, col - 1).Merge().Style.Alignment.SetWrapText(true).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            row += 4;
            // Signatures
            void AddSig(int r, int c, string role, string? name, string? title)
            {
                ws.Cell(r, c).Value = role;
                ws.Cell(r, c).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(r + 1, c).Value = title ?? "";
                ws.Cell(r + 2, c).Value = name ?? "";
                ws.Range(r, c, r + 2, c).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            AddSig(row, 1, "Logistics", pVM.LogisticsNameBA, pVM.LogisticsTitleBA);
            AddSig(row, 4, "Finance", pVM.FinanceNameBA, pVM.FinanceTitleBA);
            AddSig(row, 7, "Approval", pVM.HeadNameBA, pVM.HeadTitleBA);
        }

        private static void ExportGRN(IXLWorksheet ws, object vm)
        {
            var wVM = vm as WarehouseViewModel;
            if (wVM?.CurrentGRN == null) return;
            var grn = wVM.CurrentGRN;

            AddHeader(ws, "Goods Receiving Note / مذكرة استلام بضائع", 6, wVM.Settings);
            ws.Cell(3, 1).Value = "GRN No:"; ws.Cell(3, 2).Value = grn.GRNNumber;
            ws.Cell(3, 4).Value = "PO No:"; ws.Cell(3, 5).Value = wVM.SelectedPO?.PONumber;
            ws.Cell(4, 1).Value = "Invoice:"; ws.Cell(4, 2).Value = grn.InvoiceNumber;
            ws.Cell(4, 4).Value = "Date:"; ws.Cell(4, 5).Value = grn.Date.ToShortDateString();

            var row = 6;
            ws.Cell(row, 1).Value = "Item Description";
            ws.Cell(row, 2).Value = "Unit";
            ws.Cell(row, 3).Value = "Received";
            ws.Cell(row, 4).Value = "Accepted";
            ws.Cell(row, 5).Value = "Rejected";
            ws.Cell(row, 6).Value = "Reason";
            ws.Range(row, 1, row, 6).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#FAD7A0")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            foreach (var item in wVM.GrnItems)
            {
                row++;
                ws.Cell(row, 1).Value = item.Description;
                ws.Cell(row, 2).Value = item.Unit;
                ws.Cell(row, 3).Value = item.ReceivedQuantity;
                ws.Cell(row, 4).Value = item.AcceptedQuantity;
                ws.Cell(row, 5).Value = item.RejectedQuantity;
                ws.Cell(row, 6).Value = item.RejectReason;
                ws.Range(row, 1, row, 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            row += 2;
            ws.Cell(row, 1).Value = "Remarks:";
            ws.Cell(row, 2).Value = "Qty Comply: " + (grn.IsQtyComply ? "Yes" : "No");
            ws.Cell(row + 1, 2).Value = "Qty Match: " + (grn.IsQtyMatch ? "Yes" : "No");
            ws.Cell(row + 2, 2).Value = "Qty Intact: " + (grn.IsQtyIntact ? "Yes" : "No");

            row += 4;
            ws.Cell(row, 1).Value = "Receiver Details:";
            ws.Cell(row + 1, 1).Value = "Name:"; ws.Cell(row + 1, 2).Value = wVM.ReceiverName;
            ws.Cell(row + 2, 1).Value = "Position:"; ws.Cell(row + 2, 2).Value = wVM.ReceiverPosition;
            ws.Range(row, 1, row + 3, 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }

        private static void ExportTWM(IXLWorksheet ws, object vm)
        {
            var tVM = vm as ThreeWayMatchViewModel;
            if (tVM?.CurrentMatch == null) return;
            var match = tVM.CurrentMatch;

            AddHeader(ws, "Three-Way Match / مطابقة ثلاثية", 10, tVM.Settings);

            ws.Cell(3, 1).Value = "PO Number:"; ws.Cell(3, 2).Value = tVM.SelectedPO?.PONumber;
            ws.Cell(3, 4).Value = "Invoice No:"; ws.Cell(3, 5).Value = match.InvoiceNumber;
            ws.Cell(3, 7).Value = "Date:"; ws.Cell(3, 8).Value = match.Date.ToShortDateString();

            var row = 5;
            ws.Cell(row, 1).Value = "Item Description";
            ws.Cell(row, 2).Value = "Unit";
            ws.Cell(row, 3).Value = "PO Price";
            ws.Cell(row, 4).Value = "PO Qty";
            ws.Cell(row, 5).Value = "Inv Price";
            ws.Cell(row, 6).Value = "Inv Qty";
            ws.Cell(row, 7).Value = "GRN Price";
            ws.Cell(row, 8).Value = "GRN Qty";
            ws.Cell(row, 9).Value = "Diff Note";
            ws.Cell(row, 10).Value = "Clarification";
            ws.Range(row, 1, row, 10).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F2F4F4")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            foreach (var item in match.Items)
            {
                row++;
                ws.Cell(row, 1).Value = item.Description;
                ws.Cell(row, 2).Value = item.Unit;
                ws.Cell(row, 3).Value = item.POPrice;
                ws.Cell(row, 4).Value = item.POQuantity;
                ws.Cell(row, 5).Value = item.ExtractPrice;
                ws.Cell(row, 6).Value = item.ExtractQuantity;
                ws.Cell(row, 7).Value = item.GRNPrice;
                ws.Cell(row, 8).Value = item.GRNQuantity;
                ws.Cell(row, 9).Value = item.DiffNote;
                ws.Cell(row, 10).Value = item.ClarifyDiff;
                ws.Range(row, 1, row, 10).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            row += 3;
            // Signatures
            void AddSig(int r, int c, string role, string? name, string? title)
            {
                ws.Cell(r, c).Value = role;
                ws.Cell(r, c).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(r + 1, c).Value = title ?? "";
                ws.Cell(r + 2, c).Value = name ?? "";
                ws.Range(r, c, r + 2, c).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            AddSig(row, 1, "Checked By", tVM.LogisticsNameTWM, tVM.LogisticsTitleTWM);
            AddSig(row, 4, "Reviewed By", tVM.FinanceNameTWM, tVM.FinanceTitleTWM);
            AddSig(row, 7, "Approved By", tVM.HeadNameTWM, tVM.HeadTitleTWM);
        }

        private static void ExportReport(IXLWorksheet ws, object vm)
        {
            var rVM = vm as ReportViewModel;
            if (rVM?.ReportContent is System.Windows.Controls.DataGrid grid)
            {
                AddHeader(ws, rVM.ReportTitle, grid.Columns.Count, rVM.Settings);

                var row = 3;
                int col = 1;
                foreach (var column in grid.Columns)
                {
                    ws.Cell(row, col++).Value = column.Header?.ToString();
                }
                ws.Range(row, 1, row, col - 1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#1B4F72")).Font.SetFontColor(XLColor.White);

                foreach (var item in grid.ItemsSource)
                {
                    row++;
                    col = 1;
                    var dictionary = item as IDictionary<string, object>;
                    foreach (var column in grid.Columns)
                    {
                        if (column is System.Windows.Controls.DataGridTextColumn textCol && textCol.Binding is System.Windows.Data.Binding binding)
                        {
                            var propName = binding.Path.Path;
                            object val = null;
                            if (dictionary != null && dictionary.ContainsKey(propName))
                            {
                                val = dictionary[propName];
                            }
                            else
                            {
                                val = item.GetType().GetProperty(propName)?.GetValue(item, null);
                            }
                            if (val == null) ws.Cell(row, col++).Value = "";
                            else if (val is decimal d) ws.Cell(row, col++).Value = d;
                            else if (val is int i) ws.Cell(row, col++).Value = i;
                            else if (val is double dbl) ws.Cell(row, col++).Value = dbl;
                            else if (val is long l) ws.Cell(row, col++).Value = l;
                            else ws.Cell(row, col++).Value = val.ToString();
                        }
                    }
                    ws.Range(row, 1, row, col - 1).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                }
                ws.Columns().AdjustToContents();
            }
        }

        private static void ExportGeneric(IXLWorksheet ws, object vm)
        {
            ws.Cell(1, 1).Value = "Logistics Document Export";
            ws.Cell(2, 1).Value = "Exported on: " + DateTime.Now.ToString();
        }
    }
}
