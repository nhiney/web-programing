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
        [HttpGet]
        public ActionResult Profile()
        {
            var userId = Session["UserID"] as string;
            if (userId == null)
                return RedirectToAction("Login");

            var user = db.TAIKHOAN.Include(u => u.KHACHHANG)
                                  .FirstOrDefault(u => u.MATAIKHOAN == userId);

            if (user == null)
                return RedirectToAction("Logout");

            var model = new UserProfileViewModel
            {
                UserName = user.TENTAIKHOAN,
                AccountType = user.LOAITAIKHOAN,
                CreatedDate = user.NGAYTAO,
                FullName = user.KHACHHANG?.HOTEN,
                Email = user.KHACHHANG?.EMAIL,
                Phone = user.KHACHHANG?.SODIENTHOAI,
                Address = user.KHACHHANG?.DIACHI,
                AvatarUrl = Session["AdminAvatar"] as string
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(UserProfileViewModel model, HttpPostedFileBase AvatarFile)
        {
            var userId = Session["UserID"] as string;
            if (userId == null)
                return RedirectToAction("Login");

            var user = db.TAIKHOAN.Include(u => u.KHACHHANG)
                                  .FirstOrDefault(u => u.MATAIKHOAN == userId);

            if (user == null)
                return RedirectToAction("Logout");

            try
            {
                // 1. UPDATE INFO
                if (user.KHACHHANG == null)
                {
                    user.MAKHACHHANG = GenerateNewCustomerId();
                    user.KHACHHANG = new KHACHHANG
                    {
                        MAKHACHHANG = user.MAKHACHHANG,
                        NGAYTAO = DateTime.Now
                    };
                    db.KHACHHANG.Add(user.KHACHHANG);
                }

                user.KHACHHANG.HOTEN = model.FullName;
                user.KHACHHANG.EMAIL = model.Email;
                user.KHACHHANG.SODIENTHOAI = model.Phone;
                user.KHACHHANG.DIACHI = model.Address;

                // 2. CHANGE PASSWORD (Optional)
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        ModelState.AddModelError("CurrentPassword", "Cần nhập mật khẩu hiện tại để thay đổi.");
                        return View(model);
                    }

                    if (!PasswordHasher.Verify(model.CurrentPassword, user.MATKHAU))
                    {
                        ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                        return View(model);
                    }

                    if (model.NewPassword != model.ConfirmNewPassword)
                    {
                        ModelState.AddModelError("ConfirmNewPassword", "Mật khẩu xác nhận không khớp.");
                        return View(model);
                    }

                    user.MATKHAU = PasswordHasher.Hash(model.NewPassword);
                    TempData["SuccessPassword"] = "Đổi mật khẩu thành công!";
                }

                // 3. UPLOAD AVATAR
                if (AvatarFile != null && AvatarFile.ContentLength > 0)
                {
                    var ext = Path.GetExtension(AvatarFile.FileName).ToLower();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };

                    if (!allowed.Contains(ext))
                    {
                        ModelState.AddModelError("", "File ảnh không hợp lệ!");
                        return View(model);
                    }

                    // Ensure folder exists
                    string folderName = "/Content/avatars/";
                    string folderPath = Server.MapPath("~" + folderName);
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    // Save file
                    string fileName = $"{userId}_{DateTime.Now.Ticks}{ext}";
                    string fullPath = Path.Combine(folderPath, fileName);
                    AvatarFile.SaveAs(fullPath);

                    // Update Session
                    string relativePath = folderName + fileName;
                    Session["AdminAvatar"] = relativePath;
                    model.AvatarUrl = relativePath;
                }

                db.SaveChanges();
                TempData["Success"] = "Cập nhật hồ sơ thành công!";
            }
            catch (DbEntityValidationException ex)
            {
                var errors = ex.EntityValidationErrors.SelectMany(e => e.ValidationErrors)
                               .Select(v => v.ErrorMessage);
                TempData["Error"] = "Lỗi dữ liệu: " + string.Join("; ", errors);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToAction("Profile");
        }


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
        // GET: Account/OrderHistory
        public ActionResult OrderHistory()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            string taiKhoanId = Session["UserID"] as string;
            
            // 1. Find the Account first to get Customer ID
            var tk = db.TAIKHOAN.FirstOrDefault(u => u.MATAIKHOAN == taiKhoanId);
            if (tk == null) return RedirectToAction("Login");

            // 2. Get orders by Customer ID (MAKHACHHANG)
            // HOADON links to KHACHHANG, not TAIKHOAN directly
            var orders = db.HOADON
                           .Where(h => h.MAKHACHHANG == tk.MAKHACHHANG)
                           .OrderByDescending(h => h.NGAYLAP)
                           .ToList();

            return View(orders);
        }

        // GET: Account/OrderDetail/5
        // GET: Account/OrderDetail/5
        [HttpGet]
        public ActionResult OrderDetail(string id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("OrderHistory");
            }

            string taiKhoanId = Session["UserID"] as string;
            var tk = db.TAIKHOAN.FirstOrDefault(u => u.MATAIKHOAN == taiKhoanId);
            if (tk == null) return RedirectToAction("Login");

            // Find order and include details
            var order = db.HOADON.Include("CHITIET_HOADON.SANPHAM")
                                 .FirstOrDefault(h => h.MAHOADON == id);

            // Security check: Ensure order belongs to current user
            if (order == null || order.MAKHACHHANG != tk.MAKHACHHANG)
            {
                TempData["Error"] = "Đơn hàng không tồn tại hoặc bạn không có quyền xem.";
                return RedirectToAction("OrderHistory");
            }

            return View(order);
        }
    }
}
