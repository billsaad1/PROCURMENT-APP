using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using JaahdLogistics.Models;
using JaahdLogistics.ViewModels;

namespace JaahdLogistics.Helpers.Export
{
    public static class PDFExportHelper
    {
        static PDFExportHelper()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static void ExportToPDF(object viewModel, string templateName, string filePath)
        {
            if (templateName.Contains("PR")) ExportPR(viewModel as PurchaseRequisitionViewModel, filePath);
            else if (templateName.Contains("PO")) ExportPO(viewModel as ProcurementViewModel, filePath);
            else if (templateName.Contains("RFQ")) ExportRFQ(viewModel as ProcurementViewModel, filePath);
            else if (templateName.Contains("BidAnalysis")) ExportBA(viewModel as ProcurementViewModel, filePath);
            else if (templateName.Contains("GRN")) ExportGRN(viewModel as WarehouseViewModel, filePath);
            else if (templateName.Contains("ThreeWayMatch")) ExportTWM(viewModel as ThreeWayMatchViewModel, filePath);
            else if (templateName.Contains("Report")) ExportReport(viewModel as ReportViewModel, filePath);
            else
            {
                // Fallback or generic
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Content().Text("Export not implemented for this type yet.");
                    });
                }).GeneratePdf(filePath);
            }
        }

        private static string T(string key) => System.Windows.Application.Current.TryFindResource(key)?.ToString() ?? key;

        private static void ExportPR(PurchaseRequisitionViewModel vm, string filePath)
        {
            if (vm?.CurrentPR == null) return;
            var pr = vm.CurrentPR;
            var settings = vm.Settings;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(settings.AssociationName).FontSize(16).Bold();
                            col.Item().Text(T("LogisticManager")).FontSize(10);
                        });

                        if (settings.LogoImage != null)
                        {
                            row.ConstantItem(60).Image(settings.LogoImage);
                        }
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Background("#1B4F72").Padding(5).AlignCenter().Text(T("PR")).FontSize(14).Bold().FontColor(Colors.White);

                        col.Item().PaddingTop(10).Border(1).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Column(c => {
                                c.Item().Text(t => { t.Span(T("PRNumber") + ": ").Bold(); t.Span(pr.PRNumber); });
                                c.Item().Text(t => { t.Span(T("Project") + ": ").Bold(); t.Span(pr.Project?.Name ?? ""); });
                            });
                            row.RelativeItem().Padding(5).Column(c => {
                                c.Item().Text(t => { t.Span(T("Date") + ": ").Bold(); t.Span(pr.Date.ToShortDateString()); });
                                c.Item().Text(t => { t.Span(T("ProjectManager") + ": ").Bold(); t.Span(pr.Project?.ProjectManager ?? ""); });
                            });
                        });

                        col.Item().PaddingTop(10).Border(1).Column(c => {
                            c.Item().Background("#922B21").Padding(2).AlignCenter().Text(T("Justification")).FontColor(Colors.White).Bold();
                            c.Item().Padding(5).Text(pr.Justification).FontSize(9);
                        });

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(100);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("NO");
                                header.Cell().Element(CellStyle).Text(T("Description"));
                                header.Cell().Element(CellStyle).Text(T("Unit"));
                                header.Cell().Element(CellStyle).Text(T("Quantity"));
                                header.Cell().Element(CellStyle).Text(T("UnitPrice"));
                                header.Cell().Element(CellStyle).Text(T("Total"));

                                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).AlignCenter();
                            });

                            int i = 1;
                            foreach (var item in pr.Items)
                            {
                                table.Cell().Element(CellStyle).Text(i++.ToString());
                                table.Cell().Element(CellStyle).Text(item.Description);
                                table.Cell().Element(CellStyle).Text(item.Unit);
                                table.Cell().Element(CellStyle).Text(item.Quantity.ToString());
                                table.Cell().Element(CellStyle).Text(item.UnitPrice.ToString("N2"));
                                table.Cell().Element(CellStyle).Text(item.TotalPrice.ToString("N2"));

                                static IContainer CellStyle(IContainer container) => container.BorderBottom(1).PaddingVertical(2).AlignCenter();
                            }
                        });

                        col.Item().AlignRight().PaddingTop(10).Text(t => {
                            t.Span(T("GrandTotal") + ": ").Bold();
                            t.Span($"{pr.TotalAmount:N2} {pr.Currency}").FontSize(14).Bold().FontColor("#1B4F72");
                        });
                    });

                    page.Footer().PaddingTop(20).Row(row =>
                    {
                        AddSig(row.RelativeItem(), T("Requester"), pr.RequesterName);
                        AddSig(row.RelativeItem(), T("Logistics"), pr.LogisticsName);
                        AddSig(row.RelativeItem(), T("Finance"), pr.FinanceName);
                        AddSig(row.RelativeItem(), T("ApprovedBy"), pr.FinalName);

                        void AddSig(IContainer c, string label, string? name)
                        {
                            c.Column(col => {
                                col.Item().AlignCenter().Text(label).Bold();
                                col.Item().PaddingVertical(5).BorderBottom(1).Height(40);
                                col.Item().AlignCenter().Text(name ?? "").FontSize(8);
                            });
                        }
                    });
                });
            }).GeneratePdf(filePath);
        }

        private static void ExportPO(ProcurementViewModel vm, string filePath)
        {
            if (vm?.CurrentPO == null) return;
            var po = vm.CurrentPO;
            var settings = vm.Settings;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(settings.AssociationName).FontSize(16).Bold();
                            col.Item().Text(T("LogisticManager")).FontSize(10);
                        });
                        if (settings.LogoImage != null) row.ConstantItem(60).Image(settings.LogoImage);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Background("#1B4F72").Padding(5).AlignCenter().Text(T("PO")).FontSize(14).Bold().FontColor(Colors.White);

                        col.Item().PaddingTop(10).Border(1).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Column(c => {
                                c.Item().Text(t => { t.Span(T("PONumberHeader") + ": ").Bold(); t.Span(po.PONumber); });
                                c.Item().Text(t => { t.Span(T("Clause") + ": ").Bold(); t.Span(po.Clause ?? ""); });
                            });
                            row.RelativeItem().Padding(5).Column(c => {
                                c.Item().Text(t => { t.Span(T("Date") + ": ").Bold(); t.Span(po.Date.ToShortDateString()); });
                            });
                        });

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Border(1).Column(c => {
                                c.Item().Background("#922B21").Padding(2).AlignCenter().Text("JAAHD DETAILS").FontColor(Colors.White).Bold();
                                c.Item().Padding(5).Text(settings.AssociationName).Bold();
                                c.Item().Padding(5).Text(settings.Address).FontSize(9);
                            });
                            row.ConstantItem(10);
                            row.RelativeItem().Border(1).Column(c => {
                                c.Item().Background("#AED6F1").Padding(2).AlignCenter().Text("VENDOR DETAILS").Bold();
                                c.Item().Padding(5).Text(po.VendorName).Bold();
                                c.Item().Padding(5).Text(po.VendorAddress).FontSize(9);
                            });
                        });

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(100);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text(T("Description"));
                                header.Cell().Element(CellStyle).Text(T("Unit"));
                                header.Cell().Element(CellStyle).Text(T("Quantity"));
                                header.Cell().Element(CellStyle).Text(T("UnitPrice"));
                                header.Cell().Element(CellStyle).Text(T("Total"));
                                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).AlignCenter();
                            });

                            foreach (var item in po.Items)
                            {
                                table.Cell().Element(CellStyle).Text(item.Description);
                                table.Cell().Element(CellStyle).Text(item.Unit);
                                table.Cell().Element(CellStyle).Text(item.Quantity.ToString());
                                table.Cell().Element(CellStyle).Text(item.UnitPrice.ToString("N2"));
                                table.Cell().Element(CellStyle).Text(item.TotalPrice.ToString("N2"));
                                static IContainer CellStyle(IContainer container) => container.BorderBottom(1).PaddingVertical(2).AlignCenter();
                            }
                        });

                        col.Item().AlignRight().PaddingTop(10).Text(t => {
                            t.Span(T("GrandTotal") + ": ").Bold();
                            t.Span($"{po.TotalAmount:N2} {po.Currency}").FontSize(14).Bold().FontColor("#1B4F72");
                        });

                        col.Item().PaddingTop(10).Border(1).Column(c => {
                            c.Item().Background(Colors.Grey.Lighten3).Padding(2).Text(T("SupplyTerms")).Bold();
                            c.Item().Padding(5).Text(po.Terms).FontSize(9);
                        });
                    });

                    page.Footer().PaddingTop(20).Row(row =>
                    {
                        AddSig(row.RelativeItem(), T("Logistics"), po.LogisticsName);
                        AddSig(row.RelativeItem(), T("Finance"), po.FinanceName);
                        AddSig(row.RelativeItem(), T("ApprovedBy"), po.FinalName);

                        void AddSig(IContainer c, string label, string? name)
                        {
                            c.Column(col => {
                                col.Item().AlignCenter().Text(label).Bold();
                                col.Item().PaddingVertical(5).BorderBottom(1).Height(40);
                                col.Item().AlignCenter().Text(name ?? "").FontSize(8);
                            });
                        }
                    });
                });
            }).GeneratePdf(filePath);
        }

        private static void ExportRFQ(ProcurementViewModel vm, string filePath)
        {
            if (vm?.CurrentRFQ == null) return;
            var rfq = vm.CurrentRFQ;
            var settings = vm.Settings;
            var pr = vm.SelectedPR;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(settings.AssociationName).FontSize(16).Bold();
                            col.Item().Text(T("LogisticManager")).FontSize(10);
                        });
                        if (settings.LogoImage != null) row.ConstantItem(60).Image(settings.LogoImage);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Background("#1B4F72").Padding(5).AlignCenter().Text(T("RFQ")).FontSize(14).Bold().FontColor(Colors.White);

                        col.Item().PaddingTop(10).Border(1).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Column(c => {
                                c.Item().Text(t => { t.Span(T("RFQNumber") + ": ").Bold(); t.Span(rfq.RFQNumber); });
                            });
                            row.RelativeItem().Padding(5).Column(c => {
                                c.Item().Text(t => { t.Span(T("ClosingDate") + ": ").Bold(); t.Span(rfq.ClosingDate?.ToShortDateString() ?? ""); });
                            });
                        });

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn();
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("NO");
                                header.Cell().Element(CellStyle).Text(T("Description"));
                                header.Cell().Element(CellStyle).Text(T("Unit"));
                                header.Cell().Element(CellStyle).Text(T("Quantity"));
                                header.Cell().Element(CellStyle).Text(T("UnitPrice"));
                                header.Cell().Element(CellStyle).Text(T("Total"));
                                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).AlignCenter();
                            });

                            if (pr != null)
                            {
                                int i = 1;
                                foreach (var item in pr.Items)
                                {
                                    table.Cell().Element(CellStyle).Text(i++.ToString());
                                    table.Cell().Element(CellStyle).Text(item.Description);
                                    table.Cell().Element(CellStyle).Text(item.Unit);
                                    table.Cell().Element(CellStyle).Text(item.Quantity.ToString());
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).Text("");
                                    static IContainer CellStyle(IContainer container) => container.BorderBottom(1).PaddingVertical(5).AlignCenter();
                                }
                            }
                        });

                        col.Item().PaddingTop(20).Border(1).Column(c => {
                            c.Item().Background("#922B21").Padding(2).AlignCenter().Text(T("Terms")).FontColor(Colors.White).Bold();
                            c.Item().Padding(5).Text(rfq.Terms).FontSize(9);
                        });

                        col.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem().Border(1).Column(c => {
                                c.Item().Background("#D5D8DC").Padding(2).AlignCenter().Text("Vendor Details").Bold();
                                c.Item().Padding(10).Height(60);
                            });
                            row.ConstantItem(10);
                            row.RelativeItem().Border(1).Column(c => {
                                c.Item().Background("#D5D8DC").Padding(2).AlignCenter().Text(T("Signature")).Bold();
                                c.Item().Padding(10).Height(60);
                            });
                        });
                    });
                });
            }).GeneratePdf(filePath);
        }

        private static void ExportBA(ProcurementViewModel vm, string filePath)
        {
            if (vm?.CurrentBidAnalysis == null) return;
            var ba = vm.CurrentBidAnalysis;
            var settings = vm.Settings;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(settings.AssociationName).FontSize(14).Bold();
                            col.Item().Text(T("LogisticManager")).FontSize(10);
                        });
                        if (settings.LogoImage != null) row.ConstantItem(50).Image(settings.LogoImage);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Background("#1B4F72").Padding(5).AlignCenter().Text(T("BidAnalysis")).FontSize(12).Bold().FontColor(Colors.White);

                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25);
                                columns.RelativeColumn();
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(80);
                                foreach (var bidder in vm.Bidders) columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().RowSpan(2).Element(HeaderStyle).Text("NO");
                                header.Cell().RowSpan(2).Element(HeaderStyle).Text(T("Description"));
                                header.Cell().RowSpan(2).Element(HeaderStyle).Text(T("Unit"));
                                header.Cell().RowSpan(2).Element(HeaderStyle).Text(T("Quantity"));
                                header.Cell().Element(HeaderStyle).AlignCenter().Text(T("EstimativeCost"));
                                foreach (var bidder in vm.Bidders) header.Cell().Element(HeaderStyle).AlignCenter().Text(bidder.Name);

                                header.Cell().Row(2).Column(5).Element(SubHeaderStyle).Text(T("Total"));
                                uint colIdx = 6;
                                foreach (var bidder in vm.Bidders) header.Cell().Row(2).Column(colIdx++).Element(SubHeaderStyle).Text(T("Total"));

                                static IContainer HeaderStyle(IContainer container) => container.DefaultTextStyle(x => x.Bold()).PaddingVertical(2).Border(1).Background(Colors.Grey.Lighten3).AlignCenter().AlignMiddle();
                                static IContainer SubHeaderStyle(IContainer container) => container.DefaultTextStyle(x => x.FontSize(8)).PaddingVertical(1).Border(1).AlignCenter();
                            });

                            foreach (var mRow in vm.MatrixRows)
                            {
                                table.Cell().Element(CellStyle).Text(mRow.Index.ToString());
                                table.Cell().Element(CellStyle).Text(mRow.Description);
                                table.Cell().Element(CellStyle).Text(mRow.Unit);
                                table.Cell().Element(CellStyle).Text(mRow.Quantity.ToString());
                                table.Cell().Element(CellStyle).Text(mRow.EstimativeTotal.ToString("N2"));
                                foreach (var price in mRow.BidderPrices) table.Cell().Element(CellStyle).Text(price.TotalPrice.ToString("N2"));

                                static IContainer CellStyle(IContainer container) => container.Border(1).PaddingHorizontal(2).AlignMiddle();
                            }
                        });

                        col.Item().PaddingTop(10).Border(1).Row(row =>
                        {
                            row.ConstantItem(150).Background("#D5F5E3").Padding(5).Text(T("RecomAwardTo")).Bold();
                            row.RelativeItem().Padding(5).Text(ba.RecommendedBidderName).Bold().FontSize(12).FontColor(Colors.Blue.Medium);
                        });

                        col.Item().PaddingTop(5).Border(1).Column(c => {
                            c.Item().Background("#FCF3CF").Padding(2).Text(T("RecommendationReasons")).Bold();
                            c.Item().Padding(5).Text(ba.RecommendationReasons).FontSize(8);
                        });
                    });

                    page.Footer().PaddingTop(10).Row(row =>
                    {
                        AddSig(row.RelativeItem(), T("Logistics"), vm.LogisticsNameBA);
                        AddSig(row.RelativeItem(), T("Finance"), vm.FinanceNameBA);
                        AddSig(row.RelativeItem(), T("ApprovedBy"), vm.HeadNameBA);

                        void AddSig(IContainer c, string label, string? name)
                        {
                            c.Column(col => {
                                col.Item().AlignCenter().Text(label).Bold().FontSize(8);
                                col.Item().PaddingVertical(2).BorderBottom(1).Height(30);
                                col.Item().AlignCenter().Text(name ?? "").FontSize(7);
                            });
                        }
                    });
                });
            }).GeneratePdf(filePath);
        }

        private static void ExportGRN(WarehouseViewModel vm, string filePath)
        {
            if (vm?.CurrentGRN == null) return;
            var grn = vm.CurrentGRN;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(c => AddHeader(c, T("GRN"), vm.Settings));

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Border(1).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Column(c =>
                            {
                                c.Item().Text(T("Project") + ":-").Bold().FontSize(8);
                                c.Item().Text(vm.SelectedPO?.Project?.Name ?? "").FontSize(10);
                            });
                            row.ConstantItem(100).BorderLeft(1).Padding(5).Column(c =>
                            {
                                c.Item().Text(T("GRNNumberHeader")).Bold().FontSize(8);
                                c.Item().Text(grn.GRNNumber).FontSize(10);
                            });
                        });

                        col.Item().PaddingTop(10).Border(1).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Column(c =>
                            {
                                c.Item().Text("VENDOR DETAILS / تفاصيل المورد").Bold().FontSize(9).FontColor(Colors.Orange.Medium);
                                c.Item().Text($"{T("Name")}: {vm.SelectedPO?.VendorName}").FontSize(9);
                                c.Item().Text($"{T("SupplyType")}: {T("Materials")}").FontSize(9);
                            });
                            row.RelativeItem().BorderLeft(1).Padding(5).Column(c =>
                            {
                                c.Item().Text("PURCHASE ORDER DETAILS / تفاصيل أمر الشراء").Bold().FontSize(9);
                                c.Item().Text($"{T("PONumberHeader")} :- {vm.SelectedPO?.PONumber}").FontSize(9);
                                c.Item().Text($"{T("InvoiceNo")} :- {grn.InvoiceNumber}").FontSize(9);
                            });
                        });

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("NO");
                                header.Cell().Element(CellStyle).Text(T("Description"));
                                header.Cell().Element(CellStyle).Text(T("Unit"));
                                header.Cell().Element(CellStyle).Text(T("ReceivedQty"));
                                header.Cell().Element(CellStyle).Text(T("AcceptedQty"));
                                header.Cell().Element(CellStyle).Text(T("RejectedQty"));
                                header.Cell().Element(CellStyle).Text(T("Remarks"));
                                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.Bold().FontSize(8)).PaddingVertical(5).Border(1).AlignCenter().AlignMiddle();
                            });

                            int i = 1;
                            foreach (var item in vm.GrnItems)
                            {
                                table.Cell().Element(CellStyle).Text(i++.ToString());
                                table.Cell().Element(CellStyle).Text(item.Description);
                                table.Cell().Element(CellStyle).Text(item.Unit);
                                table.Cell().Element(CellStyle).Text(item.ReceivedQuantity.ToString());
                                table.Cell().Element(CellStyle).Text(item.AcceptedQuantity.ToString());
                                table.Cell().Element(CellStyle).Text(item.RejectedQuantity.ToString());
                                table.Cell().Element(CellStyle).Text(item.RejectReason);
                                static IContainer CellStyle(IContainer container) => container.Border(1).Padding(3).DefaultTextStyle(x => x.FontSize(9)).AlignMiddle();
                            }
                        });

                        col.Item().PaddingTop(10).Border(1).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Column(c =>
                            {
                                c.Item().Background("#1B4F72").Padding(2).Text(T("Remarks")).Bold().FontColor(Colors.White);
                                c.Item().Text($"• {T("QtyComplySpecs")}: {(grn.IsQtyComply ? "YES" : "NO")}");
                                c.Item().Text($"• {T("QtyMatchOrder")}: {(grn.IsQtyMatch ? "YES" : "NO")}");
                                c.Item().Text($"• {T("QtyIntact")}: {(grn.IsQtyIntact ? "YES" : "NO")}");
                            });
                            row.ConstantItem(150).BorderLeft(1).Padding(5).Column(c =>
                            {
                                c.Item().AlignCenter().Text(T("Checked")).Bold();
                                c.Item().PaddingTop(10).AlignCenter().Text(grn.IsQtyComply && grn.IsQtyMatch && grn.IsQtyIntact ? "[ X ]" : "[   ]");
                            });
                        });

                        col.Item().PaddingTop(20).Border(1).Column(c =>
                        {
                            c.Item().Padding(5).Text(T("ReceiverDetails")).Bold().FontColor(Colors.Red.Medium);
                            c.Item().Padding(5).Row(r =>
                            {
                                r.RelativeItem().Text($"{T("Name")}: {vm.ReceiverName}");
                                r.RelativeItem().Text($"{T("Position")}: {vm.ReceiverPosition}");
                            });
                            c.Item().Padding(5).Row(r =>
                            {
                                r.RelativeItem().Text($"{T("Signature")}: (Signed Electronically)");
                                r.RelativeItem().Text($"{T("Date")}: {grn.Date:yyyy-MM-dd}");
                            });
                        });
                    });
                });
            }).GeneratePdf(filePath);
        }

        private static void ExportTWM(ThreeWayMatchViewModel vm, string filePath)
        {
            if (vm?.CurrentMatch == null) return;
            var match = vm.CurrentMatch;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(c => AddHeader(c, T("ThreeWayMatch"), vm.Settings));

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Border(1).Row(row =>
                        {
                            row.RelativeItem().Padding(5).Text($"{T("PONumberHeader")}: {vm.SelectedPO?.PONumber}").Bold();
                            row.RelativeItem().BorderLeft(1).Padding(5).Text($"{T("GRNNumberHeader")}: {vm.GrnNumber}").Bold();
                            row.RelativeItem().BorderLeft(1).Padding(5).Text($"{T("Date")}: {match.Date:yyyy-MM-dd}");
                        });

                        col.Item().PaddingTop(5).Border(1).Row(row =>
                        {
                            row.ConstantItem(80).Padding(5).Text($"{T("InvoiceHeader")}:").Bold();
                            row.RelativeItem().Padding(5).Text(match.InvoiceNumber);
                            row.ConstantItem(80).BorderLeft(1).Padding(5).Text($"{T("Details")}:").Bold();
                            row.RelativeItem().Padding(5).Text(match.InvoiceDetails);
                        });

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(40);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text(T("ItemHeader"));
                                header.Cell().Element(CellStyle).Text(T("Unit"));
                                header.Cell().Element(CellStyle).Text(T("POContractHeader"));
                                header.Cell().Element(CellStyle).Text(T("InvoiceHeader"));
                                header.Cell().Element(CellStyle).Text(T("GRNDetailHeader"));
                                header.Cell().Element(CellStyle).Text(T("Remarks"));
                                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.Bold().FontSize(8)).PaddingVertical(5).Border(1).AlignCenter().AlignMiddle().Background(Colors.Grey.Lighten3);
                            });

                            foreach (var item in match.Items)
                            {
                                table.Cell().Element(CellStyle).Text(item.Description);
                                table.Cell().Element(CellStyle).Text(item.Unit);
                                table.Cell().Element(CellStyle).Text($"P:{item.POPrice}\nQ:{item.POQuantity}");
                                table.Cell().Element(CellStyle).Text($"P:{item.ExtractPrice}\nQ:{item.ExtractQuantity}");
                                table.Cell().Element(CellStyle).Text($"P:{item.GRNPrice}\nQ:{item.GRNQuantity}");
                                table.Cell().Element(CellStyle).Text(item.DiffNote);
                                static IContainer CellStyle(IContainer container) => container.Border(1).Padding(3).DefaultTextStyle(x => x.FontSize(8)).AlignMiddle();
                            }
                        });

                        col.Item().PaddingTop(30).Row(row =>
                        {
                            AddSig(row.RelativeItem(), T("CheckedBy"), vm.LogisticsNameTWM, vm.LogisticsTitleTWM);
                            row.ConstantItem(20);
                            AddSig(row.RelativeItem(), T("ReviewedBy"), vm.FinanceNameTWM, vm.FinanceTitleTWM);
                            row.ConstantItem(20);
                            AddSig(row.RelativeItem(), T("ApprovedBy"), vm.HeadNameTWM, vm.HeadTitleTWM);

                            void AddSig(IContainer c, string role, string? name, string? title)
                            {
                                c.Border(1).Column(col =>
                                {
                                    col.Item().Background(Colors.Grey.Lighten2).Padding(2).AlignCenter().Text(role).Bold();
                                    col.Item().Padding(5).AlignCenter().Text(name ?? "---");
                                    col.Item().Padding(2).AlignCenter().Text(title ?? "").FontSize(8);
                                    col.Item().Padding(2).AlignCenter().Text("(Signed Electronically)").Italic().FontSize(7);
                                });
                            }
                        });
                    });
                });
            }).GeneratePdf(filePath);
        }

        private static void ExportReport(ReportViewModel vm, string filePath)
        {
            if (vm?.ReportContent is System.Windows.Controls.DataGrid grid)
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(1, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9));

                        page.Header().Element(c => AddHeader(c, vm.ReportTitle, vm.Settings));

                        page.Content().PaddingVertical(10).Table(table =>
                        {
                            var visibleColumns = grid.Columns.Where(c => c.Visibility == System.Windows.Visibility.Visible).ToList();
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (var col in visibleColumns) columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                foreach (var col in visibleColumns)
                                {
                                    header.Cell().Background("#1B4F72").Padding(5).AlignCenter().Text(col.Header?.ToString() ?? "").Bold().FontColor(Colors.White);
                                }
                            });

                            foreach (var item in grid.ItemsSource)
                            {
                                var dictionary = item as IDictionary<string, object>;
                                foreach (var col in visibleColumns)
                                {
                                    string text = "";
                                    if (col is System.Windows.Controls.DataGridTextColumn textCol && textCol.Binding is System.Windows.Data.Binding binding)
                                    {
                                        var propName = binding.Path.Path;
                                        object? val = null;
                                        if (dictionary != null && dictionary.ContainsKey(propName)) val = dictionary[propName];
                                        else val = item.GetType().GetProperty(propName)?.GetValue(item, null);
                                        text = val?.ToString() ?? "";
                                    }
                                    table.Cell().Border(0.5f).Padding(3).Text(text);
                                }
                            }
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                    });
                }).GeneratePdf(filePath);
            }
        }

        private static void AddHeader(IContainer container, string title, Settings settings)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(settings.AssociationName).FontSize(14).Bold();
                    col.Item().Text("Logistics Management").FontSize(10);
                    col.Item().PaddingTop(5).Background("#1B4F72").Padding(5).AlignCenter().Text(title).FontSize(12).Bold().FontColor(Colors.White);
                });

                if (settings.LogoImage != null)
                {
                    row.ConstantItem(60).PaddingLeft(10).Image(settings.LogoImage);
                }
            });
        }
    }
}
