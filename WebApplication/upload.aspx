<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="upload.aspx.cs" Inherits="WebApplication.WebForm1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <div id="body">

        <section class="main-content clear-fix">

            <form name="form1" method="post" enctype="multipart/form-data" action="api/upload">
                <fieldset>
                <div>
                    <label for="image1">Image File</label>
                    <input name="image1" type="file" />
                </div>
                <div>
                    <input type="submit" value="Submit" />
                </div>
                    </fieldset>
            </form>
        </section>
    </div>

</body>
</html>
