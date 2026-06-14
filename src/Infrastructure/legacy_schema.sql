
/****** Object:  User [mutahhar]    Script Date: 4/9/2026 12:45:52 PM ******/
CREATE USER [mutahhar] WITHOUT LOGIN WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_accessadmin] ADD MEMBER [mutahhar]
GO
ALTER ROLE [db_securityadmin] ADD MEMBER [mutahhar]
GO
ALTER ROLE [db_ddladmin] ADD MEMBER [mutahhar]
GO
ALTER ROLE [db_backupoperator] ADD MEMBER [mutahhar]
GO
ALTER ROLE [db_datareader] ADD MEMBER [mutahhar]
GO
ALTER ROLE [db_datawriter] ADD MEMBER [mutahhar]
GO
ALTER ROLE [db_denydatareader] ADD MEMBER [mutahhar]
GO
ALTER ROLE [db_denydatawriter] ADD MEMBER [mutahhar]
GO
/****** Object:  Schema [Test]    Script Date: 4/9/2026 12:45:52 PM ******/
CREATE SCHEMA [Test]
GO
/****** Object:  UserDefinedTableType [dbo].[Payroll]    Script Date: 4/9/2026 12:45:52 PM ******/
CREATE TYPE [dbo].[Payroll] AS TABLE(
	[Seq] [bigint] NOT NULL,
	[HRID] [varchar](5) NOT NULL,
	[PayableAccount] [varchar](20) NOT NULL,
	[ExpenseAccount] [varchar](20) NOT NULL,
	[Salary] [numeric](18, 0) NOT NULL,
	[NoOfLeaves] [numeric](18, 0) NOT NULL,
	[LeaveCharges] [numeric](18, 0) NOT NULL,
	[Overtime] [numeric](18, 0) NOT NULL,
	[OvertimeCharges] [numeric](18, 0) NOT NULL,
	[Bonus] [numeric](18, 0) NOT NULL,
	[NetSalary] [numeric](18, 0) NOT NULL,
	[Remarks] [varchar](150) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[Payroll1]    Script Date: 4/9/2026 12:45:52 PM ******/
CREATE TYPE [dbo].[Payroll1] AS TABLE(
	[VoucherNo] [varchar](15) NOT NULL,
	[Vdate] [date] NOT NULL,
	[SalaryType] [varchar](50) NOT NULL,
	[Description] [varchar](150) NULL,
	[HRID] [varchar](5) NOT NULL,
	[ExpenseAccount] [varchar](20) NOT NULL,
	[Salary] [numeric](18, 0) NOT NULL,
	[NoOfLeaves] [numeric](18, 0) NOT NULL,
	[LeaveCharges] [numeric](18, 0) NOT NULL,
	[Overtime] [numeric](18, 0) NOT NULL,
	[OvertimeCharges] [numeric](18, 0) NOT NULL,
	[Bonus] [numeric](18, 0) NOT NULL,
	[NetSalary] [numeric](18, 0) NOT NULL,
	[Remarks] [varchar](150) NOT NULL,
	[status] [numeric](1, 0) NOT NULL,
	[CreatedBy] [varchar](50) NOT NULL,
	[CreatedTime] [datetime] NOT NULL,
	[EditBy] [varchar](50) NULL,
	[EditTime] [datetime] NULL
)
GO
/****** Object:  StoredProcedure [dbo].[Change_GL_Status]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[Change_GL_Status]
@Vtype varchar(5),
@VNo varchar(10),
@Vseq varchar(10),
@Status varchar(1)
 as 
begin 
update gl1 set status = @Status where Vtype = @Vtype and VoucherNo  = @VNo and Vseq = (case when  @Vseq is null then Vseq else @Vseq end )  and status = 0; 

if(@Vtype = 'PL')
begin
update Payroll set status = @Status where VoucherNo  = @VNo and Seq  = (case when  @Vseq is null then Seq else @Vseq end )  and status = 0;
end


end;

GO
/****** Object:  StoredProcedure [dbo].[Change_PR_Status]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[Change_PR_Status]
@Vtype varchar(5),
@VNo varchar(10),
@Vseq varchar(10),
@Status varchar(1)
 as 
begin 
BEGIN TRANSACTION tr
BEGIN TRY
update PurchaseRetDetail   set status = @Status 
where Vtype = @Vtype and vno  = @VNo and seq = (case when  @Vseq is null then seq else @Vseq end )  and status = 0; 
UPDATE GL1 SET Amount = isnull((select  sum(qty * rate) from PurchaseRetDetail  where [Vno] = @Vno and Vtype = @Vtype and status = 0),0)        
WHERE VoucherNo = @Vno and Vtype = @Vtype  ; 

update PurchaseRetMaster  set 
Amount = (select sum(qty*Rate) from purchaseRetdetail where [Vno] = @Vno and Vtype = @Vtype and status = 0) 
where [Vno] = @Vno and Vtype = @Vtype ;
if(  @Vseq is null )
begin
update PurchaseRetMaster   set status = @Status where Vtype = @Vtype and vno  = @VNo and status = 0; 
UPDATE GL1 SET Amount = 0 ,status = 1  WHERE VoucherNo = @Vno and Vtype = @Vtype ;
end ;
 COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
END CATCH

end;

GO
/****** Object:  StoredProcedure [dbo].[Change_PU_Status]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[Change_PU_Status]
@Vtype varchar(5),
@VNo varchar(10),
@Vseq varchar(10),
@Status varchar(1)
 as 
begin 
BEGIN TRANSACTION tr
BEGIN TRY
update PurchaseDetail   set status = @Status 
where Vtype = @Vtype and vno  = @VNo and seq = (case when  @Vseq is null then seq else @Vseq end )  and status = 0; 
UPDATE GL1 SET Amount = isnull((select  sum(qty * rate) from purchasedetail  where [Vno] = @Vno and Vtype = @Vtype and status = 0),0)        
WHERE VoucherNo = @Vno and Vtype = @Vtype  ;
 
update PurchaseMaster  set 
Amount = (select sum((qty*Rate)+AddLess) from PurchaseDetail where [Vno] = @Vno and Vtype = @Vtype and status = 0) where [Vno] = @Vno and Vtype = @Vtype ;
	
		
if(  @Vseq is null )
begin
update PurchaseMaster   set status = @Status where Vtype = @Vtype and vno  = @VNo and status = 0; 
UPDATE GL1 SET Amount = 0 ,status = 1  WHERE VoucherNo = @Vno and Vtype = @Vtype ;
end ;
 COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
END CATCH

end;

GO
/****** Object:  StoredProcedure [dbo].[Change_SL_Status]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[Change_SL_Status]
@Vtype varchar(5),
@VNo varchar(10),
@Vseq varchar(10),
@Status varchar(1)
 as 
begin 
	BEGIN TRANSACTION tr
	BEGIN TRY
	    update Sales  set status = @Status where Vtype = @Vtype and vno  = @VNo and seq = (case when  @Vseq is null then seq else @Vseq end )  and status = 0; 
		UPDATE GL1 SET Amount = isnull((select  sum(qty * rate) from Sales  where [Vno] = @Vno and Vtype = @Vtype and status = 0),0)        
        WHERE VoucherNo = @Vno and Vtype = @Vtype and Vseq = 1  and status = 0; 
		
		update salemaster set amount = (select sum(qty*grossRate) from Sales   where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) ,
		Discount = (select sum(qty*discount) from Sales   where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) ,
		Netamount = (select sum(qty*Rate) from Sales   where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) 	
		where [Vno] = @Vno and Vtype = @Vtype ;

		if(  @Vseq is null )
		begin
		update SaleMaster  set status = @Status where Vtype = @Vtype and vno  = @VNo and status = 0; 
		UPDATE GL1 SET Amount = 0 ,status = @Status  WHERE VoucherNo = @Vno and Vtype = @Vtype  and status = 0;
		end ;
		COMMIT TRANSACTION tr;    
	END TRY
	BEGIN CATCH 
			 ROLLBACK TRAN tr; 
			 insert into errorlog values ('delete error on Change_SL_Status   vno = ' + @VNo + ', vtype =  ' + @Vtype + ', vseq = ' + @Vseq ,@Vtype);
	END CATCH
end;

GO
/****** Object:  StoredProcedure [dbo].[Change_SP_Status]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

Create procedure [dbo].[Change_SP_Status]
@Vtype varchar(5),
@VNo varchar(10),
@Vseq varchar(10),
@Status varchar(1)
 as 
begin 
	BEGIN TRANSACTION tr
	BEGIN TRY
	    update SaleSupplyDetail  set status = @Status where Vtype = @Vtype and vno  = @VNo and seq = (case when  @Vseq is null then seq else @Vseq end )  and status = 0; 
		UPDATE GL1 SET Amount = isnull((select  sum(qty * rate) from SaleSupplyDetail  where [Vno] = @Vno and Vtype = @Vtype and status = 0),0)        
        WHERE VoucherNo = @Vno and Vtype = @Vtype and Vseq = @Vseq  and status = 0; 
		
		update SaleSupplyMaster set amount = (select sum(qty*grossRate) from SaleSupplyDetail   where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) ,
		Discount = (select sum(qty*discount) from SaleSupplyDetail   where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) ,
		Netamount = (select sum(qty*Rate) from SaleSupplyDetail   where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) 	
		where [Vno] = @Vno and Vtype = @Vtype ;

		if(  @Vseq is null )
		begin
		update SaleSupplyMaster  set status = @Status where Vtype = @Vtype and vno  = @VNo and status = 0; 
		UPDATE GL1 SET Amount = 0 ,status = @Status  WHERE VoucherNo = @Vno and Vtype = @Vtype  and status = 0;
		end ;
		COMMIT TRANSACTION tr;    
	END TRY
	BEGIN CATCH 
			 ROLLBACK TRAN tr; 
			 insert into errorlog values ('delete error on Change_SL_Status   vno = ' + @VNo + ', vtype =  ' + @Vtype + ', vseq = ' + @Vseq ,@Vtype);
	END CATCH
end;


GO
/****** Object:  StoredProcedure [dbo].[Change_SR_Status]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[Change_SR_Status]
@Vtype varchar(5),
@VNo varchar(10),
@Vseq varchar(10),
@Status varchar(1)
 as 
begin 
	BEGIN TRANSACTION tr
	BEGIN TRY
	    update Saleretdetail  set status = @Status where Vtype = @Vtype and vno  = @VNo and seq = (case when  @Vseq is null then seq else @Vseq end )  and status = 0; 
		UPDATE GL1 SET Amount = isnull((select  sum(qty * rate) from Saleretdetail  where [Vno] = @Vno and Vtype = @Vtype and status = 0),0)        
        WHERE VoucherNo = @Vno and Vtype = @Vtype and Vseq = 1  and status = 0; 
		
		update SaleRetMaster  set amount = (select sum(qty*grossRate) from SaleRetDetail    where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) ,
		Discount = (select sum(qty*discount) from SaleRetDetail   where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) ,
		Netamount = (select sum(qty*Rate) from SaleRetDetail   where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) 	
		where [Vno] = @Vno and Vtype = @Vtype ;
		
		if(  @Vseq is null )
		begin
		update SaleretMaster  set status = @Status where Vtype = @Vtype and vno  = @VNo and status = 0; 
		UPDATE GL1 SET Amount = 0 ,status = @Status  WHERE VoucherNo = @Vno and Vtype = @Vtype  and status = 0;
		end ;
		COMMIT TRANSACTION tr;    
	END TRY
	BEGIN CATCH 
			 ROLLBACK TRAN tr; 
			 insert into errorlog values ('delete error on [Change_SR_Status]   vno = ' + @VNo + ', vtype =  ' + @Vtype + ', vseq = ' + @Vseq ,@Vtype);
	END CATCH
end;

GO
/****** Object:  StoredProcedure [dbo].[ChartOfAccAdd_Edit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ChartOfAccAdd_Edit]
	@account varchar(20),
	@title nvarchar(200),
	@parentid varchar(20),
	@AccType varchar(20),
	@AccLevel int,
	@user varchar(20)  
AS
BEGIN
	declare @Count int,@IVaccount varchar(20);
	select @Count = COUNT(*) from ChartOfAccount where Account = @account and status = 0
	if(@Count = 0)
	begin
	select @IVaccount = @parentid + dbo.LeadZero((select isnull(max(cast(SUBSTRING(Account,len(parentid)+1,100 ) as bigint)),0) + 1 from [ChartOfAccount] where parentid = @parentid),3);
	INSERT INTO [ChartOfAccount]([Account],[Title],[parentId] ,[AccType],[AccLevel] ,[CreatedBy],CreatedTime,status)
     VALUES(@IVaccount,@title,@parentid,@AccType,@AccLevel,@user,getdate(),0) 
    end;
    else
    begin
    UPDATE [ChartOfAccount]
   SET [Title] = @title
      ,[AccType] = @AccType
      ,[EditBy] = @user,
      EditTime = GETDATE() 
 WHERE [Account] = @account and status = 0
    end;
END

GO
/****** Object:  StoredProcedure [dbo].[ChartOFAccount_delete]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ChartOFAccount_delete]
	@account varchar(20),	
	@user varchar(20)  
AS
BEGIN
	
    UPDATE [ChartOfAccount]
   SET [EditBy] = @user,
      EditTime = GETDATE() ,
	  status = 1
 WHERE [Account] = @account
   
END

GO
/****** Object:  StoredProcedure [dbo].[CustomerInfo_Add_Edit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CustomerInfo_Add_Edit]
	@Code varchar(20),
	@Email varchar(50) ,
	@Fax varchar(50) ,
	@CNIC varchar(50) ,
	@Address varchar(150) ,
	@Qualification varchar(100) ,
	@Phone1 varchar(25) ,
	@Phone2 varchar(25) ,
	@SMSNumber varchar(50) ,
	@IBAN varchar(80) ,
	@SMSAlert numeric(1, 0) ,
	@EmailAlert numeric(1, 0) ,
	@image varbinary(max) ,
	@Active numeric(1, 0) ,
	@User varchar(200) ,
	@status numeric(1, 0),
	 @RetVal  varchar(20)   out
AS
BEGIN
	declare @Count int;
	select @Count = COUNT(*) from CustomerDetail  where Code  = @Code 
	if(@Count = 0)
	begin
	--set @Code = dbo.digitformat((select isnull(max(code),0) + 1 from CustomerDetail ),3)
	INSERT INTO [dbo].[CustomerDetail]
           ([Code]
           ,[Email]
           ,[Fax]
           ,[CNIC]
           ,[Address]
           ,[Qualification]
           ,[Phone1]
           ,[Phone2]
           ,[SMSNumber]
           ,[IBAN]
           ,[SMSAlert]
           ,[EmailAlert]
           ,[image]
           ,[Active]
           ,[CreatedBy]
		   ,CreatedTime 
           ,[status])
     VALUES
           (@Code
           ,@Email
           ,@Fax
           ,@CNIC
           ,@Address
           ,@Qualification
           ,@Phone1
           ,@Phone2
           ,@SMSNumber
           ,@IBAN
           ,@SMSAlert
           ,@EmailAlert
           ,@image
           ,@Active
           ,@User
		   ,sysdatetime()  
           ,@status)
    end;
    else
    begin
   UPDATE [CustomerDetail]
   SET [Email] = @Email
      ,[Fax] = @Fax
      ,[CNIC] = @CNIC
      ,[Address] = @Address
      ,[Qualification] = @Qualification
      ,[Phone1] = @Phone1
      ,[Phone2] = @Phone2
      ,[SMSNumber] = @SMSNumber
      ,[IBAN] = @IBAN
      ,[SMSAlert] = @SMSAlert
      ,[EmailAlert] = @EmailAlert
      ,[image] = @image
      ,[Active] = @Active
      ,[EditBy] = @User 
      ,[EditTime] = sysdatetime()
      ,[status] = @status
 WHERE [Code] = @code
    end;
	set @retval = @code
END

GO
/****** Object:  StoredProcedure [dbo].[CustomerSupplyDetail_Add_Edit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create PROCEDURE [dbo].[CustomerSupplyDetail_Add_Edit]
	@Account varchar(20),
	@ItemCode varchar(5),	
	@Rate numeric(18, 2) ,
	@Qty numeric (18,2),
	@Active numeric(1, 0),
	@status numeric(1, 0)
AS
BEGIN
	declare @Count int;
	select @Count = COUNT(*) from CustomersupplyDetail  where Item  = @ItemCode and Account = @account 
	if(@Count = 0)
	begin	
	INSERT INTO CustomersupplyDetail (account,item,rate,qty,active)
     VALUES (@account,@ItemCode,@rate,@qty,@active)
    end;
    else
    begin
   UPDATE CustomersupplyDetail
   SET rate = @rate
      ,qty = @qty
      ,active = @active      
  where Item  = @ItemCode and Account = @account 
    end;
	
END

GO
/****** Object:  StoredProcedure [dbo].[GLAdd_Edit1]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GLAdd_Edit1]
@VDate date,
@VoucherNo varchar(8),
@Vtype varchar(15),
@Vseq int,
@DrAccount varchar(20),
@Amount decimal(18, 2),
@CrAccount varchar(20),
@Narration varchar(50),
@Remarks varchar(100),
@CheckNum varchar(50),
@CheckDate date,
@CheckStatus varchar(50),
@Clear numeric(1,0),
@status numeric(1,0),
@User varchar(50)
, @RetVoucherNo   varchar(8) output 

AS
BEGIN
--begin tran
	declare @Count int--,@RetVoucherNo  varchar(8);
	select @Count = COUNT(*) from GL1 where VoucherNo = @VoucherNo and Vtype = @Vtype and Vseq = @Vseq
	if(@VoucherNo is null)
	begin
	select @VoucherNo =  dbo.digitformat (isnull(max(VoucherNo) + 1,1),5)  from GL1 where Vtype = @Vtype 
	end ;
	if(@Count = 0)
	begin
	INSERT INTO GL1
           (VDate,Vtime,VoucherNo,Vtype,Vseq,DrAccount,Amount,CrAccount,Narration,Remarks,CheckNum,CheckDate,CheckStatus,Clear,
		   status,CreatedBy,CreatedTime)
		   values(@VDate,GETDATE(),@VoucherNo,@Vtype,@Vseq,@DrAccount ,@Amount,@CrAccount,@Narration,@Remarks,@CheckNum,@CheckDate,@CheckStatus,@Clear,
		   @status,@User,GETDATE())   
    end;
    else
    begin
    UPDATE GL1
   SET VDate = @VDate,
   Vtime =  GETDATE()
      ,DrAccount = @DrAccount 
      ,Amount = @Amount 
      ,CrAccount = @CrAccount 
      ,Narration = @Narration 
      ,Remarks = @Remarks 
      ,CheckNum = @CheckNum 
      ,CheckDate = @CheckDate 
     -- ,CheckStatus = @CheckStatus 
      ,Clear =case when @CheckNum is null then  @Clear  else clear end 
      ,status = @status       
      ,EditBy = @User 
      ,EditTime = GETDATE() 
 WHERE VoucherNo = @VoucherNo and Vtype = @Vtype and Vseq = @Vseq
    end;
	--commit
 set	@RetVoucherNo = @VoucherNo
	--return '_' + @VoucherNo
END

GO
/****** Object:  StoredProcedure [dbo].[HRInfo_Add_Edit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[HRInfo_Add_Edit]
	@ID   varchar (3) ,
	 @Name   varchar (150) ,
	 @FatherName   varchar (150) ,
	 @Gender   varchar (50) ,
	 @DOB   date  ,
	 @MaritialStatus   varchar (50) ,
	 @CNIC   varchar (50) ,
	 @AppointmentDate   date  ,
	 @JoiningDate   date  ,
	 @Designation   varchar (50) ,
	 @SalaryType   varchar (50) ,
	 @Salary   numeric (18, 0) ,
	 @LeaveCharges   numeric (18, 0) ,
	 @Overtime   numeric (18, 0) ,
	 @ExpenseAccount varchar (50) ,
	 @PayableAccount varchar (50) ,
	 @Status   numeric (1, 0) 
AS
BEGIN
	declare @Count int,@IVId varchar(6);
	select @Count = COUNT(*) from HRInfo  where ID = @Id
	if(@Count = 0)
	begin
	select  @IVId = dbo.LeadZero((select isnull(MAX (ID ),0) + 1 from HRInfo),3);
	INSERT INTO HRInfo( ID , Name , FatherName , Gender , DOB , MaritialStatus , CNIC , AppointmentDate , JoiningDate , Designation , SalaryType , Salary , LeaveCharges , Overtime ,expenseAccount ,payableaccount, Status )
     VALUES(@IVId , @Name , @FatherName , @Gender , @DOB , @MaritialStatus , @CNIC , @AppointmentDate , @JoiningDate , @Designation , @SalaryType , @Salary , @LeaveCharges , @Overtime ,@ExpenseAccount,@PayableAccount , @Status) 
    end;
    else
    begin
    UPDATE HRInfo
   SET Name	= @Name 
  , FatherName = @FatherName 
  , Gender = @Gender 
  , DOB = @DOB 
  , MaritialStatus = @MaritialStatus 
  , CNIC = @CNIC 
  , AppointmentDate = @AppointmentDate
  , JoiningDate = @JoiningDate 
  , Designation = @Designation 
  , SalaryType = @SalaryType 
  , Salary = @Salary 
  , LeaveCharges = @LeaveCharges 
  , Overtime = @Overtime 
  , ExpenseAccount = @ExpenseAccount
  ,payableaccount = @PayableAccount 
  , Status = @Status 
     where ID = @ID 
    end;
END

GO
/****** Object:  StoredProcedure [dbo].[ItemAdd_Edit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ItemAdd_Edit]
	@fkItemCatagory varchar(3),
	@Id varchar(10),
	@Code varchar(5),
	@BarCode varchar(150),
	@title varchar(150),
	@ItemKey varchar(50),	
	@Prirate decimal(18,2), 
	@Secrate decimal(18,2), 
	@PrimaryUnit varchar(10),
    @SecondaryUnit varchar(10),
	@DefaultUnit varchar(10),
    @QtyInPack decimal(18,2),
    @Alert numeric ,
	@LowStockAlert decimal(18,2),
	@OpnStock decimal(18,2),
	@OpnRate decimal(18,2),	
	@status numeric 
	
AS
BEGIN
	declare @Count int,@IVId varchar(6);	
	select @Count = COUNT(*) from ItemDetail  where ID = @Id --fkItemCatagory = @fkItemCatagory and Code  = @Code 
	if(@Count = 0)
	begin
	select  @IVId = dbo.LeadZero((select isnull(MAX (cast(ID as bigint)),0) + 1 from ItemDetail),6);
	INSERT INTO ItemDetail(ID ,fkItemCatagory,Code ,Barcode ,Title,Itemkey ,PriRate,secrate,PrimaryUnit, SecondaryUnit,DefaultUnit ,QtyInPack,Alert ,LowStockAlert,opnstock,opnrate,status)
     VALUES(@IVId ,@fkItemCatagory,@Code , isnull(@BarCode,cast(cast(@fkitemcatagory as int) as varchar) + @IVId  + CHAR(13)+CHAR(10)) ,@title,@Itemkey ,@prirate,@Secrate,@PrimaryUnit, @SecondaryUnit,@DefaultUnit ,@QtyInPack ,@Alert ,@LowStockAlert ,@opnstock,@opnrate,@status ) 
    end;
    else
    begin
    UPDATE ItemDetail
   SET --Barcode = @BarCode ,
   Code  = case when fkItemCatagory = @fkItemCatagory then  @Code else (select isnull(max(cast(Code as int)),0) + 1 from ItemDetail where fkItemCatagory = @fkItemCatagory ) end ,
   fkItemCatagory = @fkItemCatagory,
   [Title] = @title,
   ItemKey =@ItemKey ,

   prirate = @Prirate,
   SecRate = @Secrate,
PrimaryUnit =@PrimaryUnit,
SecondaryUnit = @SecondaryUnit,
DefaultUnit = @DefaultUnit, 
QtyInPack = @QtyInPack,
Alert = @Alert, 
LowStockAlert = @LowStockAlert,
OpnStock = @OpnStock,
OpnRate = @OpnRate,
   status =@status
     where ID = @Id--fkItemCatagory = @fkItemCatagory and Code  = @Code 
    end;
END

GO
/****** Object:  StoredProcedure [dbo].[ItemCatagoryAdd_Edit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ItemCatagoryAdd_Edit]
	@Code varchar(3),
	@title varchar(150),
	@Active varchar(1),
	@status numeric 
	
AS
BEGIN
	declare @Count int;
	select @Count = COUNT(*) from ItemCatagory  where Code  = @Code 
	if(@Count = 0)
	begin
	INSERT INTO ItemCatagory(Code ,Title ,Active,status)
     VALUES(@Code ,@title,@Active  ,@status ) 
    end;
    else
    begin
    UPDATE ItemCatagory
   SET [Title] = @title,
   Active = @Active,
   status =@status      
 WHERE Code  = @Code 
    end;
END

GO
/****** Object:  StoredProcedure [dbo].[NarrationAdd_Edit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[NarrationAdd_Edit]
	@Code varchar(3),
	@title nvarchar(150),
	@status numeric 
	
AS
BEGIN
	declare @Count int;
	select @Count = COUNT(*) from Narration  where Code  = @Code
	if(@Count = 0)
	begin
	INSERT INTO Narration(Code ,Title ,status)
     VALUES(@Code ,@title ,@status ) 
    end;
    else
    begin
    UPDATE Narration
   SET [Title] = @title,
   status =@status      
 WHERE Code  = @Code 
    end;
END

GO
/****** Object:  StoredProcedure [dbo].[OpenningBal_Add_Edit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[OpenningBal_Add_Edit]
--@DrAccount varchar(20),
@Account varchar(20),
@DrAmount decimal(18, 2),
@CrAmount decimal(18, 2),
@User varchar(50)
AS
BEGIN
	declare @Count int,@Amount decimal(18, 2) = isnull(@dramount,0) - isnull(@cramount,0);
	select @Count = COUNT(*) from GL1 where (DRAccount  = @Account  or  craccount = @Account)   and Vtype = 'Op';
	
	if(@Count = 0)
	begin
	INSERT INTO GL1
           (VDate,Vtime,VoucherNo,Vtype,Vseq,DrAccount,Amount,CrAccount,Narration,Remarks,Clear,
		   status,CreatedBy,CreatedTime)
		   values('01/01/2018',GETDATE(),'00001','Op',1,
		   case when  @Amount >= 0 then  @Account else '0' end,
		   case when  isnull(@DrAmount,0) != 0  then  @DrAmount  else isnull(@CrAmount,0) end,
		   case when  @Amount < 0 then  @Account else '0' end,
		   null,null,0,
		   0,@User,GETDATE())   
    end;
    else
    begin
    UPDATE GL1
   SET DrAccount = case when  @Amount >= 0 then  @Account else '0' end 
      ,Amount = case when  isnull(@DrAmount,0) != 0  then  @DrAmount  else isnull(@CrAmount,0) end
      ,CrAccount = case when   @Amount < 0 then  @Account else '0' end            
      ,EditBy = @User 
      ,EditTime = GETDATE() 
 WHERE (DRAccount  = @Account  or  craccount = @Account)   and Vtype = 'Op';
    end;
	
END

GO
/****** Object:  StoredProcedure [dbo].[Payroll_Add_Edit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Payroll_Add_Edit]
	@VoucherNo varchar(15) ,
	@Vdate date ,
	@SalaryType varchar(50) ,
	@Description varchar(150) ,
	@status varchar(1),
	@user  varchar(100),
	@payroll dbo.Payroll readonly,
	@RetVoucherNo varchar(15) output 
	
AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
select * from HRInfo 
	declare @Count int,@IVId varchar(6),@errid int;
	set @errid = 1;
	select @Count = COUNT(*) from payroll   where voucherno = @VoucherNo
	set @IVId = @VoucherNo 
	if(@Count = 0)
	begin
	select  @IVId = dbo.LeadZero((select isnull(MAX (voucherno ),0) + 1 from payroll),5);
set @errid = 2;
	end;
		set @errid = 3;
	INSERT INTO payroll( VoucherNo , Vdate , SalaryType , Description , seq,HRID ,payableAccount, ExpenseAccount , Salary , NoOfLeaves , LeaveCharges , Overtime , OvertimeCharges , Bonus , NetSalary , Remarks , status , CreatedBy , CreatedTime )
	(select @IVId ,@Vdate ,@SalaryType ,@Description ,seq,HRID ,payableAccount, ExpenseAccount , Salary , NoOfLeaves , LeaveCharges , Overtime , 
	OvertimeCharges , Bonus , NetSalary , Remarks ,@status ,@user ,GETDATE () from @payroll where seq not IN (select seq from Payroll where VoucherNo = @IVId )) 
	---------------------------------------------------
	set @errid = 4;
	INSERT INTO GL1
           (VDate,Vtime,VoucherNo,Vtype,Vseq,DrAccount,Amount,CrAccount,Narration,Remarks,CheckStatus,Clear,
		   status,CreatedBy,CreatedTime)
		   (select  @VDate,GETDATE(),@IVId,'PL',seq,expenseaccount,netsalary,payableAccount,' ',remarks,0,1,
		   @status,@User,GETDATE() from @payroll where seq not IN (select seq from gl1 where VoucherNo = @IVId and Vtype = 'PL'))   
   set @errid = 5;
    if(@VoucherNo is not  null and len(@VoucherNo) >= 3 )
    begin
set @VoucherNo = @IVId 
set @errid = 6;
    update p
    set Vdate = @Vdate ,
    PayableAccount = pr.PayableAccount ,
    ExpenseAccount  = pr.ExpenseAccount ,
    HRID = pr.hrid,
   Salary = pr.salary,
   NoOfLeaves = pr.NoOfLeaves ,
   LeaveCharges = pr.LeaveCharges ,
   Overtime = pr.Overtime,
    OvertimeCharges = pr.OvertimeCharges,
    Bonus = pr.bonus,
netsalary = pr.netsalary,
    Description = @Description,
    EditBy = @user ,
    EditTime = GETDATE () ,
    status = 0
    from Payroll p 
    left join @payroll pr on p.VoucherNo = @IVId and p.Seq = pr.Seq 
    where VoucherNo = @IVId 
    set @errid = 7;

    update gl 
    set Vdate = @Vdate ,
    CRAccount  = pr.PayableAccount ,
    DRAccount  = pr.ExpenseAccount ,
    Amount = pr.netsalary,
    Remarks  = @Description,
    EditBy = @user ,
    EditTime = GETDATE () ,
    status = 0
    from GL1  gl 
    left join @payroll pr on gl.VoucherNo  = @IVId and gl.Vtype = 'PL' and gl.Vseq  = pr.Seq 
    where VoucherNo = @IVId and Vtype = 'PL'
    end; 
set @VoucherNo = @IVId 
    set	@RetVoucherNo = @VoucherNo
	COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
insert into errorlog (Error ,sender )values (@errid,'PL');
END CATCH

END

GO
/****** Object:  StoredProcedure [dbo].[puchaseMaster_AddEdit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[puchaseMaster_AddEdit]
@VDate date,
@Vno varchar(8),
@Vtype varchar(15),
@fkAccount varchar(20),
@Descr varchar(300),
@Narration 	varchar(5),	   
@Counter varchar(50),
@status numeric(1,0),
@User varchar(50),
@RetInvoiceNo  varchar(8) output

AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
	declare @Count int;--,@RetInvoiceNo  varchar(8);
	select @Count = COUNT(*) from PurchaseMaster  where [Vno] = @Vno and Vtype = @Vtype and status = 0
	if(@Vno is null)
	begin
	select @Vno =  dbo.digitformat(isnull(max(Vno) + 1,1),5)  from PurchaseMaster where Vtype = @Vtype 
	end ;
	if(@Count = 0)
	begin
	INSERT INTO [dbo].PurchaseMaster
           ([Vdate]
		   ,Vtime
           ,[Vtype]
           ,[Vno]
		   ,fkAccountId 	
		   ,Descr
		   ,Narration 			        	   
           ,[CreatedBy]
           ,[CreatedTime]          
           ,[status]
           ,[Counter])
     VALUES
		   (@VDate,GETDATE(),@Vtype,@Vno,@fkaccount  ,@Descr ,@Narration ,
		   @User,GETDATE(),@status,@Counter )   
    end;
    else
    begin
   UPDATE [dbo].PurchaseMaster
   SET [Vdate] = @VDate 
   ,fkAccountId  = @fkAccount 
   ,Descr=@Descr 
   ,Narration=@Narration
      ,[EditBy] = @User
      ,[EditTime] = GETDATE()
 where [Vno] = @Vno and Vtype = @Vtype  and status = 0
    end;
	--commit
 set	@RetInvoiceNo = @Vno
COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
END CATCH
END

GO
/****** Object:  StoredProcedure [dbo].[puchaseRetMaster_AddEdit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create PROCEDURE [dbo].[puchaseRetMaster_AddEdit]
@VDate date,
@Vno varchar(8),
@Vtype varchar(15),
@fkAccount varchar(20),
@Descr varchar(300),
@Narration 	varchar(5),	   
@Counter varchar(50),
@status numeric(1,0),
@User varchar(50),
@RetInvoiceNo  varchar(8) output

AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
	declare @Count int;--,@RetInvoiceNo  varchar(8);
	select @Count = COUNT(*) from PurchaseRetMaster  where [Vno] = @Vno and Vtype = @Vtype and status = 0
	if(@Vno is null)
	begin
	select @Vno =  dbo.digitformat(isnull(max(Vno) + 1,1),5)  from PurchaseRetMaster where Vtype = @Vtype 
	end ;
	if(@Count = 0)
	begin
	INSERT INTO [dbo].PurchaseRetMaster
           ([Vdate]
		   ,Vtime
           ,[Vtype]
           ,[Vno]
		   ,fkAccountId 	
		   ,Descr
		   ,Narration 			        	   
           ,[CreatedBy]
           ,[CreatedTime]          
           ,[status]
           ,[Counter])
     VALUES
		   (@VDate,GETDATE(),@Vtype,@Vno,@fkaccount  ,@Descr ,@Narration ,
		   @User,GETDATE(),@status,@Counter )   
    end;
    else
    begin
   UPDATE [dbo].PurchaseRetMaster
   SET [Vdate] = @VDate 
   ,fkAccountId  = @fkAccount 
   ,Descr=@Descr 
   ,Narration=@Narration
      ,[EditBy] = @User
      ,[EditTime] = GETDATE()
 where [Vno] = @Vno and Vtype = @Vtype  and status = 0
    end;
	--commit
 set	@RetInvoiceNo = @Vno
COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
END CATCH
END

GO
/****** Object:  StoredProcedure [dbo].[PurchaseDetail_AddEdit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE  PROCEDURE [dbo].[PurchaseDetail_AddEdit]
@Vno varchar(8),
@Vtype varchar(15),
@seq numeric(3,0),
@fkItem varchar(10),
@Unit varchar(3),
@Qty numeric(12,2),
@Rate numeric(18,2),
@AddLess numeric(18,2),
@status numeric(1,0)
AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
	declare @Count int;
	select @Count = COUNT(*) from purchasedetail  where [Vno] = @Vno and Vtype = @Vtype and [Seq] = @seq
	if(@Count = 0)
	begin
	INSERT INTO [dbo].purchasedetail
           ([Vtype]
           ,[Vno]
           ,[Seq]
           ,[fkItem]
		   ,unit
		   ,QtyInPack 
          ,[Qty]
           ,[Rate]
        ,AddLess 
           ,[status])
     VALUES
		   (@Vtype,@Vno,@seq,@fkItem ,@Unit ,(select qtyinpack from ItemDetail  where id =  @fkItem and status = 0),
		   @Qty,
		   @Rate ,@AddLess ,@status)   
    end;
    else
    begin
   UPDATE [dbo].purchasedetail
   SET [fkItem] = @fkItem
   ,Unit = @unit
      ,[Qty] = @Qty
	  ,rate = @Rate
	  ,AddLess = @AddLess 
	  ,status = @status 
 where [Vno] = @Vno and Vtype = @Vtype and seq = @seq
    end;	
    update PurchaseMaster  set Amount = (select sum((qty*Rate)+AddLess) from PurchaseDetail where [Vno] = @Vno and Vtype = @Vtype and status = 0) where [Vno] = @Vno and Vtype = @Vtype ;
	
	-----------------------UPdating General Ledger--------------------------------------------
	declare @Vdate date,@User varchar(50),@Amount numeric(18,2),@narration varchar(5),@Remarks varchar(300),@Account varchar(20);
	select @amount = sum((qty *Rate)+AddLess) from purchasedetail  where [Vno] = @Vno and Vtype = @Vtype  and status = 0;
	select @Vdate= vdate,@user = CreatedBy,@narration = Narration ,@remarks  = descr ,@Account = fkAccountId  from purchasemaster 
	where VNo = @Vno and Vtype = @Vtype  and status = 0;
	----------------xxxxxxxxxx-----------------xxxxxxxxxxxxx----------------------------------------------
	select @Count = COUNT(*) from GL1 where VoucherNo = @Vno and Vtype = @Vtype and Vseq = 1
	if(@Count = 0)
	begin
	INSERT INTO GL1
           (VDate,Vtime,VoucherNo,Vtype,Vseq,DrAccount,Amount,CrAccount,Narration,Remarks,clear,
		   status,CreatedBy,CreatedTime)
		   values(@VDate,GETDATE(),@Vno,@Vtype,1,dbo.Get_Def_Acc('PU') ,@amount,@Account,@narration,@remarks,0,
		   0,@User,GETDATE())   		 
    end;
    else
    begin
    UPDATE GL1
   SET VDate = @VDate,
   Vtime =  GETDATE()
      ,DrAccount = dbo.Get_Def_Acc('PU')  
      ,Amount = @amount 
      ,CrAccount = @Account
      ,Narration = @narration 
      ,Remarks = @remarks 
      ,status = 0       
      ,EditBy = @User 
      ,EditTime = GETDATE() 
 WHERE VoucherNo = @Vno and Vtype = @Vtype 
    end;
 COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
END CATCH
END

GO
/****** Object:  StoredProcedure [dbo].[PurchaseRetDetail_AddEdit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PurchaseRetDetail_AddEdit]
@Vno varchar(8),
@Vtype varchar(15),
@seq numeric(3,0),
@fkItem varchar(10),
@Unit varchar(3),
@Qty numeric(12,2),
@Rate numeric(18,2),
@status numeric(1,0)
AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
	declare @Count int;
	select @Count = COUNT(*) from purchaseRetdetail  where [Vno] = @Vno and Vtype = @Vtype and [Seq] = @seq
	if(@Count = 0)
	begin
	INSERT INTO [dbo].purchaseRetdetail
           ([Vtype]
           ,[Vno]
           ,[Seq]
           ,[fkItem]
		   ,unit
		   ,QtyInPack 
          ,[Qty]
           ,[Rate]
           ,[status])
     VALUES
		   (@Vtype,@Vno,@seq,@fkItem ,@Unit ,(select qtyinpack from ItemDetail  where id =  @fkItem and status = 0),
		   @Qty,
		   @Rate ,@status)   
    end;
    else
    begin
   UPDATE [dbo].purchaseRetdetail
   SET [fkItem] = @fkItem
   ,Unit = @unit
      ,[Qty] = @Qty
	  ,rate = @Rate
	  ,status = @status 
 where [Vno] = @Vno and Vtype = @Vtype and seq = @seq
    end;	
    update PurchaseRetMaster  set Amount = (select sum(qty*Rate) from purchaseRetdetail where [Vno] = @Vno and Vtype = @Vtype ) where [Vno] = @Vno and Vtype = @Vtype ;
	
	-----------------------UPdating General Ledger--------------------------------------------
	declare @Vdate date,@User varchar(50),@Amount numeric(18,2),@narration varchar(5),@Remarks varchar(300),@Account varchar(20);
	select @amount = sum(qty * rate) from purchaseRetdetail  where [Vno] = @Vno and Vtype = @Vtype  and status = 0;
	select @Vdate= vdate,@user = CreatedBy,@narration = Narration ,@remarks  = descr ,@Account = fkAccountId  from purchaseRetmaster 
	where VNo = @Vno and Vtype = @Vtype  and status = 0;
	----------------xxxxxxxxxx-----------------xxxxxxxxxxxxx----------------------------------------------
	select @Count = COUNT(*) from GL1 where VoucherNo = @Vno and Vtype = @Vtype and Vseq = 1
	if(@Count = 0)
	begin
	INSERT INTO GL1
           (VDate,Vtime,VoucherNo,Vtype,Vseq,DrAccount,Amount,CrAccount,Narration,Remarks,clear,
		   status,CreatedBy,CreatedTime)
		   values(@VDate,GETDATE(),@Vno,@Vtype,1,@Account,@amount,dbo.Get_Def_Acc('PR') ,@narration,@remarks,0,
		   0,@User,GETDATE())   		 
    end;
    else
    begin
    UPDATE GL1
   SET VDate = @VDate,
   Vtime =  GETDATE()
      ,DrAccount = @Account 
      ,Amount = @amount 
      ,CrAccount = dbo.Get_Def_Acc('PR') 
      ,Narration = @narration 
      ,Remarks = @remarks 
      ,status = 0       
      ,EditBy = @User 
      ,EditTime = GETDATE() 
 WHERE VoucherNo = @Vno and Vtype = @Vtype 
    end;
 COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
END CATCH
END


GO
/****** Object:  StoredProcedure [dbo].[Rep_IncomeSummery]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[Rep_IncomeSummery]
(
@Fdate date ,
@Tdate date )
as 
begin

select 'Sales' vtype,dbo.Get_Title (account) title,SUM(DR-cr) bal from GL_Detail  
where status = 0 and  vdate between @Fdate and @Tdate  and account like '003%' group by account 
union all
---------------------------------------
select 'Cost of Goods Sold' vtype,'Openning Stock' ,dbo.Get_StockValue(dateadd(day,-1,@Fdate))
union all 
--select 'Cost of Goods Sold' vtype,'PURCHASE',sum(amount)  from ItemTransaction CostOfTotalSales  
--where vtype in ('PU','PR') and  vdate between @Fdate and @Tdate  
select 'Cost of Goods Sold' vtype,'PURCHASE',SUM(DR-cr) bal from GL_Detail  
where status = 0 and  vdate between @Fdate and @Tdate  and account like '004001001001001' group by account 
union all 
select 'Cost of Goods Sold' vtype,'Closing Stock' ,-dbo.Get_StockValue(@Tdate)
----------------------------------------
union all
select 'Expenses' vtype,dbo.Get_Title(account) title,sum(dr-cr) from GL_Detail
 where vdate between @Fdate and @Tdate and
 account in (select lvl5 from vu_ChartofAccount where lvl1 = '004' and lvl5 != dbo.Get_Def_Acc('PU'))
group by account
end;

GO
/****** Object:  StoredProcedure [dbo].[Rpt_AccountStatement]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE  procedure [dbo].[Rpt_AccountStatement]
(
@Fdate date ,
@Tdate date ,
@Account varchar(20))
as 
begin
select DATEADD(DAY, -1, @Fdate)  vdate,null vno,0 vseq ,'Openning Balance' particular,(case when isnull(dbo.OpenningBefore(@Fdate,@Account),0) >= 0 then dbo.OpenningBefore(@Fdate,@Account) else 0 end)   as dr , (case when isnull(dbo.OpenningBefore(@Fdate,@Account),0) < 0 then -dbo.OpenningBefore(@Fdate,@Account) else 0 end) as Cr   
union all 
select vdate,vtype + '-' +  voucherno vno,Vseq ,dbo.Get_Title(particular)  + '  ' + isnull(remarks,''),sum(dr),sum(cr) from gl_detail 
where account = @Account  and VDate between @Fdate and @Tdate and status = 0 and Vtype != 'Op' AND (DR - CR) != 0 
group by VDate ,vtype ,VoucherNo ,Vseq,particular,remarks having  sum(dr)- sum(cr) != 0
order by vdate,vno,Vseq
end;



--select* from GL_Detail

GO
/****** Object:  StoredProcedure [dbo].[Rpt_AccountStatement_withDueDate]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE  procedure [dbo].[Rpt_AccountStatement_withDueDate]
(
@Fdate date ,
@Tdate date ,
@Account varchar(20))
as 
begin
select ROW_NUMBER() over (order by VDate ,vno )  rid,*,CAST(null as int )  duedays into #tmp from (
select DATEADD(DAY, -1, @Fdate)  vdate,null vno,0 vseq ,'Openning Balance' particular,(case when isnull(dbo.OpenningBefore(@Fdate,@Account),0) >= 0 then dbo.OpenningBefore(@Fdate,@Account) else 0 end)   as dr , (case when isnull(dbo.OpenningBefore(@Fdate,@Account),0) < 0 then -dbo.OpenningBefore(@Fdate,@Account) else 0 end) as Cr   
union all 
select vdate,vtype + '-' +  voucherno vno,Vseq ,dbo.Get_Title(particular)  + '  ' + isnull(remarks,''),sum(dr),sum(cr) from gl_detail 
where account = @Account  and VDate between @Fdate and @Tdate and status = 0 and Vtype != 'Op' AND (DR - CR) != 0 
group by VDate ,vtype ,VoucherNo ,Vseq,particular,remarks having  sum(dr)- sum(cr) != 0
) a
declare @TotBal numeric(18,2),@LoopCounter  bigint,@DueBal numeric(18,2) = 0
select @TotBal = SUM(isnull(dr,0)-isnull(cr,0)),@DueBal = SUM(isnull(dr,0)-isnull(cr,0)),@LoopCounter = MAX(rid) from #tmp
if(@TotBal > 0)
begin
WHILE ( @LoopCounter IS NOT NULL AND  @LoopCounter > 0 and @duebal > 0)
BEGIN
update #tmp set duedays  = DATEDIFF(DAY, vdate , @Tdate ),@duebal = @duebal - isnull(dr,0)-isnull(cr,0) 
where rid = @LoopCounter and dr > 0 and vno is not null
 SET @LoopCounter  = @LoopCounter  - 1        
END
end 
else if(@TotBal < 0)begin
WHILE ( @LoopCounter IS NOT NULL AND  @LoopCounter > 0 and @duebal < 0)
BEGIN
update #tmp set duedays  = DATEDIFF(DAY, vdate , @Tdate ),@duebal = @duebal + isnull(dr,0)+isnull(cr,0) 
where rid = @LoopCounter and cr > 0 and vno is not null
 SET @LoopCounter  = @LoopCounter  - 1        
END

end

select * from #tmp order by vdate,vno,Vseq
drop table #tmp 
end;

GO
/****** Object:  StoredProcedure [dbo].[Rpt_Balance]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[Rpt_Balance]
(
@TDate date ,
@Account varchar(20)
)
as 
begin
select ch.lvl5title  account,isnull(sum(CurBal),0) balance
 from (
 select account,dr - cr CurBal   
 from GL_Detail where (Vdate <= @TDate  or Vtype =  'Op') and status = 0
 ) Gl ,vu_ChartofAccount ch  
 where gl.account = ch.lvl5 and ch.lvl4 = @Account and  account != '0' 
 group by ch.lvl1,ch.lvl1title,ch.lvl2,ch.lvl2title ,ch.lvl3,ch.lvl3title ,ch.lvl4,ch.lvl4title,account,ch.lvl5title
 having isnull(sum(CurBal),0) <> 0
 order by account
 end

GO
/****** Object:  StoredProcedure [dbo].[Rpt_BalanceSheet]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[Rpt_BalanceSheet]
(
@TDate date
)
as 
begin
declare @IncomeSummary decimal(18,2);
select @IncomeSummary  = sum(bal) from (
select SUM(DR-cr) bal from GL_Detail  
where status = 0 and  vdate <= @Tdate  and account like '003%' group by account 
union all
---------------------------------------
select dbo.Get_StockValue(dateadd(day,-1,'30 Jun 2017'))
union all 
--select sum(amount)  from ItemTransaction CostOfTotalSales  
--where vtype in ('PU','PR') and  vdate <= @Tdate  
select SUM(DR-cr) bal from GL_Detail  
where status = 0 and  vdate  <= @Tdate  and account like '004001001001001' 
union all 
select -dbo.Get_StockValue(@Tdate)
----------------------------------------
union all
select sum(dr-cr) from GL_Detail
 where vdate <= @Tdate and
 account in (select lvl5 from vu_ChartofAccount where lvl1 = '004' and lvl5 != dbo.Get_Def_Acc('PU'))
group by account) a

select lvl1title lvl1,lvl2title lvl2,lvl3title lvl3,lvl4title lvl4,sum(dbo.Curr_Balance(lvl5,@TDate)) DrCr from vu_ChartofAccount 
where lvl1 in ('001','002','005')  AND ISNULL(dbo.Curr_Balance(lvl5,@TDate),0) != 0
group by lvl1title ,lvl2title ,lvl3title ,lvl4title
UNION ALL 
select 'Assets','Current Assets','MERCHANDISE INVENTORY','INVENTORY'  ,dbo.Get_StockValue(@TDate ) 
UNION ALL 
select 'Equity','Equity','Equity','Income Summary'  ,@IncomeSummary 
end;

GO
/****** Object:  StoredProcedure [dbo].[Rpt_CustomerBill]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO




CREATE procedure [dbo].[Rpt_CustomerBill](
@Fdate date ,
@Tdate date ,
@Account varchar(20))
as
begin
select * from (
select sm.Vdate date,sd.vtype+'-'+sd.vno vno,dbo.ItemTitle (fkItem) Item ,dbo.Get_UnitTitle(Unit) Unit,sum(qty) Qty,avg(rate) rate,0 AddLess, sum(qty * rate) amount 
from Sales sd 
left join SaleMaster sm on sd.Vno = sm.vno 
where sd.status = 0 and sm.status = 0
and fkaccountid = @Account and sm.VDate between @Fdate and @Tdate 
group by sm.Vdate,sd.vtype,sd.vno ,fkItem,Unit 
union all
select ssm.Vdate date,ssd.vtype+'-'+ssd.vno vno,dbo.ItemTitle (fkItemId) Item,dbo.Get_UnitTitle(Unit) Unit ,sum(qty) qty,avg(rate) rate,sum(AddLess) AddLess, sum(ssd.Amount) amount 
from SaleSupplyDetail ssd 
left join SaleSupplyMaster ssm on ssd.Vno = ssm.vno 
where ssd.status = 0 and ssm.status = 0
and ssd.fkCustomerId = @Account and ssm.VDate between @Fdate and @Tdate 
group by ssm.Vdate,ssd.vtype,ssd.vno ,fkItemId ,Unit
) ss
order by ss.date


select 
isnull(dbo.Curr_Balance(@Account,DATEADD(day, -1, @Fdate)),0) PreviousBalance,
isnull((select sum(dr - cr) from GL_Detail where account = @Account and Vdate between @Fdate and @TDate and Vtype in ('PV','RV','JV') and status = 0),0) Payment,
isnull(dbo.Curr_Balance(@Account,@Tdate),0) Balance
end;





GO
/****** Object:  StoredProcedure [dbo].[Rpt_StockBalance]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[Rpt_StockBalance]
(
@FDate date ,
@TDate date,
@Catagory varchar(50),
@Filter varchar(20),
@qty numeric(18,2)
)
as 
begin
select ItemTitle Item,dbo.Get_UnitTitle (DefaultUnit) Unit,isnull(sum(PriQty),0) PriQty,isnull(sum(Qty),0) qty,sum(QtyIn) QtyIn,sum(QtyOut ) QtyOut ,isnull(sum(QtyBal),0) QtyBal
,case when isnull(sum(case when QtyBal > 0 then  QtyBal else 0 end),0) = 0 then 0 else  isnull(sum(Amt),0) / isnull(sum(case when QtyBal > 0 then  QtyBal else 0 end),0) end  Rate
 from (
select ItemTitle,fkitem,DefaultUnit,qtyIn - qtyOut as PriQty,null as Qty,0 as QtyIn,0 as QtyOut,null as QtyBal,0 Amt   
 from ItemTransaction where Vdate < @FDate or vtype = 'Op'
 union all
 select ItemTitle,fkitem,DefaultUnit,null as PriQty,qtyIn - qtyOut as Qty,QtyIn,QtyOut,null as QtyBal,0 Amt   
 from ItemTransaction where Vdate between  @FDate and   @TDate and  vtype != 'Op'
 union all
 select ItemTitle,fkitem,DefaultUnit,null as PriQty,null as Qty,0 as QtyIn,0 as QtyOut,qtyIn - qtyOut as QtyBal,
  (case when tranType = 'in' then (QtyIn-QtyOut ) * rate else 0 end) Amt   
 from ItemTransaction where Vdate <= @TDate or vtype = 'Op'
 ) stock
 where fkitem  in (select ID  from ItemDetail it where it.fkItemCatagory  = isnull(@Catagory,it.fkItemCatagory ))
  group by ItemTitle,DefaultUnit  
 having 
(isnull(sum(QtyBal),0) = (case when @Filter = 'All' then isnull(sum(QtyBal),0) 
									when @Filter = 'Equal' then isnull(@qty,0)
									else null end )
or 
isnull(sum(QtyBal),0) >= (case when @Filter = 'Greater' then isnull(@qty,0) 									
									else null end )									
or 
isnull(sum(QtyBal),0) <= (case when @Filter = 'Less' then isnull(@qty,0) 									
									else null end ))		and isnull(sum(QtyBal),0) != 0																 
  order by ItemTitle
 end

GO
/****** Object:  StoredProcedure [dbo].[Rpt_StockBalance1]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[Rpt_StockBalance1]
(
@FDate date ,
@TDate date,
@Catagory varchar(50),
@Type varchar(50),
@Filter varchar(20),
@qty numeric(18,2)
)
as 
begin
select ItemTitle Item,dbo.Get_UnitTitle (DefaultUnit) Unit,isnull(sum(PriQty),0) PriQty,isnull(sum(Qty),0) qty,sum(QtyIn) QtyIn,sum(QtyOut ) QtyOut ,isnull(sum(QtyBal),0) QtyBal
,case when isnull(sum(case when QtyBal > 0 then  QtyBal else 0 end),0) = 0 then 0 else  isnull(sum(Amt),0) / isnull(sum(case when QtyBal > 0 then  QtyBal else 0 end),0) end  Rate
 from (
select ItemTitle,fkitem,DefaultUnit,qtyIn - qtyOut as PriQty,null as Qty,0 as QtyIn,0 as QtyOut,null as QtyBal,0 Amt   
 from ItemTransaction where Vdate < @FDate or vtype = 'Op'
 union all
 select ItemTitle,fkitem,DefaultUnit,null as PriQty,qtyIn - qtyOut as Qty,QtyIn,QtyOut,null as QtyBal,0 Amt   
 from ItemTransaction where Vdate between  @FDate and   @TDate and  vtype != 'Op'
 union all
 select ItemTitle,fkitem,DefaultUnit,null as PriQty,null as Qty,0 as QtyIn,0 as QtyOut,qtyIn - qtyOut as QtyBal,
  (case when tranType = 'in' then (QtyIn-QtyOut ) * rate else 0 end) Amt   
 from ItemTransaction where Vdate <= @TDate or vtype = 'Op'
 ) stock
 where fkitem  in (select ID  from ItemDetail it left join ItemCatagory ic on ic.Code = it.fkItemCatagory  where ic.ItemType = @Type )
  group by ItemTitle,DefaultUnit  
 having 
(isnull(sum(QtyBal),0) = (case when @Filter = 'All' then isnull(sum(QtyBal),0) 
									when @Filter = 'Equal' then isnull(@qty,0)
									else null end )
or 
isnull(sum(QtyBal),0) >= (case when @Filter = 'Greater' then isnull(@qty,0) 									
									else null end )									
or 
isnull(sum(QtyBal),0) <= (case when @Filter = 'Less' then isnull(@qty,0) 									
									else null end ))		and isnull(sum(QtyBal),0) != 0																 
  order by ItemTitle
 end

GO
/****** Object:  StoredProcedure [dbo].[Rpt_StockLedger]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE  procedure [dbo].[Rpt_StockLedger]
(
@Fdate date ,
@Tdate date ,
@fkItem varchar(20)
)
as 
begin
select DATEADD(DAY, -1, @Fdate)  vdate,null vtime,null vno,'Openning Stock' particular,(case when isnull(dbo.StockBefore(@Fdate,@fkItem  ),0) >= 0 then dbo.StockBefore(@Fdate,@fkItem ) else 0 end)   as qtyIn ,
 (case when isnull(dbo.StockBefore(@Fdate,@fkItem ),0) < 0 then -dbo.StockBefore(@Fdate,@fkItem ) else 0 end) as QtyOut,null rate
union all 
select vdate,vtime,vtype + '-' +  cast(vno as varchar(100)) vno,dbo.Get_Title(accountid) particular,qtyIn ,qtyout,rate Costrate   from ItemTransaction 
where  fkitem = @fkitem  and VDate between @Fdate and @Tdate  and Vtype != 'Op' order by vdate,vtime,vno
end;

GO
/****** Object:  StoredProcedure [dbo].[Rpt_TrialBalance]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[Rpt_TrialBalance]
(
@FDate date ,
@TDate date
)
as 
begin
select ch.lvl1title lvl1,ch.lvl2title lvl2,ch.lvl3title lvl3,ch.lvl4title lvl4,ch.lvl5title  title,isnull(sum(PriBal),0) PriBal,
isnull(sum(Dr ),0) Dr,isnull(sum(Cr),0) Cr,isnull(sum(CurBal),0) CurBal
 from (
select account,DR - CR as PriBal,null as Dr,null as Cr,null as CurBal   
 from GL_Detail  where Vdate < @FDate or Vtype =  'Op'
 union all
 select account,null ,dr ,cr ,null    
 from GL_Detail where Vdate between  @FDate and   @TDate and Vtype !=  'Op'
 union all
 select account,null,null ,null ,dr - cr    
 from GL_Detail where Vdate <= @TDate  or Vtype =  'Op'
 ) Gl ,vu_ChartofAccount ch  
 where gl.account = ch.lvl5 and  account != '0' 
 group by ch.lvl1,ch.lvl1title,ch.lvl2,ch.lvl2title ,ch.lvl3,ch.lvl3title ,ch.lvl4,ch.lvl4title,account,ch.lvl5title
 order by ch.lvl4,title,account
 end

GO
/****** Object:  StoredProcedure [dbo].[Sale_AddEdit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Sale_AddEdit]
@Vno varchar(8),
@Vtype varchar(15),
@seq numeric(3,0),
@fkItem varchar(10),
@Unit varchar(3),
@Qty numeric(12,2),
@Rate numeric(18,2),
@Discount numeric(18,2),
@status numeric(1,0)
AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
	declare @Count int;
	select @Count = COUNT(*) from Sales   where [Vno] = @Vno and Vtype = @Vtype and [Seq] = @seq
	if(@Count = 0)
	begin
	INSERT INTO [dbo].Sales
           ([Vtype]
           ,[Vno]
           ,[Seq]
           ,[fkItem]		   
		   ,unit
		   ,QtyInPack 
          ,[Qty]
           ,[GrossRate]
           ,discount 
           ,[status])
     VALUES
		   (@Vtype,@Vno,@seq,@fkItem ,@Unit ,(select qtyinpack from ItemDetail  where id =  @fkItem and status = 0),
		   @Qty,
		   @Rate ,@Discount ,@status)   
    end;
    else
    begin
   UPDATE [dbo].Sales
   SET [fkItem] = @fkItem
   ,Unit = @unit
      ,[Qty] = @Qty
	  ,[GrossRate] = @Rate
	  ,discount = @Discount 
	  ,status = @status 
 where [Vno] = @Vno and Vtype = @Vtype and seq = @seq
    end;	
    ------------------------
    update salemaster set Netamount = (select sum(qty*Rate) from Sales   where [Vno] = @Vno and Vtype = @Vtype AND status = 0) 
    where [Vno] = @Vno and Vtype = @Vtype ;
	
	
	update salemaster set amount = (select sum(qty*grossRate) from Sales   where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) ,
	Discount = (select sum(qty*discount) from Sales   where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) ,
	Netamount = (select sum(qty*Rate) from Sales   where [Vno] = @Vno and Vtype = @Vtype  AND status = 0) 	
	where [Vno] = @Vno and Vtype = @Vtype ;
	-----------------------UPdating General Ledger--------------------------------------------
declare @Vdate date,@User varchar(50),@Amount numeric(18,2),@ReceiptAmount numeric(18,2),@narration varchar(5),@Remarks varchar(300),@Account varchar(20),@Sales varchar(20);
	select @amount = sum(qty * rate) from Sales  where [Vno] = @Vno and Vtype = @Vtype and status = 0;
	select @Vdate= vdate,@user = CreatedBy,@narration = Narration ,@remarks  = descr ,@Account = fkAccountId,@ReceiptAmount = (cashreceipt - CashBack) ,@Sales = dbo.Get_Def_Acc('SL')     from SaleMaster  
	where VNo = @Vno and Vtype = @Vtype and status = 0;
	---------- xxxxxxxxxxx -----------xxxxxxxxxxxx---------------for Sales Account
	select @Count = COUNT(*) from GL1 where VoucherNo = @Vno and Vtype = @Vtype and Vseq =  1
	if(@Count = 0)
	begin
	INSERT INTO GL1
           (VDate,Vtime,VoucherNo,Vtype,Vseq,DrAccount,Amount,CrAccount,Narration,Remarks,clear,
		   status,CreatedBy,CreatedTime)
		   values(@VDate,GETDATE(),@Vno,@Vtype,1,@Account ,@amount,@sales,@narration,@remarks,0,
		   0,@User,GETDATE())   		 
    end;
    else
    begin
    UPDATE GL1
   SET VDate = @VDate,
   Vtime =  GETDATE()
      ,DrAccount = @Account
      ,Amount = @amount 
      ,CrAccount = @sales
      ,Narration = @narration 
      ,Remarks = @remarks 
      ,status = 0       
      ,EditBy = @User 
      ,EditTime = GETDATE() 
 WHERE VoucherNo = @Vno and Vtype = @Vtype and Vseq = 1  
END
---------- xxxxxxxxxxx -----------xxxxxxxxxxxx---------------for Cash Account
	select @Count = COUNT(*) from GL1 where VoucherNo = @Vno and Vtype = @Vtype and Vseq =  2
	if(@Count = 0)
	begin
	INSERT INTO GL1
           (VDate,Vtime,VoucherNo,Vtype,Vseq,DrAccount,Amount,CrAccount,Narration,Remarks,clear,
		   status,CreatedBy,CreatedTime)
		   values(@VDate,GETDATE(),@Vno,@Vtype,2,dbo.Get_Def_Acc('Cash') ,@ReceiptAmount ,@Account,@narration,@remarks,0,
		   0,@User,GETDATE())   		 
    end;
    else
    begin
    UPDATE GL1
   SET VDate = @VDate,
   Vtime =  GETDATE()
      ,DrAccount = dbo.Get_Def_Acc('Cash')
      ,Amount = @ReceiptAmount 
      ,CrAccount = @Account
      ,Narration = @narration 
      ,Remarks = @remarks 
      ,status = 0       
      ,EditBy = @User 
      ,EditTime = GETDATE() 
 WHERE VoucherNo = @Vno and Vtype = @Vtype and Vseq = 2  
END
 COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
END CATCH
end ;

GO
/****** Object:  StoredProcedure [dbo].[SaleMaster_AddEdit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SaleMaster_AddEdit]
@VDate date,
@Vno varchar(8),
@Vtype varchar(15),
@fkAccount varchar(20),
@Descr varchar(300),
@Narration 	varchar(5),
@CashReceipt numeric(18,2),
@CashBack numeric(18,2),	   
@Counter varchar(50),
@status numeric(1,0),
@User varchar(50),
@RetInvoiceNo  varchar(8) output

AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
	declare @Count int;--,@RetInvoiceNo  varchar(8);
	select @Count = COUNT(*) from SaleMaster   where [Vno] = @Vno and Vtype = @Vtype and status = 0
	if(@Vno is null)
	begin
	select @Vno =  dbo.digitformat(isnull(max(Vno) + 1,1),5)  from SaleMaster where Vtype = @Vtype 
	end ;
	if(@Count = 0)
	begin
	INSERT INTO [dbo].SaleMaster
           ([Vdate]
		   ,Vtime
           ,[Vtype]
           ,[Vno]
		   ,fkAccountId 	
		   ,Descr
		   ,Narration 	
		   ,cashreceipt
		   ,cashback
           ,[CreatedBy]
           ,[CreatedTime]          
           ,[status]
           ,[Counter])
     VALUES
		   (@VDate,GETDATE(),@Vtype,@Vno ,@fkaccount  ,@Descr ,@Narration ,@Cashreceipt,@cashBack,
		   @User,GETDATE(),@status,@Counter )   
    end;
    else
    begin
   UPDATE [dbo].SaleMaster
   SET [Vdate] = @VDate 
   ,fkAccountId  = @fkAccount 
   ,Descr=@Descr 
   ,Narration=@Narration
   ,Cashreceipt=@Cashreceipt
   ,CashBack = @CashBack
      ,[EditBy] = @User
      ,[EditTime] = GETDATE()
 where [Vno] = @Vno and Vtype = @Vtype and status = 0
    end;
	--commit
 set	@RetInvoiceNo = @Vno

	COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
END CATCH
END

GO
/****** Object:  StoredProcedure [dbo].[SaleMaster_AddEdit1]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SaleMaster_AddEdit1]
@VDate date,
@Vno varchar(8),
@Vtype varchar(15),
@Sales varchar(20),
@fkAccount varchar(20),
@Descr varchar(300),
@Narration 	varchar(5),
@CashReceipt numeric(18,2),
@CashBack numeric(18,2),	   
@Counter varchar(50),
@status numeric(1,0),
@User varchar(50),
@RetInvoiceNo  varchar(8) output

AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
	declare @Count int;--,@RetInvoiceNo  varchar(8);
	select @Count = COUNT(*) from SaleMaster   where [Vno] = @Vno and Vtype = @Vtype and status = 0
	if(@Vno is null)
	begin
	select @Vno =  dbo.digitformat(isnull(max(Vno) + 1,1),5)  from SaleMaster where Vtype = @Vtype 
	end ;
	if(@Count = 0)
	begin
	INSERT INTO [dbo].SaleMaster
           ([Vdate]
		   ,Vtime
           ,[Vtype]
           ,[Vno]
		   ,fkAccountId 	
		   ,Descr
		   ,Narration 	
		   ,cashreceipt
		   ,cashback
           ,[CreatedBy]
           ,[CreatedTime]          
           ,[status]
           ,[Counter])
     VALUES
		   (@VDate,GETDATE(),@Vtype,@Vno ,@fkaccount  ,@Descr ,@Narration ,@Cashreceipt,@cashBack,
		   @User,GETDATE(),@status,@Counter )   
    end;
    else
    begin
   UPDATE [dbo].SaleMaster
   SET [Vdate] = @VDate 
   ,fkAccountId  = @fkAccount 
   ,Descr=@Descr 
   ,Narration=@Narration
   ,Cashreceipt=@Cashreceipt
   ,CashBack = @CashBack
      ,[EditBy] = @User
      ,[EditTime] = GETDATE()
 where [Vno] = @Vno and Vtype = @Vtype and status = 0
    end;
	--commit
 set	@RetInvoiceNo = @Vno

	COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
END CATCH
END

GO
/****** Object:  StoredProcedure [dbo].[SaleRetDetail_AddEdit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SaleRetDetail_AddEdit]
@Vno varchar(8),
@Vtype varchar(15),
@seq numeric(3,0),
@fkItem varchar(10),
@Unit varchar(3),
@Qty numeric(12,2),
@Rate numeric(18,2),
@Discount numeric(18,2),
@status numeric(1,0)
AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
	declare @Count int;
	select @Count = COUNT(*) from SaleRetDetail   where [Vno] = @Vno and Vtype = @Vtype and [Seq] = @seq
	if(@Count = 0)
	begin
	INSERT INTO [dbo].SaleRetDetail
           ([Vtype]
           ,[Vno]
           ,[Seq]
           ,[fkItem]		   
		   ,unit
		   ,QtyInPack 
          ,[Qty]
           ,[GrossRate]
           ,discount 
           ,[status])
     VALUES
		   (@Vtype,@Vno,@seq,@fkItem ,@Unit ,(select qtyinpack from ItemDetail  where id =  @fkItem and status = 0),
		   @Qty,
		   @Rate ,@Discount ,@status)   
    end;
    else
    begin
   UPDATE [dbo].SaleRetDetail
   SET [fkItem] = @fkItem
   ,Unit = @unit
      ,[Qty] = @Qty
	  ,[GrossRate] = @Rate
	  ,discount = @Discount 
	  ,status = @status 
 where [Vno] = @Vno and Vtype = @Vtype and seq = @seq
    end;	
    ------------------------
    update saleretmaster set Netamount = (select sum(qty*Rate) from Saleretdetail   where [Vno] = @Vno and Vtype = @Vtype and status = 0 ) where [Vno] = @Vno and Vtype = @Vtype ;
	
	
	update saleretmaster set amount = (select sum(qty*grossRate) from Saleretdetail   where [Vno] = @Vno and Vtype = @Vtype and status = 0 ) ,
	Discount = (select sum(qty*discount) from Saleretdetail   where [Vno] = @Vno and Vtype = @Vtype and status = 0) ,
	Netamount = (select sum(qty*Rate) from Saleretdetail   where [Vno] = @Vno and Vtype = @Vtype and status = 0) 	
	where [Vno] = @Vno and Vtype = @Vtype ;
	-----------------------UPdating General Ledger--------------------------------------------
declare @Vdate date,@User varchar(50),@Amount numeric(18,2),@ReceiptAmount numeric(18,2),@narration varchar(5),@Remarks varchar(300),@Account varchar(20);
	select @amount = sum(qty * rate) from Saleretdetail  where [Vno] = @Vno and Vtype = @Vtype and status = 0;
	select @Vdate= vdate,@user = CreatedBy,@narration = Narration ,@remarks  = descr ,@Account = fkAccountId,@ReceiptAmount = (cashreceipt - CashBack)    from saleretmaster  
	where VNo = @Vno and Vtype = @Vtype and status = 0;
	---------- xxxxxxxxxxx -----------xxxxxxxxxxxx---------------for Sales Account
	select @Count = COUNT(*) from GL1 where VoucherNo = @Vno and Vtype = @Vtype and Vseq =  1
	if(@Count = 0)
	begin
	INSERT INTO GL1
           (VDate,Vtime,VoucherNo,Vtype,Vseq,DrAccount,Amount,CrAccount,Narration,Remarks,clear,
		   status,CreatedBy,CreatedTime)
		   values(@VDate,GETDATE(),@Vno,@Vtype,1,dbo.Get_Def_Acc('SR'),@amount,@Account ,@narration,@remarks,0,
		   0,@User,GETDATE())   		 
    end;
    else
    begin
    UPDATE GL1
   SET VDate = @VDate,
   Vtime =  GETDATE()
      ,DrAccount = dbo.Get_Def_Acc('SR')
      ,Amount = @amount 
      ,CrAccount = @Account
      ,Narration = @narration 
      ,Remarks = @remarks 
      ,status = 0       
      ,EditBy = @User 
      ,EditTime = GETDATE() 
 WHERE VoucherNo = @Vno and Vtype = @Vtype and Vseq = 1  
END
---------- xxxxxxxxxxx -----------xxxxxxxxxxxx---------------for Cash Account
	select @Count = COUNT(*) from GL1 where VoucherNo = @Vno and Vtype = @Vtype and Vseq =  2
	if(@Count = 0)
	begin
	INSERT INTO GL1
           (VDate,Vtime,VoucherNo,Vtype,Vseq,DrAccount,Amount,CrAccount,Narration,Remarks,clear,
		   status,CreatedBy,CreatedTime)
		   values(@VDate,GETDATE(),@Vno,@Vtype,2,@Account,@ReceiptAmount ,dbo.Get_Def_Acc('Cash') ,@narration,@remarks,0,
		   0,@User,GETDATE())   		 
    end;
    else
    begin
    UPDATE GL1
   SET VDate = @VDate,
   Vtime =  GETDATE()
      ,DrAccount = @Account
      ,Amount = @ReceiptAmount 
      ,CrAccount = dbo.Get_Def_Acc('Cash')
      ,Narration = @narration 
      ,Remarks = @remarks 
      ,status = 0       
      ,EditBy = @User 
      ,EditTime = GETDATE() 
 WHERE VoucherNo = @Vno and Vtype = @Vtype and Vseq = 2  
END
 COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
END CATCH
end ;

GO
/****** Object:  StoredProcedure [dbo].[SaleRetMaster_AddEdit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SaleRetMaster_AddEdit]
@VDate date,
@Vno varchar(8),
@Vtype varchar(15),
@fkAccount varchar(20),
@Descr varchar(300),
@Narration 	varchar(5),
@CashReceipt numeric(18,2),
@CashBack numeric(18,2),	   
@Counter varchar(50),
@status numeric(1,0),
@User varchar(50),
@RetInvoiceNo  varchar(8) output

AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
	declare @Count int;--,@RetInvoiceNo  varchar(8);
	select @Count = COUNT(*) from SaleRetMaster   where [Vno] = @Vno and Vtype = @Vtype and status = 0
	if(@Vno is null)
	begin
	select @Vno =  dbo.digitformat(isnull(max(Vno) + 1,1),5)  from SaleRetMaster where Vtype = @Vtype 
	end ;
	if(@Count = 0)
	begin
	INSERT INTO [dbo].SaleRetMaster
           ([Vdate]
		   ,Vtime
           ,[Vtype]
           ,[Vno]
		   ,fkAccountId 	
		   ,Descr
		   ,Narration 	
		   ,cashreceipt
		   ,cashback
           ,[CreatedBy]
           ,[CreatedTime]          
           ,[status]
           ,[Counter])
     VALUES
		   (@VDate,GETDATE(),@Vtype,@Vno ,@fkaccount  ,@Descr ,@Narration ,@Cashreceipt,@cashBack,
		   @User,GETDATE(),@status,@Counter )   
    end;
    else
    begin
   UPDATE [dbo].SaleRetMaster
   SET [Vdate] = @VDate 
   ,fkAccountId  = @fkAccount 
   ,Descr=@Descr 
   ,Narration=@Narration
   ,Cashreceipt=@Cashreceipt
   ,CashBack = @CashBack
      ,[EditBy] = @User
      ,[EditTime] = GETDATE()
 where [Vno] = @Vno and Vtype = @Vtype and status = 0
    end;
	--commit
 set	@RetInvoiceNo = @Vno

	COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
ROLLBACK TRAN tr; 
END CATCH
END

GO
/****** Object:  StoredProcedure [dbo].[SaleSupplyDetail_AddEdit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Stored Procedure for SaleSupplyDetail_AddEdit
CREATE PROCEDURE [dbo].[SaleSupplyDetail_AddEdit]
@Vno varchar(8),
@Vtype varchar(15),
@seq numeric(3,0),
@fkCustomerId varchar(20),
@Unit varchar(3),
@Qty numeric(12,2),
@GrossRate numeric(18,2),
@Discount numeric(18,2),
@AddLess numeric(18,2),
@status numeric(1,0)
AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
    declare @Count int, @User varchar(50);
    
    -- Get user from master record
    select @User = CreatedBy from SaleSupplyMaster where VNo = @Vno and Vtype = @Vtype and status = 0;
    
    select @Count = COUNT(*) from SaleSupplyDetail where [Vno] = @Vno and Vtype = @Vtype and [Seq] = @seq
    
    if(@Count = 0)
    begin
        INSERT INTO [dbo].SaleSupplyDetail
               ([Vtype]
               ,[Vno]
               ,[Seq]
               ,[fkCustomerId]
               ,unit
               ,[Qty]
               ,[GrossRate]
               ,discount 
			   ,AddLess
               ,[status])
         VALUES
               (@Vtype,@Vno,@seq,@fkCustomerId,@Unit,@Qty,@GrossRate,@Discount,@AddLess,@status)   
    end;
    else
    begin
       UPDATE [dbo].SaleSupplyDetail
       SET [fkCustomerId] = @fkCustomerId
          ,Unit = @unit
          ,[Qty] = @Qty
          ,[GrossRate] = @GrossRate
          ,discount = @Discount 
		  ,AddLess = @AddLess
          ,status = @status 
       where [Vno] = @Vno and Vtype = @Vtype and seq = @seq
    end;    
    
    -- Update master table totals
    update SaleSupplyMaster set 
        Amount = (select isnull(sum(qty*GrossRate),0) from SaleSupplyDetail where [Vno] = @Vno and Vtype = @Vtype AND status = 0),
        Discount = (select isnull(sum(qty*discount),0) from SaleSupplyDetail where [Vno] = @Vno and Vtype = @Vtype AND status = 0),
        NetAmount = (select isnull(sum(qty*(GrossRate-discount)) + sum(AddLess),0) from SaleSupplyDetail where [Vno] = @Vno and Vtype = @Vtype AND status = 0) 	
    where [Vno] = @Vno and Vtype = @Vtype;
    
    -- Update General Ledger entries (simplified for sale supply)
    declare @Vdate date, @Amount numeric(18,2), @narration varchar(5), 
            @Remarks varchar(300), @SaleSupplyAccount varchar(20), @ItemId varchar(20);
            
    select @amount = isnull((qty * (GrossRate - discount)),0) + AddLess from SaleSupplyDetail 
    where [Vno] = @Vno and Vtype = @Vtype and Seq = @seq and status = 0;
    
    select @Vdate = vdate, @narration = Narration, @remarks = descr, @ItemId = fkItemId
    from SaleSupplyMaster  
    where VNo = @Vno and Vtype = @Vtype  and status = 0;
    
    -- Get sale supply account (you may need to adjust this based on your chart of accounts)
    set @SaleSupplyAccount = dbo.Get_Def_Acc('SP'); -- Assuming 'SP' for Sale Supply account
    
    -- GL Entry for Sale Supply
    select @Count = COUNT(*) from GL1 where VoucherNo = @Vno and Vtype = @Vtype and Vseq = @seq
    if(@Count = 0 and @amount > 0)
    begin
        INSERT INTO GL1
               (VDate,Vtime,VoucherNo,Vtype,Vseq,DrAccount,Amount,CrAccount,Narration,Remarks,clear,
               status,CreatedBy,CreatedTime)
               values(@VDate,GETDATE(),@Vno,@Vtype,@seq,@fkCustomerId,@amount,@SaleSupplyAccount,@narration,@remarks,0,
               0,@User,GETDATE())   		 
    end;
    else if(@Count > 0)
    begin
        if(@amount > 0)
        begin
            UPDATE GL1
            SET VDate = @VDate,
                Vtime = GETDATE(),
                DrAccount = @fkCustomerId, -- Accounts Receivable
                Amount = @amount,
                CrAccount = @SaleSupplyAccount,
                Narration = @narration,
                Remarks = @remarks,
                status = 0,
                EditBy = @User,
                EditTime = GETDATE()
            WHERE VoucherNo = @Vno and Vtype = @Vtype and Vseq = @seq  
        end
        else
        begin
            -- Delete GL entry if amount is 0
            DELETE FROM GL1 WHERE VoucherNo = @Vno and Vtype = @Vtype and Vseq = @seq
        end
    end

    COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
    ROLLBACK TRAN tr; 
    THROW;
END CATCH
end;



GO
/****** Object:  StoredProcedure [dbo].[SaleSupplyMaster_AddEdit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Stored Procedure for SaleSupplyMaster_AddEdit
CREATE PROCEDURE [dbo].[SaleSupplyMaster_AddEdit]
@VDate date,
@Vno varchar(8),
@Vtype varchar(15),
@fkItemId varchar(20),
@Descr varchar(300),
@Narration varchar(5),
@Counter varchar(50),
@User varchar(50),
@RetInvoiceNo varchar(8) output
AS
BEGIN
BEGIN TRANSACTION tr
BEGIN TRY
    declare @Count int, @status numeric(1,0) = 0;
    
    select @Count = COUNT(*) from SaleSupplyMaster where [Vno] = @Vno and Vtype = @Vtype and status = 0
    
    if(@Vno is null)
    begin
        select @Vno = dbo.digitformat(isnull(max(Vno) + 1,1),5) from SaleSupplyMaster where Vtype = @Vtype 
    end;
    
    if(@Count = 0)
    begin
        INSERT INTO [dbo].SaleSupplyMaster
               ([Vdate]
               ,Vtime
               ,[Vtype]
               ,[Vno]
               ,fkItemId 
               ,Descr
               ,Narration
               ,[CreatedBy]
               ,[CreatedTime]          
               ,[status]
               ,[Counter])
         VALUES
               (@VDate,GETDATE(),@Vtype,@Vno,@fkItemId,@Descr,@Narration,
               @User,GETDATE(),@status,@Counter)   
    end;
    else
    begin
       UPDATE [dbo].SaleSupplyMaster
       SET [Vdate] = @VDate 
          ,fkItemId = @fkItemId 
          ,Descr = @Descr 
          ,Narration = @Narration
          ,[EditBy] = @User
          ,[EditTime] = GETDATE()
       where [Vno] = @Vno and Vtype = @Vtype and status = 0
    end;
    
    set @RetInvoiceNo = @Vno

    COMMIT TRANSACTION tr;    
END TRY
BEGIN CATCH 
    ROLLBACK TRAN tr; 
    THROW;
END CATCH
END


GO
/****** Object:  StoredProcedure [dbo].[SupplierInfo_Add_Edit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SupplierInfo_Add_Edit]
	@Code varchar(20),
	@Email varchar(50) ,
	@Fax varchar(50) ,
	@CNIC varchar(50) ,
	@Address varchar(150) ,
	@Qualification varchar(100) ,
	@Phone1 varchar(25) ,
	@Phone2 varchar(25) ,
	@SMSNumber varchar(50) ,
	@IBAN varchar(80) ,
	@SMSAlert numeric(1, 0) ,
	@EmailAlert numeric(1, 0) ,
	@image varbinary(max),
	@Active numeric(1, 0),
	@ShowInSales numeric(1, 0),
	@User varchar(200) ,
	@status numeric(1, 0),
	 @RetVal  varchar(5)   out
AS
BEGIN
	declare @Count int;
	select @Count = COUNT(*) from SupplierDetail  where Code  = @Code 
	if(@Count = 0)
	begin

	INSERT INTO SupplierDetail
           ([Code]
           ,[Email]
           ,[Fax]
           ,[CNIC]
           ,[Address]
           ,[Qualification]
           ,[Phone1]
           ,[Phone2]
           ,[SMSNumber]
           ,[IBAN]
           ,[SMSAlert]
           ,[EmailAlert]
           ,[image]
           ,[Active]
           ,ShowInSales 
           ,[CreatedBy]
		   ,CreatedTime 
           ,[status])
     VALUES
           (@Code
           ,@Email
           ,@Fax
           ,@CNIC
           ,@Address
           ,@Qualification
           ,@Phone1
           ,@Phone2
           ,@SMSNumber
           ,@IBAN
           ,@SMSAlert
           ,@EmailAlert
           ,@image
           ,@Active
           ,@ShowInSales 
           ,@User
		   ,sysdatetime()  
           ,@status)
    end;
    else
    begin
   UPDATE SupplierDetail
   SET [Email] = @Email
      ,[Fax] = @Fax
      ,[CNIC] = @CNIC
      ,[Address] = @Address
      ,[Qualification] = @Qualification
      ,[Phone1] = @Phone1
      ,[Phone2] = @Phone2
      ,[SMSNumber] = @SMSNumber
      ,[IBAN] = @IBAN
      ,[SMSAlert] = @SMSAlert
      ,[EmailAlert] = @EmailAlert
      ,[image] = @image
      ,[Active] = @Active
      ,ShowInSales = @ShowInSales 
      ,[EditBy] = @User 
      ,[EditTime] = sysdatetime()
      ,[status] = @status
 WHERE [Code] = @code
    end;
	set @retval = @code
END

GO
/****** Object:  StoredProcedure [dbo].[UnitsAdd_Edit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[UnitsAdd_Edit]
	@Code varchar(3),
	@title varchar(150),
	@status numeric 
	
AS
BEGIN
	declare @Count int;
	select @Count = COUNT(*) from units  where Code  = @Code 
	if(@Count = 0)
	begin
	INSERT INTO units(Code ,Title ,status)
     VALUES(@Code ,@title ,@status ) 
    end;
    else
    begin
    UPDATE units
   SET [Title] = @title,
   status =@status
      
 WHERE Code  = @Code 
    end;
END

GO
/****** Object:  StoredProcedure [dbo].[UserStats]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[UserStats]
@User varchar(25)
 as 
begin 
cREATE TABLE #sp_who2 (SPID INT,Status VARCHAR(255),
      Login  VARCHAR(255),HostName  VARCHAR(255), 
      BlkBy  VARCHAR(255),DBName  VARCHAR(255), 
      Command VARCHAR(255),CPUTime INT, 
      DiskIO INT,LastBatch VARCHAR(255), 
      ProgramName VARCHAR(255),SPID2 INT, 
      REQUESTID INT) 
INSERT INTO #sp_who2 EXEC sp_who2
SELECT      * 
FROM        #sp_who2 where Login = @User  and DBName = 'Bakery' --and CPUTime != 0
end;

GO
/****** Object:  UserDefinedFunction [dbo].[CatagoryTitle]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[CatagoryTitle]
(
	@Code varchar(5)
	 
)
RETURNS varchar(100) 
AS
BEGIN
	
	DECLARE @Title  varchar(100)

	select @Title  = title  from ItemCatagory  where  code =  @Code

	-- Return the result of the function
	RETURN @Title 
END

GO
/****** Object:  UserDefinedFunction [dbo].[Curr_Balance]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[Curr_Balance]
(
	@Account varchar(20),
	@TDate date = null
	 
)
RETURNS decimal(18,2)  
AS
BEGIN
	
	DECLARE @Bal decimal(18,2) 

	select @Bal  = sum(dr - cr)  from GL_Detail where  account  = @Account and status = 0 and (Vtype = 'Op' or Vdate <= isnull(@TDate,Vdate) )

	-- Return the result of the function
	RETURN @Bal 
END

GO
/****** Object:  UserDefinedFunction [dbo].[digitformat]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[digitformat]
(
	@number varchar(18)  ,
	@digit numeric(18) 
)
RETURNS varchar(18)
AS
BEGIN
	-- Declare the return variable here
	DECLARE  @RetVal varchar(18) 

	SELECT @RetVal =  (case when len(@number)  < @digit then  replicate('0', @digit - len(@number)) else '' end) +  cast(@number as varchar)
	-- Return the result of the function
	RETURN @RetVal

END

GO
/****** Object:  UserDefinedFunction [dbo].[floor]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[floor]
(
@value decimal (38,12)
)
RETURNS bigint 
AS
BEGIN
	
	DECLARE @Ret bigint 
	SELECT @Ret = case when  @value >= 0 then 
	floor(@value) 
	else 
    ceiling(@value) 
	end   
	RETURN @Ret

END

GO
/****** Object:  UserDefinedFunction [dbo].[Get_Def_Acc]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[Get_Def_Acc]
(
	@type varchar(50)
)
RETURNS varchar(50)
AS
BEGIN
	
	DECLARE @Account varchar(20)

	
	select @Account = Account from DefaultAccount where Title = @type 

	-- Return the result of the function
	RETURN @account

END

GO
/****** Object:  UserDefinedFunction [dbo].[Get_MonthlyIncome]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[Get_MonthlyIncome]
(
@month int,
@Year int 	 
)
RETURNS decimal(18,2) 
AS
BEGIN	

	DECLARE @Income  decimal(18,2) 

select @Income = isnull(sum(bal),0) from (
select -isnull(sum(amount),0) Bal from ItemTransaction TotalSales  
where vtype = 'SL' and  month(vdate) = @month and year(vdate) = @Year  
union all
select sum((qtyout - qtyin) * rate) from ItemTransaction CostOfTotalSales  
where vtype = 'SL' and  month(vdate) = @month and year(vdate) = @Year  

union all
select  sum(dr-cr)   from 
(SELECT [Amount] as DR,0 as CR FROM GL1
 where  month(vdate) = @month and year(vdate) = @Year and
 DRAccount  in (select lvl5 from vu_ChartofAccount where lvl1 = '004' and lvl5 != dbo.Get_Def_Acc('PU'))
union all 
SELECT  0 as DR,[Amount] as CR FROM GL1
 where  month(vdate) = @month and year(vdate) = @Year and
 CRAccount in (select lvl5 from vu_ChartofAccount where lvl1 = '004' and lvl5 != dbo.Get_Def_Acc('PU'))
 )gl
 )inc
	RETURN @Income 
END

GO
/****** Object:  UserDefinedFunction [dbo].[Get_NarrationTitle]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[Get_NarrationTitle]
(
@Code varchar(3) 
)
RETURNS nvarchar(150) 
AS
BEGIN
	
	DECLARE @Title nvarchar(150) 
	SELECT @Title = Title   from Narration   where Code  = @Code 
	RETURN @Title

END

GO
/****** Object:  UserDefinedFunction [dbo].[Get_parentAcc]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[Get_parentAcc]
(
@account varchar(20) 
)
RETURNS varchar(20) 
AS
BEGIN
	
	DECLARE @parentAccount varchar(20) 
	SELECT @parentAccount = parentId  from ChartOfAccount  where Account = @account 
	RETURN @parentAccount

END

GO
/****** Object:  UserDefinedFunction [dbo].[Get_PriItemUnit]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create FUNCTION [dbo].[Get_PriItemUnit]
(
	@fkCatagory varchar(5),
	@Code int
	 
)
RETURNS varchar(100) 
AS
BEGIN
	
	DECLARE @Title  varchar(100)

	select @Title  = dbo.Get_UnitTitle (PrimaryUnit)   from ItemDetail where fkItemCatagory = @fkCatagory  and code =  @Code  

	-- Return the result of the function
	RETURN @Title 
END

GO
/****** Object:  UserDefinedFunction [dbo].[Get_StockValue]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create FUNCTION [dbo].[Get_StockValue]
(
	@Date date 
)
RETURNS numeric(18,2)
AS
BEGIN
	
	DECLARE @Amount numeric(18,2)
	select  @amount  = SUM(Amount)from (
select isnull(sum(QtyBal),0) * case when isnull(sum(case when QtyBal > 0 then  QtyBal else 0 end),0) = 0 then 0 else  isnull(sum(Amt),0) / isnull(sum(case when QtyBal > 0 then  QtyBal else 0 end),0) end  Amount
 from (
 select ItemTitle,fkitem,DefaultUnit,null as PriQty,null as Qty,qtyIn - qtyOut as QtyBal,
  (case when tranType = 'in' then (QtyIn-QtyOut ) * rate else 0 end) Amt   
 from ItemTransaction where Vdate <= @Date or vtype = 'Op'
 ) stock
  group by ItemTitle,DefaultUnit  
  )a
  	RETURN @Amount 

END

GO
/****** Object:  UserDefinedFunction [dbo].[Get_Title]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[Get_Title]
(
@account varchar(20) 
)
RETURNS nvarchar(150) 
AS
BEGIN
	
	DECLARE @Title nvarchar(150) 
	SELECT @Title = Title   from ChartOfAccount  where Account = @account 
	RETURN @Title

END

GO
/****** Object:  UserDefinedFunction [dbo].[Get_UnitTitle]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create FUNCTION [dbo].[Get_UnitTitle]
(
@Code varchar(3) 
)
RETURNS varchar(150) 
AS
BEGIN
	
	DECLARE @Title varchar(150) 
	SELECT @Title = Title   from Units  where Code  = @Code 
	RETURN @Title

END

GO
/****** Object:  UserDefinedFunction [dbo].[ItemTitle]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[ItemTitle]
(
	@Code varchar(10)
	 
)
RETURNS nvarchar(100) 
AS
BEGIN
	
	DECLARE @Title  nvarchar(100)

	select @Title  = title  from ItemDetail where  id =  @Code  

	-- Return the result of the function
	RETURN @Title 
END

GO
/****** Object:  UserDefinedFunction [dbo].[LastDateOfMonth]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[LastDateOfMonth]
(
@Vdate date 
)
RETURNS date 
AS
BEGIN
	
	DECLARE @IVdate date
	select @IVdate = dateadd(day,-1,convert(date,cast((month(@Vdate )  + 1) as  varchar(2))+'/1/'+cast (year(@Vdate) as varchar(4))))
	
	RETURN @IVdate 
END

GO
/****** Object:  UserDefinedFunction [dbo].[LeadZero]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create FUNCTION [dbo].[LeadZero]
(
	@Value varchar(50),
	@Digit bigint
	 
)
RETURNS varchar(50) 
AS
BEGIN
	
	DECLARE @RetVal  varchar(50)
	select @RetVal = replicate ('0',case when 0 < @Digit-len(@Value) then @Digit-len(@Value) else 0 end ) + @Value
	
	RETURN @RetVal 
END

GO
/****** Object:  UserDefinedFunction [dbo].[OpenningBal]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[OpenningBal]
(
	@Account varchar(20)
	 
)
RETURNS decimal(18,2)  
AS
BEGIN
	
	DECLARE @Bal decimal(18,2) 

	select @Bal  = sum(dr - cr)  from GL_Detail where  account  = @Account and status = 0 and Vtype = 'Op'

	-- Return the result of the function
	RETURN @Bal 
END

GO
/****** Object:  UserDefinedFunction [dbo].[OpenningBefore]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[OpenningBefore]
(
	@date date,
	@Account varchar(20)
	 
)
RETURNS decimal(18,2)  
AS
BEGIN
	
	DECLARE @Bal decimal(18,2) 

	select @Bal  = sum(dr - cr)  from GL_Detail where  account  = @Account and status = 0 and (Vtype = 'Op' or Vdate < @date )

	-- Return the result of the function
	RETURN @Bal 
END

GO
/****** Object:  UserDefinedFunction [dbo].[Runn_Rate]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[Runn_Rate]
(
@rowid bigint
)
RETURNS decimal(18,2)
AS
BEGIN
	
	DECLARE @Rate decimal(18,2)
	select  @Rate =  sum(qty * rate ) / sum(qty )  
	from (
	select top 10 qtyin - qtyout qty,rate  from ItemTransaction 
	where rowid < @rowid and tranType = 'in' and 
	fkitem = (select it.fkitem from ItemTransaction it where it.rowid = @rowid )  
	 order by rowid desc
	 ) a
	 if(@Rate is null or count(@Rate) = 0) 
	 begin 	 
	  select  @Rate =  sum(qty * rate ) / sum(qty )  
	  from (
	select top 10 qtyin - qtyout qty,rate  from ItemTransaction 
	where rowid > @rowid and tranType = 'in' and 
	fkitem = (select it.fkitem from ItemTransaction it where it.rowid = @rowid )  

	 order by rowid asc
	 ) a
	 end 
	  
	RETURN isnull(@Rate,0)

END

GO
/****** Object:  UserDefinedFunction [dbo].[Runn_stock]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[Runn_stock]
(
@rowid bigint
)
RETURNS decimal(18,2)
AS
BEGIN
	
	DECLARE @Qty decimal(18,2)
	select @Qty = sum(qtyin - qtyout) from ItemTransaction 
	where rowid <= @rowid and 
	fkitem = (select it.fkitem from ItemTransaction it where it.rowid = @rowid )  
	RETURN @Qty

END

GO
/****** Object:  UserDefinedFunction [dbo].[StockBefore]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create FUNCTION [dbo].[StockBefore]
(
	@date date,
    @fkItem varchar(20)
	 
)
RETURNS decimal(18,2)  
AS
BEGIN
	
	DECLARE @Stock decimal(18,2) 

	select @Stock = sum(qtyin  - qtyout)  from ItemTransaction  
	where  fkitem = @fkItem and (Vtype = 'Op' or Vdate < @date )

	-- Return the result of the function
	RETURN @Stock
END

GO
/****** Object:  Table [dbo].[ChartOfAccount]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ChartOfAccount](
	[Account] [varchar](20) NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[parentId] [varchar](20) NOT NULL,
	[AccType] [varchar](50) NOT NULL,
	[AccLevel] [int] NOT NULL,
	[CreatedBy] [varchar](50) NULL,
	[EditBy] [varchar](50) NULL,
	[CreatedTime] [datetime] NULL,
	[EditTime] [datetime] NULL,
	[status] [numeric](1, 0) NOT NULL,
 CONSTRAINT [PK_ChartOfAccount_1] PRIMARY KEY CLUSTERED 
(
	[Account] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[CompanyDetail]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[CompanyDetail](
	[CompanyName] [varchar](150) NOT NULL,
	[UrCompanyName] [nvarchar](250) NULL,
	[Descr] [varchar](200) NULL,
	[Address] [varchar](150) NULL,
	[Phone] [varchar](50) NULL,
	[Cell] [varchar](50) NULL,
	[Cell2] [varchar](50) NULL,
	[ContactHeader] [varchar](150) NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[CustomerDetail]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[CustomerDetail](
	[Code] [varchar](20) NOT NULL,
	[Email] [varchar](50) NULL,
	[Fax] [varchar](50) NULL,
	[CNIC] [varchar](50) NULL,
	[Address] [varchar](150) NULL,
	[Qualification] [varchar](100) NULL,
	[Phone1] [varchar](25) NULL,
	[Phone2] [varchar](25) NULL,
	[SMSNumber] [varchar](50) NULL,
	[IBAN] [varchar](80) NULL,
	[SMSAlert] [numeric](1, 0) NULL,
	[EmailAlert] [numeric](1, 0) NULL,
	[image] [varbinary](max) NULL,
	[Active] [numeric](1, 0) NULL,
	[CreatedBy] [varchar](200) NULL,
	[CreatedTime] [datetime] NULL,
	[EditBy] [varchar](200) NULL,
	[EditTime] [datetime] NULL,
	[status] [numeric](1, 0) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[DefaultAccount]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[DefaultAccount](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Title] [varchar](50) NULL,
	[Account] [varchar](50) NULL,
	[MapAccount] [varchar](50) NULL,
	[a] [varchar](500) NULL,
 CONSTRAINT [PK_DefaultAccount] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[errorlog]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[errorlog](
	[Error] [varchar](max) NULL,
	[sender] [varchar](50) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[GL1]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[GL1](
	[VDate] [date] NOT NULL,
	[Vtime] [time](7) NOT NULL,
	[VoucherNo] [varchar](8) NOT NULL,
	[Vtype] [varchar](15) NOT NULL,
	[Vseq] [int] NOT NULL,
	[DRAccount] [varchar](20) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL CONSTRAINT [DF_GL1_Amount]  DEFAULT ((0)),
	[CRAccount] [varchar](20) NULL,
	[Narration] [varchar](50) NULL,
	[Remarks] [varchar](100) NULL,
	[CheckNum] [varchar](50) NULL,
	[CheckDate] [date] NULL,
	[CheckStatus] [varchar](50) NULL,
	[Clear] [numeric](1, 0) NOT NULL,
	[status] [numeric](1, 0) NOT NULL,
	[CreatedBy] [varchar](50) NOT NULL,
	[CreatedTime] [datetime] NOT NULL,
	[EditBy] [varchar](50) NULL,
	[EditTime] [datetime] NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[HRInfo]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[HRInfo](
	[ID] [varchar](3) NOT NULL,
	[Name] [varchar](150) NOT NULL,
	[FatherName] [varchar](150) NULL,
	[Gender] [varchar](50) NOT NULL,
	[DOB] [date] NOT NULL,
	[MaritialStatus] [varchar](50) NULL,
	[CNIC] [varchar](50) NULL,
	[AppointmentDate] [date] NOT NULL,
	[JoiningDate] [date] NOT NULL,
	[Designation] [varchar](50) NULL,
	[SalaryType] [varchar](50) NOT NULL,
	[Salary] [numeric](18, 0) NOT NULL,
	[LeaveCharges] [numeric](18, 0) NOT NULL,
	[Overtime] [numeric](18, 0) NOT NULL,
	[ExpenseAccount] [varchar](25) NULL,
	[PayableAccount] [varchar](25) NULL,
	[Status] [numeric](1, 0) NOT NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[ItemCatagory]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ItemCatagory](
	[Code] [varchar](3) NOT NULL,
	[Title] [varchar](150) NOT NULL,
	[ItemType] [varchar](50) NULL,
	[Active] [numeric](1, 0) NOT NULL CONSTRAINT [DF_Table_1_HasFlovour]  DEFAULT ((0)),
	[status] [numeric](1, 0) NOT NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[ItemDetail]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ItemDetail](
	[ID] [varchar](10) NOT NULL,
	[fkItemCatagory] [varchar](3) NOT NULL,
	[ItemType] [varchar](20) NULL,
	[Code] [varchar](5) NOT NULL,
	[Barcode] [varchar](150) NULL,
	[Title] [varchar](150) NOT NULL,
	[ItemKey] [varchar](50) NULL,
	[PriRate] [decimal](18, 2) NOT NULL CONSTRAINT [DF_ItemDetail_Rate_1]  DEFAULT ((0)),
	[SecRate] [decimal](18, 2) NOT NULL CONSTRAINT [DF_ItemDetail_PriRate1]  DEFAULT ((0)),
	[PrimaryUnit] [varchar](10) NULL,
	[SecondaryUnit] [varchar](10) NULL,
	[DefaultUnit] [varchar](10) NULL,
	[QtyInPack] [decimal](18, 2) NULL,
	[Alert] [numeric](1, 0) NULL,
	[LowStockAlert] [decimal](18, 2) NULL,
	[OpnStock] [decimal](18, 2) NULL,
	[OpnRate] [decimal](18, 2) NULL,
	[status] [numeric](1, 0) NOT NULL,
 CONSTRAINT [PK_ItemDetail] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Narration]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Narration](
	[Code] [varchar](3) NOT NULL,
	[Title] [nvarchar](150) NOT NULL,
	[status] [numeric](1, 0) NOT NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[newtable]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[newtable](
	[AcCode] [varchar](50) NULL,
	[AcName] [varchar](100) NULL,
	[oDr] [float] NOT NULL,
	[oCr] [float] NOT NULL,
	[Dr] [float] NULL,
	[Cr] [float] NULL,
	[Level 1] [varchar](100) NULL,
	[Level 2] [varchar](100) NULL,
	[Level 3] [varchar](100) NULL,
	[Level 4] [varchar](100) NULL,
	[Level 5] [varchar](100) NULL,
	[Code1] [bigint] NULL,
	[Code2] [bigint] NULL,
	[Code3] [bigint] NULL,
	[Code4] [bigint] NULL,
	[Code5] [bigint] NULL,
	[Code6] [int] NULL,
	[ClsDr] [float] NULL,
	[ClsCr] [float] NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Payroll]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Payroll](
	[VoucherNo] [varchar](15) NOT NULL,
	[Vdate] [date] NOT NULL,
	[SalaryType] [varchar](50) NOT NULL,
	[Description] [varchar](150) NULL,
	[Seq] [bigint] NOT NULL,
	[HRID] [varchar](5) NOT NULL,
	[PayableAccount] [varchar](20) NULL,
	[ExpenseAccount] [varchar](20) NOT NULL,
	[Salary] [numeric](18, 0) NOT NULL,
	[NoOfLeaves] [numeric](18, 0) NOT NULL,
	[LeaveCharges] [numeric](18, 0) NOT NULL,
	[Overtime] [numeric](18, 0) NOT NULL,
	[OvertimeCharges] [numeric](18, 0) NOT NULL,
	[Bonus] [numeric](18, 0) NOT NULL,
	[NetSalary] [numeric](18, 0) NOT NULL,
	[Remarks] [varchar](150) NULL,
	[status] [numeric](1, 0) NOT NULL,
	[CreatedBy] [varchar](50) NOT NULL,
	[CreatedTime] [datetime] NOT NULL,
	[EditBy] [varchar](50) NULL,
	[EditTime] [datetime] NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[PurchaseDetail]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[PurchaseDetail](
	[Vtype] [varchar](3) NOT NULL,
	[Vno] [varchar](8) NOT NULL,
	[seq] [int] NOT NULL,
	[fkItem] [varchar](10) NOT NULL,
	[Unit] [varchar](3) NULL,
	[QtyInPack] [numeric](18, 2) NULL,
	[Qty] [numeric](18, 2) NOT NULL,
	[Rate] [numeric](18, 2) NOT NULL,
	[AddLess] [numeric](18, 2) NOT NULL,
	[status] [int] NOT NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[PurchaseMaster]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[PurchaseMaster](
	[Vdate] [date] NOT NULL,
	[VTime] [time](7) NOT NULL,
	[Vtype] [varchar](3) NOT NULL,
	[Vno] [varchar](8) NOT NULL,
	[fkAccountId] [varchar](20) NOT NULL,
	[Descr] [varchar](300) NULL,
	[Narration] [varchar](5) NULL,
	[Amount] [numeric](18, 2) NULL,
	[CreatedBy] [varchar](200) NOT NULL,
	[CreatedTime] [datetime] NOT NULL,
	[EditBy] [varchar](200) NULL,
	[EditTime] [datetime] NULL,
	[status] [numeric](1, 0) NOT NULL,
	[Counter] [varchar](50) NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[PurchaseRetDetail]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[PurchaseRetDetail](
	[Vtype] [varchar](3) NOT NULL,
	[Vno] [varchar](8) NOT NULL,
	[seq] [int] NOT NULL,
	[fkItem] [varchar](10) NOT NULL,
	[Unit] [varchar](3) NULL,
	[QtyInPack] [numeric](18, 2) NULL,
	[Qty] [numeric](18, 2) NOT NULL,
	[Rate] [numeric](18, 2) NOT NULL,
	[status] [int] NOT NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[PurchaseRetMaster]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[PurchaseRetMaster](
	[Vdate] [date] NOT NULL,
	[VTime] [time](7) NOT NULL,
	[Vtype] [varchar](3) NOT NULL,
	[Vno] [varchar](8) NOT NULL,
	[fkAccountId] [varchar](20) NOT NULL,
	[Descr] [varchar](300) NULL,
	[Narration] [varchar](5) NULL,
	[Amount] [numeric](18, 2) NULL,
	[CreatedBy] [varchar](200) NOT NULL,
	[CreatedTime] [datetime] NOT NULL,
	[EditBy] [varchar](200) NULL,
	[EditTime] [datetime] NULL,
	[status] [numeric](1, 0) NOT NULL,
	[Counter] [varchar](50) NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[ReportNo]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ReportNo](
	[SaleTicket] [bigint] NOT NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[SaleMaster]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SaleMaster](
	[Vdate] [date] NOT NULL,
	[Vtime] [time](7) NOT NULL,
	[Vtype] [varchar](3) NOT NULL,
	[Vno] [varchar](8) NOT NULL,
	[fkAccountId] [varchar](20) NOT NULL,
	[Descr] [varchar](300) NULL,
	[Narration] [varchar](5) NULL,
	[Amount] [numeric](18, 2) NULL,
	[Discount] [numeric](18, 2) NULL,
	[NetAmount] [numeric](18, 2) NULL,
	[CashReceipt] [numeric](18, 2) NOT NULL CONSTRAINT [DF_SaleMaster_ReceiptAmount]  DEFAULT ((0)),
	[CashBack] [numeric](18, 2) NULL,
	[CreatedBy] [varchar](200) NOT NULL,
	[CreatedTime] [datetime] NOT NULL,
	[EditBy] [varchar](200) NULL,
	[EditTime] [datetime] NULL,
	[status] [numeric](1, 0) NOT NULL,
	[Counter] [varchar](50) NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SaleRetDetail]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SaleRetDetail](
	[Vtype] [varchar](3) NOT NULL,
	[Vno] [varchar](8) NOT NULL,
	[Seq] [int] NOT NULL,
	[fkItem] [varchar](10) NOT NULL,
	[Unit] [varchar](3) NULL,
	[QtyInPack] [numeric](18, 2) NULL,
	[Qty] [numeric](18, 2) NOT NULL,
	[GrossRate] [numeric](18, 2) NULL,
	[Rate]  AS ([GrossRate]-[Discount]),
	[Discount] [numeric](18, 2) NULL,
	[status] [int] NOT NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SaleRetMaster]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SaleRetMaster](
	[Vdate] [date] NOT NULL,
	[Vtime] [time](7) NOT NULL,
	[Vtype] [varchar](3) NOT NULL,
	[Vno] [varchar](8) NOT NULL,
	[fkAccountId] [varchar](20) NOT NULL,
	[Descr] [varchar](300) NULL,
	[Narration] [varchar](5) NULL,
	[Amount] [numeric](18, 2) NULL,
	[Discount] [numeric](18, 2) NULL,
	[NetAmount] [numeric](18, 2) NULL,
	[CashReceipt] [numeric](18, 2) NOT NULL,
	[CashBack] [numeric](18, 2) NULL,
	[CreatedBy] [varchar](200) NOT NULL,
	[CreatedTime] [datetime] NOT NULL,
	[EditBy] [varchar](200) NULL,
	[EditTime] [datetime] NULL,
	[status] [numeric](1, 0) NOT NULL,
	[Counter] [varchar](50) NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Sales]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Sales](
	[Vtype] [varchar](3) NOT NULL,
	[Vno] [varchar](8) NOT NULL,
	[Seq] [int] NOT NULL,
	[fkItem] [varchar](10) NOT NULL,
	[Unit] [varchar](3) NULL,
	[QtyInPack] [numeric](18, 2) NULL,
	[Qty] [numeric](18, 2) NOT NULL,
	[GrossRate] [numeric](18, 2) NULL,
	[Rate]  AS ([GrossRate]-[Discount]),
	[Discount] [numeric](18, 2) NULL,
	[status] [int] NOT NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SaleSupplyDetail]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SaleSupplyDetail](
	[Vtype] [varchar](3) NOT NULL,
	[Vno] [varchar](8) NOT NULL,
	[Seq] [int] NOT NULL,
	[fkCustomerId] [varchar](20) NOT NULL,
	[Unit] [varchar](3) NULL,
	[Qty] [numeric](18, 2) NOT NULL,
	[GrossRate] [numeric](18, 2) NULL,
	[Rate]  AS ([GrossRate]-[Discount]),
	[Discount] [numeric](18, 2) NULL,
	[AddLess] [numeric](18, 2) NULL,
	[Amount]  AS ([Qty]*([GrossRate]-[Discount])+isnull([AddLess],(0))) PERSISTED,
	[status] [int] NOT NULL,
 CONSTRAINT [PK_SaleSupplyDetail] PRIMARY KEY CLUSTERED 
(
	[Vtype] ASC,
	[Vno] ASC,
	[Seq] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SaleSupplyMaster]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SaleSupplyMaster](
	[Vdate] [date] NOT NULL,
	[Vtime] [time](7) NOT NULL,
	[Vtype] [varchar](3) NOT NULL,
	[Vno] [varchar](8) NOT NULL,
	[fkItemId] [varchar](20) NOT NULL,
	[Descr] [varchar](300) NULL,
	[Narration] [varchar](5) NULL,
	[Amount] [numeric](18, 2) NULL,
	[Discount] [numeric](18, 2) NULL,
	[NetAmount] [numeric](18, 2) NULL,
	[CreatedBy] [varchar](200) NOT NULL,
	[CreatedTime] [datetime] NOT NULL,
	[EditBy] [varchar](200) NULL,
	[EditTime] [datetime] NULL,
	[status] [numeric](1, 0) NOT NULL,
	[Counter] [varchar](50) NULL,
 CONSTRAINT [PK_SaleSupplyMaster] PRIMARY KEY CLUSTERED 
(
	[Vtype] ASC,
	[Vno] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[StockAdjDetail]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[StockAdjDetail](
	[Vtype] [varchar](5) NOT NULL,
	[Vno] [varchar](6) NOT NULL,
	[seq] [int] NOT NULL,
	[fkCatagory] [varchar](5) NOT NULL,
	[fkItem] [varchar](5) NOT NULL,
	[QtyIn] [numeric](18, 2) NOT NULL,
	[QtyOut] [numeric](18, 2) NOT NULL,
	[Rate] [numeric](18, 2) NOT NULL,
	[status] [int] NOT NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[StockAdjMaster]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[StockAdjMaster](
	[Vdate] [date] NOT NULL,
	[VTime] [time](7) NOT NULL,
	[Vtype] [varchar](5) NOT NULL,
	[Vno] [varchar](6) NOT NULL,
	[Descr] [varchar](300) NULL,
	[Narration] [varchar](5) NULL,
	[CreatedBy] [varchar](200) NOT NULL,
	[CreatedTime] [datetime] NOT NULL,
	[EditBy] [varchar](200) NULL,
	[EditTime] [datetime] NULL,
	[status] [numeric](1, 0) NOT NULL,
	[Terminal] [varchar](50) NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SupplierDetail]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SupplierDetail](
	[Code] [varchar](20) NOT NULL,
	[Email] [varchar](50) NULL,
	[Fax] [varchar](50) NULL,
	[CNIC] [varchar](50) NULL,
	[Address] [varchar](150) NULL,
	[Qualification] [varchar](100) NULL,
	[Phone1] [varchar](25) NULL,
	[Phone2] [varchar](25) NULL,
	[SMSNumber] [varchar](50) NULL,
	[IBAN] [varchar](80) NULL,
	[SMSAlert] [numeric](1, 0) NULL,
	[EmailAlert] [numeric](1, 0) NULL,
	[image] [varbinary](max) NULL,
	[Active] [numeric](1, 0) NULL,
	[ShowInSales] [numeric](1, 0) NULL,
	[CreatedBy] [varchar](200) NULL,
	[CreatedTime] [datetime] NULL,
	[EditBy] [varchar](200) NULL,
	[EditTime] [datetime] NULL,
	[status] [numeric](1, 0) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SupplyOrderDetail]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SupplyOrderDetail](
	[fkSupplyOrderId] [int] NULL,
	[fkCustomerId] [varchar](50) NULL,
	[SortOrder] [int] NULL,
	[Status] [int] NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SupplyOrderMaster]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupplyOrderMaster](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](150) NULL,
	[Status] [int] NULL,
 CONSTRAINT [PK_SupplyOrderMaster] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Units]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Units](
	[Code] [varchar](3) NOT NULL,
	[Title] [varchar](150) NOT NULL,
	[status] [numeric](1, 0) NOT NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Users]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Users](
	[UserId] [varchar](25) NOT NULL,
	[UserName] [varchar](50) NULL,
	[password] [varchar](50) NULL,
	[Lock] [numeric](1, 0) NULL,
	[MultiLogIn] [numeric](1, 0) NULL,
	[Status] [numeric](1, 0) NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  View [dbo].[GL_Detail]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE view  [dbo].[GL_Detail] as 

SELECT [VDate],[Vtime],[VoucherNo],[Vtype],[Vseq],[DRAccount] as account ,CRAccount as particular,[Amount] as DR,0 as CR, [Narration],[Remarks],[CheckNum],
[CheckDate],[CheckStatus],[Clear],[status],[CreatedBy],[CreatedTime],[EditBy],[EditTime] FROM GL1 where Amount != 0 and status  = 0
union all 
SELECT [VDate],[Vtime],[VoucherNo],[Vtype],[Vseq],[CRAccount] as account,DRAccount as particular, 0 as DR,[Amount] as CR,[Narration],[Remarks],[CheckNum],
[CheckDate],[CheckStatus],[Clear],[status],[CreatedBy],[CreatedTime],[EditBy],[EditTime] FROM GL1 where Amount != 0 and status  = 0
--union all
--select vdate,vtime,null,null,1,dbo.Get_Def_Acc('Capital')  as account,'0',case when amount > 0 then amount else 0 end ,
--case when amount < 0 then amount else 0 end ,null,null,null,null,null,1,0,'Auto',null,null,null  from (
--SELECT dbo.LastDateOfMonth(max(vdate)) vdate,max([Vtime]) vtime, dbo.Get_MonthlyIncome(month(vdate),year(vdate))  as amount  FROM GL1
--group by month(vdate),year(vdate)) a

GO
/****** Object:  View [dbo].[ItemTransaction]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE view  [dbo].[ItemTransaction] as 

select item.*,itd.DefaultUnit ,(case when item.Unit = itd.PrimaryUnit then PUqty else PUqty * item.QtyInPack end ) QtyIn --,dbo.Runn_stock(item.rowid) StockBal ,case when tranType = 'out' then  dbo.Runn_Rate(item.rowid) else rate end  Costrate 
,(case when item.Unit = itd.PrimaryUnit then SLqty else SLqty  * item.QtyInPack end ) QtyOut
from (
		select ROW_NUMBER() over (order by vdate,vtime) rowid,
		* from (
					select 
					'30 Jun 2019' Vdate,null vtime,'Op' vtype,'1' vno,1 seq,'in' tranType,null accountid,
					ID fkitem,dbo.ItemTitle(ID) ItemTitle,opnstock as PUqty,
					0 as SLqty,
					defaultunit unit,QtyInPack,opnrate rate,opnstock * opnrate as amount,'' Counter 
					 from ItemDetail where  status = 0 					 
					union all
					select 
					Vdate,vtime,sd.vtype,sd.vno,seq,'out' tranType,sm.fkAccountId ,
					fkitem,dbo.ItemTitle(fkitem) ItemTitle,0 as PUqty,
					qty as SLqty,
					unit,QtyInPack,rate,qty * rate as amount,Counter 
					 from Sales sd,SaleMaster sm where sd.Vno = sm.vno and sd.Vtype = sm.Vtype  and  sd.status = 0 and sm.status = 0
					union all
					select 
					Vdate,vtime,ssd.vtype,ssd.vno,seq,'out' tranType,ssd.fkCustomerId ,
					fkItemId,dbo.ItemTitle(fkItemId) ItemTitle,0 as PUqty,
					qty as SLqty,
					unit,0 QtyInPack,rate,qty * rate as amount,Counter 
					 from SaleSupplyDetail ssd,SaleSupplyMaster ssm where ssd.Vno = ssm.vno and ssd.Vtype = ssm.Vtype  and  ssd.status = 0 and ssm.status = 0 
					union all
					select 
					Vdate,vtime,pd.vtype,pd.vno,seq,'in' tranType,pm.fkAccountId,
					fkitem,dbo.ItemTitle(fkitem) ItemTitle,qty as qtyPU,0 as qtySL,
					unit,QtyInPack,
					 rate,qty * rate as amount,Counter 
					 from saleretdetail  pd,saleretMaster  pm where pd.Vno = pm.vno and pd.Vtype = pm.Vtype  and  pd.status = 0 
					  union all
					select 
					Vdate,vtime,pd.vtype,pd.vno,seq,'in' tranType,pm.fkAccountId,
					fkitem,dbo.ItemTitle(fkitem) ItemTitle,qty as qtyPU,0 as qtySL,
					unit,QtyInPack,
					 rate,qty * rate as amount,Counter 
					 from PurchaseDetail  pd,PurchaseMaster  pm where pd.Vno = pm.vno and pd.Vtype = pm.Vtype  and  pd.status = 0 
					   union all
					select 
					Vdate,vtime,pd.vtype,pd.vno,seq,'out' tranType,pm.fkAccountId,
					fkitem,dbo.ItemTitle(fkitem) ItemTitle,0 as qtyPU,qty as qtySL,
					unit,QtyInPack,
					 rate,qty * rate as amount,Counter 
					 from PurchaseRetDetail  pd,PurchaseRetMaster  pm where pd.Vno = pm.vno and pd.Vtype = pm.Vtype  and  pd.status = 0 
					
			) item) item left join 
 ItemDetail itd on itd.ID = item .fkItem


GO
/****** Object:  View [dbo].[vu_ChartofAccount]    Script Date: 4/9/2026 12:45:52 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE view  [dbo].[vu_ChartofAccount] as
select dbo.Get_parentAcc(dbo.Get_parentAcc(dbo.Get_parentAcc(parentId ) )  )  as  lvl1,
dbo.Get_Title(dbo.Get_parentAcc(dbo.Get_parentAcc(dbo.Get_parentAcc(parentId ))))  as  lvl1title,
dbo.Get_parentAcc(dbo.Get_parentAcc(parentId )  )  as  lvl2,
dbo.Get_Title(dbo.Get_parentAcc(dbo.Get_parentAcc(parentId )))  as  lvl2title,
dbo.Get_parentAcc(parentId )  as  lvl3,
dbo.Get_Title(dbo.Get_parentAcc(parentId ))  as  lvl3title,
parentId as lvl4,
dbo.Get_Title(parentId )  as  lvl4title,
Account as lvl5,
Title as lvl5title  
from chartofaccount where AccLevel = 5 and status = 0

GO
ALTER TABLE [dbo].[PurchaseDetail] ADD  CONSTRAINT [DF_PurchaseDetail_AddLess]  DEFAULT ((0)) FOR [AddLess]
GO
ALTER TABLE [dbo].[SaleRetMaster] ADD  CONSTRAINT [DF_SaleRetMaster_ReceiptAmount]  DEFAULT ((0)) FOR [CashReceipt]
GO
ALTER TABLE [dbo].[SaleSupplyDetail]  WITH CHECK ADD  CONSTRAINT [FK_SaleSupplyDetail_SaleSupplyMaster] FOREIGN KEY([Vtype], [Vno])
REFERENCES [dbo].[SaleSupplyMaster] ([Vtype], [Vno])
GO
ALTER TABLE [dbo].[SaleSupplyDetail] CHECK CONSTRAINT [FK_SaleSupplyDetail_SaleSupplyMaster]
GO
USE [master]
GO
ALTER DATABASE [BG_Choudary_MR] SET  READ_WRITE 
GO
