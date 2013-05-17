<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Main.aspx.cs" Inherits="XMLParser._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
    <style type="text/css">
        #Text2
        {
            height: 178px;
            width: 796px;
        }
    </style>
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <input id="Text1" type="text" runat="server" />
    <br />
    <asp:Button ID="Button1" Text="Go" runat="server" OnClick="Button1_Click" />
    <br />
    <asp:Label ID="Label1" runat="server" Text=""></asp:Label>
</asp:Content>
