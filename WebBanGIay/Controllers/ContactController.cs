using System;
using System.Linq;
using System.Web.Mvc;
using WebBanGIay.Models;
using WebBanGIay.Helpers;

namespace WebBanGIay.Controllers
{
    public class ContactController : Controller
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();

        // GET: Contact
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Messages()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Send(LIENHE model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.NgayGui = DateTime.Now;
                    model.TrangThai = false; // Chưa xem

                    db.LIENHE.Add(model);
                    db.SaveChanges();

                    // Gửi email thông báo cho Admin
                    string subject = "📩 Tin nhắn mới từ khách hàng: " + model.HoTen;
                    string content = $@"
                        <h3>Bạn nhận được tin nhắn mới từ website</h3>
                        <p><strong>Họ tên:</strong> {model.HoTen}</p>
                        <p><strong>Email:</strong> {model.Email}</p>
                        <p><strong>SĐT:</strong> {model.SDT}</p>
                        <p><strong>Nội dung:</strong></p>
                        <p>{model.NoiDung}</p>
                        <hr/>
                        <p><i>Vui lòng đăng nhập trang quản trị để phản hồi.</i></p>
                    ";

                    try {
                        // Email nhận là email của admin, cấu hình cứng hoặc lấy từ db. 
                        // Tạm thời lấy email cấu hình trong web.config (FromEmailAddress) để test gửi cho chính mình
                         var adminEmail = System.Configuration.ConfigurationManager.AppSettings["FromEmailAddress"];
                         MailHelper.SendMail(adminEmail, subject, content);
                    } catch (Exception) {
                        // Bỏ qua lỗi gửi mail để không chặn người dùng
                    }

                    TempData["Success"] = "Tin nhắn của bạn đã được gửi thành công! Chúng tôi sẽ phản hồi sớm nhất.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            return View("Index", model);
        }
        [HttpPost]
        public ActionResult SendAjax(LIENHE model)
        {
            try
            {
                // Auto-fill user info if logged in and fields are empty
                var userName = Session["UserName"] as string;
                if (!string.IsNullOrEmpty(userName))
                {
                    if (string.IsNullOrEmpty(model.HoTen)) model.HoTen = userName;
                }

                if (string.IsNullOrEmpty(model.HoTen) || string.IsNullOrEmpty(model.NoiDung))
                {
                     return Json(new { success = false, message = "Vui lòng điền nội dung tin nhắn!" });
                }

                model.NgayGui = DateTime.Now;
                model.TrangThai = false;

                db.LIENHE.Add(model);
                db.SaveChanges();

                // Email sending removed as requested to fix connection issues
                /* 
                try {
                    string subject = "📩 Tin nhắn hỗ trợ mới: " + model.HoTen;
                    string content = $@"
                        <h3>Tin nhắn từ Chat Support</h3>
                        <p><strong>Họ tên:</strong> {model.HoTen}</p>
                        <p><strong>Email:</strong> {model.Email}</p>
                        <p><strong>SĐT:</strong> {model.SDT}</p>
                        <p><strong>Nội dung:</strong></p>
                        <p>{model.NoiDung}</p>
                    ";
                    var adminEmail = System.Configuration.ConfigurationManager.AppSettings["FromEmailAddress"];
                    if(!string.IsNullOrEmpty(adminEmail)) 
                        MailHelper.SendMail(adminEmail, subject, content);
                } catch {} 
                */

                return Json(new { success = true, message = "Đã gửi tin nhắn! Chúng tôi sẽ phản hồi sớm nhất." });
            }
            catch (Exception ex)
            {
                // Detailed error for debugging
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = "Lỗi hệ thống: " + msg });
            }
        }

        [HttpGet]
        public ActionResult GetHistory()
        {
            var userName = Session["UserName"] as string; 
            if (string.IsNullOrEmpty(userName)) return Json(new { success = false, message = "Vui lòng đăng nhập" }, JsonRequestBehavior.AllowGet);

            // Fetch messages sent by this username (HoTen field in LIENHE often stores the username for logged in users)
            // Or if we want to be more precise, we should join with TAIKHOAN/KHACHHANG, but for now filtering by HoTen == UserName is the most direct map from SendAjax
            var list = db.LIENHE
                .Where(m => m.HoTen == userName) 
                .OrderByDescending(m => m.NgayGui)
                .Select(m => new { 
                    m.MaLH,
                    m.NoiDung,
                    NgayGui = m.NgayGui.ToString(), 
                    m.PhanHoi,
                    m.NgayPhanHoi
                }).ToList();

            return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
        }

        // Temporary Helper to Fix Database missing table
        public ActionResult FixTable()
        {
            try
            {
                db.Database.ExecuteSqlCommand(@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LIENHE]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[LIENHE](
                            [MaLH] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [HoTen] [nvarchar](100) NULL,
                            [Email] [nvarchar](100) NULL,
                            [SDT] [nvarchar](20) NULL,
                            [NoiDung] [nvarchar](max) NULL,
                            [NgayGui] [datetime] DEFAULT GETDATE(),
                            [TrangThai] [bit] DEFAULT 0
                        )
                    END
                ");
                return Content("Database LIENHE Table Fixed Successfully!");
            }
            catch (Exception ex)
            {
                return Content("Fix Failed: " + ex.Message);
            }
        }

        public ActionResult FixTable_UpdateSchema()
        {
            try
            {
                // Check and Add PhanHoi column
                db.Database.ExecuteSqlCommand(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LIENHE]') AND name = 'PhanHoi')
                    BEGIN
                        ALTER TABLE [dbo].[LIENHE] ADD [PhanHoi] nvarchar(max) NULL
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LIENHE]') AND name = 'NgayPhanHoi')
                    BEGIN
                        ALTER TABLE [dbo].[LIENHE] ADD [NgayPhanHoi] datetime NULL
                    END
                ");
                return Content("Database Schema Updated Successfully! (Added PhanHoi columns)");
            }
            catch (Exception ex)
            {
                return Content("Update Failed: " + ex.Message);
            }
        }
    }
}
