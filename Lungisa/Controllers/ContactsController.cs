// Payal and Nyanda 
// 15 August 2025
// Handles contact messages submitted via the contact form. Admins can view all contact entries from Firebase and send thank-you emails back to users who
// reached out.

using System.Net.Mail;
using System.Net;
using Lungisa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lungisa.Controllers
{
    public class ContactsController : Controller
    {
        // Firebase service to interact with Firebase Realtime Database
        private readonly FirebaseService firebase;
        // Helper service to handle sending emails
        private readonly EmailHelper emailHelper;

        // Constructor: inject FirebaseService and EmailHelper via dependency injection
        public ContactsController(FirebaseService firebase, EmailHelper emailHelper)
        {
            this.firebase = firebase;
            this.emailHelper = emailHelper;
        }

        // GET: /Contacts/Index
        // Retrieves all contact messages and displays them in the Contacts view
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            // Get all contacts from Firebase, or return an empty list if null
            var contacts = await firebase.GetAllContacts() ?? new List<Lungisa.Models.ContactModel>();

            // Pass temporary success/error messages to the view
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            // Return the Contacts view with the list of contacts
            return View("~/Views/Admin/Contacts.cshtml", contacts);
        }

        // GET: /Contacts/Contacts
        // Same as Index, can be used for a different route if needed
        [HttpGet]
        public async Task<ActionResult> Contacts()
        {
            var contacts = await firebase.GetAllContacts() ?? new List<Lungisa.Models.ContactModel>();

            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            return View("~/Views/Admin/Contacts.cshtml", contacts);
        }

        // POST: /Contacts/SendContactEmail
        // Sends a thank-you email to a user who submitted a contact form
        [HttpPost]
        public async Task<ActionResult> SendContactEmail(string email, string name)
        {
            try
            {
                // Email subject and body
                string subject = "Thank You for Contacting Us";
                string body = $"Dear {name},\n\nThank you for reaching out to Lungisa NPO. " +
                              $"We have received your message and will get back to you shortly.\n\n" +
                              "Best Regards,\nLungisa NPO Team";

                // Use the email helper service to send the email asynchronously
                await emailHelper.SendEmailAsync(email, name, subject, body);

                // Set a temporary success message to display in the view
                TempData["Success"] = "Email sent successfully!";
            }
            catch (Exception ex)
            {
                // Catch any errors during email sending and set an error message
                TempData["Error"] = "Error sending email: " + ex.Message;
            }

            // Redirect back to the Contacts page to show updated messages
            return RedirectToAction("Contacts");
        }
        [HttpPost]
        public async Task<ActionResult> SendCustomReply(string email, string name, string message)
        {
            try
            {
                string subject = "Reply from Lungisa NPO";

                string body = $"Dear {name},\n\n{message}\n\nBest Regards,\nLungisa NPO Team";

                await emailHelper.SendEmailAsync(email, name, subject, body);

                TempData["Success"] = "Custom reply sent successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error sending email: " + ex.Message;
            }

            return RedirectToAction("Contacts");
        }
        [HttpPost]
        public async Task<ActionResult> SendBulkReply(string emails, string message)
        {
            try
            {
                string subject = "Reply from Lungisa NPO";
                var emailList = emails.Split(',');

                foreach (var email in emailList)
                {
                    await emailHelper.SendEmailAsync(email, "", subject, $"Dear user,\n\n{message}\n\nBest Regards,\nLungisa NPO Team");
                }

                TempData["Success"] = "Message sent to selected contacts successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error sending emails: " + ex.Message;
            }

            return RedirectToAction("Contacts");
        }

    }
}