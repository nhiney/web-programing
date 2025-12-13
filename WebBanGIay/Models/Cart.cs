using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanGIay.Models
{
    [Serializable] // Cần để lưu vào Session
    public class CartItem
    {
        public string MaSP { get; set; }       // ID sản phẩm
        public string TenSP { get; set; }      // Tên hiển thị (kèm size + màu)
        public string HinhAnh { get; set; }    // Ảnh sản phẩm
        public decimal DonGia { get; set; }    // Giá sản phẩm
        public int SoLuong { get; set; }       // Số lượng
        public string Size { get; set; }       // Size
        public string Mau { get; set; }        // Màu sắc

        public decimal ThanhTien => DonGia * SoLuong;
    }

    public class CartService
    {
        private const string CartKey = "CART";

        // Lấy giỏ hàng
        public List<CartItem> GetCart()
        {
            var cart = HttpContext.Current.Session[CartKey] as List<CartItem>;
            if (cart == null)
            {
                cart = new List<CartItem>();
                HttpContext.Current.Session[CartKey] = cart;
            }
            return cart;
        }

        // Thêm sản phẩm
        public void Add(string maSP, string tenSP, string hinhAnh, decimal donGia, int soLuong, string size = "", string mau = "")
        {
            if (string.IsNullOrWhiteSpace(maSP)) return;

            var cart = GetCart();

            // Tạo ID biến thể để phân biệt size + màu
            string itemId = $"{maSP.Trim()}-{size}-{mau}";
            var item = cart.FirstOrDefault(x => x.MaSP != null && $"{x.MaSP}-{x.Size}-{x.Mau}" == itemId);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    MaSP = maSP.Trim(),
                    TenSP = tenSP,
                    HinhAnh = hinhAnh,
                    DonGia = donGia,
                    SoLuong = soLuong,
                    Size = size,
                    Mau = mau
                });
            }
            else
            {
                item.SoLuong += soLuong;
            }
        }

        // Cập nhật số lượng
        public void Update(string maSP, string size = "", string mau = "", int soLuong = 0)
        {
            if (string.IsNullOrWhiteSpace(maSP)) return;
            var cart = GetCart();

            string itemId = $"{maSP.Trim()}-{size}-{mau}";
            var item = cart.FirstOrDefault(x => x.MaSP != null && $"{x.MaSP}-{x.Size}-{x.Mau}" == itemId);

            if (item != null)
            {
                if (soLuong <= 0)
                    cart.Remove(item);
                else
                    item.SoLuong = soLuong;
            }
        }

        // Xóa sản phẩm
        public void Remove(string maSP, string size = "", string mau = "")
        {
            if (string.IsNullOrWhiteSpace(maSP)) return;
            var cart = GetCart();

            string itemId = $"{maSP.Trim()}-{size}-{mau}";
            cart.RemoveAll(x => x.MaSP != null && $"{x.MaSP}-{x.Size}-{x.Mau}" == itemId);
        }

        // Xóa tất cả
        public void Clear()
        {
            HttpContext.Current.Session[CartKey] = new List<CartItem>();
        }

        // Tổng tiền
        public decimal TongTien()
        {
            var cart = GetCart();
            return cart.Sum(x => x.ThanhTien);
        }
    }
}
