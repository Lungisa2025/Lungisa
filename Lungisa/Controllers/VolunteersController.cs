// Payal and Nyanda
// 15 August 2025
// Handles volunteer management in the admin panel. This controller connects with Firebase to fetch volunteer data and uses EmailHelper to notify or 
// contact volunteers. Provides functionality to view all volunteers and send emails directly from the admin dashboard.

using Lungisa.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace Lungisa.Controllers
{
    public class VolunteersController : Controller
    {
        // Firebase service to fetch and manage volunteer data
        private readonly FirebaseService firebase;
        // Helper service to send emails to volunteers
        private readonly EmailHelper emailHelper;

        // Constructor: inject FirebaseService and EmailHelper via dependency injection
        public VolunteersController(FirebaseService firebase, EmailHelper emailHelper)
        {
            this.firebase = firebase;
            this.emailHelper = emailHelper;
        }

        // GET: /Volunteers/Index
        // Loads the Volunteers management page and retrieves all volunteers
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            // Get all volunteers from Firebase, or return an empty list if none exist
            var volunteers = await firebase.GetAllVolunteers() ?? new List<Lungisa.Models.VolunteerModel>();

            // Pass any temporary success/error messages to the view
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            // Return the Volunteers admin view with the list of volunteers
            return View("~/Views/Admin/Volunteers.cshtml", volunteers);
        }

        // GET: /Volunteers/Volunteers
        // Explicit action for loading volunteers page (same as Index)
        [HttpGet]
        public async Task<ActionResult> Volunteers()
        {
            var volunteers = await firebase.GetAllVolunteers() ?? new List<Lungisa.Models.VolunteerModel>();

            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            return View("~/Views/Admin/Volunteers.cshtml", volunteers);
        }

        // POST: /Volunteers/SendVolunteerEmail
        // Sends an email to a volunteer notifying them about volunteering opportunities
        [HttpPost]
        public async Task<ActionResult> SendVolunteerEmail(string email, string name)
        {
            try
            {
                // Prepare email subject and body
                string subject = "Volunteering Opportunity";
                string body = $"Dear {name},\n\nThank you for volunteering with Lungisa NPO. " +
                              $"We have new opportunities available and would love your help!\n\n" +
                              "Best Regards,\nLungisa NPO Team";

                // Send the email asynchronously using EmailHelper
                await emailHelper.SendEmailAsync(email, name, subject, body);

                // Set temporary success message to display in the view
                TempData["Success"] = "Email sent successfully!";
            }
            catch (Exception ex)
            {
                // Set temporary error message if email sending fails
                TempData["Error"] = "Error sending email: " + ex.Message;
            }

            // Redirect back to the Volunteers page to display updated messages
            return RedirectToAction("Volunteers");
        }
        [HttpPost]
        public async Task<ActionResult> SendEmailToSelected([FromForm] string[] selectedEmails)
        {
            if (selectedEmails == null || selectedEmails.Length == 0)
            {
                TempData["Error"] = "No volunteers selected!";
                return RedirectToAction("Volunteers");
            }

            try
            {
                // Fetch all volunteers to get their names
                var allVolunteers = await firebase.GetAllVolunteers() ?? new List<Lungisa.Models.VolunteerModel>();

                string subject = "Volunteering Opportunities";
                string bodyTemplate = "Dear {0},\n\nThank you for volunteering with Lungisa NPO. " +
                                      "We have new opportunities available and would love your help!\n\n" +
                                      "Best Regards,\nLungisa NPO Team";

                foreach (var email in selectedEmails)
                {
                    var volunteer = allVolunteers.FirstOrDefault(v => v.Email == email);
                    if (volunteer != null)
                    {
                        string body = string.Format(bodyTemplate, volunteer.FullName);
                        await emailHelper.SendEmailAsync(volunteer.Email, volunteer.FullName, subject, body);
                    }
                }

                TempData["Success"] = "Emails sent to selected volunteers successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error sending emails: " + ex.Message;
            }

            return RedirectToAction("Volunteers");
        }


    }
}