using Lungisa.Models;
using Lungisa.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net;
using System.Text;
using System.Security.Cryptography;

namespace Lungisa.Controllers
{
    public class DonationsController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly FirebaseService _firebase;
        private readonly HttpClient _httpClient;

        public DonationsController(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
            _firebase = new FirebaseService(_config, _env);
            _httpClient = new HttpClient();
        }

        [HttpGet]
        public IActionResult Index() => View("~/Views/Home/Donations.cshtml");

        [HttpPost]
        public async Task<IActionResult> PayFastPay(DonationModel model)
        {
            var mPaymentId = Guid.NewGuid().ToString("N");

            var pendingDonation = new DonationModel
            {
                DonorName = $"{model.FirstName} {model.LastName}",
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Amount = model.Amount,
                Status = "Pending",
                Timestamp = DateTime.UtcNow.AddHours(2), // use CreatedAt instead
                M_PaymentId = mPaymentId
            };

            await _firebase.SaveDonation(pendingDonation);

            // Store donation session to track current donation
            HttpContext.Session.SetString("PendingDonationId", mPaymentId);


            var pfData = new SortedDictionary<string, string>
            {
                ["merchant_id"] = _config["PayFastSettings:MerchantId"],
                ["merchant_key"] = _config["PayFastSettings:MerchantKey"],
                ["return_url"] = _config["PayFastSettings:ReturnUrl"],
                ["cancel_url"] = _config["PayFastSettings:CancelUrl"],
                ["notify_url"] = _config["PayFastSettings:NotifyUrl"],
                ["name_first"] = model.FirstName,
                ["name_last"] = model.LastName,
                ["email_address"] = model.Email,
                ["m_payment_id"] = mPaymentId,
                ["amount"] = string.Format(CultureInfo.InvariantCulture, "{0:F2}", model.Amount),
                ["item_name"] = "Donation to Lungisa NPO"
            };

            var signature = GeneratePayfastSignature(pfData, _config["PayFastSettings:Passphrase"]);
            pfData.Add("signature", signature);

            var processUrl = _config["PayFastSettings:ProcessUrl"];
            var queryString = string.Join("&", pfData.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}"));

            return Redirect($"{processUrl}?{queryString}");
        }

        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Notify()
        {
            if (!Request.HasFormContentType) return BadRequest("No form data");

            var form = Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString());
            var receivedSignature = form.GetValueOrDefault("signature", "");
            form.Remove("signature");

            // Recalculate signature
            var sortedData = new SortedDictionary<string, string>(form);
            var calculatedSignature = GeneratePayfastSignature(sortedData, _config["PayFastSettings:Passphrase"]);

            if (!string.Equals(receivedSignature, calculatedSignature, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid signature");

            // Parse amount
            decimal.TryParse(form.GetValueOrDefault("amount_gross", form.GetValueOrDefault("amount", "0")),
                             NumberStyles.Any, CultureInfo.InvariantCulture, out var amount);

            // Map PayFast status
            string status = MapPayFastStatusToLocalStatus(form.GetValueOrDefault("payment_status", "Pending"));

            string mPaymentId = form.GetValueOrDefault("m_payment_id", "");

            try
            {
                // 1️⃣ Fetch the donation by M_PaymentId directly
                var donation = await _firebase.GetDonationByMPaymentId(mPaymentId);

                if (donation != null)
                {
                    // 2️⃣ Update status and PayFast ID
/*                    donation.Status = status;*/
                    donation.PayFastPaymentId = form.GetValueOrDefault("pf_payment_id", "");
                    donation.Timestamp = DateTime.UtcNow;

                    await _firebase.UpdateDonation(donation);
                }
                else
                {
                    // 3️⃣ If donation doesn't exist, create a new record
                    donation = new DonationModel
                    {
                        DonorName = $"{form.GetValueOrDefault("name_first", "")} {form.GetValueOrDefault("name_last", "")}".Trim(),
                        Email = form.GetValueOrDefault("email_address", ""),
                        Amount = amount,
/*                        Status = status,*/
                        M_PaymentId = mPaymentId,
                        PayFastPaymentId = form.GetValueOrDefault("pf_payment_id", ""),
                        Timestamp = DateTime.UtcNow
                    };
                    await _firebase.SaveDonation(donation);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save/update donation from ITN: " + ex);
                return StatusCode(500, "Failed to save donation");
            }

            return Ok("ITN processed");
        }




        [HttpGet]
        public async Task<IActionResult> Success()
        {
            var pendingDonationId = HttpContext.Session.GetString("PendingDonationId");
            if (!string.IsNullOrEmpty(pendingDonationId))
            {
                var donation = await _firebase.GetDonationByMPaymentId(pendingDonationId);
                if (donation != null)
                {
                    donation.Status = "Success"; // mark as successful
                    donation.Timestamp = DateTime.UtcNow;
                    await _firebase.UpdateDonation(donation);
                }

                HttpContext.Session.Remove("PendingDonationId");
            }

            TempData["Message"] = "Thank you! Your donation was successful.";
            return RedirectToAction("Index", "Home");
        }



        [HttpGet]
        public async Task<IActionResult> Cancel()
        {
            var pendingDonationId = HttpContext.Session.GetString("PendingDonationId");
            if (!string.IsNullOrEmpty(pendingDonationId))
            {
                var donation = await _firebase.GetDonationByMPaymentId(pendingDonationId);
                if (donation != null)
                {
                    donation.Status = "Failed"; // mark as canceled
                    donation.Timestamp = DateTime.UtcNow;
                    await _firebase.UpdateDonation(donation);
                }

                HttpContext.Session.Remove("PendingDonationId");
            }

            TempData["Message"] = "You canceled the payment.";
            return RedirectToAction("Index", "Home");
        }



        private string GeneratePayfastSignature(SortedDictionary<string, string> data, string passphrase)
        {
            var sb = new StringBuilder();
            foreach (var kv in data)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    sb.Append(kv.Key).Append('=')
                      .Append(WebUtility.UrlEncode(kv.Value).Replace("%20", "+")).Append('&');
                }
            }

            if (!string.IsNullOrEmpty(passphrase))
                sb.Append("passphrase=").Append(WebUtility.UrlEncode(passphrase).Replace("%20", "+"));
            else if (sb.Length > 0 && sb[sb.Length - 1] == '&') sb.Length--;

            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var hash = md5.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private string MapPayFastStatusToLocalStatus(string pfStatus)
        {
            if (string.IsNullOrWhiteSpace(pfStatus)) return "Pending";

            pfStatus = pfStatus.Trim().ToUpperInvariant();

            return pfStatus switch
            {
                "COMPLETE" => "Success",
                "FAILED" => "Failed",
                "FAIL" => "Failed",
                "CANCELLED" => "Failed",
                "ERROR" => "Failed",
                "EXPIRED" => "Failed",
                "PENDING" => "Pending",
                _ => "Pending"
            };
        }

    }
}
