// Payal and Nyanda
// 15 August 2025
// Provides an admin interface for managing news articles. Allows admins to create, view, edit, and delete news articles stored in Firebase.
// Handles optional image uploads and ensures a default image is used if no image is provided.

using Lungisa.Models;
using Lungisa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lungisa.Controllers
{
    public class NewsAdminController : Controller
    {
        private readonly FirebaseService firebase;
        private readonly IWebHostEnvironment _env;

        public NewsAdminController(FirebaseService firebase, IWebHostEnvironment env)
        {
            _env = env;
            this.firebase = firebase;
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var news = await firebase.GetAllNewsWithKeys()
                       ?? new List<FirebaseService.FirebaseNewsArticle>();

            return View("~/Views/Admin/NewsAdmin.cshtml", news);
        }

        // ADD NEWS ARTICLE
        [HttpPost]
        public async Task<ActionResult> AddNews(string title, string summary, string body, IFormFile image)
        {
            // Default image used if none is uploaded
            string imageUrl = "/Content/Images/newsletter.png";

            // Only replace the default if an image is uploaded
            if (image != null && image.Length > 0)
            {
                string fileName = Path.GetFileName(image.FileName);
                string path = Path.Combine(_env.WebRootPath, "Content", "Images", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                imageUrl = "/Content/Images/" + fileName;
            }

            var article = new NewsArticleModel
            {
                Title = title,
                Summary = summary,
                Body = body,
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                ImageUrl = imageUrl
            };

            await firebase.SaveNews(article);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> Details(string id)
        {
            var allNews = await firebase.GetAllNewsWithKeys();
            var article = allNews.FirstOrDefault(a => a.Key == id)?.Article;

            if (article == null)
                return NotFound();

            return View("~/Views/Home/NewsDetails.cshtml", article);
        }

        public async Task<ActionResult> Delete(string id)
        {
            await firebase.DeleteNews(id);
            TempData["Success"] = "🗑️ News article deleted successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> EditNews(string id)
        {
            var firebaseArticle = await firebase.GetAllNewsWithKeys();
            var articleData = firebaseArticle.FirstOrDefault(a => a.Key == id);

            if (articleData == null) return NotFound();

            ViewBag.ArticleToEdit = articleData.Article;
            ViewBag.ArticleKey = articleData.Key;

            return View("~/Views/Admin/NewsAdmin.cshtml", firebaseArticle);
        }


        // UPDATE NEWS ARTICLE
        [HttpPost]
        public async Task<ActionResult> UpdateNews(string articleKey, string title, string summary, string body, IFormFile image)
        {
            var existingArticle = (await firebase.GetAllNewsWithKeys())
                                  .FirstOrDefault(a => a.Key == articleKey)?.Article;

            if (existingArticle == null) return NotFound();

            // Start with default image
            string imageUrl = "/Content/Images/newsletter.png";

            // Use existing image only if it's not null/empty AND the file actually exists
            if (!string.IsNullOrEmpty(existingArticle.ImageUrl))
            {
                string physicalPath = Path.Combine(_env.WebRootPath, existingArticle.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(physicalPath))
                {
                    imageUrl = existingArticle.ImageUrl;
                }
            }

            // If a new image is uploaded, overwrite the imageUrl
            if (image != null && image.Length > 0)
            {
                string fileName = Path.GetFileName(image.FileName);
                string path = Path.Combine(_env.WebRootPath, "Content", "Images", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                    await image.CopyToAsync(stream);

                imageUrl = "/Content/Images/" + fileName;
            }

            var updatedArticle = new NewsArticleModel
            {
                Title = title,
                Summary = summary,
                Body = body,
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                ImageUrl = imageUrl
            };

            await firebase.UpdateNews(articleKey, updatedArticle);

            TempData["Success"] = "✅ News article updated successfully!";
            return RedirectToAction("Index");
        }

    }
}
