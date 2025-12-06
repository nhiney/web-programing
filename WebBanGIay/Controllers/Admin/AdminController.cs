using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebBanGIay.Models;

public class AdminController : Controller
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
        // Tổng số liệu
        ViewBag.TotalUsers = db.TAIKHOAN.Count();
        ViewBag.TotalProducts = db.SANPHAM.Count();
        ViewBag.TotalOrders = db.HOADON.Count();
        ViewBag.TotalRevenue = db.HOADON.Sum(o => (decimal?)o.TONGTIEN) ?? 0;

        // Đơn hàng mới
        ViewBag.NewOrders = db.HOADON.Count(o => o.TRANGTHAI == "CHỜ XỬ LÝ");

        // ============================
        //  LINE CHART — Doanh thu theo tháng
        // ============================
        var revenueData = db.HOADON
            .Where(o => o.NGAYLAP.HasValue)
            .GroupBy(o => o.NGAYLAP.Value.Month)
            .Select(g => new
            {
                Month = g.Key,
                Revenue = g.Sum(x => (decimal?)x.TONGTIEN) ?? 0
            })
            .OrderBy(x => x.Month)
            .ToList();

        ViewBag.SalesMonths = string.Join(",", revenueData.Select(s => "'" + s.Month + "'"));
        ViewBag.SalesRevenue = string.Join(",", revenueData.Select(s => s.Revenue));

        // ============================
        //  BAR CHART — Top sản phẩm bán chạy
        // ============================
        var topSales = db.CHITIET_HOADON
            .GroupBy(c => c.MASANPHAM)
            .Select(g => new
            {
                Product = g.Key,
                Quantity = g.Sum(x => (int?)x.SOLUONG) ?? 0
            })
            .OrderByDescending(x => x.Quantity)
            .Take(5)
            .ToList();

        ViewBag.TopProductNames = string.Join(",", topSales.Select(s => "'SP" + s.Product + "'"));
        ViewBag.TopProductQty = string.Join(",", topSales.Select(s => s.Quantity));

        // ============================
        //  PIE CHART — Sản phẩm bán chạy nhất
        // ============================
        var pieData = db.CHITIET_HOADON
            .Join(db.SANPHAM, ct => ct.MASANPHAM, sp => sp.MASANPHAM, (ct, sp) => new { ct, sp })
            .GroupBy(x => x.sp.TENSANPHAM)
            .Select(g => new
            {
                ProductName = g.Key,
                TotalSold = g.Sum(x => (int?)x.ct.SOLUONG) ?? 0
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(5)
            .ToList();

        ViewBag.PieLabels = string.Join(",", pieData.Select(x => "'" + x.ProductName + "'"));
        ViewBag.PieValues = string.Join(",", pieData.Select(x => x.TotalSold));


        // ============================
        //  ORDER STATUS CHART
        // ============================
        ViewBag.OrdersCompleted = db.HOADON.Count(o => o.TRANGTHAI == "HOÀN THÀNH");
        ViewBag.OrdersInProgress = db.HOADON.Count(o => o.TRANGTHAI == "ĐÃ GIAO");
        ViewBag.OrdersPending = db.HOADON.Count(o => o.TRANGTHAI == "CHỜ XỬ LÝ");

        // ============================
        //  ORDERS BY MONTH
        // ============================
        var ordersByMonth = db.HOADON
            .Where(o => o.NGAYLAP.HasValue)
            .GroupBy(o => o.NGAYLAP.Value.Month)
            .Select(g => new
            {
                Month = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Month)
            .ToList();

        ViewBag.OrdersMonths = string.Join(",", ordersByMonth.Select(s => "'" + s.Month + "'"));
        ViewBag.OrdersCount = string.Join(",", ordersByMonth.Select(s => s.Count));

        return View();
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

        // Lưu vào session
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

        var user = db.TAIKHOAN.Find(id);
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
        // VALIDATION
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

        // TẠO USER MỚI
        model.MATAIKHOAN = "TK" + DateTime.Now.Ticks.ToString().Substring(8);
        model.MATKHAU = HashPassword(model.MATKHAU);
        model.NGAYTAO = DateTime.Now;

        db.TAIKHOAN.Add(model);
        db.SaveChanges();

        TempData["Success"] = "Tạo tài khoản thành công!";
        return RedirectToAction("Users");
    }


    // ====================== EDIT USER ======================
    public ActionResult EditUser(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return HttpNotFound();

        var user = db.TAIKHOAN.Find(id);
        if (user == null)
            return HttpNotFound();

        ViewBag.KhachHangList = db.KHACHHANG.ToList();
        ViewBag.NhanVienList = db.NHANVIEN.ToList();

        return View(user);
    }

    [HttpPost]
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

        var user = db.TAIKHOAN.Find(model.MATAIKHOAN);
        if (user == null) return HttpNotFound();

        user.TENTAIKHOAN = model.TENTAIKHOAN;

        if (!string.IsNullOrWhiteSpace(model.MATKHAU))
            user.MATKHAU = HashPassword(model.MATKHAU);

        user.LOAITAIKHOAN = model.LOAITAIKHOAN;
        user.MAKHACHHANG = model.MAKHACHHANG;
        user.MANHANVIEN = model.MANHANVIEN;

        db.SaveChanges();

        TempData["Success"] = "Cập nhật tài khoản thành công!";
        return RedirectToAction("Users");
    }


    // ====================== DELETE USER ======================
    public ActionResult DeleteUser(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return HttpNotFound();

        var user = db.TAIKHOAN.Find(id);
        if (user == null)
            return HttpNotFound();

        return View(user);
    }

    [HttpPost, ActionName("DeleteUser")]
    public ActionResult DeleteUserConfirmed(string id)
    {
        var user = db.TAIKHOAN.Find(id);
        if (user == null)
            return HttpNotFound();

        db.TAIKHOAN.Remove(user);
        db.SaveChanges();

        TempData["Success"] = "Xóa tài khoản thành công!";
        return RedirectToAction("Users");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) db.Dispose();
        base.Dispose(disposing);
    }

}
