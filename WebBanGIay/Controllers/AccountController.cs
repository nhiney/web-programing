using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanGIay.Models;
using WebBanGIay.Security;

namespace WebBanGIay.Controllers
{
    public class AccountController : Controller
    {
        private QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();


        // ====================== REGISTER ======================
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(TAIKHOAN tk, string passwordConfirm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            if (tk.MATKHAU != passwordConfirm)
            {
                ViewBag.Error = "Mật khẩu nhập lại không khớp!";
                return View();
            }

            if (db.TAIKHOAN.Any(u => u.TENTAIKHOAN == tk.TENTAIKHOAN))
            {
                ViewBag.Error = "Tên tài khoản đã tồn tại!";
                return View();
            }

            // ---- Tạo mã tự động ----
            string newAccountId = "TK" + DateTime.Now.Ticks;
            string newCustomerId = GenerateNewCustomerId();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Tạo khách hàng mới
                    KHACHHANG kh = new KHACHHANG
                    {
                        MAKHACHHANG = newCustomerId,
                        HOTEN = null,
                        EMAIL = null,
                        SODIENTHOAI = null,
                        DIACHI = null,
                        NGAYTAO = DateTime.Now
                    };
                    db.KHACHHANG.Add(kh);

                    // 2. Tạo tài khoản
                    tk.MATAIKHOAN = newAccountId;
                    tk.MATKHAU = PasswordHasher.Hash(tk.MATKHAU);
                    tk.LOAITAIKHOAN = "KHÁCH HÀNG";
                    tk.MAKHACHHANG = newCustomerId;
                    tk.NGAYTAO = DateTime.Now;
                    db.TAIKHOAN.Add(tk);

                    db.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ViewBag.Error = "Lỗi ghi dữ liệu: " + ex.Message;
                    return View();
                }
            }

            TempData["Success"] = "Đăng ký thành công!";
            return RedirectToAction("Login");
        }


        // ====================== LOGIN ======================
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string TENTAIKHOAN, string MATKHAU, string returnUrl)
        {
            var user = db.TAIKHOAN.FirstOrDefault(u => u.TENTAIKHOAN == TENTAIKHOAN);

            if (user == null || !PasswordHasher.Verify(MATKHAU, user.MATKHAU))
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
                return View();
            }

            // Đăng nhập thành công
            Session["UserID"] = user.MATAIKHOAN;
            Session["UserName"] = user.TENTAIKHOAN;
            Session["UserRole"] = user.LOAITAIKHOAN;

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            if (user.LOAITAIKHOAN == "QUẢN TRỊ")
                return RedirectToAction("Index", "Admin");

            if (user.LOAITAIKHOAN == "NHÂN VIÊN")
                return RedirectToAction("Index", "Admin");

            return RedirectToAction("Index", "TrangChu");
        }


        // ====================== LOGOUT ======================
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }


        // ====================== PROFILE ======================
        // [HttpGet]
        // public ActionResult Profile()
        // {
        //     var userId = Session["UserID"] as string;
        //     if (userId == null)
        //         return RedirectToAction("Login");

        //     var user = db.TAIKHOAN.Include(u => u.KHACHHANG)
        //                           .FirstOrDefault(u => u.MATAIKHOAN == userId);

        //     if (user == null)
        //         return RedirectToAction("Logout");

        //     var model = new UserProfileViewModel
        //     {
        //         UserName = user.TENTAIKHOAN,
        //         AccountType = user.LOAITAIKHOAN,
        //         CreatedDate = user.NGAYTAO,
        //         FullName = user.KHACHHANG?.HOTEN,
        //         Email = user.KHACHHANG?.EMAIL,
        //         Phone = user.KHACHHANG?.SODIENTHOAI,
        //         Address = user.KHACHHANG?.DIACHI,
        //         AvatarUrl = Session["AvatarUrl"] as string
        //     };

        //     return View(model);
        // }


        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public ActionResult Profile(UserProfileViewModel model, HttpPostedFileBase AvatarFile)
        // {
        //     var userId = Session["UserID"] as string;
        //     if (userId == null)
        //         return RedirectToAction("Login");

        //     var user = db.TAIKHOAN.Include(u => u.KHACHHANG)
        //                           .FirstOrDefault(u => u.MATAIKHOAN == userId);

        //     if (user == null)
        //         return RedirectToAction("Logout");

        //     try
        //     {
        //         // Nếu khách hàng chưa tồn tại → Tạo mới
        //         if (user.KHACHHANG == null)
        //         {
        //             user.MAKHACHHANG = GenerateNewCustomerId();
        //             user.KHACHHANG = new KHACHHANG
        //             {
        //                 MAKHACHHANG = user.MAKHACHHANG,
        //                 NGAYTAO = DateTime.Now
        //             };
        //             db.KHACHHANG.Add(user.KHACHHANG);
        //         }

        //         // Cập nhật
        //         user.KHACHHANG.HOTEN = model.FullName;
        //         user.KHACHHANG.EMAIL = model.Email;
        //         user.KHACHHANG.SODIENTHOAI = model.Phone;
        //         user.KHACHHANG.DIACHI = model.Address;

        //         // Upload Avatar
        //         if (AvatarFile != null && AvatarFile.ContentLength > 0)
        //         {
        //             var ext = Path.GetExtension(AvatarFile.FileName).ToLower();
        //             var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };

        //             if (!allowed.Contains(ext))
        //             {
        //                 ModelState.AddModelError("", "File ảnh không hợp lệ!");
        //                 return View(model);
        //             }

        //             var fileName = $"{userId}_{DateTime.Now.Ticks}{ext}";
        //             string folder = Server.MapPath("~/Content/avatars/");
        //             if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        //             AvatarFile.SaveAs(Path.Combine(folder, fileName));

        //             Session["AvatarUrl"] = Url.Content("~/Content/avatars/" + fileName);
        //         }

        //         db.SaveChanges();
        //         TempData["Success"] = "Cập nhật hồ sơ thành công!";
        //     }
        //     catch (DbEntityValidationException ex)
        //     {
        //         TempData["Error"] = string.Join(" | ",
        //             ex.EntityValidationErrors.SelectMany(e => e.ValidationErrors)
        //               .Select(v => v.ErrorMessage));
        //     }
        //     catch (Exception ex)
        //     {
        //         TempData["Error"] = "Lỗi: " + ex.Message;
        //     }

        //     return RedirectToAction("Profile");
        // }


        // ====================== TẠO MÃ KH TIẾP THEO ======================
        private string GenerateNewCustomerId()
        {
            string lastId = db.KHACHHANG
                .OrderByDescending(k => k.MAKHACHHANG)
                .Select(k => k.MAKHACHHANG)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(lastId))
                return "KH001";

            int num = int.Parse(lastId.Substring(2));
            return "KH" + (num + 1).ToString("D3");
        }
    }
}
