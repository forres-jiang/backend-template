namespace My.XXX.Service.Common
{
    public class EmailTemplate
    {
        public static readonly string Exception = @"<!DOCTYPE html>
<html lang=""en"">
  <head>
    <meta charset=""UTF-8"" />
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <meta name=""viewport"" content=""width=<device-width>, initial-scale=1.0"" />
    <title>Document</title>
  </head>
  <body>
    <p>Time: {0}</p>
    <p>AppCode: {1}</p>
    <p>HostName: {2} </p>
    <p>ControllerAction: {3}</p>
    <p style=""font-weight:bold;"">Message: {4}</p>
    <p style=""font-weight:bold;"">StackTrace: <br /> {5}</p>
  </body>
</html>";
    }
}