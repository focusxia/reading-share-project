using book.Common;
using book.Models;
using E6GPS.Framework.Dapper;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace book.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "欢迎使用 ASP.NET MVC!";

            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult BookPage(int pageId)
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            IDbConnection connection = new SqlConnection(conStr);
            connection.Open();

            DynamicParameters dp = new DynamicParameters();
            dp.Add("@PageId", pageId, DbType.Int32);

            SqlMapper.GridReader result = connection.QueryMultiple("prBookPage", dp, commandType: CommandType.StoredProcedure);
            List<Book> bks = new List<Book>();
            Book models1 = result.Read<Book>().FirstOrDefault();
            bks.Add(models1);
            Book models2 = result.Read<Book>().FirstOrDefault();
            bks.Add(models2);
            Book models3 = result.Read<Book>().FirstOrDefault();
            bks.Add(models3);
            Book models4 = result.Read<Book>().FirstOrDefault();
            bks.Add(models4);

            ViewBag.Page = result.Read<FanHobbyInfo>().FirstOrDefault();

            SqlMapper.GridReader result2 = connection.QueryMultiple("GetWXBG", commandType: CommandType.StoredProcedure);
            var list = result2.Read<WXBGModel>().ToList();
            ViewBag.EWX = list[0];
            ViewBag.LJ = list[1];

            connection.Dispose();

            return View(bks);
        }

        [HttpGet]
        public JsonResult GetWXConfig(string url)
        {
            var data = WXJDK.GetWXConfig(url);

            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult DeletePage(string ids, string type)
        {
            try
            {
                var idList = JsonConvert.DeserializeObject<List<string>>(ids);
                var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                using (IDbConnection con = new SqlConnection(conStr))
                {
                    if (type == "all")
                    {
                        con.Execute("DeletePageInfoAll", null, null, null, CommandType.StoredProcedure);
                    }
                    else
                    {
                        if (idList != null && idList.Count() > 0)
                        {
                            foreach (var id in idList)
                            {
                                DynamicParameters dp = new DynamicParameters();
                                dp.Add("@PageId", id, DbType.Int32);
                                con.Execute("DeletePageInfo", dp, null, null, CommandType.StoredProcedure);
                            }
                        }
                        else
                        {
                            return Json(new { isSuccess = false, message = "请您选择要删除的书单" });
                        }
                    }
                }

                return Json(new { isSuccess = true });
            }
            catch (Exception e)
            {
                return Json(new { isSuccess = false, message = e.Message });
            }
        }

        [MyAuthorizeAttribute]
        public ActionResult BookList()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetBookTypeListAjax(int pBookTypeId)
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            IDbConnection connection = new SqlConnection(conStr);
            connection.Open();

            DynamicParameters dp = new DynamicParameters();
            dp.Add("@pBookTypeId", pBookTypeId, DbType.Int32);

            SqlMapper.GridReader result = connection.QueryMultiple("prBookTypeList", dp, commandType: CommandType.StoredProcedure);
            var model = result.Read<BookType>().ToList();

            return Json(new { model }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBookListAjax(int PageIndex = 1, int PageSize = 10)
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            IDbConnection connection = new SqlConnection(conStr);
            connection.Open();

            DynamicParameters dp = new DynamicParameters();
            dp.Add("@PageIndex", PageIndex, DbType.Int32);
            dp.Add("@PageSize", PageSize, DbType.Int32);

            SqlMapper.GridReader result = connection.QueryMultiple("prBookListPage", dp, commandType: CommandType.StoredProcedure);
            var recordCount = result.Read<int>().FirstOrDefault();
            var model = result.Read<Book>().ToList();

            return Json(new { model = model, recordCount = recordCount });
        }

        [MyAuthorizeAttribute]
        public ActionResult BookPageList()
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            IDbConnection connection = new SqlConnection(conStr);
            DynamicParameters dp = new DynamicParameters();
            dp.Add("@Id", 2, DbType.Int32);
            WXBGModel result = connection.Query<WXBGModel>("GetWXBG", dp, commandType: CommandType.StoredProcedure).FirstOrDefault();

            connection.Dispose();
            ViewBag.LJ = result.WXBG;
            return View();
        }

        public JsonResult ImportExcel()
        {
            HttpPostedFileBase file = Request.Files["bookPageExcel"];
            if (file == null)
            {
                return Json(new { isSuccess = false, message = "请选择上传的Excel文件" });
            }
            else
            {
                if (file.FileName.Split('.')[1] == "xls" || file.FileName.Split('.')[1] == "xlsx")
                {
                    //对文件的格式判断，此处省略
                    List<FanHobbyInfo> fanHobbyInfos = new List<FanHobbyInfo>();
                    Stream inputStream = file.InputStream;
                    HSSFWorkbook hssfworkbook = new HSSFWorkbook(inputStream);
                    NPOI.SS.UserModel.ISheet sheet = hssfworkbook.GetSheetAt(0);
                    int rowCount = sheet.LastRowNum;//LastRowNum = PhysicalNumberOfRows - 1

                    for (int i = (sheet.FirstRowNum + 2); i <= rowCount; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        FanHobbyInfo model = new FanHobbyInfo();

                        if (row != null)
                        {
                            model.SerialNumber = row.GetCell(0) != null ? GetCellValue(row.GetCell(0)) : string.Empty;
                            model.MainReadTypeName1 = row.GetCell(1) != null ? GetCellValue(row.GetCell(1)) : string.Empty;
                            model.MainReadTypeName2 = row.GetCell(2) != null ? GetCellValue(row.GetCell(2)) : string.Empty;
                            model.FoodTypeName = row.GetCell(3) != null ? GetCellValue(row.GetCell(3)) : string.Empty;
                            model.FruitsTypeName = row.GetCell(4) != null ? GetCellValue(row.GetCell(4)) : string.Empty;
                            model.SweetTypeName = row.GetCell(5) != null ? GetCellValue(row.GetCell(5)) : string.Empty;
                            model.WXNickName = row.GetCell(6) != null ? GetCellValue(row.GetCell(6)) : string.Empty;
                            model.WXId = row.GetCell(7) != null ? GetCellValue(row.GetCell(7)) : string.Empty;
                            model.Phone = row.GetCell(8) != null ? GetCellValue(row.GetCell(8)) : string.Empty;
                            model.Age = row.GetCell(9) != null ? GetCellValue(row.GetCell(9)) : string.Empty;
                            model.Profession = row.GetCell(10) != null ? GetCellValue(row.GetCell(10)) : string.Empty;
                            model.Education = row.GetCell(11) != null ? GetCellValue(row.GetCell(11)) : string.Empty;
                            //model.WXType = row.GetCell(12) != null ? GetCellValue(row.GetCell(12)) : string.Empty;
                            model.Remark = row.GetCell(12) != null ? GetCellValue(row.GetCell(12)) : string.Empty;
                            model.CreateUser = row.GetCell(13) != null ? GetCellValue(row.GetCell(13)) : string.Empty;
                            model.CreateTime = row.GetCell(14) != null ? GetCellValue(row.GetCell(14)) : string.Empty;
                            model.UpdateTime = row.GetCell(15) != null ? GetCellValue(row.GetCell(15)) : string.Empty;
                            model.Device = row.GetCell(16) != null ? GetCellValue(row.GetCell(16)) : string.Empty;
                            model.System = row.GetCell(17) != null ? GetCellValue(row.GetCell(17)) : string.Empty;
                            model.Browser = row.GetCell(18) != null ? GetCellValue(row.GetCell(18)) : string.Empty;
                            model.IP = row.GetCell(19) != null ? GetCellValue(row.GetCell(19)) : string.Empty;
                        }

                        fanHobbyInfos.Add(model);
                    }

                    var errorMsg = "";
                    var errorCount = 0; var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                    using (IDbConnection con = new SqlConnection(conStr))
                    {
                        foreach (var book in fanHobbyInfos)
                        {
                            try
                            {
                                DynamicParameters dp = new DynamicParameters();
                                dp.Add("@SerialNumber", book.SerialNumber.Trim(), DbType.String);
                                //dp.Add("@WXType", book.WXType.Trim(), DbType.String);
                                dp.Add("@MainReadTypeName1", book.MainReadTypeName1.Trim(), DbType.String);
                                dp.Add("@MainReadTypeName2", book.MainReadTypeName2.Trim(), DbType.String);
                                dp.Add("@FoodTypeName", book.FoodTypeName.Trim(), DbType.String);
                                dp.Add("@FruitsTypeName", book.FruitsTypeName.Trim(), DbType.String);
                                dp.Add("@SweetTypeName", book.SweetTypeName.Trim(), DbType.String);
                                dp.Add("@WXNickName", book.WXNickName.Trim(), DbType.String);
                                dp.Add("@WXId", book.WXId.Trim(), DbType.String);
                                dp.Add("@Phone", book.Phone.Trim(), DbType.String);
                                dp.Add("@Age", book.Age.Trim(), DbType.String);
                                dp.Add("@Profession", book.Profession.Trim(), DbType.String);
                                dp.Add("@Education", book.Education.Trim(), DbType.String);
                                dp.Add("@Remark", book.Remark.Trim(), DbType.String);
                                dp.Add("@CreateUser", book.CreateUser.Trim(), DbType.String);
                                dp.Add("@CreateTime", DateTime.Now, DbType.DateTime);
                                dp.Add("@UpdateTime", book.UpdateTime != "" ? Convert.ToDateTime(book.UpdateTime.Trim()) : DateTime.Now, DbType.DateTime);
                                dp.Add("@Device", book.Device.Trim(), DbType.String);
                                dp.Add("@System", book.System.Trim(), DbType.String);
                                dp.Add("@Browser", book.Browser.Trim(), DbType.String);
                                dp.Add("@IP", book.IP.Trim(), DbType.String);
                                dp.Add("@error", dbType: DbType.Int32, direction: ParameterDirection.Output, size: 1000);
                                dp.Add("@errorMsg", dbType: DbType.String, direction: ParameterDirection.Output, size: 1000);

                                con.Execute("BuildBookPage", dp, null, null, CommandType.StoredProcedure);


                                if (!string.IsNullOrEmpty(dp.Get<string>("@errorMsg")))
                                {
                                    errorMsg += dp.Get<string>("@errorMsg") + "<br />";
                                }

                                //errorCount = errorCount + dp.Get<int>("@error");
                            }
                            catch (Exception e)
                            {
                                errorMsg = "导入失败";
                                errorCount++;
                            }
                        }
                    }
                    return Json(new { isSuccess = (errorCount == 0), message = errorMsg });
                }
                else
                {
                    return Json(new { isSuccess = false, message = "只能上传excel格式的文件" });
                }
            }

        }

        public string EntityListToExcel2003(Dictionary<string, string> cellHeard, IList enList, string sheetName)
        {
            try
            {
                string fileName = sheetName + "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".xls"; // 文件名称
                string urlPath = "UpFiles/ExcelFiles/" + fileName; // 文件下载的URL地址，供给前台下载
                string filePath = System.Web.HttpContext.Current.Server.MapPath("\\" + urlPath); // 文件路径

                // 1.检测是否存在文件夹，若不存在就建立个文件夹
                string directoryName = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                // 2.解析单元格头部，设置单元头的中文名称
                HSSFWorkbook workbook = new HSSFWorkbook(); // 工作簿
                ISheet sheet = workbook.CreateSheet(sheetName); // 工作表
                IRow row = sheet.CreateRow(0);
                List<string> keys = cellHeard.Keys.ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    row.CreateCell(i).SetCellValue(cellHeard[keys[i]]); // 列名为Key的值
                }

                // 3.List对象的值赋值到Excel的单元格里
                int rowIndex = 1; // 从第二行开始赋值(第一行已设置为单元头)
                foreach (var en in enList)
                {
                    IRow rowTmp = sheet.CreateRow(rowIndex);
                    for (int i = 0; i < keys.Count; i++) // 根据指定的属性名称，获取对象指定属性的值
                    {
                        string cellValue = ""; // 单元格的值
                        object properotyValue = null; // 属性的值
                        System.Reflection.PropertyInfo properotyInfo = null; // 属性的信息

                        // 3.1 若属性头的名称包含'.',就表示是子类里的属性，那么就要遍历子类，eg：UserEn.UserName
                        if (keys[i].IndexOf(".") >= 0)
                        {
                            // 3.1.1 解析子类属性(这里只解析1层子类，多层子类未处理)
                            string[] properotyArray = keys[i].Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                            string subClassName = properotyArray[0]; // '.'前面的为子类的名称
                            string subClassProperotyName = properotyArray[1]; // '.'后面的为子类的属性名称
                            System.Reflection.PropertyInfo subClassInfo = en.GetType().GetProperty(subClassName); // 获取子类的类型
                            if (subClassInfo != null)
                            {
                                // 3.1.2 获取子类的实例
                                var subClassEn = en.GetType().GetProperty(subClassName).GetValue(en, null);
                                // 3.1.3 根据属性名称获取子类里的属性类型
                                properotyInfo = subClassInfo.PropertyType.GetProperty(subClassProperotyName);
                                if (properotyInfo != null)
                                {
                                    properotyValue = properotyInfo.GetValue(subClassEn, null); // 获取子类属性的值
                                }
                            }
                        }
                        else
                        {
                            // 3.2 若不是子类的属性，直接根据属性名称获取对象对应的属性
                            properotyInfo = en.GetType().GetProperty(keys[i]);
                            if (properotyInfo != null)
                            {
                                properotyValue = properotyInfo.GetValue(en, null);
                            }
                        }

                        // 3.3 属性值经过转换赋值给单元格值
                        if (properotyValue != null)
                        {
                            cellValue = properotyValue.ToString();
                            // 3.3.1 对时间初始值赋值为空
                            if (cellValue.Trim() == "0001/1/1 0:00:00" || cellValue.Trim() == "0001/1/1 23:59:59")
                            {
                                cellValue = "";
                            }
                        }

                        // 3.4 填充到Excel的单元格里
                        rowTmp.CreateCell(i).SetCellValue(cellValue);
                    }
                    rowIndex++;
                }

                // 4.生成文件
                FileStream file = new FileStream(filePath, FileMode.Create);
                workbook.Write(file);
                file.Close();

                // 5.返回下载路径
                return urlPath;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void ExportExcel(DateTime StartDate, DateTime EndDate)
        {
            try
            {
                var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                IDbConnection connection = new SqlConnection(conStr);
                connection.Open();

                DynamicParameters dp1 = new DynamicParameters();
                dp1.Add("@Id", 2, DbType.Int32);
                WXBGModel LJ = connection.Query<WXBGModel>("GetWXBG", dp1, commandType: CommandType.StoredProcedure).FirstOrDefault();

                DynamicParameters dp = new DynamicParameters();
                dp.Add("@PageIndex", 1, DbType.Int32);
                dp.Add("@PageSize", 5000, DbType.Int32);
                dp.Add("@StartDate", StartDate, DbType.DateTime);
                dp.Add("@EndDate", EndDate, DbType.DateTime);

                SqlMapper.GridReader result = connection.QueryMultiple("prFanHobbyInfoList", dp, commandType: CommandType.StoredProcedure);
                var recordCount = result.Read<int>().FirstOrDefault();
                var model = result.Read<FanHobbyInfo>().ToList();

                connection.Dispose();
                // 1.获取数据集合
                List<FanHobbyInfo> enlist = model;

                foreach (var item in enlist)
                {
                    item.Href = LJ.WXBG + " http://www.learnercode.com/Home/BookPage?pageId=" + item.Id;
                }

                // 2.设置单元格抬头
                // key：实体对象属性名称，可通过反射获取值
                // value：Excel列的名称
                Dictionary<string, string> cellheader = new Dictionary<string, string> {
            { "SerialNumber", "序号" },
            { "WXNickName", "微信昵称" },
            { "WXId", "微信号" },
            { "Href", "书单链接" },
        };

                // 3.进行Excel转换操作，并返回转换的文件下载链接
                string urlPath = EntityListToExcel2003(cellheader, enlist, "书单");
                System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
                HttpContext.Response.ContentType = "text/plain";
                HttpContext.Response.Write(model == null || model.Count() == 0 ? "" : js.Serialize(urlPath)); // 返回Json格式的内容
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static string GetCellValue(ICell cell)
        {
            if (cell == null)
                return string.Empty;
            switch (cell.CellType)
            {
                case CellType.Blank:
                    return string.Empty;
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Error:
                    return cell.ErrorCellValue.ToString();
                case CellType.Numeric:
                case CellType.Unknown:
                default:
                    return cell.ToString();//This is a trick to get the correct value of the cell. NumericCellValue will return a numeric value no matter the cell value is a date or a number
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Formula:
                    try
                    {
                        HSSFFormulaEvaluator e = new HSSFFormulaEvaluator(cell.Sheet.Workbook);
                        e.EvaluateInCell(cell);
                        return cell.ToString();
                    }
                    catch
                    {
                        return cell.NumericCellValue.ToString();
                    }
            }
        }

        public JsonResult GetBookPageListAjax(DateTime StartDate, DateTime EndDate, int PageIndex = 1, int PageSize = 10)
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            IDbConnection connection = new SqlConnection(conStr);
            connection.Open();

            DynamicParameters dp = new DynamicParameters();
            dp.Add("@PageIndex", PageIndex, DbType.Int32);
            dp.Add("@PageSize", PageSize, DbType.Int32);
            dp.Add("@StartDate", StartDate, DbType.DateTime);
            dp.Add("@EndDate", EndDate, DbType.DateTime);

            SqlMapper.GridReader result = connection.QueryMultiple("prFanHobbyInfoList", dp, commandType: CommandType.StoredProcedure);
            var recordCount = result.Read<int>().FirstOrDefault();
            var model = result.Read<FanHobbyInfo>().ToList();

            connection.Dispose();
            return Json(new { model = model, recordCount = recordCount });
        }

        public ActionResult GetBookByBookName(string bookName)
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            using (IDbConnection con = new SqlConnection(conStr))
            {
                DynamicParameters dp1 = new DynamicParameters();
                dp1.Add("@BookName", bookName, DbType.String);
                var result = 0;
                result = con.Query<int>("GetBookInfoByName", dp1, commandType: CommandType.StoredProcedure).FirstOrDefault();
                return Json(new { isSuccess = (result == 0), message = "" });
            }
        }

        [MyAuthorizeAttribute]
        public ActionResult AddBook(int bookId = 0)
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            IDbConnection connection = new SqlConnection(conStr);
            connection.Open();
            Book bk = new Book();
            DynamicParameters dp = new DynamicParameters();
            dp.Add("@BookId", bookId, DbType.Int32);

            Book result = connection.Query<Book>("GetBookInfoById", dp, commandType: CommandType.StoredProcedure).FirstOrDefault();
            if (result != null)
            {
                bk = result;
            }

            List<BookType> bookTypeList = connection.Query<BookType>("GetBookType", commandType: CommandType.StoredProcedure).ToList();
            List<SelectListItem> selectList = new List<SelectListItem>()
            {
                new SelectListItem() { Value = "", Text = "--请选择--",Selected=true }
            };
            List<SelectListItem> selectList1 = new List<SelectListItem>()
            {
                new SelectListItem() { Value = "", Text = "--请选择--",Selected=true }
            };
            List<SelectListItem> selectList2 = new List<SelectListItem>()
            {
                new SelectListItem() { Value = "", Text = "--请选择--",Selected=true }
            };

            foreach (var item in bookTypeList)
            {
                if (item.Grade == 1)
                {
                    if (result != null && result.BookTypeId == item.BookTypeId)
                    {
                        selectList.Add(new SelectListItem() { Value = item.BookTypeId.ToString(), Text = item.BookTypeName, Selected = true });
                    }
                    else
                    {
                        selectList.Add(new SelectListItem() { Value = item.BookTypeId.ToString(), Text = item.BookTypeName });
                    }
                }
                if (item.Grade == 2)
                {
                    if (result != null && result.BookTypeId == item.BookTypeId)
                    {
                        selectList1.Add(new SelectListItem() { Value = item.BookTypeId.ToString(), Text = item.BookTypeName, Selected = true });
                    }
                    else
                    {
                        selectList1.Add(new SelectListItem() { Value = item.BookTypeId.ToString(), Text = item.BookTypeName });
                    }
                }
                if (item.Grade == 3)
                {
                    if (result != null && result.BookTypeId == item.BookTypeId)
                    {
                        selectList2.Add(new SelectListItem() { Value = item.BookTypeId.ToString(), Text = item.BookTypeName, Selected = true });
                    }
                    else
                    {
                        selectList2.Add(new SelectListItem() { Value = item.BookTypeId.ToString(), Text = item.BookTypeName });
                    }
                }
            }

            ViewBag.bookTypeList = selectList;
            ViewBag.bookTypeList1 = selectList1;
            ViewBag.bookTypeList2 = selectList2;
            connection.Dispose();
            return View(bk);
        }

        [HttpPost]
        public void AddBook(Book book)
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            DynamicParameters dp = new DynamicParameters();
            dp.Add("@Id", book.Id, DbType.String);
            dp.Add("@BookName", book.BookName, DbType.String);
            dp.Add("@BookTypeId", book.BookTypeId, DbType.Int32);
            dp.Add("@BookTypeId1", book.BookTypeId1, DbType.Int32);
            dp.Add("@BookTypeId2", book.BookTypeId2, DbType.Int32);
            dp.Add("@ImageData", book.ImageData, DbType.String);
            dp.Add("@Author", book.Author, DbType.String);
            dp.Add("@Score", book.Score, DbType.String);
            dp.Add("@BookDes", book.BookDes, DbType.String);
            dp.Add("@RecommendReason", book.RecommendReason, DbType.String);
            dp.Add("@CreateUser", book.CreateUser, DbType.String);
            dp.Add("@ErrorCount", 0, DbType.Int32, ParameterDirection.Output);

            using (IDbConnection con = new SqlConnection(conStr))
            {
                con.Execute("AddBookInfo", dp, null, null, CommandType.StoredProcedure);
            }

            Response.Redirect("BookList");
        }

        [HttpPost]
        public JsonResult DeleteBookInfo(int bookId)
        {
            try
            {
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@BookId", bookId, DbType.Int32);

                var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                using (IDbConnection con = new SqlConnection(conStr))
                {
                    con.Execute("DeleteBookInfo", dp, null, null, CommandType.StoredProcedure);
                }

                return Json(new { model = true });
            }
            catch (Exception)
            {
                return Json(new { model = false });
            }
        }

        public ActionResult Login()
        {
            return View();
        }

        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return Redirect("/Home/Login");
        }

        [HttpPost]
        public JsonResult Login(string UserName, string Password)
        {
            var userName = ConfigurationManager.AppSettings["UserName"].ToString();
            var password = ConfigurationManager.AppSettings["Password"].ToString();

            if (UserName.Trim().Equals(userName.Trim()) && Password.Trim().Equals(password.Trim()))
            {
                FormsAuthentication.SetAuthCookie(UserName, false);
                return Json(new { isSuccess = true });
            }
            else
            {
                return Json(new { isSuccess = false });
            }
        }

        [MyAuthorizeAttribute]
        public ActionResult GetWXBG()
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            IDbConnection connection = new SqlConnection(conStr);
            DynamicParameters dp = new DynamicParameters();
            dp.Add("@Id", 1, DbType.Int32);
            WXBGModel result = connection.Query<WXBGModel>("GetWXBG", dp, commandType: CommandType.StoredProcedure).FirstOrDefault();

            connection.Dispose();

            return View(result != null ? result : new WXBGModel());
        }

        public JsonResult SetWXBG(int Id, string WXName, string WXBG)
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DynamicParameters dp = new DynamicParameters();
            dp.Add("@Id", Id, DbType.Int32);
            dp.Add("@WXName", WXName, DbType.String);
            dp.Add("@WXBG", WXBG, DbType.String);

            using (IDbConnection con = new SqlConnection(conStr))
            {
                con.Execute("SetWXBG", dp, null, null, CommandType.StoredProcedure);
            }

            return Json(new { });
        }

        [MyAuthorizeAttribute]
        public ActionResult GetLJ()
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            IDbConnection connection = new SqlConnection(conStr);
            DynamicParameters dp = new DynamicParameters();
            dp.Add("@Id", 2, DbType.Int32);
            WXBGModel result = connection.Query<WXBGModel>("GetWXBG", dp, commandType: CommandType.StoredProcedure).FirstOrDefault();

            connection.Dispose();

            return View(result != null ? result : new WXBGModel());
        }

        [ValidateInput(false)]
        public JsonResult SetLJ(int Id, string WXName, string WXBG)
        {
            var conStr = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            DynamicParameters dp = new DynamicParameters();
            dp.Add("@Id", Id, DbType.Int32);
            dp.Add("@WXName", WXName, DbType.String);
            dp.Add("@WXBG", WXBG, DbType.String);

            using (IDbConnection con = new SqlConnection(conStr))
            {
                con.Execute("SetWXBG", dp, null, null, CommandType.StoredProcedure);
            }

            return Json(new { });
        }
    }
}
