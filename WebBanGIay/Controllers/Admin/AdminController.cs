using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class AdminController : BaseAdminController
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();



        // ====== HASH MẬT KHẨU ======
        private string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return null;

            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        // ====== DASHBOARD ======
        public ActionResult Index()
        {
            var model = new DashboardViewModel();

            // 1. Tổng số liệu
            model.TotalUsers = db.TAIKHOAN.Count();
            model.TotalProducts = db.SANPHAM.Count();
            model.TotalOrders = db.HOADON.Count();
            model.TotalRevenue = db.HOADON.Sum(o => (decimal?)o.TONGTIEN) ?? 0;

            // 2. Trạng thái đơn hàng
            model.NewOrdersCount = db.HOADON.Count(o => o.TRANGTHAI == "CHỜ XỬ LÝ");
            model.PendingOrdersCount = model.NewOrdersCount;
            model.ShippingOrdersCount = db.HOADON.Count(o => o.TRANGTHAI == "ĐÃ GIAO");
            model.CompletedOrdersCount = db.HOADON.Count(o => o.TRANGTHAI == "HOÀN THÀNH");
            model.CancelledOrdersCount = db.HOADON.Count(o => o.TRANGTHAI == "ĐÃ HỦY");

            model.OrderStatusLabels = new System.Collections.Generic.List<string> { "Chờ xử lý", "Đang giao", "Hoàn thành", "Đã hủy" };
            model.OrderStatusData = new System.Collections.Generic.List<int>
            {
                model.PendingOrdersCount,
                model.ShippingOrdersCount,
                model.CompletedOrdersCount,
                model.CancelledOrdersCount
            };

            // 3. Doanh thu theo tháng
            var revenueStats = db.HOADON
                .Where(o => o.NGAYLAP != null)
                .GroupBy(o => new { o.NGAYLAP.Value.Year, o.NGAYLAP.Value.Month })
                .Select(g => new
                {
                    Label = g.Key.Month + "/" + g.Key.Year,
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Revenue = g.Sum(x => (decimal?)x.TONGTIEN) ?? 0,
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            foreach (var item in revenueStats)
            {
                model.RevenueLabels.Add(item.Label);
                model.RevenueData.Add(item.Revenue);
                model.MonthlyOrderLabels.Add(item.Label);
                model.MonthlyOrderData.Add(item.OrderCount);
            }

            // 4. Top sản phẩm bán chạy
            var topProducts = db.CHITIET_HOADON
                .GroupBy(d => d.MASANPHAM)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Sold = g.Sum(x => (int?)x.SOLUONG) ?? 0
                })
                .OrderByDescending(x => x.Sold)
                .Take(5)
                .ToList();

            var productIds = topProducts.Select(x => x.ProductId).ToList();
            var productNames = db.SANPHAM
                .Where(p => productIds.Contains(p.MASANPHAM))
                .ToDictionary(p => p.MASANPHAM, p => p.TENSANPHAM);

            foreach (var item in topProducts)
            {
                string name = productNames.ContainsKey(item.ProductId) ? productNames[item.ProductId] : item.ProductId;
                if (name.Length > 20) name = name.Substring(0, 17) + "...";
                model.TopProductLabels.Add(name);
                model.TopProductData.Add(item.Sold);
            }

            model.CategoryLabels = model.TopProductLabels;
            model.CategoryData = model.TopProductData;

            return View(model);
        }

        // ============================
        //  UPLOAD AVATAR 
        // ============================
        [HttpPost]
        public ActionResult UploadAvatar(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return Json(new { success = false, message = "Không có file" });

            string folder = Server.MapPath("~/Content/AdminAvatars/");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string fileName = "admin_" + Guid.NewGuid() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(folder, fileName);

            file.SaveAs(filePath);

            string url = "/Content/AdminAvatars/" + fileName;

            Session["AdminAvatar"] = url;
            return Json(new { success = true, url = url });
        }

        // ====================== DANH SÁCH USER ======================
        public ActionResult Users(string q = "", int page = 1, int pageSize = 15)
        {
            var query = db.TAIKHOAN.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(t =>
                    t.TENTAIKHOAN.Contains(q) ||
                    (t.MATAIKHOAN != null && t.MATAIKHOAN.Contains(q)) ||
                    (t.LOAITAIKHOAN != null && t.LOAITAIKHOAN.Contains(q))
                );
            }

            int total = query.Count();

            var users = query.OrderByDescending(u => u.NGAYTAO)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                             .ToList();

            ViewBag.Query = q;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(users);
        }

        // ====================== USER DETAILS ======================
        public ActionResult DetailsUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();

            id = id.Trim();
            var user = db.TAIKHOAN.AsEnumerable().FirstOrDefault(t => t.MATAIKHOAN != null && t.MATAIKHOAN.Trim() == id);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // ====================== CREATE USER ======================
        public ActionResult CreateUser()
        {
            ViewBag.KhachHangList = db.KHACHHANG.ToList();
            ViewBag.NhanVienList = db.NHANVIEN.ToList();
            return View(new TAIKHOAN());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(TAIKHOAN model, string passwordConfirm)
        {
            if (string.IsNullOrWhiteSpace(model.TENTAIKHOAN))
                ModelState.AddModelError("TENTAIKHOAN", "Tên tài khoản không được để trống!");

            if (string.IsNullOrWhiteSpace(model.MATKHAU))
                ModelState.AddModelError("MATKHAU", "Mật khẩu không được để trống!");

            if (model.MATKHAU != passwordConfirm)
                ModelState.AddModelError("MATKHAU", "Mật khẩu và xác nhận không khớp!");

            if (db.TAIKHOAN.Any(t => t.TENTAIKHOAN == model.TENTAIKHOAN))
                ModelState.AddModelError("TENTAIKHOAN", "Tên tài khoản đã tồn tại!");

            if (model.LOAITAIKHOAN == "KHÁCH HÀNG" && model.MAKHACHHANG == null)
                ModelState.AddModelError("MAKHACHHANG", "Vui lòng chọn khách hàng!");

            if (model.LOAITAIKHOAN == "NHÂN VIÊN" && model.MANHANVIEN == null)
                ModelState.AddModelError("MANHANVIEN", "Vui lòng chọn nhân viên!");

            if (!ModelState.IsValid)
            {
                ViewBag.KhachHangList = db.KHACHHANG.ToList();
                ViewBag.NhanVienList = db.NHANVIEN.ToList();
                return View(model);
            }

            model.MATAIKHOAN = "TK" + DateTime.Now.Ticks.ToString().Substring(8);
            model.MATKHAU = HashPassword(model.MATKHAU);
            model.NGAYTAO = DateTime.Now;

            try
            {
                db.TAIKHOAN.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Tạo tài khoản thành công!";
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi tạo tài khoản: " + ex.Message;
                ViewBag.KhachHangList = db.KHACHHANG.ToList();
                ViewBag.NhanVienList = db.NHANVIEN.ToList();
                return View(model);
            }
        }

        // ====================== EDIT USER ======================
        public ActionResult EditUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();

            id = id.Trim();
            var user = db.TAIKHOAN.AsEnumerable().FirstOrDefault(t => t.MATAIKHOAN != null && t.MATAIKHOAN.Trim() == id);
            if (user == null)
                return HttpNotFound();

            ViewBag.KhachHangList = db.KHACHHANG.ToList();
            ViewBag.NhanVienList = db.NHANVIEN.ToList();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(TAIKHOAN model)
        {
            if (string.IsNullOrWhiteSpace(model.TENTAIKHOAN))
                ModelState.AddModelError("TENTAIKHOAN", "Tên tài khoản không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.KhachHangList = db.KHACHHANG.ToList();
                ViewBag.NhanVienList = db.NHANVIEN.ToList();
                return View(model);
            }

            model.MATAIKHOAN = model.MATAIKHOAN?.Trim();
            var user = db.TAIKHOAN.Find(model.MATAIKHOAN);
            if (user == null) return HttpNotFound();

            user.TENTAIKHOAN = model.TENTAIKHOAN;

            if (!string.IsNullOrWhiteSpace(model.MATKHAU))
                user.MATKHAU = HashPassword(model.MATKHAU);

            user.LOAITAIKHOAN = model.LOAITAIKHOAN;
            user.MAKHACHHANG = model.MAKHACHHANG;
            user.MANHANVIEN = model.MANHANVIEN;

            try
            {
                db.SaveChanges();
                TempData["Success"] = "Cập nhật tài khoản thành công!";
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi cập nhật tài khoản: " + ex.Message;
                ViewBag.KhachHangList = db.KHACHHANG.ToList();
                ViewBag.NhanVienList = db.NHANVIEN.ToList();
                return View(model);
            }
        }

        // ====================== DELETE USER ======================
        public ActionResult DeleteUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();

            id = id.Trim();
            var user = db.TAIKHOAN.AsEnumerable().FirstOrDefault(t => t.MATAIKHOAN != null && t.MATAIKHOAN.Trim() == id);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUserConfirmed(string id)
        {
            id = id?.Trim();
            var user = db.TAIKHOAN.Find(id);
            if (user == null)
                return HttpNotFound();

            try
            {
                db.TAIKHOAN.Remove(user);
                db.SaveChanges();
                TempData["Success"] = "Xóa tài khoản thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể xóa tài khoản: " + ex.Message;
            }

            return RedirectToAction("Users");
        }

        // ====================== USER LOCK ======================
        public ActionResult ToggleLock(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();

            id = id.Trim();
            var user = db.TAIKHOAN.Find(id);
            if (user == null)
                return HttpNotFound();

            // Toggle logic: If null or true -> set to false. If false -> set to true.
            bool isActive = user.TRANGTHAI ?? true;
            user.TRANGTHAI = !isActive;
            
            db.SaveChanges();

            TempData["Success"] = user.TRANGTHAI.Value ? "Đã mở khóa tài khoản!" : "Đã khóa tài khoản thành công!";
            
            // Redirect back to where the request came from if possible, else Users
            if (Request.UrlReferrer != null)
                return Redirect(Request.UrlReferrer.ToString());

            return RedirectToAction("Users");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
