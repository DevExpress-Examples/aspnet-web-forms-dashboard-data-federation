<%@ Page Language="vb" AutoEventWireup="true" MasterPageFile="~/Main.Master" CodeBehind="Default.aspx.vb" Inherits="AspNetWebFormsDataFederation.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
<script type="text/javascript">
	function onBeforeRender(sender) {
		var control = sender.GetDashboardControl();
		control.registerExtension(new DevExpress.Dashboard.DashboardPanelExtension(control, { dashboardThumbnail: "./Content/DashboardThumbnail/{0}.png" }));
	}
</script>
	<dx:ASPxDashboard ID="ASPxDashboard1" runat="server" Width="100%" Height="100%" 
		OnDataLoading="DataLoading" OnConfigureDataConnection="ASPxDashboard1_ConfigureDataConnection">
		<ClientSideEvents BeforeRender="onBeforeRender" />
	</dx:ASPxDashboard>
</asp:Content>