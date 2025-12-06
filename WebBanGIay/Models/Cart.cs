using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanGIay.Models
{
    public class CartItem
    {
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string HinhAnh { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }

        public decimal ThanhTien => DonGia * SoLuong;
    }


    public class CartService
    {
        private const string CartKey = "CART";

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

        public void Add(string maSP, string tenSP, string hinhAnh, decimal donGia, int soLuong)
        {
            if (string.IsNullOrWhiteSpace(maSP)) return;

            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.MaSP != null && x.MaSP.Trim() == maSP.Trim());

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    MaSP = maSP.Trim(),
                    TenSP = tenSP,
                    HinhAnh = hinhAnh,
                    DonGia = donGia,
                    SoLuong = soLuong
                });
            }
            else
            {
                item.SoLuong += soLuong;
            }
        }

        public void Update(string maSP, int soLuong)
        {
            if (string.IsNullOrWhiteSpace(maSP)) return;
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.MaSP != null && x.MaSP.Trim() == maSP.Trim());
            if (item != null)
            {
                if (soLuong <= 0)
                    cart.Remove(item);
                else
                    item.SoLuong = soLuong;
            }
        }

        public void Remove(string maSP)
        {
            if (string.IsNullOrWhiteSpace(maSP)) return;
            var cart = GetCart();
            cart.RemoveAll(x => x.MaSP != null && x.MaSP.Trim() == maSP.Trim());
        }

        public void Clear()
        {
            HttpContext.Current.Session[CartKey] = new List<CartItem>();
        }
        public decimal TongTien()
        {
            var cart = GetCart();
            return cart.Sum(x => x.ThanhTien);
        }
    }
}