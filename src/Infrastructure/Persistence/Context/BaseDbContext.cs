using System.Data;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Domain.Payroll;
using Retailer.Domain.Common.Contracts;
using Retailer.Domain.Identity;
using Retailer.Domain.Legacy;
using Retailer.Infrastructure.Auditing;
using Retailer.Infrastructure.Persistence.Transactions;
using Retailer.Infrastructure.State;
using Retailer.Shared.Common;
using Retailer.Shared.Common.Constants;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Persistence.Context;

public abstract class BaseDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string, IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>, ApplicationRoleClaim, IdentityUserToken<string>>, IMultiTenantDbContext
{
    protected readonly ICurrentUser _currentUser;
    private readonly ISerializerService _serializer;
    private readonly DatabaseSettings _dbSettings;
    private readonly EfTransactionManager _transactionManager;
    private readonly IMultiTenantContextAccessor<AppTenantInfo>? _multiTenantContextAccessor;

    protected BaseDbContext(DbContextOptions options, ICurrentUser currentUser, ISerializerService serializer, IOptions<DatabaseSettings> dbSettings, EfTransactionManager transactionManager, IMultiTenantContextAccessor<AppTenantInfo>? multiTenantContextAccessor = null)
        : base(options)
    {
        _currentUser = currentUser;
        _serializer = serializer;
        _dbSettings = dbSettings.Value;
        _transactionManager = transactionManager;
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }

    public ITenantInfo? TenantInfo => _multiTenantContextAccessor?.MultiTenantContext?.TenantInfo;
    public TenantMismatchMode TenantMismatchMode => TenantMismatchMode.Overwrite;
    public TenantNotSetMode TenantNotSetMode => TenantNotSetMode.Overwrite;

    // Used by Dapper
    public IDbConnection Connection => Database.GetDbConnection();

    public DbSet<Trail> AuditTrails => Set<Trail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        ConfigureLegacyBooleanFlags(modelBuilder);
        ConfigureMultiTenantPrimaryKeys(modelBuilder);

        // QueryFilters need to be applied before base.OnModelCreating
        modelBuilder.AppendGlobalQueryFilter<ISoftDelete>(s => s.DeletedOn == null, GlobalQueryFilterConstants.SoftDelete);
    }

    private static void ConfigureMultiTenantPrimaryKeys(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!entityType.IsMultiTenant() || entityType.ClrType is null)
            {
                continue;
            }

            var entityBuilder = modelBuilder.Entity(entityType.ClrType);
            var multiTenantBuilder = entityBuilder.IsMultiTenant();
            var primaryKey = entityBuilder.Metadata.FindPrimaryKey();

            if (primaryKey is not null)
            {
                multiTenantBuilder.AdjustKey(primaryKey, modelBuilder);
            }
        }
    }

    private static void ConfigureLegacyBooleanFlags(ModelBuilder modelBuilder)
    {
        ConfigureNullableBoolAsNumericFlag(modelBuilder.Entity<CustomerDetail>().Property(x => x.SmsAlert));
        ConfigureNullableBoolAsNumericFlag(modelBuilder.Entity<CustomerDetail>().Property(x => x.EmailAlert));
        ConfigureNullableBoolAsNumericFlag(modelBuilder.Entity<CustomerDetail>().Property(x => x.Active));

        ConfigureNullableBoolAsNumericFlag(modelBuilder.Entity<SupplierDetail>().Property(x => x.SmsAlert));
        ConfigureNullableBoolAsNumericFlag(modelBuilder.Entity<SupplierDetail>().Property(x => x.EmailAlert));
        ConfigureNullableBoolAsNumericFlag(modelBuilder.Entity<SupplierDetail>().Property(x => x.Active));
        ConfigureNullableBoolAsNumericFlag(modelBuilder.Entity<SupplierDetail>().Property(x => x.ShowInSales));

        ConfigureBoolAsNumericFlag(modelBuilder.Entity<ItemCategory>().Property(x => x.Active));
        ConfigureBoolAsNumericFlag(modelBuilder.Entity<CustomerCategory>().Property(x => x.Active));
        ConfigureNullableBoolAsNumericFlag(modelBuilder.Entity<ItemDetail>().Property(x => x.Alert));
    }

    private static void ConfigureBoolAsNumericFlag(PropertyBuilder<bool> propertyBuilder)
    {
        propertyBuilder
            .HasConversion(
                v => v ? 1m : 0m,
                v => v == 1m)
            .HasColumnType("numeric(1,0)");
    }

    private static void ConfigureNullableBoolAsNumericFlag(PropertyBuilder<bool?> propertyBuilder)
    {
        propertyBuilder
            .HasConversion(
                v => v.HasValue ? (decimal?)(v.Value ? 1m : 0m) : null,
                v => v.HasValue ? v.Value == 1m : (bool?)null)
            .HasColumnType("numeric(1,0)");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // TODO: We want this only for development probably... maybe better make it configurable in logger.json config?
        //optionsBuilder.EnableSensitiveDataLogging();

        // If you want to see the sql queries that efcore executes:

        // Uncomment the next line to see them in the output window of visual studio
        //optionsBuilder.LogTo(m => System.Diagnostics.Debug.WriteLine(m), Microsoft.Extensions.Logging.LogLevel.Information);

        // Or uncomment the next line if you want to see them in the console
        //optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);

        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        if (!optionsBuilder.IsConfigured)
        {
            // Use tenant-specific connection string when available, fall back to default
            var tenantInfo = _multiTenantContextAccessor?.MultiTenantContext?.TenantInfo;
            var tenantConnectionString = tenantInfo?.ConnectionString;
            var connectionString = !string.IsNullOrWhiteSpace(tenantConnectionString)
                ? tenantConnectionString
                : _dbSettings.ConnectionString;

            var dbProvider = tenantInfo != null && !string.IsNullOrWhiteSpace(tenantInfo.DbProvider)
                ? tenantInfo.DbProvider
                : _dbSettings.DBProvider;

            optionsBuilder.UseDatabase(dbProvider, connectionString);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        // Begin transaction if not already started for this request
        if (_transactionManager.Transaction == null && ApplicationState.IsStarted)
        {
            _transactionManager.Transaction = await Database.BeginTransactionAsync(cancellationToken);
        }

        this.EnforceMultiTenant();
        await EnforceAccountingPeriodLockAsync(cancellationToken);
        var userId = _currentUser.GetUserId();
        var username = string.IsNullOrWhiteSpace(_currentUser.Username) ? userId : _currentUser.Username;
        var auditEntries = HandleAuditingBeforeSaveChanges(userId, username);
        this.EnforceMultiTenant();
        int result = await base.SaveChangesAsync(cancellationToken);
        await HandleAuditingAfterSaveChangesAsync(auditEntries, cancellationToken);
        return result;
    }


    private static readonly HashSet<string> AllowedClosedItemTxProps = new(StringComparer.Ordinal)
    {
        nameof(ItemTransaction.RemainingQty),
        nameof(ItemTransaction.CostPrice),
        nameof(ItemTransaction.CostAmount),
        nameof(ItemTransaction.RunningQtyBalance),
        nameof(IAuditableEntity.LastModifiedOn),
        nameof(IAuditableEntity.LastModifiedBy)
    };

    private static readonly HashSet<string> AllowedClosedGlReconcileProps = new(StringComparer.Ordinal)
    {
        nameof(GlEntry.Clear),
        nameof(GlEntry.ClearingDate),
        nameof(GlEntry.CheckStatus),
        nameof(IAuditableEntity.LastModifiedOn),
        nameof(IAuditableEntity.LastModifiedBy)
    };

    private async Task EnforceAccountingPeriodLockAsync(CancellationToken cancellationToken)
    {
        if (!ApplicationState.IsStarted || PeriodClosingScope.IsActive)
        {
            return;
        }

        var guardedEntries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => TryGetGuardedDatePropertyName(e.Entity) != null)
            .ToList();

        if (guardedEntries.Count == 0)
        {
            return;
        }

        var tenantId = TenantInfo?.Id ?? "__default__";
        if (!PeriodLockCache.TryGet(tenantId, out var lockDate))
        {
            lockDate = await Set<AccountingPeriodClose>()
                .AsNoTracking()
                .Where(x => x.Status == "Closed")
                .MaxAsync(x => (DateOnly?)x.ClosingDate, cancellationToken);

            PeriodLockCache.Set(tenantId, lockDate);
        }

        if (!lockDate.HasValue)
        {
            return;
        }

        var effectiveLock = lockDate.Value;
        var lockMsg = $"Accounting period is closed through {effectiveLock:dd-MMM-yyyy}. Transactions on or before this date cannot be created, modified, or deleted.";

        foreach (var entry in guardedEntries)
        {
            var datePropName = TryGetGuardedDatePropertyName(entry.Entity)!;

            if (entry.Entity is GlEntry glEntry && string.Equals(glEntry.VType, "Op", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException($"Opening balances cannot be modified after an accounting period has been closed (locked through {effectiveLock:dd-MMM-yyyy}).");
            }

            bool isSoftDelete = entry.State == EntityState.Modified
                && entry.Entity is ISoftDelete
                && entry.Property(nameof(ISoftDelete.DeletedOn)).OriginalValue == null
                && entry.Property(nameof(ISoftDelete.DeletedOn)).CurrentValue != null;

            if (entry.State == EntityState.Modified && !isSoftDelete)
            {
                var changedProps = entry.Properties
                    .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                    .Select(p => p.Metadata.Name)
                    .ToList();

                if (changedProps.Count == 0)
                {
                    continue;
                }

                // Allow FIFO consumption & running balance cache updates on historical ItemTransaction batches
                if (entry.Entity is ItemTransaction && changedProps.All(p => AllowedClosedItemTxProps.Contains(p)))
                {
                    continue;
                }

                // Allow Bank Reconciliation clearing status updates on pre-close cheques without modifying amounts/dates
                if (entry.Entity is GlEntry && changedProps.All(p => AllowedClosedGlReconcileProps.Contains(p)))
                {
                    continue;
                }
            }

            var dateProp = entry.Property(datePropName);
            var currentDate = dateProp.CurrentValue is DateOnly cd ? cd : default;
            var originalDate = dateProp.OriginalValue is DateOnly od ? od : currentDate;

            if (entry.State == EntityState.Added)
            {
                if (currentDate <= effectiveLock)
                {
                    throw new BadRequestException(lockMsg);
                }
            }
            else if (entry.State == EntityState.Deleted || isSoftDelete)
            {
                if (originalDate <= effectiveLock)
                {
                    throw new BadRequestException(lockMsg);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (originalDate <= effectiveLock || currentDate <= effectiveLock)
                {
                    throw new BadRequestException(lockMsg);
                }
            }
        }
    }

    private static string? TryGetGuardedDatePropertyName(object entity) => entity switch
    {
        GlEntry => nameof(GlEntry.VDate),
        ItemTransaction => nameof(ItemTransaction.VDate),
        SaleMaster => nameof(SaleMaster.VDate),
        SaleRetMaster => nameof(SaleRetMaster.VDate),
        SaleSupplyMaster => nameof(SaleSupplyMaster.VDate),
        PurchaseMaster => nameof(PurchaseMaster.VDate),
        PurchaseRetMaster => nameof(PurchaseRetMaster.VDate),
        StockAdjMaster => nameof(StockAdjMaster.VDate),
        ProductionMaster => nameof(ProductionMaster.VDate),
        Payroll => nameof(Payroll.VDate),
        PayrollRun => nameof(PayrollRun.PayDate),
        PayrollDisbursement => nameof(PayrollDisbursement.PaymentDate),
        ImeiCostAddition => nameof(ImeiCostAddition.Date),
        _ => null
    };

    private List<AuditTrail> HandleAuditingBeforeSaveChanges(string userId, string username)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = username;
                    entry.Entity.LastModifiedBy = username;
                    break;

                case EntityState.Modified:
                    if (entry.Properties.Any(p => p.IsModified) && entry.Properties.Any(p => p.OriginalValue?.Equals(p.CurrentValue) == false))
                    {
                        entry.Entity.LastModifiedOn = PakistanTime.Now;
                        entry.Entity.LastModifiedBy = username;
                    }

                    break;

                case EntityState.Deleted:
                    if (entry.Entity is ISoftDelete softDelete)
                    {
                        softDelete.DeletedBy = userId;
                        softDelete.DeletedOn = PakistanTime.Now;
                        entry.State = EntityState.Modified;
                    }

                    break;
            }
        }

        ChangeTracker.DetectChanges();

        var trailEntries = new List<AuditTrail>();
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Deleted or EntityState.Modified)
            .ToList())
        {
            AuditTrail? trailEntry = null;
            if (entry.Entity.GetType().GetCustomAttributes(typeof(IgnoreAuditTrailAttribute), false).Length == 0 &&
                entry.Properties.Any(p => p.OriginalValue?.Equals(p.CurrentValue) == false &&
                                          p.Metadata.PropertyInfo?.GetCustomAttributes(typeof(IgnoreAuditTrailAttribute), false).Length == 0))
            {
                trailEntry = new AuditTrail(entry, _serializer)
                {
                    TableName = entry.Entity.GetType().Name,
                    UserId = userId
                };
                trailEntries.Add(trailEntry);
            }

            foreach (var property in entry.Properties)
            {
                bool isPropertyAuditIgnored = property.Metadata.PropertyInfo?.GetCustomAttributes(typeof(IgnoreAuditTrailAttribute), false).Length > 0;

                if (property.IsTemporary && trailEntry is not null && !isPropertyAuditIgnored)
                {
                    trailEntry.TemporaryProperties.Add(property);
                    continue;
                }

                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey() && trailEntry is not null && !isPropertyAuditIgnored)
                {
                    trailEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        if (trailEntry is not null && !isPropertyAuditIgnored)
                        {
                            trailEntry.TrailType = TrailType.Create;
                            trailEntry.NewValues[propertyName] = property.CurrentValue;
                        }

                        break;

                    case EntityState.Deleted:
                        if (trailEntry is not null && !isPropertyAuditIgnored)
                        {

                            trailEntry.TrailType = TrailType.Delete;
                            trailEntry.OldValues[propertyName] = property.OriginalValue;
                        }

                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            if (entry.Entity is ISoftDelete && property.OriginalValue == null && property.CurrentValue != null)
                            {
                                if (trailEntry is not null && !isPropertyAuditIgnored)
                                {
                                    trailEntry.ChangedColumns.Add(propertyName);
                                    trailEntry.TrailType = TrailType.Delete;
                                    trailEntry.OldValues[propertyName] = property.OriginalValue;
                                    trailEntry.NewValues[propertyName] = property.CurrentValue;
                                }
                            }
                            else if (property.OriginalValue?.Equals(property.CurrentValue) == false)
                            {
                                if (trailEntry is not null && !isPropertyAuditIgnored)
                                {
                                    trailEntry.ChangedColumns.Add(propertyName);
                                    trailEntry.TrailType = TrailType.Update;
                                    trailEntry.OldValues[propertyName] = property.OriginalValue;
                                    trailEntry.NewValues[propertyName] = property.CurrentValue;
                                }
                            }
                            else
                            {
                                property.IsModified = false;
                            }
                        }

                        break;
                }
            }
        }

        foreach (var auditEntry in trailEntries.Where(e => !e.HasTemporaryProperties))
        {
            AuditTrails.Add(auditEntry.ToAuditTrail());
        }

        return trailEntries.Where(e => e.HasTemporaryProperties).ToList();
    }

    private Task HandleAuditingAfterSaveChangesAsync(List<AuditTrail> trailEntries, CancellationToken cancellationToken = new())
    {
        if (trailEntries == null || trailEntries.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (var entry in trailEntries)
        {
            foreach (var prop in entry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    entry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                else
                {
                    entry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            AuditTrails.Add(entry.ToAuditTrail());
        }

        return SaveChangesAsync(cancellationToken);
    }
}