namespace Lungisa.Models
{
    public class DonationModel
    {
        public string M_PaymentId { get; set; } // Internal unique ID
        public string PayFastPaymentId { get; set; } // PayFast ID
        public string DonorName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } // Pending, Success, Failed
        public DateTime Timestamp { get; set; }

        // 🔑 Additions for PayFast integration
        // From PayFast config
        public string MerchantId { get; set; }
        // From PayFast config
        public string MerchantKey { get; set; }
        // Redirect after success
        public string ReturnUrl { get; set; }
        // Redirect after cancel
        public string CancelUrl { get; set; }
        // PayFast calls this to confirm payment
        public string NotifyUrl { get; set; }

        public string PaymentReference { get; set; }  
    }
}
