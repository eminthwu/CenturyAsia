<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="CenturyAsia._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <br />


    <%
        foreach (var date in NeedDates)
        {
            this.GetRooms(date);

    %>

    <table border="1">
        <caption><%=date.ToString("MM/dd") %></caption>
        <tr>
            <th>廳</th>
            <th>Data</th>
        </tr>

        <%        
            foreach (var room in this.NeedRooms)
            {
        %>
        <tr>
            <td><%=room.Id%></td>
            <td>
                <table border="1">
                    <tr>
                        <th style="width: 300px">Movie</th>
                        <th>時間</th>
                    </tr>
                    <%
                        foreach (var movie in room.TimeTable)
                        {
                    %>
                    <tr>
                        <td><%=movie.Key %></td>
                        <td><%=string.Join(",", movie.Value.Select(t=>t.ToString("HH:mm"))) %></td>
                    </tr>
                    <%
                        }
                    %>
                </table>
            </td>

        </tr>
        <%
            }
        %>
    </table>
    <%
        }
    %>
</asp:Content>
