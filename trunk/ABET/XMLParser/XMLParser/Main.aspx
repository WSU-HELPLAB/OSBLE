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
    <input id="CoursePlanTitle" type="text" runat="server" />
    <br />
    <input id="webPage_Courses" type="text" runat="server" />
    <br />
    <asp:Button ID="Build" Text="Build" runat="server" OnClick="BuildCourses" />
    <br />
    <asp:Label ID="Label1" runat="server" Text=""></asp:Label>
</asp:Content>
