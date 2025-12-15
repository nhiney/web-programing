using System;
using System.Linq;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class ImportAdminController : Controller
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            ViewBag.NewOrdersCount = db.HOADON.Count(o => o.TRANGTHAI == "CHỜ XỬ LÝ");
        }

        // ======================= CREATE IMPORT SLIP =======================
        public ActionResult Create()
        {
            ViewBag.Suppliers = new SelectList(db.NHACUNGCAP.ToList(), "MANHACUNGCAP", "TENNHACUNGCAP");
            
            var products = db.SANPHAM.Select(s => new {
                Id = s.MASANPHAM.Trim(),
                Name = s.TENSANPHAM,
                Stock = s.SOLUONGTON ?? 0,
                Price = s.GIA,
                Image = s.HINHANH
            }).ToList();

            ViewBag.ProductsJson = System.Web.Helpers.Json.Encode(products);
            ViewBag.Products = db.SANPHAM.ToList(); // Fallback/Dropdown

            return View();
        }

        // ======================= GET VARIANTS FOR IMPORT =======================
        [HttpGet]
        public ActionResult GetProductVariants(string productId)
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
        [ValidateAntiForgeryToken]
        public ActionResult Create(string MANHACUNGCAP, string[] itemProducts, int[] itemVariants, int[] itemSizes, int[] itemQuantities, decimal[] itemPrices)
        {
            if (itemProducts == null || itemProducts.Length == 0)
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất một sản phẩm!");
            }

            if (ModelState.IsValid)
            {
                try 
                {
                    int totalItems = 0;
                    decimal totalValue = 0;

                    for (int i = 0; i < itemProducts.Length; i++)
                    {
                        var productId = itemProducts[i];
                        var variantId = itemVariants[i]; // IDBienThe
                        var size = itemSizes[i];
                        var qty = itemQuantities[i];
                        var price = itemPrices[i];

                        if (!string.IsNullOrEmpty(productId) && qty > 0)
                        {
                            productId = productId.Trim();
                            
                            // 1. Find or Create Stock Record (TONKHO_SIZE)
                            var stockRecord = db.TONKHO_SIZE
                                .FirstOrDefault(t => t.MASANPHAM == productId && t.IDBienThe == variantId && t.SIZE == size);

                            if (stockRecord == null)
                            {
                                stockRecord = new TONKHO_SIZE
                                {
                                    MASANPHAM = productId,
                                    IDBienThe = variantId,
                                    SIZE = size,
                                    SOLUONG = 0 // Initial
                                };
                                db.TONKHO_SIZE.Add(stockRecord);
                            }

                            // 2. Update Stock
                            stockRecord.SOLUONG += qty;
                            
                            totalItems += qty;
                            totalValue += (qty * price);
                        }
                    }
                    
                    db.SaveChanges();

                    // 3. Update Aggregate Stock for Products (Optional but good for display)
                    var distinctProductIds = itemProducts.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).Distinct().ToList();
                    foreach (var pid in distinctProductIds)
                    {
                        var p = db.SANPHAM.Find(pid);
                        if(p != null)
                        {
                            var totalStock = db.TONKHO_SIZE.Where(t => t.MASANPHAM == pid).Sum(t => (int?)t.SOLUONG) ?? 0;
                            p.SOLUONGTON = totalStock;
                        }
                    }
                    
                    db.SaveChanges(); // Save aggregate updates

                    TempData["Success"] = $"Đã nhập kho thành công {totalItems} sản phẩm. Tổng giá trị: {totalValue:N0}₫";
                    return RedirectToAction("Index", "ProductAdmin");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi xử lý: " + ex.Message);
                }
            }

            // Reload data on error
            ViewBag.Suppliers = new SelectList(db.NHACUNGCAP.ToList(), "MANHACUNGCAP", "TENNHACUNGCAP", MANHACUNGCAP);
            var products = db.SANPHAM.Select(s => new {
                Id = s.MASANPHAM.Trim(),
                Name = s.TENSANPHAM,
                Stock = s.SOLUONGTON ?? 0,
                Price = s.GIA,
                Image = s.HINHANH
            }).ToList();
            ViewBag.ProductsJson = System.Web.Helpers.Json.Encode(products);
            ViewBag.Products = db.SANPHAM.ToList();

            return View();
        }
    }
}
