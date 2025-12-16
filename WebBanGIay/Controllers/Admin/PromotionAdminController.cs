using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers.Admin
{
    // [Authorize(Roles = "QUẢN TRỊ,ADMIN")]
    public class PromotionAdminController : BaseAdminController
    {
        private QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();

        // GET: PromotionAdmin
        public ActionResult Index(string search, string status)
        {
            var query = db.KHUYENMAI.AsQueryable();
            var now = DateTime.Now;

            // Stats (Global)
            var allPromos = db.KHUYENMAI.ToList(); // Fetch to memory to avoid complex EF Date comparisons if issues arise, dataset likely small
            ViewBag.TotalCount = allPromos.Count;
            ViewBag.ActiveCount = allPromos.Count(p => p.NGAYBATDAU <= now && p.NGAYKETTHUC >= now);
            ViewBag.UpcomingCount = allPromos.Count(p => p.NGAYBATDAU > now);
            ViewBag.ExpiredCount = allPromos.Count(p => p.NGAYKETTHUC < now);

            // Filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.TENKHUYENMAI.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                    query = query.Where(p => p.NGAYBATDAU <= now && p.NGAYKETTHUC >= now);
                else if (status == "upcoming")
                    query = query.Where(p => p.NGAYBATDAU > now);
                else if (status == "expired")
                    query = query.Where(p => p.NGAYKETTHUC < now);
            }

            var promotions = query.OrderByDescending(p => p.NGAYBATDAU).ToList();
            
            // Keep filter state
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;

            return View(promotions);
        }

        // GET: PromotionAdmin/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PromotionAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KHUYENMAI model)
        {
            if (ModelState.IsValid)
            {
                if (model.NGAYKETTHUC <= model.NGAYBATDAU)
                {
                    ModelState.AddModelError("NGAYKETTHUC", "Ngày kết thúc phải sau ngày bắt đầu.");
                    return View(model);
                }

                model.TRANGTHAI = true;
                db.KHUYENMAI.Add(model);
                db.SaveChanges();
                TempData["Success"] = "Tạo chương trình khuyến mãi thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // GET: PromotionAdmin/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            KHUYENMAI promo = db.KHUYENMAI.Find(id);
            if (promo == null) return HttpNotFound();
            return View(promo);
        }

        // POST: PromotionAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KHUYENMAI model)
        {
            if (ModelState.IsValid)
            {
                if (model.NGAYKETTHUC <= model.NGAYBATDAU)
                {
                    ModelState.AddModelError("NGAYKETTHUC", "Ngày kết thúc phải sau ngày bắt đầu.");
                    return View(model);
                }

                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Success"] = "Cập nhật khuyến mãi thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // GET: Apply Promotion to Products
        public ActionResult Apply(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var promo = db.KHUYENMAI.Find(id);
            if (promo == null) return HttpNotFound();

            ViewBag.Promotion = promo;
            // Get all products, mark checked if they belong to this promotion
            var products = db.SANPHAM.ToList();
            return View(products);
        }

        // POST: Apply Promotion
        [HttpPost]
        public ActionResult Apply(int promotionId, List<string> selectedProducts)
        {
            var promo = db.KHUYENMAI.Find(promotionId);
            if (promo == null) return HttpNotFound();

            // 1. Clear this promotion from all products initially (optional strategy: or just overwrite)
            // But user might want to Add to existing list. Let's assume "Apply" sets the list for this promotion.
            // Actually, a product can only have ONE promotion (as per schema MAKHUYENMAI is a single int).
            // So we find all products that currently have THIS promotionId and clear them if not in selected list.
            
            var currentProducts = db.SANPHAM.Where(p => p.MAKHUYENMAI == promotionId).ToList();
            foreach (var p in currentProducts)
            {
                if (selectedProducts == null || !selectedProducts.Contains(p.MASANPHAM))
                {
                    p.MAKHUYENMAI = null; // Remove promotion
                }
            }

            // 2. Add promotion to selected products
            if (selectedProducts != null)
            {
                foreach (var pid in selectedProducts)
                {
                    var product = db.SANPHAM.Find(pid);
                    if (product != null)
                    {
                        product.MAKHUYENMAI = promotionId;
                    }
                }
            }

            db.SaveChanges();
            TempData["Success"] = "Đã áp dụng khuyến mãi cho các sản phẩm đã chọn!";
            return RedirectToAction("Index");
        }

        // POST: Apply to ALL products
        [HttpPost]
        public ActionResult ApplyAll(int promotionId)
        {
            var promo = db.KHUYENMAI.Find(promotionId);
            if (promo == null) return HttpNotFound();

            var allProducts = db.SANPHAM.ToList();
            foreach (var p in allProducts)
            {
                p.MAKHUYENMAI = promotionId;
            }
            db.SaveChanges();

            TempData["Success"] = $"Đã áp dụng khuyến mãi '{promo.TENKHUYENMAI}' cho TOÀN BỘ sản phẩm!";
            return RedirectToAction("Index");
        }
        
        // POST: Delete
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var promo = db.KHUYENMAI.Find(id);
            if(promo != null)
            {
                // Set FK in products to null
                var products = db.SANPHAM.Where(p => p.MAKHUYENMAI == id).ToList();
                foreach(var p in products) p.MAKHUYENMAI = null;

                db.KHUYENMAI.Remove(promo);
                db.SaveChanges();
                TempData["Success"] = "Đã xóa chương trình khuyến mãi";
            }
            return RedirectToAction("Index");
        }
    }
}
