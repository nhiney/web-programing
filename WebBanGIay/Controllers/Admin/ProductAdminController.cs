using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class ProductAdminController : Controller
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            ViewBag.NewOrdersCount = db.HOADON.Count(o => o.TRANGTHAI == "CHỜ XỬ LÝ");
        }

        // ===================== LIST PRODUCTS =====================
        public ActionResult Index(string q = "", int page = 1, int pageSize = 12)
        {
            var query = db.SANPHAM.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                string k = q.Trim();
                query = query.Where(p => p.TENSANPHAM.Contains(k) || p.MASANPHAM.Contains(k));
            }

            int total = query.Count();

            var products = query
                .OrderByDescending(p => p.NGAYTAO)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Query = q;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(products);
        }

        // ===================== CREATE PRODUCT =====================
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SANPHAM model, HttpPostedFileBase imageFile)
        {
            if (string.IsNullOrWhiteSpace(model.TENSANPHAM))
                ModelState.AddModelError("TENSANPHAM", "Tên sản phẩm không được để trống");

            if (model.GIA == null || model.GIA <= 0)
                ModelState.AddModelError("GIA", "Giá không hợp lệ");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.MASANPHAM))
            {
                model.MASANPHAM = "SP" + DateTime.Now.Ticks.ToString().Substring(6);
            }

            if (imageFile != null && imageFile.ContentLength > 0)
            {
                string ext = Path.GetExtension(imageFile.FileName);
                string fileName = model.MASANPHAM + "_" + Guid.NewGuid().ToString("N") + ext;

                string folder = Server.MapPath("~/images/products");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, fileName);
                imageFile.SaveAs(path);

                model.HINHANH = "/images/products/" + fileName;
            }

            model.NGAYTAO = DateTime.Now;

            try
            {
                db.SANPHAM.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi thêm sản phẩm: " + ex.Message;
                return View(model);
            }
        }

        // ===================== EDIT PRODUCT =====================
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = db.SANPHAM.Find(id);
            if (product == null) return HttpNotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SANPHAM model, HttpPostedFileBase imageFile)
        {
            var product = db.SANPHAM.Find(model.MASANPHAM);
            if (product == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(model.TENSANPHAM))
                ModelState.AddModelError("TENSANPHAM", "Tên sản phẩm không được để trống");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            product.TENSANPHAM = model.TENSANPHAM;
            product.GIA = model.GIA;
            product.MOTA = model.MOTA;
            product.SOLUONGTON = model.SOLUONGTON;

            if (imageFile != null && imageFile.ContentLength > 0)
            {
                string ext = Path.GetExtension(imageFile.FileName);
                string fileName = model.MASANPHAM + "_" + Guid.NewGuid().ToString("N") + ext;

                string folder = Server.MapPath("~/images/products");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, fileName);
                imageFile.SaveAs(path);

                product.HINHANH = "/images/products/" + fileName;
            }

            try
            {
                db.SaveChanges();
                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi cập nhật sản phẩm: " + ex.Message;
                return View(model);
            }
        }

        // ===================== DETAILS PRODUCT =====================
        public ActionResult Details(string id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = db.SANPHAM.Find(id);
            if (product == null) return HttpNotFound();

            return View(product);
        }

        // ===================== DELETE PRODUCT =====================
        public ActionResult Delete(string id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = db.SANPHAM.Find(id);
            if (product == null) return HttpNotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            var product = db.SANPHAM.Find(id);
            if (product == null) return HttpNotFound();

            try
            {
                db.SANPHAM.Remove(product);
                db.SaveChanges();
                TempData["Success"] = "Xóa sản phẩm thành công!";
            }
            catch
            {
                TempData["Error"] = "Không thể xóa sản phẩm vì đang có đơn hàng liên quan.";
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
