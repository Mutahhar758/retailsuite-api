using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Kots;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.Kots;

internal class KotService : IKotService
{
    private readonly IRepository<KotOrder> _kotOrderRepository;
    private readonly IRepository<KotOrderItem> _kotOrderItemRepository;
    private readonly IRepository<DiningTable> _diningTableRepository;
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;

    public KotService(
        IRepository<KotOrder> kotOrderRepository,
        IRepository<KotOrderItem> kotOrderItemRepository,
        IRepository<DiningTable> diningTableRepository,
        IRepository<ItemDetail> itemRepository,
        IRepository<ChartOfAccount> chartOfAccountRepository)
    {
        _kotOrderRepository = kotOrderRepository;
        _kotOrderItemRepository = kotOrderItemRepository;
        _diningTableRepository = diningTableRepository;
        _itemRepository = itemRepository;
        _chartOfAccountRepository = chartOfAccountRepository;
    }

    #region KOT Orders

    public async Task<KotOrderResponse> CreateAsync(KotOrderCreateRequest request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        
        // 1. Generate sequential token number for today
        var maxTokenNo = await _kotOrderRepository.GetAll()
            .Where(x => x.OrderDate == today)
            .MaxAsync(x => (int?)x.TokenNo, cancellationToken);
        
        int tokenNo = (maxTokenNo ?? 0) + 1;

        // 2. Compute total amount
        decimal totalAmount = request.Lines.Sum(x => x.Qty * x.Rate);

        // 3. Create the order
        var order = new KotOrder
        {
            TokenNo = tokenNo,
            OrderDate = today,
            OrderTime = TimeOnly.FromDateTime(DateTime.Now),
            OrderType = request.OrderType,
            TableId = request.TableId,
            CustomerId = request.CustomerId,
            Remarks = request.Remarks,
            TotalAmount = totalAmount,
            Status = "Pending"
        };

        await _kotOrderRepository.AddAsync(order, false);

        foreach (var line in request.Lines)
        {
            await _kotOrderItemRepository.AddAsync(new KotOrderItem
            {
                KotOrder = order,
                ItemId = line.ItemId,
                Qty = line.Qty,
                Rate = line.Rate,
                Notes = line.Notes,
                Status = "Pending"
            }, false);
        }

        // 4. Update dining table status if Occupied
        if (request.OrderType == "DineIn" && request.TableId.HasValue)
        {
            var table = await _diningTableRepository.GetByIdAsync(request.TableId.Value, cancellationToken);
            if (table != null)
            {
                table.Status = "Occupied";
                await _diningTableRepository.UpdateAsync(table, false);
            }
        }

        await _kotOrderRepository.SaveChangesAsync(cancellationToken);

        return await GetResponseByIdAsync(order.Id, cancellationToken);
    }

    public async Task<List<KotOrderResponse>> GetActiveListAsync(string? prepStationId, CancellationToken cancellationToken)
    {
        var activeStatuses = new[] { "Pending", "Preparing", "Ready" };

        var query = _kotOrderRepository.GetAll()
            .Include(o => o.Table)
            .Where(o => activeStatuses.Contains(o.Status));

        var orders = await query.ToListAsync(cancellationToken);
        var responses = new List<KotOrderResponse>();

        foreach (var order in orders)
        {
            // Join items with their category to filter by PrepStation
            var itemsQuery = from d in _kotOrderItemRepository.GetAll().AsNoTracking()
                             join i in _itemRepository.GetAll().AsNoTracking() on d.ItemId equals i.Id
                             where d.KotOrderId == order.Id
                             select new
                             {
                                 Detail = d,
                                 ItemTitle = i.Title,
                                 CategoryId = i.ItemCategoryId,
                                 CategoryTitle = i.ItemCategory != null ? i.ItemCategory.Title : string.Empty,
                                 PrepStationId = i.ItemCategory != null ? i.ItemCategory.PrepStationId : null
                             };

            var items = await itemsQuery.ToListAsync(cancellationToken);

            // Filter items by prep station if provided
            var filteredItems = items.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(prepStationId) && prepStationId.ToUpper() != "ALL")
            {
                // If prepStationId is "KITCHEN" (default/main), include items with KITCHEN prep station OR null prep station
                if (prepStationId.ToUpper() == "KITCHEN")
                {
                    filteredItems = items.Where(x => x.PrepStationId == null || x.PrepStationId.ToUpper() == "KITCHEN");
                }
                else
                {
                    filteredItems = items.Where(x => x.PrepStationId != null && x.PrepStationId.Equals(prepStationId, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (!filteredItems.Any())
            {
                continue; // Skip orders that have no items matching this prep station
            }

            var customerName = string.Empty;
            if (!string.IsNullOrEmpty(order.CustomerId))
            {
                var customer = await _chartOfAccountRepository.GetAll().AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == order.CustomerId, cancellationToken);
                customerName = customer?.Title;
            }

            responses.Add(new KotOrderResponse
            {
                Id = order.Id,
                TokenNo = order.TokenNo,
                OrderDate = order.OrderDate,
                OrderTime = order.OrderTime,
                OrderType = order.OrderType,
                TableId = order.TableId,
                TableName = order.Table?.Name,
                Status = order.Status,
                SaleVoucherNo = order.SaleVoucherNo,
                CustomerId = order.CustomerId,
                CustomerName = customerName,
                TotalAmount = order.TotalAmount,
                Remarks = order.Remarks,
                CreatedBy = order.CreatedBy ?? string.Empty,
                CreatedOn = order.CreatedOn,
                Lines = filteredItems.Select(x => new KotOrderItemResponse
                {
                    Id = x.Detail.Id,
                    ItemId = x.Detail.ItemId,
                    ItemTitle = x.ItemTitle,
                    ItemCategoryCode = x.CategoryId,
                    PrepStationId = x.PrepStationId,
                    Qty = x.Detail.Qty,
                    Rate = x.Detail.Rate,
                    Notes = x.Detail.Notes,
                    Status = x.Detail.Status
                }).ToList()
            });
        }

        return responses.OrderBy(r => r.OrderDate).ThenBy(r => r.OrderTime).ToList();
    }

    public async Task<KotOrderResponse?> GetByTokenOrIdAsync(string query, CancellationToken cancellationToken)
    {
        KotOrder? order = null;

        if (int.TryParse(query, out int idVal))
        {
            // First search by direct ID
            order = await _kotOrderRepository.GetAll()
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == idVal, cancellationToken);
        }

        if (order == null && int.TryParse(query, out int tokenVal))
        {
            // Search by today's Token #
            var today = DateOnly.FromDateTime(DateTime.Today);
            order = await _kotOrderRepository.GetAll()
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.TokenNo == tokenVal && o.OrderDate == today && o.Status != "Paid" && o.Status != "Cancelled", cancellationToken);

            if (order == null)
            {
                // Search token globally as fallback
                order = await _kotOrderRepository.GetAll()
                    .Include(o => o.Table)
                    .FirstOrDefaultAsync(o => o.TokenNo == tokenVal && o.Status != "Paid" && o.Status != "Cancelled", cancellationToken);
            }
        }

        if (order == null)
        {
            return null;
        }

        return await GetResponseByIdAsync(order.Id, cancellationToken);
    }

    public async Task UpdateItemStatusAsync(int orderId, int itemId, string status, CancellationToken cancellationToken)
    {
        var item = await _kotOrderItemRepository.GetAll()
            .FirstOrDefaultAsync(x => x.KotOrderId == orderId && x.Id == itemId, cancellationToken);

        if (item == null)
            throw new NotFoundException($"Kot Order Item with ID '{itemId}' not found on order '{orderId}'.");

        item.Status = status;
        await _kotOrderItemRepository.UpdateAsync(item, false);

        // Check if all items in this KOT order are complete/ready
        var allItems = await _kotOrderItemRepository.GetAll()
            .Where(x => x.KotOrderId == orderId)
            .ToListAsync(cancellationToken);

        var order = await _kotOrderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order != null)
        {
            if (allItems.All(x => x.Status == "Ready"))
            {
                order.Status = "Ready";
                await _kotOrderRepository.UpdateAsync(order, false);
            }
            else if (allItems.Any(x => x.Status == "Preparing" || x.Status == "Ready") && order.Status == "Pending")
            {
                order.Status = "Preparing";
                await _kotOrderRepository.UpdateAsync(order, false);
            }
        }

        await _kotOrderItemRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken)
    {
        var order = await _kotOrderRepository.GetAll()
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
            throw new NotFoundException($"KOT Order with ID '{orderId}' not found.");

        order.Status = status;
        await _kotOrderRepository.UpdateAsync(order, false);

        // Update all items status to match if setting to Cancelled
        if (status == "Cancelled")
        {
            var items = await _kotOrderItemRepository.GetAll()
                .Where(x => x.KotOrderId == orderId)
                .ToListAsync(cancellationToken);
            foreach (var item in items)
            {
                item.Status = "Cancelled";
                await _kotOrderItemRepository.UpdateAsync(item, false);
            }

            if (order.TableId.HasValue)
            {
                var table = await _diningTableRepository.GetByIdAsync(order.TableId.Value, cancellationToken);
                if (table != null)
                {
                    table.Status = "Available";
                    await _diningTableRepository.UpdateAsync(table, false);
                }
            }
        }

        await _kotOrderRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task FinalizePaymentAsync(int orderId, string saleVoucherNo, CancellationToken cancellationToken)
    {
        var order = await _kotOrderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new NotFoundException($"KOT Order with ID '{orderId}' not found.");

        order.Status = "Paid";
        order.SaleVoucherNo = saleVoucherNo;
        await _kotOrderRepository.UpdateAsync(order, false);

        // Mark all items as Served/Ready if they aren't
        var items = await _kotOrderItemRepository.GetAll()
            .Where(x => x.KotOrderId == orderId)
            .ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            if (item.Status == "Pending" || item.Status == "Preparing")
            {
                item.Status = "Ready";
                await _kotOrderItemRepository.UpdateAsync(item, false);
            }
        }

        // Release Table
        if (order.TableId.HasValue)
        {
            var table = await _diningTableRepository.GetByIdAsync(order.TableId.Value, cancellationToken);
            if (table != null)
            {
                table.Status = "Available";
                await _diningTableRepository.UpdateAsync(table, false);
            }
        }

        await _kotOrderRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<KotOrderResponse> GetResponseByIdAsync(int id, CancellationToken cancellationToken)
    {
        var order = await _kotOrderRepository.GetAll()
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            throw new NotFoundException($"KOT Order '{id}' not found.");

        var itemsQuery = from d in _kotOrderItemRepository.GetAll().AsNoTracking()
                         join i in _itemRepository.GetAll().AsNoTracking() on d.ItemId equals i.Id
                         where d.KotOrderId == order.Id
                         select new
                         {
                             Detail = d,
                             ItemTitle = i.Title,
                             CategoryId = i.ItemCategoryId,
                             PrepStationId = i.ItemCategory != null ? i.ItemCategory.PrepStationId : null
                         };

        var items = await itemsQuery.ToListAsync(cancellationToken);

        var customerName = string.Empty;
        if (!string.IsNullOrEmpty(order.CustomerId))
        {
            var customer = await _chartOfAccountRepository.GetAll().AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == order.CustomerId, cancellationToken);
            customerName = customer?.Title;
        }

        return new KotOrderResponse
        {
            Id = order.Id,
            TokenNo = order.TokenNo,
            OrderDate = order.OrderDate,
            OrderTime = order.OrderTime,
            OrderType = order.OrderType,
            TableId = order.TableId,
            TableName = order.Table?.Name,
            Status = order.Status,
            SaleVoucherNo = order.SaleVoucherNo,
            CustomerId = order.CustomerId,
            CustomerName = customerName,
            TotalAmount = order.TotalAmount,
            Remarks = order.Remarks,
            CreatedBy = order.CreatedBy ?? string.Empty,
            CreatedOn = order.CreatedOn,
            Lines = items.Select(x => new KotOrderItemResponse
            {
                Id = x.Detail.Id,
                ItemId = x.Detail.ItemId,
                ItemTitle = x.ItemTitle,
                ItemCategoryCode = x.CategoryId,
                PrepStationId = x.PrepStationId,
                Qty = x.Detail.Qty,
                Rate = x.Detail.Rate,
                Notes = x.Detail.Notes,
                Status = x.Detail.Status
            }).ToList()
        };
    }

    #endregion

}
