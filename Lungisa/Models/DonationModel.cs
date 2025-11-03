namespace Lungisa.Models
{
    public class DonationModel
    {
        public string DonorName { get; set; }

        public string Email { get; set; }
        public decimal? Amount { get; set; } // nullable decimal
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string Status { get; set; }

        // For form binding
        public string FirstName { get; set; }
        public string LastName { get; set; }

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

        // Optional: Track PayFast reference
        public string PayFastPaymentId { get; set; }
        // Your merchant reference (unique per donation)
        public string M_PaymentId { get; set; }
        // Unique order/donation reference
        public string PaymentReference { get; set; }  
    }
}
