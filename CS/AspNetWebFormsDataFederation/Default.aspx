<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/Main.Master" CodeBehind="Default.aspx.cs" Inherits="AspNetWebFormsDataFederation.Default" %>
<%@ Register Assembly="DevExpress.Dashboard.v20.2.Web.WebForms, Version=20.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.DashboardWeb" TagPrefix="dx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
<script type="text/javascript">
    function onBeforeRender(sender) {
        var control = sender.GetDashboardControl();
        control.registerExtension(new DevExpress.Dashboard.DashboardPanelExtension(control, { dashboardThumbnail: "./Content/DashboardThumbnail/{0}.png" }));
    }
</script>
    <dx:ASPxDashboard ID="ASPxDashboard1" runat="server" Width="100%" Height="100%" ondataloading="DataLoading">
        <ClientSideEvents BeforeRender="onBeforeRender" />
    </dx:ASPxDashboard>
</asp:Content>