# Daily Stock & Cash Closing Feature Implementation Plan

## Goal Description
Implement a **Daily Stock & Cash Closing** screen and workflow across RetailSuite (`retailsuite-web-retail` and `retailsuite-desktop`) for tenants who do not record individual counter sales and purchases throughout the day.

Instead of entering every bill, the tenant records a **Daily Closing Summary** at the end of the day:
1. **Physical Closing Stock Count** per item (plus optional restocked/purchased quantity if an item was both bought and sold on the same day).
2. **Aggregated Financial Totals**:
   - **Total Buying (Purchase) Amount** for the day.
   - **Total Selling (Sale) Amount** for the day.
3. **Credit Customers & Credit Suppliers Breakdown**:
   - For customers who bought on credit, the tenant simply enters **`[Customer Account | Credit Amount | Remarks]`** without needing to itemize which products that credit customer took.
   - The remaining sale amount ($\text{Total Sale Amount} - \sum \text{Credit Customer Amounts}$) is automatically treated as **Cash Sales Received**.
   - Similarly, if any portion of the day's buying was on credit from specific suppliers, the tenant can list **`[Vendor Account | Credit Amount | Remarks]`**, and the remainder is booked as **Cash Paid**.
4. **Automatic Voucher & Ledger Generation (with In-Place Update Support)**:
   - Generates a real `PurchaseMaster` voucher first (so FIFO cost layers in `TransactionFifoMapping` are available immediately).
   - Generates a real `SaleMaster` voucher second, updating `ItemTransaction` balances and posting balanced double-entry `GlEntry` records for Sales, Purchases, Cash, and every Credit Customer/Vendor.
   - Links the generated Purchase (`PU`) and Sale (`SL`) vouchers via a closing reference tag (`[DC:YYYY-MM-DD]`) so re-opening a saved Daily Closing updates the existing vouchers in place without creating duplicates.
