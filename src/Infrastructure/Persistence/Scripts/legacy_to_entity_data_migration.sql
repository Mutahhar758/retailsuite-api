SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @TenantId nvarchar(64) = N'Waqar_MR';
DECLARE @SystemUser nvarchar(200) = N'Admin';

BEGIN TRY
    BEGIN TRANSACTION;
    
    -- Disable all foreign key constraints to allow clearing and re-populating tables
    EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

    -- 1. CompanyDetail
    DELETE FROM dbo.CompanyDetail;
    INSERT INTO dbo.CompanyDetail
    (
        company_name, ur_company_name, descr, address, phone, cell, cell2, contact_header,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on
    )
    SELECT
        src.CompanyName,
        src.UrCompanyName,
        src.Descr,
        src.Address,
        src.Phone,
        src.Cell,
        src.Cell2,
        src.ContactHeader,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME()
    FROM [BG_Choudary_MR].dbo.CompanyDetail src;

    -- 2. ChartOfAccount
    DELETE FROM dbo.ChartOfAccount;
    INSERT INTO dbo.ChartOfAccount
    (
        id, title, parent_id, acc_type, acc_level,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Account,
        src.Title,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(src.parentId)), '') IS NULL THEN NULL
            WHEN LTRIM(RTRIM(src.parentId)) = '0' THEN NULL
            ELSE src.parentId
        END,
        src.AccType,
        src.AccLevel,
        @TenantId,
        isnull(src.CreatedBy,@SystemUser),
        isnull(src.CreatedTime,SYSUTCDATETIME()),
        isnull(src.EditBy,@SystemUser), 
        isnull(src.EditTime,SYSUTCDATETIME()),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.ChartOfAccount src
    WHERE ISNULL(src.status, 0) = 0;

    -- 3. DefaultAccount
    DELETE FROM dbo.DefaultAccount;
    INSERT INTO dbo.DefaultAccount
    (
        title, account_id, map_account_id, a,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on
    )
    SELECT
        src.Title,
        src.Account,
        src.MapAccount,
        src.a,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME()
     FROM [BG_Choudary_MR].dbo.DefaultAccount src where src.Account in (select id from ChartOfAccount);

    -- 4. CustomerDetail
    DELETE FROM dbo.CustomerDetail;
    INSERT INTO dbo.CustomerDetail
    (
        id, email, fax, cnic, address, qualification,
        phone1, phone2, sms_number, iban,
        sms_alert, email_alert, image, active,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Code, src.Email, src.Fax, src.CNIC, src.Address, src.Qualification,
        src.Phone1, src.Phone2, src.SMSNumber, src.IBAN,
        src.SMSAlert, src.EmailAlert, src.image, src.Active,
        @TenantId,
        ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser),
        ISNULL(src.CreatedTime, SYSUTCDATETIME()),
        ISNULL(NULLIF(src.EditBy, ''), ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser)),
        ISNULL(src.EditTime, ISNULL(src.CreatedTime, SYSUTCDATETIME())),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.CustomerDetail src
    WHERE ISNULL(src.status, 0) = 0;

    -- 5. SupplierDetail
    DELETE FROM dbo.SupplierDetail;
    INSERT INTO dbo.SupplierDetail
    (
        id, email, fax, cnic, address, qualification,
        phone1, phone2, sms_number, iban,
        sms_alert, email_alert, image, active, show_in_sales,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Code, src.Email, src.Fax, src.CNIC, src.Address, src.Qualification,
        src.Phone1, src.Phone2, src.SMSNumber, src.IBAN,
        src.SMSAlert, src.EmailAlert, src.image, src.Active, src.ShowInSales,
        @TenantId,
        ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser),
        ISNULL(src.CreatedTime, SYSUTCDATETIME()),
        ISNULL(NULLIF(src.EditBy, ''), ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser)),
        ISNULL(src.EditTime, ISNULL(src.CreatedTime, SYSUTCDATETIME())),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.SupplierDetail src
    WHERE ISNULL(src.status, 0) = 0;

    -- 6. ItemCatagory
    DELETE FROM dbo.ItemCatagory;
    INSERT INTO dbo.ItemCatagory
    (
        id, title, item_type, active,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Code,
        src.Title,
        src.ItemType,
        src.Active,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.ItemCatagory src
    WHERE ISNULL(src.status, 0) = 0;

    -- 7. Narration
    DELETE FROM dbo.Narration;
    INSERT INTO dbo.Narration
    (
        id, title,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Code,
        src.Title,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.Narration src
    WHERE ISNULL(src.status, 0) = 0;

    -- 8. Units
    DELETE FROM dbo.Units;
    INSERT INTO dbo.Units
    (
        id, title,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Code,
        src.Title,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.Units src
    WHERE ISNULL(src.status, 0) = 0;

    -- 9. ItemDetail
    DELETE FROM dbo.ItemDetail;
    INSERT INTO dbo.ItemDetail
    (
        id,  item_type, barcode,
        title, item_key, pri_rate, sec_rate,
        primary_unit_id, secondary_unit_id, default_unit_id,
        qty_in_pack, alert, low_stock_alert, opn_stock, opn_rate,
        item_category_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.ID,
        src.ItemType,
        src.Barcode,
        src.Title,
        src.ItemKey,
        src.PriRate,
        src.SecRate,
        src.PrimaryUnit,
        src.SecondaryUnit,
        src.DefaultUnit,
        src.QtyInPack,
        CONVERT(bit, src.Alert),
        CONVERT(bit, src.LowStockAlert),
        src.OpnStock,
        src.OpnRate,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(src.fkItemCatagory)), '') IS NULL THEN NULL
            WHEN LTRIM(RTRIM(src.fkItemCatagory)) = '0' THEN NULL
            ELSE src.fkItemCatagory
        END,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.ItemDetail src
    WHERE ISNULL(src.status, 0) = 0;

    -- 10. HRInfo
    DELETE FROM dbo.HRInfo;
    INSERT INTO dbo.HRInfo
    (
        id, name, father_name, gender, dob, maritial_status,
        cnic, appointment_date, joining_date, designation,
        salary_type, salary, leave_charges, overtime,
        expense_account, payable_account,
        expense_account_id, payable_account_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.ID,
        src.Name,
        src.FatherName,
        src.Gender,
        src.DOB,
        src.MaritialStatus,
        src.CNIC,
        src.AppointmentDate,
        src.JoiningDate,
        src.Designation,
        src.SalaryType,
        src.Salary,
        src.LeaveCharges,
        src.Overtime,
        src.ExpenseAccount,
        src.PayableAccount,
        src.ExpenseAccount,
        src.PayableAccount,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.HRInfo src
    WHERE ISNULL(src.Status, 0) = 0;

    -- 11. GL1
    DELETE FROM dbo.GL1;
    INSERT INTO dbo.GL1
    (
        v_date, v_time, voucher_no, v_type, v_seq,
        amount,
        narration_id, remarks,
        check_num, check_date, check_status, clear,
        dr_account_id, cr_account_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.VDate,
        src.Vtime,
        src.VoucherNo,
        src.Vtype,
        src.Vseq,
        src.Amount,
        src.Narration,
        src.Remarks,
        src.CheckNum,
        src.CheckDate,
        src.CheckStatus,
        src.Clear,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(src.DRAccount)), '') IS NULL THEN NULL
            WHEN LTRIM(RTRIM(src.DRAccount)) = '0' THEN NULL
            ELSE src.DRAccount
        END,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(src.CRAccount)), '') IS NULL THEN NULL
            WHEN LTRIM(RTRIM(src.CRAccount)) = '0' THEN NULL
            ELSE src.CRAccount
        END,
        @TenantId,
        ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser),
        ISNULL(src.CreatedTime, SYSUTCDATETIME()),
        ISNULL(NULLIF(src.EditBy, ''), ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser)),
        ISNULL(src.EditTime, ISNULL(src.CreatedTime, SYSUTCDATETIME())),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.GL1 src 
    WHERE ISNULL(src.status, 0) = 0 AND Vtype != 'Op';

    -- 12. PurchaseMaster
    DELETE FROM dbo.PurchaseMaster;
    INSERT INTO dbo.PurchaseMaster
    (
        v_date, v_time, v_type, v_no,
        descr, narration_id, amount, counter,
        account_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vdate,
        src.Vtime,
        src.Vtype,
        src.Vno,
        src.Descr,
        src.Narration,
        src.Amount,
        src.Counter,
        src.fkAccountId,
        @TenantId,
        ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser),
        ISNULL(src.CreatedTime, SYSUTCDATETIME()),
        ISNULL(NULLIF(src.EditBy, ''), ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser)),
        ISNULL(src.EditTime, ISNULL(src.CreatedTime, SYSUTCDATETIME())),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.PurchaseMaster src
    WHERE ISNULL(src.status, 0) = 0;

    -- 13. PurchaseRetMaster
    DELETE FROM dbo.PurchaseRetMaster;
    INSERT INTO dbo.PurchaseRetMaster
    (
        v_date, v_time, v_type, v_no,
        descr, narration_id, amount, counter,
        account_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vdate,
        src.Vtime,
        src.Vtype,
        src.Vno,
        src.Descr,
        src.Narration,
        src.Amount,
        src.Counter,
        src.fkAccountId,
        @TenantId,
        ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser),
        ISNULL(src.CreatedTime, SYSUTCDATETIME()),
        ISNULL(NULLIF(src.EditBy, ''), ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser)),
        ISNULL(src.EditTime, ISNULL(src.CreatedTime, SYSUTCDATETIME())),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.PurchaseRetMaster src
    WHERE ISNULL(src.status, 0) = 0;

    -- 14. SaleMaster
    DELETE FROM dbo.SaleMaster;
    INSERT INTO dbo.SaleMaster
    (
        v_date, v_time, v_type, v_no,
        descr, narration_id, amount, discount, net_amount,
        cash_receipt, cash_back, counter,
        account_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vdate,
        src.Vtime,
        src.Vtype,
        src.Vno,
        src.Descr,
        src.Narration,
        src.Amount,
        src.Discount,
        src.NetAmount,
        src.CashReceipt,
        src.CashBack,
        src.Counter,
        src.fkAccountId,
        @TenantId,
        ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser),
        ISNULL(src.CreatedTime, SYSUTCDATETIME()),
        ISNULL(NULLIF(src.EditBy, ''), ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser)),
        ISNULL(src.EditTime, ISNULL(src.CreatedTime, SYSUTCDATETIME())),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.SaleMaster src
    WHERE ISNULL(src.status, 0) = 0;

    -- 15. SaleRetMaster
    DELETE FROM dbo.SaleRetMaster;
    INSERT INTO dbo.SaleRetMaster
    (
        v_date, v_time, v_type, v_no,
        descr, narration_id, amount, discount, net_amount,
        cash_receipt, cash_back, counter,
        account_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vdate,
        src.Vtime,
        src.Vtype,
        src.Vno,
        src.Descr,
        src.Narration,
        src.Amount,
        src.Discount,
        src.NetAmount,
        src.CashReceipt,
        src.CashBack,
        src.Counter,
        src.fkAccountId,
        @TenantId,
        ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser),
        ISNULL(src.CreatedTime, SYSUTCDATETIME()),
        ISNULL(NULLIF(src.EditBy, ''), ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser)),
        ISNULL(src.EditTime, ISNULL(src.CreatedTime, SYSUTCDATETIME())),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.SaleRetMaster src
    WHERE ISNULL(src.status, 0) = 0;

    -- 16. SaleSupplyMaster
    DELETE FROM dbo.SaleSupplyMaster;
    INSERT INTO dbo.SaleSupplyMaster
    (
        v_date, v_time, v_type, v_no,
        descr, narration_id, amount, discount, net_amount, counter,
        item_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vdate,
        src.Vtime,
        src.Vtype,
        src.Vno,
        src.Descr,
        src.Narration,
        src.Amount,
        src.Discount,
        src.NetAmount,
        src.Counter,
        src.fkItemId,
        @TenantId,
        ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser),
        ISNULL(src.CreatedTime, SYSUTCDATETIME()),
        ISNULL(NULLIF(src.EditBy, ''), ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser)),
        ISNULL(src.EditTime, ISNULL(src.CreatedTime, SYSUTCDATETIME())),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.SaleSupplyMaster src
    WHERE ISNULL(src.status, 0) = 0;

    -- 17. StockAdjMaster
    DELETE FROM dbo.StockAdjMaster;
    INSERT INTO dbo.StockAdjMaster
    (
        v_date, v_time, v_type, v_no, descr, narration_id, terminal,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vdate,
        src.Vtime,
        src.Vtype,
        src.Vno,
        src.Descr,
        src.Narration,
        src.Terminal,
        @TenantId,
        ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser),
        ISNULL(src.CreatedTime, SYSUTCDATETIME()),
        ISNULL(NULLIF(src.EditBy, ''), ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser)),
        ISNULL(src.EditTime, ISNULL(src.CreatedTime, SYSUTCDATETIME())),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.StockAdjMaster src
    WHERE ISNULL(src.status, 0) = 0;

    -- 18. SupplyOrderMaster
    DELETE FROM dbo.SupplyOrderMaster;
    SET IDENTITY_INSERT dbo.SupplyOrderMaster ON;

    INSERT INTO dbo.SupplyOrderMaster
    (
        id, title,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Id,
        src.Title,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.SupplyOrderMaster src
    WHERE ISNULL(src.Status, 0) = 0;

    SET IDENTITY_INSERT dbo.SupplyOrderMaster OFF;

    -- -- 19. Payroll
    -- DELETE FROM dbo.Payroll;
    -- INSERT INTO dbo.Payroll
    -- (
    --     voucher_no, v_date, salary_type, description, seq,
    --     salary, no_of_leaves, leave_charges, overtime, overtime_charges, bonus, net_salary,
    --     remarks,
    --     hr_info_id, payable_account_id, expense_account_id,
    --     tenant_id, created_by, created_on, last_modified_by, last_modified_on,
    --     deleted_on, deleted_by
    -- )
    -- SELECT
    --     src.VoucherNo,
    --     src.Vdate,
    --     src.SalaryType,
    --     src.Description,
    --     src.Seq,
    --     src.Salary,
    --     src.NoOfLeaves,
    --     src.LeaveCharges,
    --     src.Overtime,
    --     src.OvertimeCharges,
    --     src.Bonus,
    --     src.NetSalary,
    --     src.Remarks,
    --     CASE
    --         WHEN NULLIF(LTRIM(RTRIM(src.HRID)), '') IS NULL THEN NULL
    --         WHEN LTRIM(RTRIM(src.HRID)) = '0' THEN NULL
    --         ELSE src.HRID
    --     END,
    --     CASE
    --         WHEN NULLIF(LTRIM(RTRIM(src.PayableAccount)), '') IS NULL THEN NULL
    --         WHEN LTRIM(RTRIM(src.PayableAccount)) = '0' THEN NULL
    --         ELSE src.PayableAccount
    --     END,
    --     CASE
    --         WHEN NULLIF(LTRIM(RTRIM(src.ExpenseAccount)), '') IS NULL THEN NULL
    --         WHEN LTRIM(RTRIM(src.ExpenseAccount)) = '0' THEN NULL
    --         ELSE LTRIM(RTRIM(src.ExpenseAccount))
    --     END,
    --     @TenantId,
    --     ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser),
    --     ISNULL(src.CreatedTime, SYSUTCDATETIME()),
    --     ISNULL(NULLIF(src.EditBy, ''), ISNULL(NULLIF(src.CreatedBy, ''), @SystemUser)),
    --     ISNULL(src.EditTime, ISNULL(src.CreatedTime, SYSUTCDATETIME())),
    --     NULL,
    --     NULL
    -- FROM [BG_Choudary_MR].dbo.Payroll src
    -- WHERE ISNULL(src.status, 0) = 0;

    -- 20. PurchaseDetail
    DELETE FROM dbo.PurchaseDetail;
    INSERT INTO dbo.PurchaseDetail
    (
        v_type, v_no, seq,
        unit_id, qty_in_pack, qty, rate, add_less,
        item_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vtype,
        src.Vno,
        src.seq,
        src.Unit,
        src.QtyInPack,
        src.Qty,
        src.Rate,
        src.AddLess,
        src.fkItem,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.PurchaseDetail src
    INNER JOIN [BG_Choudary_MR].dbo.PurchaseMaster m ON src.Vtype = m.Vtype AND src.Vno = m.Vno
    WHERE ISNULL(src.status, 0) = 0 AND ISNULL(m.status, 0) = 0;

    -- 21. PurchaseRetDetail
    DELETE FROM dbo.PurchaseRetDetail;
    INSERT INTO dbo.PurchaseRetDetail
    (
        v_type, v_no, seq,
        unit_id, qty_in_pack, qty, rate,
        item_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vtype,
        src.Vno,
        src.seq,
        src.Unit,
        src.QtyInPack,
        src.Qty,
        src.Rate,
        src.fkItem,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.PurchaseRetDetail src
    INNER JOIN [BG_Choudary_MR].dbo.PurchaseRetMaster m ON src.Vtype = m.Vtype AND src.Vno = m.Vno
    WHERE ISNULL(src.status, 0) = 0 AND ISNULL(m.status, 0) = 0;

    -- 22. Sales
    DELETE FROM dbo.Sales;
    INSERT INTO dbo.Sales
    (
        v_type, v_no, seq,
        unit_id, qty_in_pack, qty, gross_rate, discount,
        item_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vtype,
        src.Vno,
        src.Seq,
        src.Unit,
        src.QtyInPack,
        src.Qty,
        src.GrossRate,
        src.Discount,
        src.fkItem,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.Sales src
    INNER JOIN [BG_Choudary_MR].dbo.SaleMaster m ON src.Vtype = m.Vtype AND src.Vno = m.Vno
    WHERE ISNULL(src.status, 0) = 0 AND ISNULL(m.status, 0) = 0;

    -- 23. SaleRetDetail
    DELETE FROM dbo.SaleRetDetail;
    INSERT INTO dbo.SaleRetDetail
    (
        v_type, v_no, seq,
        unit_id, qty_in_pack, qty, gross_rate, discount,
        item_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vtype,
        src.Vno,
        src.Seq,
        src.Unit,
        src.QtyInPack,
        src.Qty,
        src.GrossRate,
        src.Discount,
        src.fkItem,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.SaleRetDetail src
    INNER JOIN [BG_Choudary_MR].dbo.SaleRetMaster m ON src.Vtype = m.Vtype AND src.Vno = m.Vno
    WHERE ISNULL(src.status, 0) = 0 AND ISNULL(m.status, 0) = 0;

    -- 24. SaleSupplyDetail
    DELETE FROM dbo.SaleSupplyDetail;
    INSERT INTO dbo.SaleSupplyDetail
    (
        v_type, v_no, seq,
        unit_id, qty, gross_rate, discount, add_less,
        customer_account_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vtype,
        src.Vno,
        src.Seq,
        src.Unit,
        src.Qty,
        src.GrossRate,
        src.Discount,
        src.AddLess,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(src.fkCustomerId)), '') IS NULL THEN NULL
            WHEN LTRIM(RTRIM(src.fkCustomerId)) = '0' THEN NULL
            ELSE LTRIM(RTRIM(src.fkCustomerId))
        END,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.SaleSupplyDetail src
    INNER JOIN [BG_Choudary_MR].dbo.SaleSupplyMaster m ON src.Vtype = m.Vtype AND src.Vno = m.Vno
    WHERE ISNULL(src.status, 0) = 0 AND ISNULL(m.status, 0) = 0;

    -- 25. StockAdjDetail
    DELETE FROM dbo.StockAdjDetail;
    INSERT INTO dbo.StockAdjDetail
    (
        v_type, v_no, seq,
        qty_in, qty_out, rate,
        category_id, item_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.Vtype,
        src.Vno,
        src.seq,
        src.QtyIn,
        src.QtyOut,
        src.Rate,
        src.fkCatagory,
        src.fkItem,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.StockAdjDetail src
    INNER JOIN [BG_Choudary_MR].dbo.StockAdjMaster m ON src.Vtype = m.Vtype AND src.Vno = m.Vno
    WHERE ISNULL(src.status, 0) = 0 AND ISNULL(m.status, 0) = 0;

    -- 26. SupplyOrderDetail
    DELETE FROM dbo.SupplyOrderDetail;
    INSERT INTO dbo.SupplyOrderDetail
    (
        sort_order,
        supply_order_master_id, customer_account_id,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on,
        deleted_on, deleted_by
    )
    SELECT
        src.SortOrder,
        src.fkSupplyOrderId,
        src.fkCustomerId,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME(),
        NULL,
        NULL
    FROM [BG_Choudary_MR].dbo.SupplyOrderDetail src
    INNER JOIN [BG_Choudary_MR].dbo.SupplyOrderMaster m ON src.fkSupplyOrderId = m.Id
    WHERE ISNULL(src.Status, 0) = 0 AND ISNULL(m.Status, 0) = 0;

    -- 27. ItemTransaction
    DELETE FROM dbo.ItemTransaction;

    -- A. Opening Stock from ItemDetail (following InventoryService.cs logic: VType='OP', VNo=ItemId, Seq=1)
    INSERT INTO dbo.ItemTransaction
    (
        v_date, v_time, v_type, v_no, seq, tran_type,
        account_id, item_id, unit_id, qty_in, qty_out, rate, amount, counter,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on
    )
    SELECT
        '2024-01-01', -- Default opening date
        '00:00:00',
        'OP',
        src.ID,
        1,
        'in',
        NULL,
        src.ID,
        src.PrimaryUnit,
        ISNULL(src.OpnStock, 0),
        0,
        ISNULL(src.OpnRate, 0),
        ISNULL(src.OpnStock, 0) * ISNULL(src.OpnRate, 0),
        '001',
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME()
    FROM [BG_Choudary_MR].dbo.ItemDetail src
    WHERE ISNULL(src.OpnStock, 0) > 0 AND ISNULL(src.status, 0) = 0;

    -- B. Other Transactions from ItemTransaction view (excluding 'Op')
    INSERT INTO dbo.ItemTransaction
    (
        v_date, v_time, v_type, v_no, seq, tran_type,
        account_id, item_id, unit_id, qty_in, qty_out, rate, amount, counter,
        tenant_id, created_by, created_on, last_modified_by, last_modified_on
    )
    SELECT
        src.VDate,
        src.VTime,
        src.VType,
        src.VNo,
        src.Seq,
        src.TranType,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(src.AccountId)), '') IS NULL THEN NULL
            WHEN LTRIM(RTRIM(src.AccountId)) = '0' THEN NULL
            ELSE src.AccountId
        END,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(src.fkitem)), '') IS NULL THEN NULL
            WHEN LTRIM(RTRIM(src.fkitem)) = '0' THEN NULL
            ELSE src.fkitem
        END,
        src.Unit,
        ISNULL(src.QtyIn, 0),
        ISNULL(src.QtyOut, 0),
        ISNULL(src.Rate, 0),
        ISNULL(src.Amount, 0),
        src.Counter,
        @TenantId,
        @SystemUser,
        SYSUTCDATETIME(),
        @SystemUser,
        SYSUTCDATETIME()
    FROM [BG_Choudary_MR].dbo.ItemTransaction src
    INNER JOIN [BG_Choudary_MR].dbo.ItemDetail i ON src.fkitem = i.ID
    WHERE ISNULL(src.VType, '') != 'Op' AND ISNULL(i.status, 0) = 0;

    -- Re-enable all foreign key constraints
    EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

