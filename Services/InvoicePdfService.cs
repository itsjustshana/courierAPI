using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WarehouseApi.Models;

namespace WarehouseApi.Services;

public sealed class InvoicePdfService(IHttpClientFactory httpClientFactory)
{
    public async Task<byte[]> GenerateBatchAsync(
        PackageBatch batch,
        string appName,
        string? globalLogoUrl,
        CancellationToken cancellationToken)
    {
        var logo = await LoadLogoAsync(globalLogoUrl, cancellationToken);
        var packageSubtotal = batch.Items.Sum(item => item.Package.Assignment?.InvoiceCost ?? 0);
        var total = packageSubtotal + batch.DeliveryFee;
        var paid = batch.PaidDate.HasValue;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(42);
                page.DefaultTextStyle(style => style.FontSize(9).FontColor("#243437"));
                page.Header().Row(row =>
                {
                    row.RelativeItem().Height(58).AlignMiddle().Element(container =>
                    {
                        if (logo is not null) container.AlignLeft().MaxWidth(155).Image(logo).FitArea();
                        else container.Text(appName).FontSize(20).SemiBold().FontColor("#142426");
                    });
                    row.ConstantItem(220).AlignRight().Column(column =>
                    {
                        column.Item().AlignRight().Text("BATCH INVOICE").FontSize(25).Bold().FontColor("#142426");
                        column.Item().PaddingTop(4).AlignRight().Text(batch.BatchNumber).FontColor("#687775");
                    });
                });
                page.Content().PaddingTop(22).Column(column =>
                {
                    column.Spacing(17);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(details =>
                        {
                            details.Item().Text("BILLED TO").FontSize(8).Bold().LetterSpacing(1.2f).FontColor("#7B8986");
                            details.Item().PaddingTop(5).Text(batch.Client.CompanyName).FontSize(14).SemiBold();
                            if (!string.IsNullOrWhiteSpace(batch.DeliveryAddress)) details.Item().PaddingTop(3).Text(batch.DeliveryAddress).FontColor("#687775");
                        });
                        row.ConstantItem(190).AlignRight().Column(meta =>
                        {
                            meta.Item().AlignRight().Text(paid ? "PAID" : "UNPAID").FontSize(11).Bold().FontColor(paid ? "#2F7437" : "#A13F32");
                            meta.Item().PaddingTop(5).AlignRight().Text($"Invoice date: {DateTime.UtcNow:dd MMM yyyy}").FontColor("#687775");
                            meta.Item().PaddingTop(3).AlignRight().Text(paid ? $"Paid date: {batch.PaidDate:dd MMM yyyy}" : "Payment pending").FontColor("#687775");
                        });
                    });
                    column.Item().Background("#F2F4EF").Padding(14).Row(row =>
                    {
                        Summary(row.RelativeItem(), "Batch type", batch.FulfillmentMethod);
                        Summary(row.RelativeItem(), "Status", batch.Status);
                        Summary(row.RelativeItem(), "Packages", batch.Items.Count.ToString());
                        Summary(row.RelativeItem(), "Area", batch.DeliveryArea ?? "Not set");
                    });
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns => { columns.RelativeColumn(1.2f); columns.RelativeColumn(2); columns.RelativeColumn(); });
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("PACKAGE");
                            header.Cell().Element(HeaderCell).Text("CUSTOMER / TRACKING");
                            header.Cell().Element(HeaderCell).AlignRight().Text("INVOICE");
                        });
                        foreach (var item in batch.Items.OrderBy(item => item.Package.PackageNumber))
                        {
                            table.Cell().Element(BodyCell).Text($"#{item.Package.PackageNumber}").SemiBold();
                            table.Cell().Element(BodyCell).Column(cell => { cell.Item().Text(item.Package.FullName ?? "Unknown customer"); cell.Item().Text(item.Package.TrackingId ?? "No tracking ID").FontSize(8).FontColor("#687775"); });
                            table.Cell().Element(BodyCell).AlignRight().Text($"JMD {item.Package.Assignment?.InvoiceCost ?? 0:N2}");
                        }
                        Line(table, "Batch delivery charge", batch.DeliveryArea ?? batch.FulfillmentMethod, batch.DeliveryFee);
                    });
                    column.Item().AlignRight().Width(275).Column(totals =>
                    {
                        totals.Item().Row(row => { row.RelativeItem().Text("Package subtotal").FontColor("#687775"); row.ConstantItem(125).AlignRight().Text($"JMD {packageSubtotal:N2}"); });
                        totals.Item().PaddingTop(7).Row(row => { row.RelativeItem().Text("Delivery charge").FontColor("#687775"); row.ConstantItem(125).AlignRight().Text($"JMD {batch.DeliveryFee:N2}"); });
                        totals.Item().PaddingTop(9).BorderTop(1).BorderColor("#D7DDD7").Row(row => { row.RelativeItem().Text("Invoice total").SemiBold(); row.ConstantItem(125).AlignRight().Text($"JMD {total:N2}").FontSize(14).Bold(); });
                        totals.Item().PaddingTop(7).Row(row => { row.RelativeItem().Text("Amount due").FontColor("#687775"); row.ConstantItem(125).AlignRight().Text($"JMD {(paid ? 0 : total):N2}").SemiBold(); });
                    });
                    if (!string.IsNullOrWhiteSpace(batch.Notes)) column.Item().Text($"Batch notes: {batch.Notes}").FontColor("#687775");
                });
                page.Footer().BorderTop(1).BorderColor("#E1E5E0").PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text($"Issued by {appName}").FontSize(8).FontColor("#7B8986");
                    row.RelativeItem().AlignRight().Text(text => { text.DefaultTextStyle(style => style.FontSize(8).FontColor("#7B8986")); text.Span("Page "); text.CurrentPageNumber(); });
                });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerateAsync(
        UserPackage package,
        UserPackageAssignment assignment,
        Client client,
        CancellationToken cancellationToken)
    {
        var logo = await LoadLogoAsync(client.LogoUrl, cancellationToken);
        var rate = assignment.PerLbCost + assignment.PerLbMarkup;
        var freight = decimal.Round((package.Weight ?? 0) * rate, 2);
        var customs = package.CustomsCharges ?? 0;
        var invoiceTotal = decimal.Round(freight + customs, 2);
        var paid = package.PaidDate.HasValue;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(42);
                page.DefaultTextStyle(style => style.FontSize(10).FontColor("#243437"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Height(62).AlignMiddle().Element(container =>
                    {
                        if (logo is not null)
                            container.AlignLeft().MaxWidth(155).Image(logo).FitArea();
                        else
                            container.Text(client.CompanyName).FontSize(20).SemiBold().FontColor("#142426");
                    });

                    row.ConstantItem(210).AlignRight().Column(column =>
                    {
                        column.Item().AlignRight().Text("INVOICE").FontSize(28).Bold().FontColor("#142426");
                        column.Item().PaddingTop(4).AlignRight().Text($"Package #{package.PackageNumber}").FontSize(10).FontColor("#687775");
                    });
                });

                page.Content().PaddingTop(25).Column(column =>
                {
                    column.Spacing(20);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(details =>
                        {
                            details.Item().Text("BILLED TO").FontSize(8).Bold().LetterSpacing(1.2f).FontColor("#7B8986");
                            details.Item().PaddingTop(5).Text(package.FullName ?? assignment.User.FullName ?? assignment.User.Username).FontSize(14).SemiBold();
                            if (!string.IsNullOrWhiteSpace(assignment.User.Email))
                                details.Item().PaddingTop(3).Text(assignment.User.Email).FontColor("#687775");
                        });

                        row.ConstantItem(180).AlignRight().Column(meta =>
                        {
                            meta.Item().AlignRight().Text(paid ? "PAID" : "UNPAID")
                                .FontSize(11).Bold().FontColor(paid ? "#2F7437" : "#A13F32");
                            meta.Item().PaddingTop(6).AlignRight().Text($"Invoice date: {DateTime.UtcNow:dd MMM yyyy}").FontColor("#687775");
                            meta.Item().PaddingTop(3).AlignRight().Text(paid
                                ? $"Paid date: {package.PaidDate:dd MMM yyyy}"
                                : "Payment pending").FontColor("#687775");
                        });
                    });

                    column.Item().Background("#F2F4EF").Padding(15).Row(row =>
                    {
                        Summary(row.RelativeItem(), "Tracking ID", package.TrackingId ?? "Not available");
                        Summary(row.RelativeItem(), "Status", package.Status ?? "Not set");
                        Summary(row.RelativeItem(), "Weight", $"{package.Weight ?? 0:0.##} lb");
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("DESCRIPTION");
                            header.Cell().Element(HeaderCell).AlignRight().Text("RATE");
                            header.Cell().Element(HeaderCell).AlignRight().Text("AMOUNT");
                        });

                        Line(table, $"Freight ({package.Weight ?? 0:0.##} lb)", $"JMD {rate:N2}/lb", freight);
                        Line(table, "Customs duties", string.Empty, customs);
                    });

                    column.Item().AlignRight().Width(260).Column(totals =>
                    {
                        totals.Item().BorderTop(1).BorderColor("#D7DDD7").PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text("Invoice total").SemiBold();
                            row.ConstantItem(120).AlignRight().Text($"JMD {invoiceTotal:N2}").FontSize(14).Bold();
                        });
                        totals.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text("Amount due").FontColor("#687775");
                            row.ConstantItem(120).AlignRight().Text($"JMD {package.AmountDue ?? 0:N2}").SemiBold();
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(package.Description))
                        column.Item().PaddingTop(10).Column(notes =>
                        {
                            notes.Item().Text("PACKAGE NOTES").FontSize(8).Bold().LetterSpacing(1.2f).FontColor("#7B8986");
                            notes.Item().PaddingTop(5).Text(package.Description).FontColor("#687775");
                        });
                });

                page.Footer().BorderTop(1).BorderColor("#E1E5E0").PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text($"Issued by {client.CompanyName}").FontSize(8).FontColor("#7B8986");
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor("#7B8986"));
                        text.Span("Page ");
                        text.CurrentPageNumber();
                    });
                });
            });
        }).GeneratePdf();
    }

    private async Task<byte[]?> LoadLogoAsync(string? logoUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logoUrl)) return null;
        try
        {
            if (logoUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var comma = logoUrl.IndexOf(',');
                return comma > 0 ? Convert.FromBase64String(logoUrl[(comma + 1)..]) : null;
            }

            if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return null;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            var bytes = await httpClientFactory.CreateClient().GetByteArrayAsync(uri, timeout.Token);
            return bytes.Length <= 5_000_000 ? bytes : null;
        }
        catch
        {
            return null;
        }
    }

    private static void Summary(IContainer container, string label, string value) =>
        container.Column(column =>
        {
            column.Item().Text(label.ToUpperInvariant()).FontSize(7).Bold().LetterSpacing(1).FontColor("#7B8986");
            column.Item().PaddingTop(4).Text(value).FontSize(10).SemiBold();
        });

    private static void Line(TableDescriptor table, string description, string rate, decimal amount)
    {
        table.Cell().Element(BodyCell).Text(description);
        table.Cell().Element(BodyCell).AlignRight().Text(rate).FontColor("#687775");
        table.Cell().Element(BodyCell).AlignRight().Text($"JMD {amount:N2}");
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background("#142426").PaddingVertical(9).PaddingHorizontal(10)
            .DefaultTextStyle(style => style.FontSize(8).Bold().FontColor(Colors.White));

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor("#E4E8E2").PaddingVertical(12).PaddingHorizontal(10);
}
