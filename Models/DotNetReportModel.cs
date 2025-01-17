﻿using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ReportBuilder.Web.Models
{
    public class DotNetReportModel
    {
        public int ReportId { get; set; }
        public string ReportName { get; set; }
        public string ReportDescription { get; set; }
        public string ReportSql { get; set; }

        public bool IncludeSubTotals { get; set; }
        public bool ShowUniqueRecords { get; set; }
        public string ReportFilter { get; set; }
        public string ReportType { get; set; }
        public bool ShowDataWithGraph { get; set; }

        public string ConnectKey { get; set; }
        public bool IsDashboard { get; set; }
        public int SelectedFolder { get; set; }
    }

    public class DotNetReportResultModel
    {
        public string ReportSql { get; set; }
        public DotNetReportDataModel ReportData { get; set; }
        public DotNetReportPagerModel Pager { get; set; }

        public bool HasError { get; set; }
        public string Exception { get; set; }
        public string Warnings { get; set; }

        public bool ReportDebug { get; set; }
    }

    public class DotNetReportPagerModel
    {
        public int CurrentPage { get; set; }
        public int TotalRecords { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class DotNetReportDataColumnModel
    {
        public string SqlField { get; set; }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsNumeric { get; set; }
    }

    public class DotNetReportDataRowItemModel
    {
        public string Value { get; set; }
        public string FormattedValue { get; set; }
        public string LabelValue { get; set; }
        public double? NumericValue { get; set; }
        public DotNetReportDataColumnModel Column { get; set; }

    }

    public class DotNetReportDataRowModel
    {
        public DotNetReportDataRowItemModel[] Items { get; set; }
    }

    public class DotNetReportDataModel
    {
        public List<DotNetReportDataRowModel> Rows { get; set; }
        public List<DotNetReportDataColumnModel> Columns { get; set; }
    }

    public class TableViewModel
    {
        public int Id { get; set; }
        public string TableName { get; set; }
        public string DisplayName { get; set; }
        public bool Selected { get; set; }
        public bool IsView { get; set; }
        public int DisplayOrder { get; set; }
        public string AccountIdField { get; set; }

        public List<ColumnViewModel> Columns { get; set; }
    }

    public class RelationModel
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public int JoinedTableId { get; set; }
        public string JoinType { get; set; }
        public string FieldName { get; set; }
        public string JoinFieldName { get; set; }
    }

    public enum FieldTypes
    {
        Boolean,
        DateTime,
        Varchar,
        Money,
        Int,
        Double
    }

    public enum JoinTypes
    {
        Inner,
        Left,
        Right
    }

    public class ColumnViewModel
    {
        public int Id { get; set; }
        public string ColumnName { get; set; }
        public string DisplayName { get; set; }
        public bool Selected { get; set; }

        public int DisplayOrder { get; set; }

        public string FieldType { get; set; }
        public bool PrimaryKey { get; set; }

        public bool ForeignKey { get; set; }

        public bool AccountIdField { get; set; }

        public string ForeignTable { get; set; }

        public JoinTypes ForeignJoin { get; set; }

        public string ForeignKeyField { get; set; }

        public string ForeignValueField { get; set; }
        public bool DoNotDisplay { get; set; }
    }

    public class ConnectViewModel
    {
        public string Provider { get; set; }
        public string ServerName { get; set; }
        public string InitialCatalog { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IntegratedSecurity { get; set; }


        public string AccountApiKey { get; set; }
        public string DatabaseApiKey { get; set; }
    }

    public class ManageViewModel
    {
        public string AccountApiKey { get; set; }
        public string DatabaseApiKey { get; set; }

        public List<TableViewModel> Tables { get; set; }
    }

    public class DotNetReportHelper
    {
        public static byte[] GetExcelFile(string reportSql, string connectKey, string reportName)
        {
            var sql = Decrypt(reportSql);

            // Execute sql
            var dt = new DataTable();
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[connectKey].ConnectionString))
            {
                conn.Open();
                var command = new SqlCommand(sql, conn);
                var adapter = new SqlDataAdapter(command);

                adapter.Fill(dt);
            }

            using (ExcelPackage xp = new ExcelPackage())
            {

                ExcelWorksheet ws = xp.Workbook.Worksheets.Add(reportName);

                int rowstart = 1;
                int colstart = 1;
                int rowend = rowstart;
                int colend = dt.Columns.Count;

                ws.Cells[rowstart, colstart, rowend, colend].Merge = true;
                ws.Cells[rowstart, colstart, rowend, colend].Value = reportName;
                ws.Cells[rowstart, colstart, rowend, colend].Style.Font.Bold = true;
                ws.Cells[rowstart, colstart, rowend, colend].Style.Font.Size = 14;

                rowstart += 2;
                rowend = rowstart + dt.Rows.Count;
                ws.Cells[rowstart, colstart].LoadFromDataTable(dt, true);
                ws.Cells[rowstart, colstart, rowstart, colend].Style.Font.Bold = true;

                int i = 1;
                foreach (DataColumn dc in dt.Columns)
                {
                    if (dc.DataType == typeof(decimal))
                        ws.Column(i).Style.Numberformat.Format = "#0.00";

                    if (dc.DataType == typeof(DateTime))
                        ws.Column(i).Style.Numberformat.Format = "dd/mm/yyyy";

                    i++;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                return xp.GetAsByteArray();
            }
        }

        /// <summary>
        /// Method to Deycrypt encrypted sql statement. PLESE DO NOT CHANGE THIS METHOD
        /// </summary>
        public static string Decrypt(string encryptedText)
        {

            byte[] initVectorBytes = Encoding.ASCII.GetBytes("yk0z8f39lgpu70gi"); // PLESE DO NOT CHANGE THIS KEY
            int keysize = 256;

            byte[] cipherTextBytes = Convert.FromBase64String(encryptedText.Replace("%3D", "="));
            var passPhrase = ConfigurationManager.AppSettings["dotNetReport.privateApiToken"].ToLower();
            using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
            {
                byte[] keyBytes = password.GetBytes(keysize / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }
    }
}
