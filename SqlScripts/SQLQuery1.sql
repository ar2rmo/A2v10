USE [Segment1_master]
GO
/****** Object:  StoredProcedure [a2].[Entity.Index]    Script Date: 17.10.2018 16:29:13 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
------------------------------------------------
ALTER   procedure [a2].[Entity.Index]
	@TenantId   int,
	@UserId     bigint,
	@Id         bigint = null,
	@Offset     int = 0,
	@PageSize   int = 20,
	@Order      nvarchar(255) = N'Name',
	@Dir        nvarchar(20) = N'asc',
	@BrowseKind nvarchar(255) = null,
	@Kind       nchar(4) = N'ALL_',
	@Warehouse  bigint = null,
	@PriceKind  bigint = null,
	@Date       datetime = null,
	@Fragment   nvarchar(255) = null
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;

	if @TenantId = 119
		set @TenantId = 44444;

	declare @PriceKinds table (PriceList bigint, Main bit, [No] int, [Name] nvarchar(255));

	exec a2.[Entity.List] @TenantId, @UserId, @Id, @Offset, @PageSize, @Order, @Dir, @BrowseKind, @Kind output, @Warehouse, @PriceKind, @Date, @Fragment;

	---
	select
		[Operations!TOperation!Array] = null,
		[Id!!Id] = Id,
		[Name!!Name] = [Name],
		Category = isnull(Category, N'@[Other]'),
		[Url],
		[Order] = coalesce([Order], 0)
	from a2.Operations
	where TenantId = @TenantId and ForEntity = 1

	union all

	select null, N'ALL_', N'<@[AllOperations]>', null, null, -1
	order by [Order];

	---
	insert into @PriceKinds (PriceList, Main, [No], [Name])
	select PriceList, PL.Main, row_number() over (partition by PriceList order by PK.Main desc, PK.[Name]), PK.[Name]
	from
		a2.PriceLists as PL
		inner join a2.PriceKinds as PK on PL.TenantId = PK.TenantId and PL.Id = PK.PriceList
	where PL.TenantId = @TenantId and PL.Void = 0 and PK.Void = 0;

	select [PriceLists!TPriceList!Array] = null, [Id!!Id] = PriceList, Main, Name1 = [1], Name2 = [2], Name3 = [3], Name4 = [4], Name5 = [5]
	from 
		(
			select PriceList, Main, [No], [Name]
			from @PriceKinds
		) as SourceTable  
		pivot
		(  
			max([Name])
			for [No] in ([1], [2], [3], [4], [5])  
		) as PivotTable;

	---
	select
		[!TDocument!Array] = null,
		[Id!!Id] = D.Id,
		[Date],
		[No],
		D.Memo,
		[Sum],
		VATSum,
		D.TotalSum,
		Done,
		[Operation.Id!TOperation!Id] = O.Id,
		[Operation.Name!TOperation!Name] = O.[Name],
		[Operation.Url!TOperation!] = O.[Url],
		[Agent.Id!TAgent!Id] = D.Agent,
		[Agent.Name!TAgent!Name] = A.[Name]
	from
		a2.Documents as D
		inner join a2.Operations as O on D.TenantId = O.TenantId and D.Operation = O.Id
		inner join a2.Agents as A     on D.TenantId = A.TenantId and D.Agent     = A.Id
	where 0 <> 0;

	---
	select
		[!TPrice!Array] = null,
		[Id!!Id] = Id,
		[Date],
		Value1 = [Value],
		Value2 = [Value],
		Value3 = [Value],
		Value4 = [Value],
		Value5 = [Value]
	from a2.Prices as P
	where 0 <> 0

	---
	select
		[!$System!]                   = null, 
		[!Entities!Offset]            = @Offset,
		[!Entities!PageSize]          = @PageSize,
		[!Entities!SortOrder]         = @Order, 
		[!Entities!SortDir]           = @Dir,
		[!Entities.BrowseKind!Filter] = @BrowseKind,
		[!Entities.Kind!Filter]       = @Kind,
		[!Entities.Fragment!Filter]   = @Fragment,
		[!Entities!HasRows] = case when exists(select * from a2.Entities where TenantId=@TenantId and Id <> 0) then 1 else 0 end;
end

go
exec [a2].[Entity.Index] 119, 119

--update a2security.Tenants set TrialPeriodExpired = N'20181020' where Id=119
