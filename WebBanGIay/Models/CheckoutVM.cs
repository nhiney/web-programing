using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanGIay.Models
{
    public class CheckoutVM
    {
        public List<CartItem> Items { get; set; }
        public int TongTien { get; set; }
    }
}