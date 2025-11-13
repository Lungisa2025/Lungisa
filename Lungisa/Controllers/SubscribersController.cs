// Payal and Nyanda
// 15 August 2025
// Handles subscriber management for the admin dashboard. Connects with Firebase to fetch and manage newsletter subscriber data. Provides functionality to 
// send notification emails to subscribers regarding updates, news, and events.


using System.Net.Mail;
using System.Net;
using Lungisa.Services;
using Microsoft.AspNetCore.Mvc;


using System.Net.Mail;
using System.Net;
using Lungisa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lungisa.Controllers
{
    public class SubscribersController : Controller
    {
        // Firebase service to fetch and manage subscriber data
        private readonly FirebaseService firebase;
        // Email helper service to send emails to subscribers
        private readonly EmailHelper emailHelper;

        // Constructor: inject FirebaseService and EmailHelper via dependency injection
        public SubscribersController(FirebaseService firebase, EmailHelper emailHelper)
        {
            this.firebase = firebase;
            this.emailHelper = emailHelper;
        }

        // GET: /Subscribers/Index
        // Loads the Subscribers management page and retrieves all subscribers
        public async Task<ActionResult> Index()
        {
            // Get all subscribers from Firebase, or return an empty list if none exist
            var subscribers = await firebase.GetAllSubscribers() ?? new List<Lungisa.Models.SubscriberModel>();

            // Pass any temporary success/error messages to the view
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            // Return the Subscribers admin view with the list of subscribers
            return View("~/Views/Admin/Subscribers.cshtml", subscribers);
        }

        // GET: /Subscribers/Subscribers
        // Explicit action for loading subscribers page (same as Index)
        [HttpGet]
        public async Task<ActionResult> Subscribers()
        {
            var subscribers = await firebase.GetAllSubscribers() ?? new List<Lungisa.Models.SubscriberModel>();

            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            return View("~/Views/Admin/Subscribers.cshtml", subscribers);
        }

        // POST: /Subscribers/NotifySubscriber
        // Sends a notification email to a subscriber
        [HttpPost]
        public async Task<ActionResult> NotifySubscriber(string email, string name)
        {
            try
            {
                // Prepare the email subject and body
                string subject = "Lungisa Update!";
                string body = $"Dear {name},\n\nWe have an important update for you! " +
                              $"Please check our website for the latest news and opportunities.\n\n" +
                              "Best Regards,\nLungisa NPO Team";

                // Send the email asynchronously using EmailHelper
                await emailHelper.SendEmailAsync(email, name, subject, body);

                // Set temporary success message to display in the view
                TempData["Success"] = $"Email sent to {name}!";
            }
            catch (Exception ex)
            {
                // Set temporary error message if email sending fails
                TempData["Error"] = "Error sending email: " + ex.Message;
            }

            // Redirect back to the Subscribers page to show updated messages
            return RedirectToAction("Subscribers");
        }
        [HttpPost]
        public async Task<ActionResult> NotifyAllSubscribers(string message)
        {
            try
            {
                var subscribers = await firebase.GetAllSubscribers() ?? new List<Lungisa.Models.SubscriberModel>();

                string subject = "Lungisa Update!";
                foreach (var sub in subscribers)
                {
                    await emailHelper.SendEmailAsync(sub.Email, sub.Email, subject, $"Dear subscriber,\n\n{message}\n\nBest Regards,\nLungisa NPO Team");
                }

                TempData["Success"] = "Message sent to all subscribers successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error sending emails: " + ex.Message;
            }

            return RedirectToAction("Subscribers");
        }

    }
}