using GestaoOficina.Data;
using GestaoOficina.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoOficina.Features.Tenants
{
    public class TenantPlanValidator
    {
        private readonly AppDbContext _context;

        public TenantPlanValidator(AppDbContext context)
        {
            _context = context;
        }

        public async Task EnsureCanCreateUserAsync(int tenantId)
        {
            var limits = await GetLimitsAsync(tenantId);

            var usage = await GetOrCreateUsageAsync(tenantId);
            var current = usage.CurrentUsers;

            if (current >= limits.MaxUsers)
                throw new InvalidOperationException($"Limite do plano atingido: máximo {limits.MaxUsers} funcionários.");
        }

        public async Task EnsureCanCreateUnitAsync(int tenantId)
        {
            var limits = await GetLimitsAsync(tenantId);

            var usage = await GetOrCreateUsageAsync(tenantId);
            var current = usage.CurrentUnits;

            if (current >= limits.MaxUnits)
                throw new InvalidOperationException($"Limite do plano atingido: máximo {limits.MaxUnits} lojas.");
        }

        public async Task EnsureCanCreateCustomerAsync(int tenantId)
        {
            var limits = await GetLimitsAsync(tenantId);

            var usage = await GetOrCreateUsageAsync(tenantId);
            var current = usage.CurrentCustomers;

            if (current >= limits.MaxCustomers)
                throw new InvalidOperationException($"Limite do plano atingido: máximo {limits.MaxCustomers} clientes.");
        }

        public async Task EnsureCanCreateVehicleAsync(int tenantId)
        {
            var limits = await GetLimitsAsync(tenantId);

            var usage = await GetOrCreateUsageAsync(tenantId);
            var current = usage.CurrentVehicles;

            if (current >= limits.MaxVehicles)
                throw new InvalidOperationException($"Limite do plano atingido: máximo {limits.MaxVehicles} veículos.");
        }

        public async Task EnsureCanCreateServiceOrderInMonthAsync(int tenantId, DateTime? referenceDateUtc = null)
        {
            var limits = await GetLimitsAsync(tenantId);

            var usage = await GetOrCreateUsageAsync(tenantId, referenceDateUtc);
            var current = usage.CurrentServicesInMonth;

            if (current >= limits.MaxServicesPerMonth)
                throw new InvalidOperationException($"Limite do plano atingido: máximo {limits.MaxServicesPerMonth} serviços por mês.");
        }

        public async Task RegisterUserCreatedAsync(int tenantId)
        {
            var usage = await GetOrCreateUsageAsync(tenantId);
            usage.CurrentUsers += 1;
            usage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RegisterUserDeletedAsync(int tenantId)
        {
            var usage = await GetOrCreateUsageAsync(tenantId);
            usage.CurrentUsers = Math.Max(0, usage.CurrentUsers - 1);
            usage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RegisterUnitCreatedAsync(int tenantId)
        {
            var usage = await GetOrCreateUsageAsync(tenantId);
            usage.CurrentUnits += 1;
            usage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RegisterUnitDeletedAsync(int tenantId)
        {
            var usage = await GetOrCreateUsageAsync(tenantId);
            usage.CurrentUnits = Math.Max(0, usage.CurrentUnits - 1);
            usage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RegisterCustomerCreatedAsync(int tenantId)
        {
            var usage = await GetOrCreateUsageAsync(tenantId);
            usage.CurrentCustomers += 1;
            usage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RegisterCustomerDeletedAsync(int tenantId)
        {
            var usage = await GetOrCreateUsageAsync(tenantId);
            usage.CurrentCustomers = Math.Max(0, usage.CurrentCustomers - 1);
            usage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RegisterVehicleCreatedAsync(int tenantId)
        {
            var usage = await GetOrCreateUsageAsync(tenantId);
            usage.CurrentVehicles += 1;
            usage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RegisterVehicleDeletedAsync(int tenantId)
        {
            var usage = await GetOrCreateUsageAsync(tenantId);
            usage.CurrentVehicles = Math.Max(0, usage.CurrentVehicles - 1);
            usage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RegisterServiceOrderCreatedAsync(int tenantId, DateTime createdAtUtc)
        {
            var usage = await GetOrCreateUsageAsync(tenantId, createdAtUtc);
            var monthStart = GetMonthStart(createdAtUtc);

            if (usage.ServicesMonthReference == monthStart)
            {
                usage.CurrentServicesInMonth += 1;
                usage.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RegisterServiceOrderDeletedAsync(int tenantId, DateTime createdAtUtc)
        {
            var usage = await GetOrCreateUsageAsync(tenantId, DateTime.UtcNow);
            var monthStart = GetMonthStart(createdAtUtc);

            if (usage.ServicesMonthReference == monthStart)
            {
                usage.CurrentServicesInMonth = Math.Max(0, usage.CurrentServicesInMonth - 1);
                usage.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RegisterServiceOrdersDeletedInCurrentMonthAsync(int tenantId, int quantity)
        {
            if (quantity <= 0) return;

            var usage = await GetOrCreateUsageAsync(tenantId, DateTime.UtcNow);
            usage.CurrentServicesInMonth = Math.Max(0, usage.CurrentServicesInMonth - quantity);
            usage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private async Task<TenantUsage> GetOrCreateUsageAsync(int tenantId, DateTime? referenceDateUtc = null)
        {
            var usage = await _context.TenantUsages
                .FirstOrDefaultAsync(x => x.TenantId == tenantId);

            if (usage is null)
            {
                usage = await BuildUsageSnapshotAsync(tenantId, referenceDateUtc);
                _context.TenantUsages.Add(usage);
                await _context.SaveChangesAsync();
                return usage;
            }

            var reference = referenceDateUtc ?? DateTime.UtcNow;
            var monthStart = GetMonthStart(reference);

            if (usage.ServicesMonthReference != monthStart)
            {
                usage.ServicesMonthReference = monthStart;
                usage.CurrentServicesInMonth = await _context.ServiceOrders
                    .CountAsync(so =>
                        so.TenantId == tenantId &&
                        so.IsActive &&
                        so.CreatedAt >= monthStart &&
                        so.CreatedAt < monthStart.AddMonths(1));

                usage.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return usage;
        }

        private async Task<TenantUsage> BuildUsageSnapshotAsync(int tenantId, DateTime? referenceDateUtc)
        {
            var reference = referenceDateUtc ?? DateTime.UtcNow;
            var monthStart = GetMonthStart(reference);
            var nextMonthStart = monthStart.AddMonths(1);

            var currentUnits = await _context.Units.CountAsync(u => u.TenantId == tenantId && u.IsActive);
            var currentUsers = await _context.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive);
            var currentCustomers = await _context.Customers.CountAsync(c => c.TenantId == tenantId && c.IsActive);
            var currentVehicles = await _context.Vehicles.CountAsync(v => v.TenantId == tenantId && v.IsActive);
            var currentServicesInMonth = await _context.ServiceOrders.CountAsync(so =>
                so.TenantId == tenantId &&
                so.IsActive &&
                so.CreatedAt >= monthStart &&
                so.CreatedAt < nextMonthStart);

            return new TenantUsage
            {
                TenantId = tenantId,
                CurrentUnits = currentUnits,
                CurrentUsers = currentUsers,
                CurrentCustomers = currentCustomers,
                CurrentVehicles = currentVehicles,
                CurrentServicesInMonth = currentServicesInMonth,
                ServicesMonthReference = monthStart,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static DateTime GetMonthStart(DateTime reference)
            => new(reference.Year, reference.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        private async Task<TenantPlanLimits> GetLimitsAsync(int tenantId)
        {
            var plan = await _context.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => t.Plan)
                .FirstOrDefaultAsync();

            if (!Enum.IsDefined(plan))
                throw new InvalidOperationException("Tenant inválido para validação de plano.");

            return TenantPlanCatalog.Get(plan);
        }
    }
}