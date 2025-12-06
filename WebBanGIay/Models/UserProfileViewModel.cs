using System;
using System.Collections.Generic;
using WebBanGIay.Models.Admin;

namespace WebBanGIay.Models
{
    public class UserProfileViewModel
    {
        public string UserName { get; set; }
        public string AccountType { get; set; }
        public DateTime? CreatedDate { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        public string EmployeeName { get; set; }
        public string Position { get; set; }
        public decimal? Salary { get; set; }

        public string AvatarUrl { get; set; }

        public List<OrderViewModel> Orders { get; set; }
        public List<AddressViewModel> Addresses { get; set; }

        public UserProfileViewModel()
        {
            Orders = new List<OrderViewModel>();
            Addresses = new List<AddressViewModel>();
        }
    }
}
