Imports System
Imports System.Web.Hosting
Imports DevExpress.DashboardCommon
Imports DevExpress.DashboardWeb
Imports DevExpress.DataAccess.DataFederation
Imports DevExpress.DataAccess.Excel
Imports DevExpress.DataAccess.Json
Imports DevExpress.DataAccess.Sql

Namespace AspNetWebFormsDataFederation
	Partial Public Class [Default]
		Inherits System.Web.UI.Page

		Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
			Dim dashboardFileStorage As New DashboardFileStorage("~/App_Data/Dashboards")
			ASPxDashboard1.SetDashboardStorage(dashboardFileStorage)

			' Uncomment this string to allow end users to create new data sources based on predefined connection strings.
			'ASPxDashboard1.SetConnectionStringsProvider(new DevExpress.DataAccess.Web.ConfigFileConnectionStringsProvider());

			Dim dataSourceStorage As New DataSourceInMemoryStorage()

			' Configures an SQL data source.
			Dim sqlDataSource As New DashboardSqlDataSource("SQL Data Source", "NWindConnectionString")
			Dim query As SelectQuery = SelectQueryFluentBuilder.AddTable("Orders").SelectAllColumnsFromTable().Build("SQL Orders")
			sqlDataSource.Queries.Add(query)

			' Configures an Object data source.
			Dim objDataSource As New DashboardObjectDataSource("Object Data Source")
			objDataSource.DataId = "odsInvoices"

			' Configures an Excel data source.
			Dim excelDataSource As New DashboardExcelDataSource("Excel Data Source")
			excelDataSource.ConnectionName = "excelSales"
			excelDataSource.FileName = HostingEnvironment.MapPath("~/App_Data/SalesPerson.xlsx")
			excelDataSource.SourceOptions = New ExcelSourceOptions(New ExcelWorksheetSettings("Data"))

			' Configures a JSON data source.
			Dim jsonDataSource As New DashboardJsonDataSource("JSON Data Source")
			jsonDataSource.ConnectionName = "jsonCategories"
			Dim fileUri As New Uri(HostingEnvironment.MapPath("~/App_Data/Categories.json"), UriKind.RelativeOrAbsolute)
			jsonDataSource.JsonSource = New UriJsonSource(fileUri)

			' Registers a Federated data source.
			dataSourceStorage.RegisterDataSource("federatedDataSource", CreateFederatedDataSource(sqlDataSource, excelDataSource, objDataSource, jsonDataSource).SaveToXml())

			ASPxDashboard1.SetDataSourceStorage(dataSourceStorage)
		End Sub

		Protected Sub ASPxDashboard1_ConfigureDataConnection(ByVal sender As Object, ByVal e As ConfigureDataConnectionWebEventArgs)
			If e.ConnectionName = "excelSales" Then
				TryCast(e.ConnectionParameters, ExcelDataSourceConnectionParameters).FileName = HostingEnvironment.MapPath("~/App_Data/SalesPerson.xlsx")
			ElseIf e.ConnectionName = "jsonCategories" Then
				e.ConnectionParameters = New JsonSourceConnectionParameters() With {.JsonSource = New UriJsonSource(New Uri(HostingEnvironment.MapPath("~/App_Data/Categories.json"), UriKind.RelativeOrAbsolute))}
			End If
		End Sub

		Protected Sub DataLoading(ByVal sender As Object, ByVal e As DataLoadingWebEventArgs)
			If e.DataId = "odsInvoices" Then
				e.Data = Invoices.CreateData()
			End If
		End Sub

		Private Shared Function CreateFederatedDataSource(ByVal sqlDS As DashboardSqlDataSource, ByVal excelDS As DashboardExcelDataSource, ByVal objDS As DashboardObjectDataSource, ByVal jsonDS As DashboardJsonDataSource) As DashboardFederationDataSource
			Dim federationDataSource As New DashboardFederationDataSource("Federated Data Source")

			Dim sqlSource As New Source("sqlSource", sqlDS, "SQL Orders")
			Dim excelSource As New Source("excelSource", excelDS, "")
			Dim objectSource As New Source("objectSource", objDS, "")
			Dim jsonSourceNode As New SourceNode(New Source("json", jsonDS, ""))

			' Join
			Dim joinQuery As SelectNode = sqlSource.From().Select("OrderDate", "ShipCity", "ShipCountry").Join(excelSource, "[excelSource.OrderID] = [sqlSource.OrderID]").Select("CategoryName", "ProductName", "Extended Price").Join(objectSource, "[objectSource.Country] = [excelSource.Country]").Select("Country", "UnitPrice").Build("Join query")
			federationDataSource.Queries.Add(joinQuery)

			' Union and UnionAll
			Dim queryUnionAll As UnionNode = sqlSource.From().Select("OrderID", "OrderDate").Build("OrdersSqlite").UnionAll(excelSource.From().Select("OrderID", "OrderDate").Build("OrdersExcel")).Build("OrdersUnionAll")
			queryUnionAll.Alias = "Union query"

			Dim queryUnion As UnionNode = sqlSource.From().Select("OrderID", "OrderDate").Build("OrdersSqlite").Union(excelSource.From().Select("OrderID", "OrderDate").Build("OrdersExcel")).Build("OrdersUnion")
			queryUnion.Alias = "UnionAll query"

			federationDataSource.Queries.Add(queryUnionAll)
			federationDataSource.Queries.Add(queryUnion)

			' Transformation

			Dim unfoldRule = New TransformationRule
			With unfoldRule
				.ColumnName = "Products"
				.Unfold = True
				.Flatten = False
			End With
			Dim unfoldNode As New TransformationNode(jsonSourceNode)
			With unfoldNode
				.Alias = "Unfold"
				.Rules.Add(unfoldRule)
			End With

			Dim unfoldFlattenRule = New TransformationRule
			With unfoldFlattenRule
				.ColumnName = "Products"
				.Alias = "Product"
				.Unfold = True
				.Flatten = True
			End With
			Dim unfoldFlattenNode As New TransformationNode(jsonSourceNode)
			With unfoldFlattenNode
				.Alias = "Unfold and Flatten"
				.Rules.Add(unfoldFlattenRule)
			End With

			federationDataSource.Queries.Add(unfoldNode)
			federationDataSource.Queries.Add(unfoldFlattenNode)

			Return federationDataSource
		End Function
	End Class
End Namespace