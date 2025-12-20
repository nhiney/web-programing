using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebBanGIay.Models
{
    public class CheckoutVM
    {
        public List<CartItem> Items { get; set; }
        public decimal TongTien { get; set; }

        // Customer Info
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string DienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string DiaChi { get; set; }
        
        public string GhiChu { get; set; }
    }
}
