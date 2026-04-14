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
                Margin = new Margin { Top = "20px", Right = "20px", Bottom = "20px", Left = "20px" }
            });
        }

        private static string BuildHtml(ServiceOrder so, List<ServiceOrderPart> parts)
        {
            static string Safe(string? value) => WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "-" : value);
            static string Date(DateTime? value) => value.HasValue ? value.Value.ToLocalTime().ToString("dd/MM/yyyy", PtBr) : "-";
            static string Money(decimal value) => value.ToString("N2", PtBr);

            var unitAddress = $"{Safe(so.Unit?.AddressStreet)}, {Safe(so.Unit?.AddressNumber)} - {Safe(so.Unit?.AddressDistrict)} - {Safe(so.Unit?.AddressCity)}/{Safe(so.Unit?.AddressState)}";
            var unitContact = $"Fone: {Safe(so.Unit?.Phone)}";
            var unitEmail = Safe(so.Unit?.Email);
            var footerCity = Safe(so.Unit?.AddressCity);
            var vehicleLabel = Safe(so.Vehicle?.Model ?? so.Vehicle?.Plate);
            var bodyworkValue = so.BodyworkValue;
            var paintValue = so.PaintValue;
            var partsValue = parts.Sum(p => p.TotalPrice);
            var totalAmount = so.TotalAmount;

            var vinRowHtml = !string.IsNullOrWhiteSpace(so.Vehicle?.Vin)
                ? $$"""
      <div class="customer-row">
        <div class="customer-label">VIN:</div>
        <div class="customer-value">{{Safe(so.Vehicle?.Vin)}}</div>
      </div>
"""
                : string.Empty;

            var renavamRowHtml = !string.IsNullOrWhiteSpace(so.Vehicle?.Renavam)
                ? $$"""
      <div class="customer-row">
        <div class="customer-label">Renavam:</div>
        <div class="customer-value">{{Safe(so.Vehicle?.Renavam)}}</div>
      </div>
"""
                : string.Empty;

            var insuranceClaimRowHtml = !string.IsNullOrWhiteSpace(so.Vehicle?.InsuranceClaimNumber)
                ? $$"""
      <div class="customer-row">
        <div class="customer-label">Sinistro:</div>
        <div class="customer-value">{{Safe(so.Vehicle?.InsuranceClaimNumber)}}</div>
      </div>
"""
                : string.Empty;

            var partsHtml = new StringBuilder();
            if (parts.Count > 0)
            {
                partsHtml.AppendLine("<tbody>");
                foreach (var part in parts)
                {
                    partsHtml.AppendLine($"""
                        <tr>
                            <td class="parts-description">{Safe(part.Description)}</td>
                            <td class="parts-value">{Money(part.TotalPrice)}</td>
                        </tr>
                        """);
                }
                partsHtml.AppendLine("</tbody>");
            }

            var partsSectionHtml = parts.Count > 0
                ? $$"""
    <div class="service-section">
      <div class="section-header">Peças</div>
      <div class="section-content">
        <table class="parts-table">
          {{partsHtml}}
        </table>
        <div class="value-row" style="margin-top: 8px;">
          <span class="value-label">Subtotal Peças:</span>
          <span class="value-amount">R$ {{Money(partsValue)}}</span>
        </div>
      </div>
    </div>
"""
                : string.Empty;

            var partsSummaryRowHtml = parts.Count > 0
                ? $$"""
      <div class="summary-row subtotal">
        <span class="summary-label">Peças:</span>
        <span class="summary-value">R$ {{Money(partsValue)}}</span>
      </div>
"""
                : string.Empty;

            return $$"""
<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8" />
  <style>
    * {
      margin: 0;
      padding: 0;
      box-sizing: border-box;
    }

    body {
      font-family: 'Segoe UI', 'Roboto', 'Helvetica Neue', sans-serif;
      color: #2c3e50;
      font-size: 11px;
      line-height: 1.4;
      background-color: #fff;
    }

    .page {
      width: 100%;
      background-color: #fff;
    }

    .header {
      border-bottom: 3px solid #1a5490;
      padding-bottom: 16px;
      margin-bottom: 20px;
    }

    .header-company {
      text-align: center;
      margin-bottom: 8px;
    }

    .company-name {
      font-size: 20px;
      font-weight: 700;
      color: #1a5490;
      margin-bottom: 2px;
    }

    .company-cnpj {
      font-size: 12px;
      color: #555;
      font-weight: 500;
    }

    .company-info {
      text-align: center;
      font-size: 11px;
      color: #666;
      line-height: 1.3;
      margin-bottom: 6px;
    }

    .company-contact {
      text-align: center;
      font-size: 11px;
      color: #666;
    }

    .customer-section {
      background-color: #f8f9fa;
      border: 1px solid #dee2e6;
      border-radius: 4px;
      padding: 12px;
      margin-bottom: 16px;
    }

    .customer-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 8px 24px;
      margin-bottom: 0;
    }
    .customer-cell {
      display: flex;
      flex-direction: column;
      font-size: 11px;
      background: none;
      padding: 0;
    }
    .customer-label {
      font-weight: 600;
      color: #1a5490;
      margin-bottom: 2px;
    }
    .customer-value {
      color: #2c3e50;
      word-break: break-word;
    }

    .service-section {
      margin-bottom: 16px;
      border: 1px solid #dee2e6;
      border-radius: 4px;
      overflow: hidden;
    }

    .section-header {
      background-color: #1a5490;
      color: #fff;
      padding: 10px 12px;
      font-weight: 700;
      font-size: 12px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .section-content {
      padding: 12px;
    }

    .description-text {
      color: #2c3e50;
      margin-bottom: 6px;
      font-size: 11px;
      line-height: 1.4;
      min-height: 20px;
    }

    .value-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 6px;
      border-top: 1px solid #dee2e6;
      font-weight: 600;
    }

    .value-label {
      color: #555;
      font-size: 10px;
    }

    .value-amount {
      color: #d40000;
      font-size: 12px;
    }

    .parts-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 11px;
    }

    .parts-table tbody tr {
      border-bottom: 1px solid #dee2e6;
    }

    .parts-table tbody tr:last-child {
      border-bottom: none;
    }

    .parts-table tbody tr:nth-child(even) {
      background-color: #f8f9fa;
    }

    .parts-description {
      padding: 8px 12px;
      text-align: left;
      color: #2c3e50;
    }

    .parts-value {
      padding: 8px 12px;
      text-align: right;
      color: #d40000;
      font-weight: 600;
      width: 80px;
    }

    .summary-section {
      margin-top: 20px;
      margin-bottom: 16px;
      border: 1px solid #dee2e6;
      border-radius: 4px;
      overflow: hidden;
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 10px 12px;
      border-bottom: 1px solid #dee2e6;
      font-size: 11px;
    }

    .summary-row:last-child {
      border-bottom: none;
    }

    .summary-row.subtotal {
      background-color: #f8f9fa;
    }

    .summary-row.total {
      background-color: #1a5490;
      color: #fff;
      font-weight: 700;
      font-size: 13px;
    }

    .summary-label {
      font-weight: 600;
    }

    .summary-value {
      text-align: right;
      font-weight: 600;
    }

    .summary-row.total .summary-value {
      color: #fff;
    }

    .footer {
      margin-top: 30px;
      padding-top: 12px;
      border-top: 1px solid #dee2e6;
      text-align: center;
      font-size: 10px;
      color: #666;
    }

    .footer-date {
      margin-bottom: 4px;
    }

    .footer-signature {
      margin-top: 20px;
      display: flex;
      justify-content: space-around;
    }

    .signature-line {
      width: 120px;
      text-align: center;
    }

    .signature-space {
      border-top: 1px solid #333;
      height: 40px;
      margin-bottom: 4px;
    }

    .signature-label {
      font-size: 9px;
      color: #555;
    }
  </style>
</head>
<body>
  <div class="page">
    <div class="header">
      <div class="header-company">
        <div class="company-name">{{Safe(so.Unit?.Name)}}</div>
        <div class="company-cnpj">CNPJ: {{Safe(so.Unit?.Cnpj)}}</div>
      </div>
      <div class="company-info">
        {{unitAddress}}<br>
        {{unitContact}}
      </div>
      <div class="company-contact">
        {{unitEmail}}
      </div>
    </div>

    <div class="customer-section">
      <div class="customer-grid">
        <div class="customer-cell">
          <div class="customer-label">Cliente:</div>
          <div class="customer-value">{{Safe(so.OwnerCustomer?.Name)}}</div>
        </div>
        <div class="customer-cell">
          <div class="customer-label">Placa:</div>
          <div class="customer-value">{{Safe(so.Vehicle?.Plate)}}</div>
        </div>
        <div class="customer-cell">
          <div class="customer-label">Veículo:</div>
          <div class="customer-value">{{vehicleLabel}}</div>
        </div>
        <div class="customer-cell">
          <div class="customer-label">VIN:</div>
          <div class="customer-value">{{Safe(so.Vehicle?.Vin)}}</div>
        </div>
        <div class="customer-cell">
          <div class="customer-label">Renavam:</div>
          <div class="customer-value">{{Safe(so.Vehicle?.Renavam)}}</div>
        </div>
        <div class="customer-cell">
          <div class="customer-label">Sinistro:</div>
          <div class="customer-value">{{Safe(so.Vehicle?.InsuranceClaimNumber)}}</div>
        </div>
      </div>
    </div>

    <div class="service-section">
      <div class="section-header">Funilaria</div>
      <div class="section-content">
        <div class="description-text">{{Safe(so.BodyworkDescription)}}</div>
        <div class="value-row">
          <span class="value-label">Valor:</span>
          <span class="value-amount">R$ {{Money(bodyworkValue)}}</span>
        </div>
      </div>
    </div>

    <div class="service-section">
      <div class="section-header">Pintura</div>
      <div class="section-content">
        <div class="description-text">{{Safe(so.PaintDescription)}}</div>
        <div class="value-row">
          <span class="value-label">Valor:</span>
          <span class="value-amount">R$ {{Money(paintValue)}}</span>
        </div>
      </div>
    </div>

    {{partsSectionHtml}}

    <div class="summary-section">
      <div class="summary-row subtotal">
        <span class="summary-label">Funilaria:</span>
        <span class="summary-value">R$ {{Money(bodyworkValue)}}</span>
      </div>
      <div class="summary-row subtotal">
        <span class="summary-label">Pintura:</span>
        <span class="summary-value">R$ {{Money(paintValue)}}</span>
      </div>
      {{partsSummaryRowHtml}}
      <div class="summary-row total">
        <span class="summary-label">VALOR TOTAL:</span>
        <span class="summary-value">R$ {{Money(totalAmount)}}</span>
      </div>
    </div>

    <div class="footer">
      <div class="footer-date">{{footerCity}}, {{Date(DateTime.Now)}}</div>
    </div>
  </div>
</body>
</html>
""";
        }
    }
}
