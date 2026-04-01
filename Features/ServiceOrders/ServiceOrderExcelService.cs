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

            var extension = Path.GetExtension(file.FileName).Trim().ToLowerInvariant();
            if (extension is not (".xlsx" or ".csv"))
                throw new InvalidOperationException("Formato inválido. Envie arquivo .xlsx ou .csv.");

            var allowedUnits = await _context.Units
                .Where(u => u.TenantId == tenantId && u.IsActive && (fullAccess || unitIds.Contains(u.Id)))
                .Select(u => new AllowedUnit(u.Id, u.Name))
                .ToListAsync();

            return extension == ".xlsx"
                ? ParseFromXlsx(file, fullAccess, unitIds, allowedUnits)
                : await ParseFromCsvAsync(file, fullAccess, unitIds, allowedUnits);
        }

        private ServiceOrderExcelSummaryResponse ParseFromXlsx(
            IFormFile file,
            bool fullAccess,
            List<int> unitIds,
            List<AllowedUnit> allowedUnits)
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidOperationException("Planilha não encontrada.");

            var headerRow = worksheet.Row(1);
            var headers = headerRow.CellsUsed()
                .ToDictionary(
                    c => Normalize(c.GetString()),
                    c => c.Address.ColumnNumber);

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

                AddRecord(
                    rowNumber: rowNumber,
                    loja: row.Cell(colLoja).GetString().Trim(),
                    valorRawPrimary: row.Cell(colValor).GetString(),
                    valorRawSecondary: row.Cell(colValor).Value.ToString(),
                    placa: row.Cell(colPlaca).GetString().Trim(),
                    tpServico: row.Cell(colTpServico).GetString().Trim(),
                    fornecedor: row.Cell(colFornecedor).GetString().Trim(),
                    obsConsultor: row.Cell(colObsConsultor).GetString().Trim(),
                    fullAccess: fullAccess,
                    unitIds: unitIds,
                    allowedUnits: allowedUnits,
                    grouped: grouped,
                    result: result);
            }

            FinalizeResult(grouped, result);
            return result;
        }

        private async Task<ServiceOrderExcelSummaryResponse> ParseFromCsvAsync(
            IFormFile file,
            bool fullAccess,
            List<int> unitIds,
            List<AllowedUnit> allowedUnits)
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            var headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
                throw new InvalidOperationException("Cabeçalho do CSV não encontrado.");

            var delimiter = DetectDelimiter(headerLine);
            var headerFields = SplitCsvLine(headerLine, delimiter);

            var headers = headerFields
                .Select((h, i) => new { Header = Normalize(h), Index = i })
                .ToDictionary(x => x.Header, x => x.Index);

            var colPlaca = GetRequiredColumn(headers, "PLACA");
            var colTpServico = GetRequiredColumn(headers, "TPSERVICO");
            var colValor = GetRequiredColumn(headers, "VALOR");
            var colLoja = GetRequiredColumn(headers, "LOJA");
            var colFornecedor = GetRequiredColumn(headers, "FORNECEDOR");
            var colObsConsultor = GetRequiredColumn(headers, "OBSCONSULTOR");

            var result = new ServiceOrderExcelSummaryResponse();
            var grouped = new Dictionary<string, ServiceOrderExcelStoreSummaryResponse>(StringComparer.OrdinalIgnoreCase);

            var rowNumber = 1;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                rowNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var fields = SplitCsvLine(line, delimiter);

                string GetField(int index) => index >= 0 && index < fields.Count ? fields[index].Trim() : string.Empty;

                AddRecord(
                    rowNumber: rowNumber,
                    loja: GetField(colLoja),
                    valorRawPrimary: GetField(colValor),
                    valorRawSecondary: GetField(colValor),
                    placa: GetField(colPlaca),
                    tpServico: GetField(colTpServico),
                    fornecedor: GetField(colFornecedor),
                    obsConsultor: GetField(colObsConsultor),
                    fullAccess: fullAccess,
                    unitIds: unitIds,
                    allowedUnits: allowedUnits,
                    grouped: grouped,
                    result: result);
            }

            FinalizeResult(grouped, result);
            return result;
        }

        private static void AddRecord(
            int rowNumber,
            string loja,
            string? valorRawPrimary,
            string? valorRawSecondary,
            string placa,
            string tpServico,
            string fornecedor,
            string? obsConsultor,
            bool fullAccess,
            List<int> unitIds,
            List<AllowedUnit> allowedUnits,
            Dictionary<string, ServiceOrderExcelStoreSummaryResponse> grouped,
            ServiceOrderExcelSummaryResponse result)
        {
            if (string.IsNullOrWhiteSpace(loja))
                return;

            if (!HasAccessToStore(loja, fullAccess, unitIds, allowedUnits))
                return;

            if (!TryParseDecimal(valorRawPrimary, out var valor) && !TryParseDecimal(valorRawSecondary, out valor))
                throw new InvalidOperationException($"Valor inválido na linha {rowNumber}: '{valorRawPrimary}'.");

            if (string.IsNullOrWhiteSpace(placa))
                return;

            var servico = new ServiceOrderExcelServiceItemResponse
            {
                TpServico = tpServico,
                Valor = valor,
                Fornecedor = fornecedor,
                ObsConsultor = obsConsultor
            };

            if (!grouped.TryGetValue(loja, out var lojaSummary))
            {
                lojaSummary = new ServiceOrderExcelStoreSummaryResponse { Loja = loja };
                grouped[loja] = lojaSummary;
            }

            var placaSummary = lojaSummary.Placas.FirstOrDefault(p =>
                string.Equals(p.Placa, placa, StringComparison.OrdinalIgnoreCase));

            if (placaSummary is null)
            {
                placaSummary = new ServiceOrderExcelPlateSummaryResponse { Placa = placa };
                lojaSummary.Placas.Add(placaSummary);
            }

            placaSummary.Servicos.Add(servico);
            placaSummary.TotalPlaca += servico.Valor;
            placaSummary.QuantidadeServicos = placaSummary.Servicos.Count;

            lojaSummary.TotalLoja += servico.Valor;

            result.TotalLinhasProcessadas++;
            result.TotalGeral += servico.Valor;
        }

        private static void FinalizeResult(
            Dictionary<string, ServiceOrderExcelStoreSummaryResponse> grouped,
            ServiceOrderExcelSummaryResponse result)
        {
            foreach (var loja in grouped.Values)
            {
                loja.QuantidadePlacas = loja.Placas.Count;

                foreach (var placa in loja.Placas)
                {
                    placa.Servicos = placa.Servicos
                        .OrderBy(s => s.TpServico)
                        .ToList();
                }

                loja.Placas = loja.Placas
                    .OrderBy(p => p.Placa)
                    .ToList();
            }

            result.Lojas = grouped.Values
                .OrderBy(l => l.Loja)
                .ToList();
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
                return unitIds.Contains(unitIdFromExcel);

            var normalizedLoja = Normalize(loja);
            return allowedUnits.Any(u => normalizedLoja.Contains(Normalize(u.Name)));
        }

        private static bool TryParseDecimal(string? value, out decimal result)
        {
            result = 0m;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var raw = value.Trim()
                .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("\u00A0", string.Empty)
                .Replace(" ", string.Empty);

            raw = new string(raw.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray());
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var lastComma = raw.LastIndexOf(',');
            var lastDot = raw.LastIndexOf('.');
            var decimalPos = Math.Max(lastComma, lastDot);

            string normalized;

            if (decimalPos < 0)
            {
                normalized = new string(raw.Where(char.IsDigit).ToArray());
            }
            else
            {
                var integerPart = new string(raw[..decimalPos].Where(char.IsDigit).ToArray());
                var fractionPart = new string(raw[(decimalPos + 1)..].Where(char.IsDigit).ToArray());

                normalized = fractionPart.Length > 0
                    ? $"{integerPart}.{fractionPart}"
                    : integerPart;
            }

            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
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

        private static char DetectDelimiter(string headerLine)
        {
            var semicolonCount = headerLine.Count(c => c == ';');
            var commaCount = headerLine.Count(c => c == ',');
            return semicolonCount >= commaCount ? ';' : ',';
        }

        private static List<string> SplitCsvLine(string line, char delimiter = ';')
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (ch == delimiter && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(ch);
            }

            result.Add(sb.ToString());
            return result;
        }

        private sealed record AllowedUnit(int Id, string Name);
    }
}