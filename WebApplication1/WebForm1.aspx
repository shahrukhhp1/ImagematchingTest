<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm1.aspx.cs" Inherits="WebApplication1.WebForm1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        Upload 2 Images you want to match and then click Match Button.
        <br />
        <asp:FileUpload ID="FileUpload1"  AllowMultiple="true" runat="server" />
        <br />
         <asp:Button ID="Button1" runat="server" Text="Match" onclick="Button1_Click"/>

        <div style="width:45%;height:50%;display:inline-block;">
            <asp:Image ID="Image1" Height="250" runat="server" />
            </div>
        
                <div style="width:45%;height:50%;float:right;">
            <asp:Image ID="Image2" Height="250" runat="server" />
            </div>
        

        <%--<asp:Button ID="Button1" runat="server" Text="Browse Img1" OnClick="Button1_Click" />
        <asp:Button ID="Button2" runat="server" Text="Browse Img2" />--%>
    </div>
        Images Matching Ratio is : <asp:Label ID="Label1" runat="server" Text="0%"></asp:Label>
    </form>
</body>
</html>
