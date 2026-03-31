using GestaoOficina.Data;
using GestaoOficina.DTOs.ServiceOrders;
using GestaoOficina.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoOficina.Features.ServiceOrders
{
    public class ServiceOrderService
    {
        private readonly AppDbContext _context;

        public ServiceOrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ServiceOrder>> GetServiceOrdersByTenantAndUnits(int tenantId, List<int> unitIds, bool fullAccess)
        {
            return await _context.ServiceOrders
                .Include(so => so.Unit)
                .Include(so => so.Vehicle)
                .Include(so => so.OwnerCustomer)
                .Include(so => so.Status)
                .Include(so => so.Parts.Where(p => p.IsActive))
                .Where(so => so.TenantId == tenantId)
                .Where(so => so.IsActive)
                .Where(so => fullAccess || unitIds.Contains(so.UnitId))
                .OrderByDescending(so => so.CreatedAt)
                .ToListAsync();
        }

        public async Task<ServiceOrder?> GetServiceOrderById(int id)
        {
            return await _context.ServiceOrders
                .Include(so => so.Unit)
                .Include(so => so.Vehicle)
                .Include(so => so.OwnerCustomer)
                .Include(so => so.Status)
                .Include(so => so.Parts.Where(p => p.IsActive))
                .FirstOrDefaultAsync(so => so.Id == id && so.IsActive);
        }

        public bool HasAccess(ServiceOrder serviceOrder, int tenantId, List<int> unitIds, bool fullAccess)
        {
            if (serviceOrder.TenantId != tenantId) return false;
            if (!serviceOrder.IsActive) return false;
            if (fullAccess) return true;

            return unitIds.Contains(serviceOrder.UnitId);
        }

        public async Task<ServiceOrder> CreateServiceOrder(
            CreateServiceOrderRequest dto,
            int tenantId,
            List<int> unitIds,
            bool fullAccess)
        {
            if (!fullAccess && !unitIds.Contains(dto.UnitId))
            {
                throw new InvalidOperationException("Usuário sem acesso à unidade informada.");
            }

            var unitExists = await _context.Units
                .AnyAsync(u => u.Id == dto.UnitId && u.TenantId == tenantId && u.IsActive);

            if (!unitExists)
            {
                throw new InvalidOperationException("Unidade inválida para o tenant informado.");
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId && v.TenantId == tenantId && v.IsActive);

            if (vehicle == null)
            {
                throw new InvalidOperationException("Veículo inválido para o tenant informado.");
            }

            var ownerCustomer = await _context.Customers
                .Include(c => c.CustomerUnits)
                .FirstOrDefaultAsync(c => c.Id == dto.OwnerCustomerId && c.TenantId == tenantId && c.IsActive);

            if (ownerCustomer == null)
            {
                throw new InvalidOperationException("Cliente responsável inválido para o tenant informado.");
            }

            var customerLinkedToUnit = ownerCustomer.CustomerUnits.Any(cu => cu.IsActive && cu.UnitId == dto.UnitId);
            if (!customerLinkedToUnit)
            {
                throw new InvalidOperationException("Cliente responsável não está vinculado à unidade informada.");
            }

            var initialStatus = await _context.ServiceOrderStatuses
                .FirstOrDefaultAsync(s => s.Code == "ENVIADO");

            if (initialStatus == null)
            {
                throw new InvalidOperationException("Status inicial ENVIADO não encontrado.");
            }

            var now = DateTime.UtcNow;
            var bodyworkValue = dto.BodyworkValue ?? 0m;
            var paintValue = dto.PaintValue ?? 0m;
            var partsValue = dto.Parts?.Sum(p => p.Quantity * p.UnitPrice) ?? 0m;
            var totalAmount = bodyworkValue + paintValue + partsValue;

            if (totalAmount < 0)
            {
                totalAmount = 0;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var serviceOrder = new ServiceOrder
            {
                TenantId = tenantId,
                UnitId = dto.UnitId,
                VehicleId = dto.VehicleId,
                OwnerCustomerId = dto.OwnerCustomerId,
                StatusId = initialStatus.Id,
                EntryDate = dto.EntryDate!.Value,
                EstimatedDeliveryDate = dto.EstimatedDeliveryDate,
                BodyworkDescription = dto.BodyworkDescription,
                BodyworkValue = bodyworkValue,
                PaintDescription = dto.PaintDescription,
                PaintValue = paintValue,
                PartsValue = partsValue,
                TotalAmount = totalAmount,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.ServiceOrders.Add(serviceOrder);
            await _context.SaveChangesAsync();

            if (dto.Parts is { Count: > 0 })
            {
                var parts = dto.Parts.Select(p => new ServiceOrderPart
                {
                    TenantId = tenantId,
                    ServiceOrderId = serviceOrder.Id,
                    Description = p.Description,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice,
                    TotalPrice = p.Quantity * p.UnitPrice,
                    IsActive = true,
                    CreatedAt = now
                }).ToList();

                _context.ServiceOrderParts.AddRange(parts);
            }

            _context.ServiceOrderTimelines.Add(new ServiceOrderTimeline
            {
                TenantId = tenantId,
                ServiceOrderId = serviceOrder.Id,
                EventType = "CREATED",
                Message = "Ordem de serviço criada com status ENVIADO.",
                OldStatusId = null,
                NewStatusId = initialStatus.Id,
                IsActive = true,
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetServiceOrderById(serviceOrder.Id) ?? serviceOrder;
        }

        public async Task<ServiceOrder?> UpdateServiceOrder(
            int id,
            UpdateServiceOrderRequest dto,
            int tenantId,
            List<int> unitIds,
            bool fullAccess)
        {
            var serviceOrder = await _context.ServiceOrders
                .FirstOrDefaultAsync(so => so.Id == id && so.TenantId == tenantId && so.IsActive);

            if (serviceOrder == null) return null;

            var targetUnitId = dto.UnitId ?? serviceOrder.UnitId;
            var targetVehicleId = dto.VehicleId ?? serviceOrder.VehicleId;
            var targetOwnerCustomerId = dto.OwnerCustomerId ?? serviceOrder.OwnerCustomerId;

            if (!fullAccess && (!unitIds.Contains(serviceOrder.UnitId) || !unitIds.Contains(targetUnitId)))
            {
                throw new InvalidOperationException("Usuário sem acesso à unidade informada.");
            }

            var unitExists = await _context.Units
                .AnyAsync(u => u.Id == targetUnitId && u.TenantId == tenantId && u.IsActive);

            if (!unitExists)
            {
                throw new InvalidOperationException("Unidade inválida para o tenant informado.");
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == targetVehicleId && v.TenantId == tenantId && v.IsActive);

            if (vehicle == null)
            {
                throw new InvalidOperationException("Veículo inválido para o tenant informado.");
            }

            var ownerCustomer = await _context.Customers
                .Include(c => c.CustomerUnits)
                .FirstOrDefaultAsync(c => c.Id == targetOwnerCustomerId && c.TenantId == tenantId && c.IsActive);

            if (ownerCustomer == null)
            {
                throw new InvalidOperationException("Cliente responsável inválido para o tenant informado.");
            }

            var customerLinkedToUnit = ownerCustomer.CustomerUnits.Any(cu => cu.IsActive && cu.UnitId == targetUnitId);
            if (!customerLinkedToUnit)
            {
                throw new InvalidOperationException("Cliente responsável não está vinculado à unidade informada.");
            }

            if (dto.StatusId.HasValue)
            {
                var statusExists = await _context.ServiceOrderStatuses.AnyAsync(s => s.Id == dto.StatusId.Value);
                if (!statusExists)
                {
                    throw new InvalidOperationException("Status inválido.");
                }
            }

            var now = DateTime.UtcNow;

            var partsValue = serviceOrder.PartsValue;
            if (dto.Parts is not null)
            {
                partsValue = dto.Parts.Sum(p => p.Quantity * p.UnitPrice);
            }

            var bodyworkValue = dto.BodyworkValue ?? serviceOrder.BodyworkValue;
            var paintValue = dto.PaintValue ?? serviceOrder.PaintValue;
            var totalAmount = bodyworkValue + paintValue + partsValue;

            if (totalAmount < 0)
            {
                totalAmount = 0;
            }

            var oldStatusId = serviceOrder.StatusId;
            var statusChanged = dto.StatusId.HasValue && dto.StatusId.Value != oldStatusId;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            serviceOrder.UnitId = targetUnitId;
            serviceOrder.VehicleId = targetVehicleId;
            serviceOrder.OwnerCustomerId = targetOwnerCustomerId;
            if (dto.EntryDate.HasValue) serviceOrder.EntryDate = dto.EntryDate.Value;
            if (dto.EstimatedDeliveryDate is not null) serviceOrder.EstimatedDeliveryDate = dto.EstimatedDeliveryDate;
            if (dto.DeliveryDate is not null) serviceOrder.DeliveryDate = dto.DeliveryDate;
            if (dto.BodyworkDescription is not null) serviceOrder.BodyworkDescription = dto.BodyworkDescription;
            serviceOrder.BodyworkValue = bodyworkValue;
            if (dto.PaintDescription is not null) serviceOrder.PaintDescription = dto.PaintDescription;
            serviceOrder.PaintValue = paintValue;
            serviceOrder.PartsValue = partsValue;
            serviceOrder.TotalAmount = totalAmount;
            serviceOrder.UpdatedAt = now;

            if (dto.StatusId.HasValue)
            {
                serviceOrder.StatusId = dto.StatusId.Value;
            }

            _context.ServiceOrders.Update(serviceOrder);

            if (dto.Parts is not null)
            {
                var existingParts = await _context.ServiceOrderParts
                    .Where(p => p.ServiceOrderId == serviceOrder.Id && p.IsActive)
                    .ToListAsync();

                foreach (var existingPart in existingParts)
                {
                    existingPart.IsActive = false;
                }

                if (existingParts.Count > 0)
                {
                    _context.ServiceOrderParts.UpdateRange(existingParts);
                }

                if (dto.Parts.Count > 0)
                {
                    var parts = dto.Parts.Select(p => new ServiceOrderPart
                    {
                        TenantId = tenantId,
                        ServiceOrderId = serviceOrder.Id,
                        Description = p.Description,
                        Quantity = p.Quantity,
                        UnitPrice = p.UnitPrice,
                        TotalPrice = p.Quantity * p.UnitPrice,
                        IsActive = true,
                        CreatedAt = now
                    }).ToList();

                    _context.ServiceOrderParts.AddRange(parts);
                }
            }

            _context.ServiceOrderTimelines.Add(new ServiceOrderTimeline
            {
                TenantId = tenantId,
                ServiceOrderId = serviceOrder.Id,
                EventType = statusChanged ? "STATUS_CHANGED" : "UPDATED",
                Message = statusChanged
                    ? "Status da ordem de serviço atualizado."
                    : "Ordem de serviço atualizada.",
                OldStatusId = statusChanged ? oldStatusId : null,
                NewStatusId = statusChanged ? serviceOrder.StatusId : null,
                IsActive = true,
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetServiceOrderById(serviceOrder.Id) ?? serviceOrder;
        }

        public async Task<ServiceOrder?> ChangeStatus(
            int id,
            string targetStatusCode,
            int tenantId,
            List<int> unitIds,
            bool fullAccess)
        {
            var serviceOrder = await _context.ServiceOrders
                .Include(so => so.Status)
                .FirstOrDefaultAsync(so => so.Id == id && so.IsActive);

            if (serviceOrder == null) return null;
            if (!HasAccess(serviceOrder, tenantId, unitIds, fullAccess))
                throw new UnauthorizedAccessException("Usuário sem acesso à OS.");

            targetStatusCode = targetStatusCode.Trim().ToUpperInvariant();

            var targetStatus = await _context.ServiceOrderStatuses
                .FirstOrDefaultAsync(s => s.Code == targetStatusCode);

            if (targetStatus == null)
                throw new InvalidOperationException("Status inválido.");

            var currentCode = serviceOrder.Status?.Code?.ToUpperInvariant();
            var allowed =
                (currentCode == "ENVIADO" && targetStatusCode == "FEITO") ||
                (currentCode == "FEITO" && targetStatusCode == "FINALIZADO") ||
                (currentCode == "FEITO" && targetStatusCode == "ENVIADO") ||
                (currentCode == "FINALIZADO" && targetStatusCode == "ENVIADO");

            if (!allowed)
                throw new InvalidOperationException($"Transição inválida: {currentCode} -> {targetStatusCode}.");

            var now = DateTime.UtcNow;
            var oldStatusId = serviceOrder.StatusId;

            serviceOrder.StatusId = targetStatus.Id;

            if (targetStatusCode == "FINALIZADO" && serviceOrder.DeliveryDate is null)
                serviceOrder.DeliveryDate = now;

            if (currentCode == "FINALIZADO" && targetStatusCode != "FINALIZADO")
                serviceOrder.DeliveryDate = null;

            serviceOrder.UpdatedAt = now;

            _context.ServiceOrderTimelines.Add(new ServiceOrderTimeline
            {
                TenantId = serviceOrder.TenantId,
                ServiceOrderId = serviceOrder.Id,
                EventType = "STATUS_CHANGED",
                Message = $"Status alterado para {targetStatus.Name}.",
                OldStatusId = oldStatusId,
                NewStatusId = targetStatus.Id,
                IsActive = true,
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            return await GetServiceOrderById(serviceOrder.Id);
        }

        public async Task<bool> DeleteServiceOrder(
            int id,
            int tenantId,
            List<int> unitIds,
            bool fullAccess)
        {
            var serviceOrder = await _context.ServiceOrders
                .FirstOrDefaultAsync(so => so.Id == id && so.TenantId == tenantId && so.IsActive);

            if (serviceOrder == null) return false;

            if (!fullAccess && !unitIds.Contains(serviceOrder.UnitId))
            {
                throw new InvalidOperationException("Usuário sem acesso à unidade informada.");
            }

            serviceOrder.IsActive = false;
            serviceOrder.UpdatedAt = DateTime.UtcNow;

            var parts = await _context.ServiceOrderParts
                .Where(p => p.ServiceOrderId == serviceOrder.Id && p.IsActive)
                .ToListAsync();

            foreach (var part in parts)
            {
                part.IsActive = false;
            }

            var timelines = await _context.ServiceOrderTimelines
                .Where(t => t.ServiceOrderId == serviceOrder.Id && t.IsActive)
                .ToListAsync();

            foreach (var timeline in timelines)
            {
                timeline.IsActive = false;
            }

            _context.ServiceOrders.Update(serviceOrder);
            if (parts.Count > 0) _context.ServiceOrderParts.UpdateRange(parts);
            if (timelines.Count > 0) _context.ServiceOrderTimelines.UpdateRange(timelines);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
