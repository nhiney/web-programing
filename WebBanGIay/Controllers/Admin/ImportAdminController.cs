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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string MANHACUNGCAP, string[] itemProducts, int[] itemQuantities, decimal[] itemPrices)
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
                        var qty = itemQuantities[i];
                        var price = itemPrices[i];

                        if (!string.IsNullOrEmpty(productId) && qty > 0)
                        {
                            var product = db.SANPHAM.Find(productId);
                            if (product != null)
                            {
                                product.SOLUONGTON = (product.SOLUONGTON ?? 0) + qty;
                                // Could optionally update Cost Price if we had a field for it
                                
                                totalItems += qty;
                                totalValue += (qty * price);
                            }
                        }
                    }

                    db.SaveChanges();
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
