Imports System
Imports System.Web.Hosting
Imports DevExpress.DashboardCommon
Imports DevExpress.DashboardWeb
Imports DevExpress.DataAccess.DataFederation
Imports DevExpress.DataAccess.Excel
Imports DevExpress.DataAccess.Json
Imports DevExpress.DataAccess.Sql

Namespace AspNetWebFormsDataFederation

    Public Partial Class [Default]
        Inherits Web.UI.Page

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
            Dim dashboardFileStorage As DashboardFileStorage = New DashboardFileStorage("~/App_Data/Dashboards")
            ASPxDashboard1.SetDashboardStorage(dashboardFileStorage)
            ' Uncomment this string to allow end users to create new data sources based on predefined connection strings.
            'ASPxDashboard1.SetConnectionStringsProvider(new DevExpress.DataAccess.Web.ConfigFileConnectionStringsProvider());
            Dim dataSourceStorage As DataSourceInMemoryStorage = New DataSourceInMemoryStorage()
            ' Configures an SQL data source.
            Dim sqlDataSource As DashboardSqlDataSource = New DashboardSqlDataSource("SQL Data Source", "NWindConnectionString")
            Dim query As SelectQuery = SelectQueryFluentBuilder.AddTable("Orders").SelectAllColumnsFromTable().Build("SQL Orders")
            sqlDataSource.Queries.Add(query)
            ' Configures an Object data source.
            Dim objDataSource As DashboardObjectDataSource = New DashboardObjectDataSource("Object Data Source")
            objDataSource.DataId = "odsInvoices"
            ' Configures an Excel data source.
            Dim excelDataSource As DashboardExcelDataSource = New DashboardExcelDataSource("Excel Data Source")
            excelDataSource.ConnectionName = "excelSales"
            excelDataSource.FileName = HostingEnvironment.MapPath("~/App_Data/SalesPerson.xlsx")
            excelDataSource.SourceOptions = New ExcelSourceOptions(New ExcelWorksheetSettings("Data"))
            ' Configures a JSON data source.
            Dim jsonDataSource As DashboardJsonDataSource = New DashboardJsonDataSource("JSON Data Source")
            jsonDataSource.ConnectionName = "jsonCategories"
            Dim fileUri As Uri = New Uri(HostingEnvironment.MapPath("~/App_Data/Categories.json"), UriKind.RelativeOrAbsolute)
            jsonDataSource.JsonSource = New UriJsonSource(fileUri)
            ' Registers a Federated data source.
            dataSourceStorage.RegisterDataSource("federatedDataSource", CreateFederatedDataSource(sqlDataSource, excelDataSource, objDataSource, jsonDataSource).SaveToXml())
            ASPxDashboard1.SetDataSourceStorage(dataSourceStorage)
        End Sub

        Protected Sub ASPxDashboard1_ConfigureDataConnection(ByVal sender As Object, ByVal e As ConfigureDataConnectionWebEventArgs)
            If Equals(e.ConnectionName, "excelSales") Then
                TryCast(e.ConnectionParameters, ExcelDataSourceConnectionParameters).FileName = HostingEnvironment.MapPath("~/App_Data/SalesPerson.xlsx")
            ElseIf Equals(e.ConnectionName, "jsonCategories") Then
                e.ConnectionParameters = New JsonSourceConnectionParameters() With {.JsonSource = New UriJsonSource(New Uri(HostingEnvironment.MapPath("~/App_Data/Categories.json"), UriKind.RelativeOrAbsolute))}
            End If
        End Sub

        Protected Sub DataLoading(ByVal sender As Object, ByVal e As DataLoadingWebEventArgs)
            If Equals(e.DataId, "odsInvoices") Then
                e.Data = Invoices.CreateData()
            End If
        End Sub

        Private Shared Function CreateFederatedDataSource(ByVal sqlDS As DashboardSqlDataSource, ByVal excelDS As DashboardExcelDataSource, ByVal objDS As DashboardObjectDataSource, ByVal jsonDS As DashboardJsonDataSource) As DashboardFederationDataSource
            Dim federationDataSource As DashboardFederationDataSource = New DashboardFederationDataSource("Federated Data Source")
            Dim sqlSource As Source = New Source("sqlSource", sqlDS, "SQL Orders")
            Dim excelSource As Source = New Source("excelSource", excelDS, "")
            Dim objectSource As Source = New Source("objectSource", objDS, "")
            Dim jsonSourceNode As SourceNode = New SourceNode(New Source("json", jsonDS, ""))
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
            Dim unfoldNode As TransformationNode = New TransformationNode(jsonSourceNode) With {.[Alias] = "Unfold"}
            Dim unfoldFlattenNode As TransformationNode = New TransformationNode(jsonSourceNode) With {.[Alias] = "Unfold and Flatten"}
            federationDataSource.Queries.Add(unfoldNode)
            federationDataSource.Queries.Add(unfoldFlattenNode)
            Return federationDataSource
        End Function
    End Class
End Namespace
