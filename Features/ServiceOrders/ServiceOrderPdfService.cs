using System.Globalization;
using System.Net;
using System.Text;
using GestaoOficina.Data;
using GestaoOficina.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace GestaoOficina.Features.ServiceOrders
{
    public class ServiceOrderPdfService
    {
        private static readonly CultureInfo PtBr = new("pt-BR");
        private readonly AppDbContext _context;

        public ServiceOrderPdfService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateAsync(ServiceOrder so)
        {
            var parts = await _context.ServiceOrderParts
                .Where(p => p.ServiceOrderId == so.Id && p.IsActive)
                .OrderBy(p => p.Id)
                .ToListAsync();

            var html = BuildHtml(so, parts);

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var page = await browser.NewPageAsync();
            await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });

            return await page.PdfAsync(new PagePdfOptions
            {
                Format = "A4",
                PrintBackground = true,
                Margin = new Margin { Top = "24px", Right = "28px", Bottom = "28px", Left = "28px" }
            });
        }

        private static string BuildHtml(ServiceOrder so, List<ServiceOrderPart> parts)
        {
            static string Safe(string? value) => WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "-" : value);
            static string Date(DateTime? value) => value.HasValue ? value.Value.ToLocalTime().ToString("dd/MM/yyyy", PtBr) : "-";
            static string Money(decimal value) => value.ToString("N2", PtBr);

            var unitAddress = $"{Safe(so.Unit?.AddressStreet)}, {Safe(so.Unit?.AddressNumber)} - {Safe(so.Unit?.AddressDistrict)} - {Safe(so.Unit?.AddressCity)}/{Safe(so.Unit?.AddressState)}";
            var unitContact = $"Fone: {Safe(so.Unit?.Phone)}";
            var vehicleLabel = Safe(so.Vehicle?.Model ?? so.Vehicle?.Plate);

            var partsHtml = new StringBuilder();
            if (parts.Count == 0)
            {
                partsHtml.AppendLine("<div>-</div>");
            }
            else
            {
                foreach (var part in parts)
                {
                    partsHtml.AppendLine($"""<div>{Safe(part.Description)} <span class="money">{Money(part.TotalPrice)}</span></div>""");
                }
            }

            return $$"""
<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8" />
  <style>
    body { font-family: "Times New Roman", serif; color: #000; font-size: 13px; line-height: 1.25; }
    .page { width: 100%; }
    .center { text-align: center; }
    .header-title { font-size: 30px; font-weight: 700; margin-top: 8px; }
    .header-line { margin: 12px auto 16px auto; width: 78%; border-bottom: 1px solid #333; }
    .top-info { margin-bottom: 22px; font-size: 20px; font-weight: 700; }
    .section { margin-top: 14px; }
    .section-title { font-size: 35px; font-weight: 700; margin-bottom: 2px; }
    .money { color: #d40000; }
    .spacer { height: 130px; }
    .total-row { display: flex; align-items: center; margin-top: 6px; font-size: 30px; }
    .total-label { white-space: nowrap; }
    .total-line { flex: 1; border-bottom: 1px solid #333; margin: 0 10px; transform: translateY(-4px); }
    .total-value { white-space: nowrap; }
    .footer-date { margin-top: 40px; font-size: 12px; }
  </style>
</head>
<body>
  <div class="page">
    <div class="center">
      <div class="header-title">{{Safe(so.Unit?.Name)}} CNPJ:{{Safe(so.Unit?.Cnpj)}}</div>
      <div>Av: {{unitAddress}} - São Paulo-SP {{unitContact}}</div>
      <div>{{Safe(so.Unit?.Email)}}</div>
      <div class="header-line"></div>
    </div>

    <div class="top-info">
      Nome: {{Safe(so.OwnerCustomer?.Name)}} Placa: {{Safe(so.Vehicle?.Plate)}} Veículo: {{vehicleLabel}}
    </div>

    <div class="section">
      <div class="section-title">FUNILARIA:</div>
      <div>{{Safe(so.BodyworkDescription)}}</div>
      <div><span class="money">Valor: {{Money(so.BodyworkValue)}}</span></div>
    </div>

    <div class="section">
      <div class="section-title">PINTURA:</div>
      <div>{{Safe(so.PaintDescription)}}</div>
      <div><span class="money">Valor: {{Money(so.PaintValue)}}</span></div>
    </div>

    <div class="section">
      <div class="section-title">PEÇAS:</div>
      {{partsHtml}}
    </div>

    <div class="spacer"></div>

    <div class="total-row">
      <div class="total-label">Valor Total</div>
      <div class="total-line"></div>
      <div class="total-value">{{Money(so.TotalAmount)}}</div>
    </div>

    <div class="footer-date">São Paulo {{Date(DateTime.Now)}}</div>
  </div>
</body>
</html>
""";
        }
    }
}