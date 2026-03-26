using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using GestaoOficina.Data;
using GestaoOficina.DTOs.ServiceOrders;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GestaoOficina.Features.ServiceOrders
{
    public class ServiceOrderExcelService
    {
        private readonly AppDbContext _context;

        public ServiceOrderExcelService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceOrderExcelSummaryResponse> ParseExcelSummaryByStore(
            IFormFile file,
            int tenantId,
            List<int> unitIds,
            bool fullAccess)
        {
            if (file.Length == 0)
                throw new InvalidOperationException("Arquivo vazio.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
                throw new InvalidOperationException("Formato inválido. Envie arquivo .xlsx.");

            var allowedUnits = await _context.Units
                .Where(u => u.TenantId == tenantId && u.IsActive && (fullAccess || unitIds.Contains(u.Id)))
                .Select(u => new AllowedUnit(u.Id, u.Name))
                .ToListAsync();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidOperationException("Planilha não encontrada.");

            var headerRow = worksheet.Row(1);
            var headers = headerRow.CellsUsed()
                .ToDictionary(
                    c => Normalize(c.GetString()),
                    c => c.Address.ColumnNumber);

            var colDataLcto = GetRequiredColumn(headers, "DATALCTO");
            var colPlaca = GetRequiredColumn(headers, "PLACA");
            var colTpServico = GetRequiredColumn(headers, "TPSERVICO");
            var colValor = GetRequiredColumn(headers, "VALOR");
            var colLoja = GetRequiredColumn(headers, "LOJA");
            var colFornecedor = GetRequiredColumn(headers, "FORNECEDOR");
            var colObsConsultor = GetRequiredColumn(headers, "OBSCONSULTOR");

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            var result = new ServiceOrderExcelSummaryResponse();

            var grouped = new Dictionary<string, ServiceOrderExcelStoreSummaryResponse>(StringComparer.OrdinalIgnoreCase);

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                if (row.IsEmpty())
                    continue;

                var loja = row.Cell(colLoja).GetString().Trim();
                if (string.IsNullOrWhiteSpace(loja))
                    continue;

                if (!HasAccessToStore(loja, fullAccess, unitIds, allowedUnits))
                    continue;

                var valorRaw = row.Cell(colValor).GetString();
                if (!TryParseDecimal(valorRaw, out var valor) && !TryParseDecimal(row.Cell(colValor).Value.ToString(), out valor))
                {
                    throw new InvalidOperationException($"Valor inválido na linha {rowNumber}: '{valorRaw}'.");
                }

                var placa = row.Cell(colPlaca).GetString().Trim();
                if (string.IsNullOrWhiteSpace(placa))
                    continue;

                var servico = new ServiceOrderExcelServiceItemResponse
                {
                    DataLcto = TryParseDate(row.Cell(colDataLcto).Value.ToString(), out var data) ? data : null,
                    TpServico = row.Cell(colTpServico).GetString().Trim(),
                    Valor = valor,
                    Fornecedor = row.Cell(colFornecedor).GetString().Trim(),
                    ObsConsultor = row.Cell(colObsConsultor).GetString().Trim()
                };

                if (!grouped.TryGetValue(loja, out var lojaSummary))
                {
                    lojaSummary = new ServiceOrderExcelStoreSummaryResponse
                    {
                        Loja = loja
                    };
                    grouped[loja] = lojaSummary;
                }

                var placaSummary = lojaSummary.Placas.FirstOrDefault(p =>
                    string.Equals(p.Placa, placa, StringComparison.OrdinalIgnoreCase));

                if (placaSummary is null)
                {
                    placaSummary = new ServiceOrderExcelPlateSummaryResponse
                    {
                        Placa = placa
                    };
                    lojaSummary.Placas.Add(placaSummary);
                }

                placaSummary.Servicos.Add(servico);
                placaSummary.TotalPlaca += servico.Valor;
                placaSummary.QuantidadeServicos = placaSummary.Servicos.Count;

                lojaSummary.TotalLoja += servico.Valor;

                result.TotalLinhasProcessadas++;
                result.TotalGeral += servico.Valor;
            }

            foreach (var loja in grouped.Values)
            {
                loja.QuantidadePlacas = loja.Placas.Count;

                foreach (var placa in loja.Placas)
                {
                    placa.Servicos = placa.Servicos
                        .OrderBy(s => s.DataLcto)
                        .ThenBy(s => s.TpServico)
                        .ToList();
                }

                loja.Placas = loja.Placas
                    .OrderBy(p => p.Placa)
                    .ToList();
            }

            result.Lojas = grouped.Values
                .OrderBy(l => l.Loja)
                .ToList();

            return result;
        }

        private static int GetRequiredColumn(Dictionary<string, int> headers, string key)
        {
            if (!headers.TryGetValue(key, out var column))
                throw new InvalidOperationException($"Coluna obrigatória não encontrada: {key}.");

            return column;
        }

        private static bool HasAccessToStore(
            string loja,
            bool fullAccess,
            List<int> unitIds,
            List<AllowedUnit> allowedUnits)
        {
            if (fullAccess) return true;

            var match = Regex.Match(loja, @"^\s*(\d+)\s*[-]");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var unitIdFromExcel))
            {
                return unitIds.Contains(unitIdFromExcel);
            }

            var normalizedLoja = Normalize(loja);
            return allowedUnits.Any(u => normalizedLoja.Contains(Normalize(u.Name)));
        }

        private static bool TryParseDecimal(string? value, out decimal result)
        {
            result = 0m;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized = value.Trim();

            return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out result)
                || decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result)
                || decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryParseDate(string? value, out DateTime result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var text = value.Trim();

            return DateTime.TryParse(text, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.AssumeLocal, out result)
                || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result);
        }

        private static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var formD = text.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);

            foreach (var ch in formD)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            return sb
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .ToUpperInvariant()
                .Replace(" ", string.Empty);
        }

        private sealed record AllowedUnit(int Id, string Name);
    }
}