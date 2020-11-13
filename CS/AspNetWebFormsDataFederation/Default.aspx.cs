using DevExpress.DashboardCommon;
using DevExpress.DashboardWeb;
using DevExpress.DataAccess.DataFederation;
using DevExpress.DataAccess.Excel;
using DevExpress.DataAccess.Json;
using DevExpress.DataAccess.Sql;
using System;
using System.Web.Hosting;

namespace AspNetWebFormsDataFederation {
    public partial class Default : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            DashboardFileStorage dashboardFileStorage = new DashboardFileStorage("~/App_Data/Dashboards");
            ASPxDashboard1.SetDashboardStorage(dashboardFileStorage);

            // Uncomment this string to allow end users to create new data sources based on predefined connection strings.
            //ASPxDashboard1.SetConnectionStringsProvider(new DevExpress.DataAccess.Web.ConfigFileConnectionStringsProvider());
            
            DataSourceInMemoryStorage dataSourceStorage = new DataSourceInMemoryStorage();

            // Configures an SQL data source.
            DashboardSqlDataSource sqlDataSource = new DashboardSqlDataSource("SQL Data Source", "NWindConnectionString");
            SelectQuery query = SelectQueryFluentBuilder
                .AddTable("Orders")
                .SelectAllColumnsFromTable()
                .Build("SQL Orders");
            sqlDataSource.Queries.Add(query);

            // Configures an Object data source.
            DashboardObjectDataSource objDataSource = new DashboardObjectDataSource("Object Data Source");

            // Configures an Excel data source.
            DashboardExcelDataSource excelDataSource = new DashboardExcelDataSource("Excel Data Source");
            excelDataSource.FileName = HostingEnvironment.MapPath(@"~/App_Data/SalesPerson.xlsx");
            excelDataSource.SourceOptions = new ExcelSourceOptions(new ExcelWorksheetSettings("Data"));

            // Configures a JSON data source.
            DashboardJsonDataSource jsonDataSource = new DashboardJsonDataSource("JSON Data Source");
            Uri fileUri = new Uri(HostingEnvironment.MapPath(@"~/App_Data/Categories.json"), UriKind.RelativeOrAbsolute);
            jsonDataSource.JsonSource = new UriJsonSource(fileUri);

            // Registers a Federated data source.
            dataSourceStorage.RegisterDataSource("federatedDataSource", CreateFederatedDataSource(sqlDataSource,
                excelDataSource, objDataSource, jsonDataSource).SaveToXml());

            ASPxDashboard1.SetDataSourceStorage(dataSourceStorage);
        }

        protected void DataLoading(object sender, DataLoadingWebEventArgs e) {
            if(e.DataSourceName == "Object Data Source") {
                e.Data = Invoices.CreateData();
            }
        }

        private static DashboardFederationDataSource CreateFederatedDataSource(DashboardSqlDataSource sqlDS,
            DashboardExcelDataSource excelDS, DashboardObjectDataSource objDS, DashboardJsonDataSource jsonDS) {

            DashboardFederationDataSource federationDataSource = new DashboardFederationDataSource("Federated Data Source");

            Source sqlSource = new Source("sqlSource", sqlDS, "SQL Orders");
            Source excelSource = new Source("excelSource", excelDS, "");
            Source objectSource = new Source("objectSource", objDS, "");
            SourceNode jsonSourceNode = new SourceNode(new Source("json", jsonDS, ""));

            // Join
            SelectNode joinQuery =
            sqlSource.From()
            .Select("OrderDate", "ShipCity", "ShipCountry")
            .Join(excelSource, "[excelSource.OrderID] = [sqlSource.OrderID]")
                .Select("CategoryName", "ProductName", "Extended Price")
                .Join(objectSource, "[objectSource.Country] = [excelSource.Country]")
                    .Select("Country", "UnitPrice")
                    .Build("Join query");
            federationDataSource.Queries.Add(joinQuery);

            // Union and UnionAll
            UnionNode queryUnionAll = sqlSource.From().Select("OrderID", "OrderDate").Build("OrdersSqlite")
                .UnionAll(excelSource.From().Select("OrderID", "OrderDate").Build("OrdersExcel"))
                .Build("OrdersUnionAll");
            queryUnionAll.Alias = "Union query";

            UnionNode queryUnion = sqlSource.From().Select("OrderID", "OrderDate").Build("OrdersSqlite")
                .Union(excelSource.From().Select("OrderID", "OrderDate").Build("OrdersExcel"))
                .Build("OrdersUnion");
            queryUnion.Alias = "UnionAll query";

            federationDataSource.Queries.Add(queryUnionAll);
            federationDataSource.Queries.Add(queryUnion);

            // Transformation
            TransformationNode unfoldNode = new TransformationNode(jsonSourceNode) {
                Alias = "Unfold",
                Rules = { new TransformationRule { ColumnName = "Products", Alias = "Product", Unfold = true, Flatten = false } }
            };

            TransformationNode unfoldFlattenNode = new TransformationNode(jsonSourceNode) {
                Alias = "Unfold and Flatten",
                Rules = { new TransformationRule { ColumnName = "Products", Unfold = true, Flatten = true } }
            };

            federationDataSource.Queries.Add(unfoldNode);
            federationDataSource.Queries.Add(unfoldFlattenNode);

            return federationDataSource;
        }
    }
}