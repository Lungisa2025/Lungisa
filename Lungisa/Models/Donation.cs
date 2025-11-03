namespace Lungisa.Models
{
    public class Donation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public decimal Amount { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PaymentId { get; set; }
/*        public string Status { get; set; } = "Pending";*/
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
