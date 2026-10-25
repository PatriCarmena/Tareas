using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OrderDesk.Web.Services;

namespace OrderDesk.Web.Tests;

[TestClass]
public sealed class CsvExportTests
{
    [TestMethod]
    public void UserText_IsEscapedForExcelAndQuotedForCsv()
    {
        var bytes = CsvExport.Build([new string?[] { "=HYPERLINK(\"https://example.test\")", "Ana; Pérez" }]);
        var csv = Encoding.UTF8.GetString(bytes);
        StringAssert.StartsWith(csv, "\uFEFF\"'=HYPERLINK(\"\"https://example.test\"\")\";\"Ana; Pérez\"");
    }
}
