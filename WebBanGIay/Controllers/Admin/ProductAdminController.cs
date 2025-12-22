using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class ProductAdminController : BaseAdminController
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();



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
            ViewBag.NHACUNGCAP = db.NHACUNGCAP.ToList();
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
                string fileName = Guid.NewGuid().ToString("N") + ext;

                string folder = Server.MapPath("~/source/images/Products");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, fileName);
                imageFile.SaveAs(path);

                model.HINHANH = "/source/images/Products/" + fileName;
            }

            model.NGAYTAO = DateTime.Now;

            try
            {
                db.SANPHAM.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Thêm sản phẩm thành công! Vui lòng thiết lập màu sắc và tồn kho.";
                return RedirectToAction("Edit", new { id = model.MASANPHAM.Trim() });
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

            id = id.Trim();
            var product = db.SANPHAM.Find(id);
            if (product == null) return HttpNotFound();

            ViewBag.NHACUNGCAP = db.NHACUNGCAP.ToList();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SANPHAM model, HttpPostedFileBase imageFile)
        {
            model.MASANPHAM = model.MASANPHAM?.Trim();
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
            product.MANHACUNGCAP = model.MANHACUNGCAP;
            product.NGAYTAO = DateTime.Now; // Update timestamp when edited

            // product.SOLUONGTON = model.SOLUONGTON; // Disabled: Managed by Import/Inventory

            if (imageFile != null && imageFile.ContentLength > 0)
            {
                string ext = Path.GetExtension(imageFile.FileName);
                string fileName = Guid.NewGuid().ToString("N") + ext;

                string folder = Server.MapPath("~/source/images/Products");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, fileName);
                imageFile.SaveAs(path);

                product.HINHANH = "/source/images/Products/" + fileName;
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

            id = id.Trim();
            var product = db.SANPHAM.Find(id);
            if (product == null) return HttpNotFound();

            return View(product);
        }

        // ===================== DELETE PRODUCT =====================
        public ActionResult Delete(string id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            id = id.Trim();
            var product = db.SANPHAM.Find(id);
            if (product == null) return HttpNotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            id = id?.Trim();
            var product = db.SANPHAM.Find(id);
            if (product == null) return HttpNotFound();

            try
            {
                // 1. Kiểm tra chính xác xem có đơn hàng nào không (dùng Trim để tránh lỗi CHAR(20))
                // Chúng ta sẽ kiểm tra cả trong bộ nhớ để chắc chắn nhất
                var idTrimmed = id.Trim();
                bool hasOrders = db.CHITIET_HOADON.AsEnumerable().Any(c => c.MASANPHAM != null && c.MASANPHAM.Trim() == idTrimmed);
                
                if (hasOrders)
                {
                    TempData["Error"] = "Sản phẩm này đã có đơn hàng thực tế nên không thể xóa!";
                    return RedirectToAction("Index");
                }

                // 2. Xóa các bản ghi liên quan (Variants, Stock, Reviews)
                // Sử dụng SQL trực tiếp để đảm bảo sạch sẽ hoàn toàn dù có lỗi padding
                db.Database.ExecuteSqlCommand("DELETE FROM TONKHO_SIZE WHERE MASANPHAM = @p0", id);
                db.Database.ExecuteSqlCommand("DELETE FROM BIEN_THE_SAN_PHAM WHERE MASANPHAM = @p0", id);
                db.Database.ExecuteSqlCommand("DELETE FROM DANHGIASANPHAM WHERE MASANPHAM = @p0", id);

                // 3. Xóa sản phẩm chính
                // Tìm lại bản ghi gốc để tránh lỗi cache
                var productToDelete = db.SANPHAM.ToList().FirstOrDefault(s => s.MASANPHAM.Trim() == idTrimmed);
                if (productToDelete != null)
                {
                    db.SANPHAM.Remove(productToDelete);
                    db.SaveChanges();
                    TempData["Success"] = "Xóa sản phẩm '" + productToDelete.TENSANPHAM + "' thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm để xóa (ID: " + id + ")";
                }
            }
            catch (Exception ex)
            {
                // Nếu vẫn lỗi, báo lỗi chi tiết để debug
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message + (ex.InnerException != null ? " -> " + ex.InnerException.Message : "");
            }

            return RedirectToAction("Index");
        }

        // ===================== PRODUCT VARIANTS (AJAX) =====================
        
        [HttpGet]
        public ActionResult GetVariants(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            productId = productId.Trim();
            var variants = db.BIEN_THE_SAN_PHAM
                .Where(v => v.MASANPHAM == productId)
                .Select(v => new {
                    v.ID,
                    v.MAUSAC,
                    v.GIATHEOMAU
                })
                .ToList();

            return Json(new { success = true, data = variants }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddVariant(string productId, string color, decimal price)
        {
            try
            {
                if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(color))
                    return Json(new { success = false, message = "Thiếu thông tin!" });

                productId = productId.Trim();
                // Check if exists
                var exists = db.BIEN_THE_SAN_PHAM
                    .Any(v => v.MASANPHAM == productId && v.MAUSAC == color);
                
                if (exists)
                    return Json(new { success = false, message = "Màu sắc này đã tồn tại!" });

                var variant = new BIEN_THE_SAN_PHAM
                {
                    MASANPHAM = productId,
                    MAUSAC = color,
                    GIATHEOMAU = price
                };

                db.BIEN_THE_SAN_PHAM.Add(variant);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteVariant(int id)
        {
            try
            {
                var variant = db.BIEN_THE_SAN_PHAM.Find(id);
                if (variant == null)
                    return Json(new { success = false, message = "Không tìm thấy biến thể!" });

                // Check if used in Stocks
                if (variant.TONKHO_SIZE.Any(t => t.SOLUONG > 0))
                {
                    // Allow delete but warn, or delete cascade? For safety, let's delete stock first
                     db.TONKHO_SIZE.RemoveRange(variant.TONKHO_SIZE);
                }
                
                // If 0 stock, we can probably delete the stock records first if FK requires it, 
                // but for now let's assume we just delete the variant and generic FK cascade or manual delete
                if (variant.TONKHO_SIZE.Any())
                {
                    db.TONKHO_SIZE.RemoveRange(variant.TONKHO_SIZE);
                }

                db.BIEN_THE_SAN_PHAM.Remove(variant);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===================== STOCK/SIZE MANAGEMENT (AJAX) =====================
        [HttpGet]
        public ActionResult GetSizes(int variantId)
        {
            var sizes = db.TONKHO_SIZE
                .Where(t => t.IDBienThe == variantId)
                .Select(t => new { t.ID, t.SIZE, t.SOLUONG })
                .OrderBy(t => t.SIZE)
                .ToList();
            return Json(new { success = true, data = sizes }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SaveSize(int variantId, int size, int quantity)
        {
            try
            {
                var variant = db.BIEN_THE_SAN_PHAM.Find(variantId);
                if (variant == null) return Json(new { success = false, message = "Biến thể không tồn tại" });

                var exist = db.TONKHO_SIZE.FirstOrDefault(t => t.IDBienThe == variantId && t.SIZE == size);
                if (exist != null)
                {
                    exist.SOLUONG = quantity; // Update
                }
                else
                {
                    var newStock = new TONKHO_SIZE
                    {
                        IDBienThe = variantId,
                        MASANPHAM = variant.MASANPHAM,
                        SIZE = size,
                        SOLUONG = quantity
                    };
                    db.TONKHO_SIZE.Add(newStock);
                }
                
                // Update Total Stock of Product
                var product = db.SANPHAM.Find(variant.MASANPHAM);
                if(product != null)
                {
                    // Recalculate total
                    // Note: This only counts saved ones. The current transaction includes the new changes.
                    // But to be safe, we can trigger this after SaveChanges or do math now.
                    // Let's do simple math for now or re-query.
                    // Re-query might not see local changes unless we save first.
                    
                    // Simple approach: Save first
                    db.SaveChanges();
                    
                    product.SOLUONGTON = db.TONKHO_SIZE.Where(t => t.MASANPHAM == product.MASANPHAM).Sum(t => (int?)t.SOLUONG) ?? 0;
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteSize(int id)
        {
            try
            {
                var item = db.TONKHO_SIZE.Find(id);
                if (item == null) return Json(new { success = false, message = "Không tìm thấy" });

                string productId = item.MASANPHAM;
                db.TONKHO_SIZE.Remove(item);
                db.SaveChanges();

                // Update Total
                var product = db.SANPHAM.Find(productId);
                if (product != null)
                {
                    product.SOLUONGTON = db.TONKHO_SIZE.Where(t => t.MASANPHAM == productId).Sum(t => (int?)t.SOLUONG) ?? 0;
                    db.SaveChanges();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
