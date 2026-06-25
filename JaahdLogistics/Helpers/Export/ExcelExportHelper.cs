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
                worksheet.RightToLeft = true;
                worksheet.ShowGridLines = false; // Hide gridlines for a clean form look

                if (templateName.Contains("PR")) ExportPR(worksheet, viewModel);
                else if (templateName.Contains("PO")) ExportPO(worksheet, viewModel);
                else if (templateName.Contains("RFQ")) ExportRFQ(worksheet, viewModel);
                else if (templateName.Contains("BidAnalysis")) ExportBA(worksheet, viewModel);
                else if (templateName.Contains("GRN")) ExportGRN(worksheet, viewModel);
                else if (templateName.Contains("ThreeWayMatch")) ExportTWM(worksheet, viewModel);
                else if (templateName.Contains("Report")) ExportReport(worksheet, viewModel);
                else ExportGeneric(worksheet, viewModel);

                // Setup Page for Printing - Exact A4 Fit
                worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
                worksheet.PageSetup.Margins.SetTop(0.25).SetBottom(0.25).SetLeft(0.25).SetRight(0.25);
                worksheet.PageSetup.CenterHorizontally = true;
                worksheet.PageSetup.FitToPages(1, 0); // Fit to 1 page wide

                workbook.SaveAs(filePath);
            }
        }

        private static void AddHeader(IXLWorksheet ws, string title, int colSpan, Settings? settings = null)
        {
            // Set worksheet background to White
            ws.Style.Fill.BackgroundColor = XLColor.White;

            // Standardize Column Widths for A4 Portrait Feel
            if (ws.PageSetup.PageOrientation == XLPageOrientation.Portrait)
            {
                ws.Column(1).Width = 6;   // SI NO
                ws.Column(2).Width = 42;  // Description
                ws.Column(3).Width = 10;  // Unit / Budget
                ws.Column(4).Width = 8;   // Qty
                ws.Column(5).Width = 12;  // Price
                ws.Column(6).Width = 14;  // Total
                if (colSpan > 6) ws.Column(7).Width = 12;
            }
            else
            {
                // Landscape default
                ws.Columns(1, colSpan).Width = 12;
                ws.Column(2).Width = 30;
            }

            // Row 1: Logo and Association Name
            ws.Row(1).Height = 75;
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
                catch { }
            }

            var associationHeader = ws.Cell(1, 2);
            associationHeader.Value = (settings?.AssociationName ?? "Jaahd Logistics") + "\nLogistics Management / إدارة اللوجستيات";
            ws.Range(1, 2, 1, colSpan).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true);

            // Row 2: Title Bar (Themed)
            ws.Row(2).Height = 30;
            var titleCell = ws.Cell(2, 1);
            titleCell.Value = title;
            ws.Range(2, 1, 2, colSpan).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(14)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1B4F72"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }

        private static void ApplyBoxStyle(IXLRange range, string bgColor = "#FFFFFF", bool bold = false, bool mediumBorder = false)
        {
            range.Style
                .Border.SetOutsideBorder(mediumBorder ? XLBorderStyleValues.Medium : XLBorderStyleValues.Thin)
                .Fill.SetBackgroundColor(XLColor.FromHtml(bgColor))
                .Font.SetBold(bold)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            if (mediumBorder)
            {
                range.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }
        }

        private static void ExportPR(IXLWorksheet ws, object vm)
        {
            var prVM = vm as PurchaseRequisitionViewModel;
            if (prVM?.CurrentPR == null) return;
            var pr = prVM.CurrentPR;

            AddHeader(ws, "Purchase Requisition / طلب شراء", 7, prVM.Settings);
            // Outer Box Border - Approximate A4 length
            ws.Range(1, 1, 55, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);

            // Row 3: Info Bar
            ws.Row(3).Height = 25;
            ws.Cell(3, 1).Value = "PR NUMBER / رقم الطلب :";
            ApplyBoxStyle(ws.Range(3, 1, 3, 1), "#F2F4F4", true);
            ws.Cell(3, 2).Value = pr.PRNumber;
            ApplyBoxStyle(ws.Range(3, 2, 3, 3), "#FFFFFF", true);
            ws.Range(3, 2, 3, 3).Merge();

            ws.Cell(3, 4).Value = "Project / المشروع :";
            ApplyBoxStyle(ws.Range(3, 4, 3, 4), "#F2F4F4", true);
            ws.Cell(3, 5).Value = pr.Project?.Name;
            ApplyBoxStyle(ws.Range(3, 5, 3, 5));

            ws.Cell(3, 6).Value = "DATE / التاريخ :";
            ApplyBoxStyle(ws.Range(3, 6, 3, 6), "#F2F4F4", true);
            ws.Cell(3, 7).Value = pr.Date.ToShortDateString();
            ApplyBoxStyle(ws.Range(3, 7, 3, 7), "#FFFFFF", true);

            // Row 4-5: Justification and Currency Details
            ws.Row(4).Height = 20;
            ws.Cell(4, 1).Value = "Justification For The Request / مبررات الطلب";
            ws.Range(4, 1, 4, 5).Merge().Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#922B21")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(4, 6).Value = "Details";
            ws.Range(4, 6, 4, 7).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#FAD7A0")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(5, 1).Value = pr.Justification;
            ws.Range(5, 1, 5, 5).Merge().Style.Alignment.SetWrapText(true).Alignment.SetVertical(XLAlignmentVerticalValues.Top).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Row(5).Height = 60;

            var detailsRange = ws.Range(5, 6, 5, 7);
            detailsRange.Merge().Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Fill.SetBackgroundColor(XLColor.FromHtml("#FAD7A0")).Font.SetFontSize(9);
            ws.Cell(5, 6).Value = $"Currency: {pr.Currency}\nEx. Rate: {pr.ExchangeRate}\nType: {pr.PRType}";
            ws.Cell(5, 6).Style.Alignment.SetWrapText(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            // Items Table
            var row = 7;
            string[] headers = { "SI NO.", "Item Description / المواصفات", "Clause / البند", "Unit / الوحدة", "Qty / الكمية", "Unit Price / السعر", "Total / الإجمالي" };
            for (int h = 0; h < headers.Length; h++)
            {
                ws.Cell(row, h + 1).Value = headers[h];
                ws.Cell(row, h + 1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Border.SetOutsideBorder(XLBorderStyleValues.Thin).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            int i = 1;
            foreach (var item in pr.Items)
            {
                row++;
                ws.Cell(row, 1).Value = i++;
                ws.Cell(row, 2).Value = item.Description;
                ws.Cell(row, 3).Value = item.BudgetLineId;
                ws.Cell(row, 4).Value = item.Unit;
                ws.Cell(row, 5).Value = item.Quantity;
                ws.Cell(row, 6).Value = item.UnitPrice;
                ws.Cell(row, 7).Value = item.TotalPrice;
                ws.Range(row, 1, row, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                ws.Cell(row, 2).Style.Alignment.SetWrapText(true);
            }

            // Fill empty rows to maintain box shape if needed
            while (i <= 10)
            {
                row++;
                ws.Cell(row, 1).Value = i++;
                ws.Range(row, 1, row, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            row++;
            ws.Cell(row, 1).Value = "Total in Words / الإجمالي كتابة:";
            ws.Range(row, 1, row, 5).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F2F4F4")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Cell(row + 1, 1).Value = CurrencyHelper.ToWords(pr.TotalAmount, pr.Currency);
            ws.Range(row + 1, 1, row + 1, 5).Merge().Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            ws.Cell(row, 6).Value = "GRAND TOTAL";
            ws.Cell(row, 6).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F2F4F4")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Cell(row, 7).Value = pr.TotalAmount;
            ws.Range(row, 7, row + 1, 7).Merge().Style.Font.SetBold().Font.SetFontSize(14).Border.SetOutsideBorder(XLBorderStyleValues.Thin).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

            row += 3;
            // Signatures
        void AddPRSig(int r, int c, string role, string? name, string? title)
            {
                ws.Cell(r, c).Value = role;
                ws.Cell(r, c).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                ws.Cell(r + 1, c).Value = title ?? "";
            ws.Cell(r + 1, c).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold().Font.SetFontSize(8).Border.SetLeftBorder(XLBorderStyleValues.Thin).Border.SetRightBorder(XLBorderStyleValues.Thin);
                ws.Cell(r + 2, c).Value = "(Signed Electronically)";
            ws.Cell(r + 2, c).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetItalic().Font.SetFontSize(7).Border.SetLeftBorder(XLBorderStyleValues.Thin).Border.SetRightBorder(XLBorderStyleValues.Thin);
                ws.Cell(r + 3, c).Value = name ?? "";
            ws.Cell(r + 3, c).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold().Border.SetBottomBorder(XLBorderStyleValues.Thin).Border.SetLeftBorder(XLBorderStyleValues.Thin).Border.SetRightBorder(XLBorderStyleValues.Thin);
            }

            AddPRSig(row, 1, "Requester / جهة الطلب", pr.RequesterName, pr.RequesterTitle);
            AddPRSig(row, 3, "Verified / تم المراجعة (Log)", pr.LogisticsName, pr.LogisticsTitle);
            AddPRSig(row, 5, "Verified / تم المراجعة (Fin)", pr.FinanceName, pr.FinanceTitle);
            AddPRSig(row, 7, "Approved / تم الاعتماد", pr.FinalName, pr.FinalTitle);
        }

        private static void ExportPO(IXLWorksheet ws, object vm)
        {
            var pVM = vm as ProcurementViewModel;
            if (pVM?.CurrentPO == null) return;
            var po = pVM.CurrentPO;

            AddHeader(ws, "PURCHASE ORDER / أمر شراء", 6, pVM.Settings);
            ws.Range(1, 1, 55, 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);

            // Row 3: Info Bar
            ws.Row(3).Height = 25;
            ws.Cell(3, 1).Value = "PO NUMBER / رقم الأمر :";
            ApplyBoxStyle(ws.Range(3, 1, 3, 1), "#F2F4F4", true);
            ws.Cell(3, 2).Value = po.PONumber;
            ApplyBoxStyle(ws.Range(3, 2, 3, 2), "#FFFFFF", true);

            ws.Cell(3, 3).Value = "CLAUSE / البند :";
            ApplyBoxStyle(ws.Range(3, 3, 3, 3), "#F2F4F4", true);
            ws.Cell(3, 4).Value = po.Clause;
            ApplyBoxStyle(ws.Range(3, 4, 3, 4));

            ws.Cell(3, 5).Value = "DATE / التاريخ :";
            ApplyBoxStyle(ws.Range(3, 5, 3, 5), "#F2F4F4", true);
            ws.Cell(3, 6).Value = po.Date.ToShortDateString();
            ApplyBoxStyle(ws.Range(3, 6, 3, 6), "#FFFFFF", true);

            // Row 4-10: Details (JAAHD vs VENDOR)
            ws.Cell(4, 1).Value = "DETAILS: JAAHD / تفاصيل جهة الطلب";
            ws.Range(4, 1, 4, 3).Merge().Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#922B21")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cell(4, 4).Value = "VENDOR / تفاصيل المورد";
            ws.Range(4, 4, 4, 6).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#AED6F1")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            void AddDetailRow(int r, string label, string val1, string val2)
            {
                ws.Cell(r, 1).Value = label;
                ws.Cell(r, 2).Value = val1;
                ws.Range(r, 2, r, 3).Merge();
                ws.Cell(r, 4).Value = label;
                ws.Cell(r, 5).Value = val2;
                ws.Range(r, 5, r, 6).Merge();
                ApplyBoxStyle(ws.Range(r, 1, r, 1), "#F2F3F4", true);
                ApplyBoxStyle(ws.Range(r, 2, r, 3));
                ApplyBoxStyle(ws.Range(r, 4, r, 4), "#F2F3F4", true);
                ApplyBoxStyle(ws.Range(r, 5, r, 6));
                ws.Row(r).Height = 20;
            }

            AddDetailRow(5, "NAME / الاسم:", pVM.Settings.AssociationName, po.VendorName ?? "");
            AddDetailRow(6, "Contact / التواصل:", pVM.Settings.ContactInfo ?? "", po.VendorContact ?? "");
            AddDetailRow(7, "Tel / الهاتف:", pVM.Settings.Tel ?? "", po.VendorTel ?? "");
            AddDetailRow(8, "Email / البريد:", pVM.Settings.Email ?? "", po.VendorEmail ?? "");
            AddDetailRow(9, "ADDRESS / العنوان:", pVM.Settings.Address ?? "", po.VendorAddress ?? "");
            ws.Row(9).Height = 35; ws.Cell(9, 2).Style.Alignment.SetWrapText(true); ws.Cell(9, 5).Style.Alignment.SetWrapText(true);

            // Items Table
            var row = 11;
            string[] headers = { "Description / المواصفات", "Unit / الوحدة", "Qty / الكمية", "U.Price / السعر", "Total / الإجمالي" };
            ws.Cell(row, 1).Value = headers[0]; ws.Range(row, 1, row, 2).Merge();
            for (int h = 1; h < headers.Length; h++) ws.Cell(row, h + 2).Value = headers[h];
            ws.Range(row, 1, row, 6).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Border.SetOutsideBorder(XLBorderStyleValues.Thin).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int poItemCount = 0;
            foreach (var item in po.Items)
            {
                row++; poItemCount++;
                ws.Cell(row, 1).Value = item.Description;
                ws.Range(row, 1, row, 2).Merge().Style.Alignment.SetWrapText(true);
                ws.Cell(row, 3).Value = item.Unit;
                ws.Cell(row, 4).Value = item.Quantity;
                ws.Cell(row, 5).Value = item.UnitPrice;
                ws.Cell(row, 6).Value = item.TotalPrice;
                ws.Range(row, 1, row, 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }
            while (poItemCount < 8)
            {
                row++; poItemCount++;
                ws.Range(row, 1, row, 2).Merge();
                ws.Range(row, 1, row, 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            row++;
            ws.Cell(row, 1).Value = "Total Cost / التكلفة الإجمالية:";
            ws.Range(row, 1, row, 4).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Cell(row, 5).Value = "GRAND TOTAL";
            ws.Cell(row, 5).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Cell(row, 6).Value = po.TotalAmount;
            ws.Cell(row, 6).Style.Font.SetBold().Font.SetFontSize(12).Border.SetOutsideBorder(XLBorderStyleValues.Thin).NumberFormat.Format = "#,##0.00";

            row++;
            ws.Cell(row, 1).Value = CurrencyHelper.ToWords(po.TotalAmount, po.Currency);
            ws.Range(row, 1, row, 4).Merge().Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Font.SetBold();

            row += 2;
            ws.Cell(row, 1).Value = "Supply Terms / شروط التوريد:";
            ws.Range(row, 1, row, 6).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Cell(row + 1, 1).Value = po.Terms;
            ws.Range(row + 1, 1, row + 3, 6).Merge().Style.Alignment.SetWrapText(true).Alignment.SetVertical(XLAlignmentVerticalValues.Top).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            row += 5;
            // Signatures
        void AddPOSig(int r, int c, string role, string? name, string? title, XLColor color)
            {
                ws.Cell(r, c).Value = role;
                ws.Range(r, c, r, c + 1).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(color).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                ws.Cell(r + 1, c).Value = title ?? "";
            ws.Range(r + 1, c, r + 1, c + 1).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold().Font.SetFontSize(8).Border.SetLeftBorder(XLBorderStyleValues.Thin).Border.SetRightBorder(XLBorderStyleValues.Thin);
                ws.Cell(r + 2, c).Value = name ?? "";
            ws.Range(r + 2, c, r + 2, c + 1).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold().Border.SetBottomBorder(XLBorderStyleValues.Thin).Border.SetLeftBorder(XLBorderStyleValues.Thin).Border.SetRightBorder(XLBorderStyleValues.Thin);
            }

            AddPOSig(row, 1, "Checked By", po.LogisticsName, po.LogisticsTitle, XLColor.FromHtml("#AED6F1"));
            AddPOSig(row, 3, "Reviewed By", po.FinanceName, po.FinanceTitle, XLColor.FromHtml("#D5D8DC"));
            AddPOSig(row, 5, "Final Approved", po.FinalName, po.FinalTitle, XLColor.FromHtml("#D5D8DC"));
        }

        private static void ExportRFQ(IXLWorksheet ws, object vm)
        {
            var pVM = vm as ProcurementViewModel;
            if (pVM?.CurrentRFQ == null) return;
            var rfq = pVM.CurrentRFQ;

            AddHeader(ws, "Request for Quotation (RFQ) / طلب عرض سعر", 6, pVM.Settings);
            ws.Range(1, 1, 55, 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);

            // Row 3: Info Bar
            ws.Row(3).Height = 25;
            ws.Cell(3, 1).Value = "RFQ Number / رقم طلب عرض السعر :";
            ApplyBoxStyle(ws.Range(3, 1, 3, 2), "#F2F4F4", true);
            ws.Cell(3, 3).Value = rfq.RFQNumber;
            ApplyBoxStyle(ws.Range(3, 3, 3, 4), "#FFFFFF", true);
            ws.Range(3, 3, 3, 4).Merge();

            ws.Cell(3, 5).Value = "Closing Date / تاريخ الإغلاق";
            ApplyBoxStyle(ws.Range(3, 5, 3, 5), "#F2F4F4", true);
            ws.Cell(3, 6).Value = rfq.ClosingDate?.ToShortDateString();
            ApplyBoxStyle(ws.Range(3, 6, 3, 6), "#FFFFFF", true);

            // Items Table
            var row = 5;
            string[] headers = { "SI NO.", "Item Description / المواصفات", "Unit / الوحدة", "Qty / الكمية", "Unit Price / السعر", "Total / الإجمالي" };
            for (int h = 0; h < headers.Length; h++)
            {
                ws.Cell(row, h + 1).Value = headers[h];
                ws.Cell(row, h + 1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Border.SetOutsideBorder(XLBorderStyleValues.Thin).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            int rfqI = 1;
            if (pVM.SelectedPR != null)
            {
                foreach (var item in pVM.SelectedPR.Items)
                {
                    row++;
                    ws.Cell(row, 1).Value = rfqI++;
                    ws.Cell(row, 2).Value = item.Description;
                    ws.Cell(row, 3).Value = item.Unit;
                    ws.Cell(row, 4).Value = item.Quantity;
                    ws.Range(row, 1, row, 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    ws.Cell(row, 2).Style.Alignment.SetWrapText(true);
                    ws.Range(row, 5, row, 6).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FDFEFE"));
                }
            }
            while (rfqI <= 12)
            {
                row++;
                ws.Cell(row, 1).Value = rfqI++;
                ws.Range(row, 1, row, 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            row += 2;
            ws.Cell(row, 1).Value = "Terms & Conditions / الشروط والأحكام";
            ws.Range(row, 1, row, 6).Merge().Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#922B21")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cell(row + 1, 1).Value = rfq.Terms;
            ws.Range(row + 1, 1, row + 3, 6).Merge().Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top).Alignment.SetWrapText(true).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            row += 5;
            ws.Cell(row, 1).Value = "Service Provider Details / تفاصيل مزود الخدمة";
            ws.Range(row, 1, row, 3).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Cell(row, 4).Value = "Vendor Signature & Stamp / توقيع وختم المورد";
            ws.Range(row, 4, row, 6).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Range(row + 1, 1, row + 4, 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Cell(row + 1, 1).Value = "Name:\nContact:\nTel:\nEmail:"; ws.Cell(row + 1, 1).Style.Alignment.SetWrapText(true).Font.SetFontSize(8);

            ws.Range(row + 1, 4, row + 4, 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }

        private static void ExportBA(IXLWorksheet ws, object vm)
        {
            var pVM = vm as ProcurementViewModel;
            if (pVM?.CurrentBidAnalysis == null) return;
            var ba = pVM.CurrentBidAnalysis;

            // Matrix documents need landscape feel
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            int bidderCount = pVM.Bidders.Count;
            int colSpan = 6 + (bidderCount * 2);

            AddHeader(ws, "Analysis of RFQs / تحليل العروض", colSpan, pVM.Settings);
            ws.Range(1, 1, 45, colSpan).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);

            // Info Bar
            ws.Row(3).Height = 25;
            ws.Cell(3, 1).Value = "RFQ Number:";
            ApplyBoxStyle(ws.Range(3, 1, 3, 1), "#F2F4F4", true);
            ws.Cell(3, 2).Value = pVM.SelectedRFQ?.RFQNumber;
            ApplyBoxStyle(ws.Range(3, 2, 3, 3), "#FFFFFF", true); ws.Range(3, 2, 3, 3).Merge();

            ws.Cell(3, 4).Value = "Date of Analysis:";
            ApplyBoxStyle(ws.Range(3, 4, 3, 4), "#F2F4F4", true);
            ws.Cell(3, 5).Value = ba.Date.ToShortDateString();
            ApplyBoxStyle(ws.Range(3, 5, 3, 5));

            // Justification
            ws.Row(4).Height = 30;
            ws.Cell(4, 1).Value = "Justification For The Request";
            ws.Range(4, 1, 4, 2).Merge().Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#922B21")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cell(4, 3).Value = ba.Justification;
            ws.Range(4, 3, 4, colSpan).Merge().Style.Alignment.SetWrapText(true).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Matrix Header
            var row = 6;
            ws.Row(row).Height = 30;
            string[] baseHeaders = { "NO", "Description", "Unit", "QTY", "Estimative Cost" };
            for(int h=0; h<4; h++) { ws.Cell(row, h+1).Value = baseHeaders[h]; ApplyBoxStyle(ws.Range(row, h+1, row+1, h+1), "#F2F4F4", true); ws.Range(row, h+1, row+1, h+1).Merge(); }

            ws.Cell(row, 5).Value = "Estimative Cost";
            ApplyBoxStyle(ws.Range(row, 5, row, 6), "#FAD7A0", true); ws.Range(row, 5, row, 6).Merge();
            ws.Cell(row+1, 5).Value = "Unit Price"; ws.Cell(row+1, 6).Value = "Total";
            ApplyBoxStyle(ws.Range(row+1, 5, row+1, 5), "#FAD7A0"); ApplyBoxStyle(ws.Range(row+1, 6, row+1, 6), "#FAD7A0");

            int col = 7;
            foreach (var bidder in pVM.Bidders)
            {
                ws.Cell(row, col).Value = bidder.Name;
                ApplyBoxStyle(ws.Range(row, col, row, col + 1), "#D5D8DC", true); ws.Range(row, col, row, col + 1).Merge();
                ws.Cell(row+1, col).Value = "Unit Price"; ws.Cell(row+1, col+1).Value = "Total";
                ApplyBoxStyle(ws.Range(row+1, col, row+1, col), "#D5D8DC"); ApplyBoxStyle(ws.Range(row+1, col+1, row+1, col+1), "#D5D8DC");
                col += 2;
            }

            row += 2;
            foreach (var mRow in pVM.MatrixRows)
            {
                ws.Cell(row, 1).Value = mRow.Index;
                ws.Cell(row, 2).Value = mRow.Description;
                ws.Cell(row, 3).Value = mRow.Unit;
                ws.Cell(row, 4).Value = mRow.Quantity;
                ws.Cell(row, 5).Value = mRow.EstimativeUnitPrice;
                ws.Cell(row, 6).Value = mRow.EstimativeTotal;

                int itemCol = 7;
                foreach (var price in mRow.BidderPrices)
                {
                    ws.Cell(row, itemCol).Value = price.UnitPrice;
                    ws.Cell(row, itemCol + 1).Value = price.TotalPrice;
                    itemCol += 2;
                }
                ws.Range(row, 1, row, col - 1).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                ws.Cell(row, 2).Style.Alignment.SetWrapText(true);
                row++;
            }

            // Totals section
            void AddTotalRow(int r, string label, Func<Bidder, decimal> selector, string bgColor = "#EBF5FB")
            {
                ws.Cell(r, 1).Value = label;
                ws.Range(r, 1, r, 6).Merge().Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right).Fill.SetBackgroundColor(XLColor.FromHtml(bgColor)).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                int c = 7;
                foreach(var b in pVM.Bidders)
                {
                    ws.Cell(r, c).Value = selector(b);
                    ws.Range(r, c, r, c + 1).Merge().Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    ws.Cell(r, c).Style.NumberFormat.Format = "#,##0.00";
                    c += 2;
                }
            }

            AddTotalRow(row, "Sub-Total:", b => b.CalculatedSubTotal); row++;
            AddTotalRow(row, "Discount:", b => b.Discount); row++;
            AddTotalRow(row, "Misc Costs:", b => b.MiscCosts); row++;
            AddTotalRow(row, "GRAND TOTAL:", b => b.CalculatedTotal, "#D5D8DC"); row++;

            // Recommendation
            row++;
            ws.Cell(row, 1).Value = "Recommended Award To:";
            ws.Range(row, 1, row, 3).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5F5E3")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Cell(row, 4).Value = ba.RecommendedBidderName;
            ws.Range(row, 4, row, col - 1).Merge().Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.Blue).Border.SetOutsideBorder(XLBorderStyleValues.Thin).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            row++;
            ws.Cell(row, 1).Value = "Recommendation Reasons / مبررات الاختيار";
            ws.Range(row, 1, row, 3).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#FCF3CF")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Cell(row, 4).Value = ba.RecommendationReasons;
            ws.Range(row, 4, row + 1, col - 1).Merge().Style.Alignment.SetWrapText(true).Alignment.SetVertical(XLAlignmentVerticalValues.Top).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            row += 3;
            // Signatures
            void AddBASig(int r, int c, string role, string? name, string? title)
            {
                ws.Cell(r, c).Value = role;
                ws.Range(r, c, r, c + 2).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                ws.Cell(r + 1, c).Value = $"NAME: {name}\nPOSITION: {title}";
                ws.Range(r + 1, c, r + 3, c + 2).Merge().Style.Alignment.SetWrapText(true).Alignment.SetVertical(XLAlignmentVerticalValues.Top).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            AddBASig(row, 1, "Checked by / فحص بواسطة", pVM.LogisticsNameBA, pVM.LogisticsTitleBA);
            AddBASig(row, 5, "Reviewed by / مراجعة بواسطة", pVM.FinanceNameBA, pVM.FinanceTitleBA);
            AddBASig(row, 9, "Approved by / اعتمد بواسطة", pVM.HeadNameBA, pVM.HeadTitleBA);
        }

        private static void ExportGRN(IXLWorksheet ws, object vm)
        {
            var wVM = vm as WarehouseViewModel;
            if (wVM?.CurrentGRN == null) return;
            var grn = wVM.CurrentGRN;

            AddHeader(ws, "GOODS RECEIVING NOTE / مذكرة استلام بضائع", 7, wVM.Settings);
            ws.Range(1, 1, 55, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);

            // Row 3: Info Bar
            ws.Row(3).Height = 25;
            ws.Cell(3, 1).Value = "DISTRIBUTION :-";
            ApplyBoxStyle(ws.Range(3, 1, 3, 1), "#F2F4F4", true);
            ws.Cell(3, 2).Value = wVM.SelectedPO?.Project?.Name;
            ws.Range(3, 2, 3, 4).Merge(); ApplyBoxStyle(ws.Range(3, 2, 3, 4));

            ws.Cell(3, 5).Value = "GRN NO.";
            ApplyBoxStyle(ws.Range(3, 5, 3, 6), "#F2F4F4", true); ws.Range(3, 5, 3, 6).Merge();
            ws.Cell(3, 7).Value = grn.GRNNumber;
            ApplyBoxStyle(ws.Range(3, 7, 3, 7), "#FFFFFF", true);

            // Row 4-5: Details
            ws.Cell(4, 1).Value = "VENDOR DETAILS";
            ws.Range(4, 1, 4, 3).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#FAD7A0")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cell(4, 4).Value = "PURCHASE ORDER DETAILS";
            ws.Range(4, 4, 4, 7).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.White).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Row(5).Height = 40;
            ws.Cell(5, 1).Value = $"NAME: {wVM.SelectedPO?.VendorName}\nSUPPLY TYPE: Materials";
            ws.Range(5, 1, 5, 3).Merge().Style.Alignment.SetWrapText(true).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Cell(5, 4).Value = $"PO. NO. :- {wVM.SelectedPO?.PONumber}\nINVOICE NO. :- {grn.InvoiceNumber}";
            ws.Range(5, 4, 5, 7).Merge().Style.Alignment.SetWrapText(true).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Items Table
            var row = 7;
            string[] headers = { "SI NO.", "ITEM DESCRIPTION", "UNIT", "TOT ITEMS RECEIVED", "NO. ACCEPTED", "NO. REJECTED", "REJECT REASON(S)" };
            for (int h = 0; h < headers.Length; h++)
            {
                ws.Cell(row, h + 1).Value = headers[h];
                ws.Cell(row, h + 1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.White).Border.SetOutsideBorder(XLBorderStyleValues.Thin).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            int i = 1;
            foreach (var item in wVM.GrnItems)
            {
                row++;
                ws.Cell(row, 1).Value = i++;
                ws.Cell(row, 2).Value = item.Description;
                ws.Cell(row, 3).Value = item.Unit;
                ws.Cell(row, 4).Value = item.ReceivedQuantity;
                ws.Cell(row, 5).Value = item.AcceptedQuantity;
                ws.Cell(row, 6).Value = item.RejectedQuantity;
                ws.Cell(row, 7).Value = item.RejectReason;
                ws.Range(row, 1, row, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }
            while (i <= 10)
            {
                row++;
                ws.Cell(row, 1).Value = i++;
                ws.Range(row, 1, row, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            // Remarks
            row += 2;
            ws.Cell(row, 1).Value = "REMARKS";
            ws.Range(row, 1, row, 5).Merge().Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1B4F72")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            ws.Cell(row, 6).Value = "CHECKED";
            ws.Range(row, 6, row, 7).Merge().Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            void AddRemark(int r, string text, bool val) {
                ws.Cell(r, 1).Value = "• " + text; ws.Range(r, 1, r, 5).Merge().Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                ws.Cell(r, 6).Value = val ? "X" : ""; ws.Range(r, 6, r, 7).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            AddRemark(row + 1, "Quantity Comply to The Required Specifications", grn.IsQtyComply);
            AddRemark(row + 2, "Quantity Matches The Required Number", grn.IsQtyMatch);
            AddRemark(row + 3, "Quantity Intact", grn.IsQtyIntact);

            row += 5;
            ws.Cell(row, 1).Value = "RECEIVER DETAILS";
            ws.Range(row, 1, row, 7).Merge().Style.Font.SetBold().Font.SetFontColor(XLColor.Brown).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Cell(row + 1, 1).Value = "NAME"; ws.Cell(row + 1, 2).Value = wVM.ReceiverName; ws.Range(row + 1, 2, row + 1, 3).Merge();
            ws.Cell(row + 1, 4).Value = "POSITION"; ws.Cell(row + 1, 5).Value = wVM.ReceiverPosition; ws.Range(row + 1, 5, row + 1, 7).Merge();
            ws.Cell(row + 2, 1).Value = "SIGNATURE"; ws.Range(row + 2, 2, row + 2, 3).Merge().Value = "(Signed)";
            ws.Cell(row + 2, 4).Value = "DATE"; ws.Cell(row + 2, 5).Value = grn.Date.ToShortDateString(); ws.Range(row + 2, 5, row + 2, 7).Merge();

            ws.Range(row + 1, 1, row + 2, 7).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }

        private static void ExportTWM(IXLWorksheet ws, object vm)
        {
            var tVM = vm as ThreeWayMatchViewModel;
            if (tVM?.CurrentMatch == null) return;
            var match = tVM.CurrentMatch;

            AddHeader(ws, "THREE-WAY MATCH / مطابقة ثلاثية", 7, tVM.Settings);
            ws.Range(1, 1, 55, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);

            // Info Bar 1
            ws.Row(3).Height = 25;
            ws.Cell(3, 1).Value = "PO NO:"; ApplyBoxStyle(ws.Range(3, 1, 3, 1), "#E9F7EF", true);
            ws.Cell(3, 2).Value = tVM.SelectedPO?.PONumber; ApplyBoxStyle(ws.Range(3, 2, 3, 2));
            ws.Cell(3, 3).Value = "GRN NO:"; ApplyBoxStyle(ws.Range(3, 3, 3, 3), "#FAD7A0", true);
            ws.Cell(3, 4).Value = tVM.GrnNumber; ApplyBoxStyle(ws.Range(3, 4, 3, 4));
            ws.Cell(3, 5).Value = "DATE:"; ApplyBoxStyle(ws.Range(3, 5, 3, 5), "#D5D8DC", true);
            ws.Cell(3, 6).Value = match.Date.ToShortDateString(); ws.Range(3, 6, 3, 7).Merge(); ApplyBoxStyle(ws.Range(3, 6, 3, 7));

            // Info Bar 2
            ws.Row(4).Height = 25;
            ws.Cell(4, 1).Value = "INVOICE:"; ApplyBoxStyle(ws.Range(4, 1, 4, 1), "#D6EAF8", true);
            ws.Cell(4, 2).Value = match.InvoiceNumber; ApplyBoxStyle(ws.Range(4, 2, 4, 2));
            ws.Cell(4, 3).Value = "DETAILS:"; ApplyBoxStyle(ws.Range(4, 3, 4, 3), "#D6EAF8", true);
            ws.Cell(4, 4).Value = match.InvoiceDetails; ws.Range(4, 4, 4, 7).Merge(); ApplyBoxStyle(ws.Range(4, 4, 4, 7));

            // Items Table Header
            var row = 6;
            ws.Row(row).Height = 20;
            string[] headers = { "Item", "Unit", "PO CONTRACT", "INVOICE", "GRN DETAIL", "Diff", "Clarify" };
            for(int h=0; h<headers.Length; h++) { ws.Cell(row, h+1).Value = headers[h]; ApplyBoxStyle(ws.Range(row, h+1, row, h+1), "#F2F4F4", true); }

            foreach (var item in match.Items)
            {
                row++;
                ws.Cell(row, 1).Value = item.Description;
                ws.Cell(row, 2).Value = item.Unit;
                ws.Cell(row, 3).Value = $"P:{item.POPrice}\nQ:{item.POQuantity}";
                ws.Cell(row, 4).Value = $"P:{item.ExtractPrice}\nQ:{item.ExtractQuantity}";
                ws.Cell(row, 5).Value = $"P:{item.GRNPrice}\nQ:{item.GRNQuantity}";
                ws.Cell(row, 6).Value = item.DiffNote;
                ws.Cell(row, 7).Value = item.ClarifyDiff;
                ws.Range(row, 1, row, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                ws.Cell(row, 3).Style.Alignment.SetWrapText(true); ws.Cell(row, 4).Style.Alignment.SetWrapText(true); ws.Cell(row, 5).Style.Alignment.SetWrapText(true);
            }

            row += 3;
            // Signatures
            void AddTWMSig(int r, int c, string role, string? name, string? title)
            {
                ws.Cell(r, c).Value = role;
                ws.Range(r, c, r, c + 1).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D5D8DC")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                ws.Cell(r + 1, c).Value = name + "\n" + title;
                ws.Range(r + 1, c, r + 2, c + 1).Merge().Style.Alignment.SetWrapText(true).Alignment.SetVertical(XLAlignmentVerticalValues.Top).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            AddTWMSig(row, 1, "Checked by / فحص بواسطة", tVM.LogisticsNameTWM, tVM.LogisticsTitleTWM);
            AddTWMSig(row, 4, "Reviewed by / مراجعة بواسطة", tVM.FinanceNameTWM, tVM.FinanceTitleTWM);
            AddTWMSig(row, 7, "Approved by / اعتمد بواسطة", tVM.HeadNameTWM, tVM.HeadTitleTWM);
        }

        private static void ExportReport(IXLWorksheet ws, object vm)
        {
            var rVM = vm as ReportViewModel;
            if (rVM?.ReportContent is System.Windows.Controls.DataGrid grid)
            {
                // Reports need landscape orientation
                ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;

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
