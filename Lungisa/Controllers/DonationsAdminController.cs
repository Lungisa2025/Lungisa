// Payal and Nyanda
// 15 August 2025
// Provides an admin interface for managing donations. Allows administrators to view all donations stored in Firebase and send personalized thank-you emails
// to donors via the EmailHelper service.

using Lungisa.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace Lungisa.Controllers
{
    public class DonationsAdminController : Controller
    {
        // Firebase service to interact with donation data
        private readonly FirebaseService firebase;
        // Helper service for sending emails
        private readonly EmailHelper emailHelper;

        // Constructor: inject FirebaseService and EmailHelper via dependency injection
        public DonationsAdminController(FirebaseService firebase, EmailHelper emailHelper)
        {
            this.firebase = firebase;
            this.emailHelper = emailHelper;
        }

        [HttpGet]
        public async Task<IActionResult> AdminDonations()
        {
            var donationsWithKeys = await firebase.GetDonationsWithKeys();
            var donations = donationsWithKeys
                                .Select(d => d.Donation)
                                .OrderByDescending(d => d.Timestamp)
                                .ToList();

            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            return View("~/Views/Admin/DonationsAdmin.cshtml", donations);
        }

        // GET: /DonationsAdmin/Index
        // Loads the Donations management page and retrieves all donations
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            // Fetch all donations from Firebase; return empty list if null
            var donations = await firebase.GetDonations() ?? new List<Lungisa.Models.DonationModel>();

            // Pass any temporary success/error messages to the view
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            // Return the DonationsAdmin view with the list of donations
            return View("~/Views/Admin/DonationsAdmin.cshtml", donations);
        }

        // GET: /DonationsAdmin/Donations
        // Explicit Donations action; functions the same as Index
        [HttpGet]
        public async Task<ActionResult> Donations()
        {
            var donations = await firebase.GetDonations() ?? new List<Lungisa.Models.DonationModel>();

            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            return View("~/Views/Admin/DonationsAdmin.cshtml", donations);
        }

        // POST: /DonationsAdmin/SendDonorEmail
        // Sends a personalized thank-you email to a donor
        [HttpPost]
        public async Task<ActionResult> SendDonorEmail(string email, string name, decimal amount)
        {
            try
            {
                // Prepare the email subject and body
                string subject = "Thank You for Your Donation";
                string body = $"Dear {name},\n\nWe are deeply grateful for your generous donation of R{amount}. " +
                              $"Your support helps Lungisa NPO continue its mission and make a positive impact in our community.\n\n" +
                              "Best Regards,\nLungisa NPO Team";

                // Send the email asynchronously using the EmailHelper service
                await emailHelper.SendEmailAsync(email, name, subject, body);

                // Set a temporary success message to display in the view
                TempData["Success"] = "Thank-you email sent successfully!";
            }
            catch (Exception ex)
            {
                // Catch any errors during email sending and set an error message
                TempData["Error"] = "Error sending email: " + ex.Message;
            }

            // Redirect back to the Donations page to show updated messages
            return RedirectToAction("Donations");
        }

    }

}