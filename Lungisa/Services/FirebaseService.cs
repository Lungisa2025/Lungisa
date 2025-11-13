using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Lungisa.Models;

namespace Lungisa.Services
{
    public class FirebaseService
    {
        private readonly FirebaseApp _firebaseApp;
        private readonly FirebaseClient _firebaseClient;

        public FirebaseService(IConfiguration config, IWebHostEnvironment env)
        {
            GoogleCredential credential = null;

            // Load Firebase service account JSON (local development)
            string path = Path.Combine(env.ContentRootPath, "Config", "firebaseServiceAccount.json");
            if (!File.Exists(path))
                throw new Exception("Firebase service account file not found.");

            credential = GoogleCredential.FromFile(path);

            // Initialize Firebase Admin SDK
            if (FirebaseApp.DefaultInstance == null)
            {
                _firebaseApp = FirebaseApp.Create(new AppOptions
                {
                    Credential = credential
                });
            }
            else
            {
                _firebaseApp = FirebaseApp.DefaultInstance;
            }

            // Initialize Firebase Realtime Database client (Admin SDK handles auth)
            _firebaseClient = new FirebaseClient(config["Firebase:DatabaseUrl"]);
        }

        // ===================== AUTH =====================
        public async Task<UserRecord> CreateUserAsync(string email, string password)
        {
            var args = new UserRecordArgs
            {
                Email = email,
                Password = password
            };
            return await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
        }

        public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
            => await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

        // ===================== PROJECTS =====================
        public async Task<List<FirebaseProject>> GetAllProjectsWithKeys()
        {
            var firebaseProjects = await _firebaseClient.Child("Projects").OnceAsync<ProjectModel>();
            return firebaseProjects.Select(p => new FirebaseProject { Key = p.Key, Project = p.Object }).ToList();
        }

        public async Task SaveProject(ProjectModel project)
            => await _firebaseClient.Child("Projects").PostAsync(project);

        public async Task DeleteProject(string key)
            => await _firebaseClient.Child("Projects").Child(key).DeleteAsync();

        public class FirebaseProject
        {
            public string Key { get; set; }
            public ProjectModel Project { get; set; }
        }

        public async Task<List<ProjectModel>> GetAllProjects()
        {
            var projects = await _firebaseClient.Child("Projects").OnceAsync<ProjectModel>();
            return projects.Select(p => p.Object).ToList();
        }

        public async Task<FirebaseProject> GetProjectByKey(string key)
        {
            var project = await _firebaseClient.Child("Projects").Child(key).OnceSingleAsync<ProjectModel>();
            if (project == null) return null;
            return new FirebaseProject { Key = key, Project = project };
        }

        public async Task UpdateProject(string key, ProjectModel project)
            => await _firebaseClient.Child("Projects").Child(key).PutAsync(project);

        // ===================== EVENTS =====================
        public async Task<List<FirebaseEvent>> GetAllEventsWithKeys()
        {
            var firebaseEvents = await _firebaseClient.Child("Events").OnceAsync<EventModel>();
            return firebaseEvents.Select(e => new FirebaseEvent { Key = e.Key, Event = e.Object }).ToList();
        }

        public async Task SaveEvent(EventModel eventModel)
            => await _firebaseClient.Child("Events").PostAsync(eventModel);

        public async Task DeleteEvent(string key)
            => await _firebaseClient.Child("Events").Child(key).DeleteAsync();

        public class FirebaseEvent
        {
            public string Key { get; set; }
            public EventModel Event { get; set; }
        }

        public async Task<List<EventModel>> GetAllEvents()
        {
            var events = await _firebaseClient.Child("Events").OnceAsync<EventModel>();
            return events.Select(e => e.Object).ToList();
        }

        public async Task<FirebaseEvent> GetEventByKey(string key)
        {
            var eventModel = await _firebaseClient.Child("Events").Child(key).OnceSingleAsync<EventModel>();
            if (eventModel == null) return null;
            return new FirebaseEvent { Key = key, Event = eventModel };
        }

        public async Task UpdateEvent(string key, EventModel eventModel)
            => await _firebaseClient.Child("Events").Child(key).PutAsync(eventModel);

        // ===================== NEWS =====================
        public async Task<List<FirebaseNewsArticle>> GetAllNewsWithKeys()
        {
            var newsList = await _firebaseClient.Child("News").OnceAsync<NewsArticleModel>();
            return newsList.Select(n => new FirebaseNewsArticle { Key = n.Key, Article = n.Object }).ToList();
        }

        public async Task SaveNews(NewsArticleModel article)
            => await _firebaseClient.Child("News").PostAsync(article);

        public async Task DeleteNews(string key)
            => await _firebaseClient.Child("News").Child(key).DeleteAsync();

        public class FirebaseNewsArticle
        {
            public string Key { get; set; }
            public NewsArticleModel Article { get; set; }
        }

        public async Task UpdateNews(string key, NewsArticleModel article)
            => await _firebaseClient.Child("News").Child(key).PutAsync(article);

        // ===================== SUBSCRIBERS =====================
        public async Task SaveSubscriber(SubscriberModel subscriber)
            => await _firebaseClient.Child("Subscribers").PostAsync(subscriber);

        public async Task<List<SubscriberModel>> GetAllSubscribers()
        {
            var subscribers = await _firebaseClient.Child("Subscribers").OnceAsync<SubscriberModel>();
            return subscribers.Select(x => x.Object).ToList();
        }

        // ===================== CONTACTS =====================
        public async Task SaveContact(ContactModel contact)
            => await _firebaseClient.Child("Contacts").PostAsync(contact);

        public async Task<List<ContactModel>> GetAllContacts()
        {
            var contacts = await _firebaseClient.Child("Contacts").OnceAsync<ContactModel>();
            return contacts.Select(x => x.Object).ToList();
        }

        // ===================== VOLUNTEERS =====================
        public async Task SaveVolunteer(VolunteerModel volunteer)
            => await _firebaseClient.Child("Volunteers").PostAsync(volunteer);

        public async Task<List<VolunteerModel>> GetAllVolunteers()
        {
            var volunteers = await _firebaseClient.Child("Volunteers").OnceAsync<VolunteerModel>();
            return volunteers.Select(x => x.Object).ToList();
        }

        // ===================== DONATIONS =====================
        public async Task SaveDonation(DonationModel donation)
            => await _firebaseClient.Child("Donations").PostAsync(donation);

        public async Task<List<DonationModel>> GetDonations()
        {
            var donationsData = await _firebaseClient.Child("Donations").OnceAsync<dynamic>();
            var donations = new List<DonationModel>();

            foreach (var d in donationsData)
            {
                var obj = d.Object;

                DateTime timestamp = DateTime.UtcNow;
                try
                {
                    if (obj.Timestamp != null)
                        timestamp = DateTime.Parse(obj.Timestamp.ToString(), null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                }
                catch { timestamp = DateTime.UtcNow; }

                donations.Add(new DonationModel
                {
                    DonorName = obj.DonorName ?? "",
                    Email = obj.Email ?? "",
                    Amount = obj.Amount != null ? Convert.ToDecimal(obj.Amount) : 0,
                    Timestamp = timestamp,
                    Status = obj.Status ?? "Pending",
                    FirstName = obj.FirstName ?? "",
                    LastName = obj.LastName ?? "",
                    PayFastPaymentId = obj.PayFastPaymentId ?? "",
                    M_PaymentId = obj.M_PaymentId ?? "",
                    PaymentReference = obj.PaymentReference ?? ""
                });
            }

            return donations;
        }

        public async Task<List<(string Key, DonationModel Donation)>> GetDonationsWithKeys()
        {
            var donationsData = await _firebaseClient.Child("Donations").OnceAsync<DonationModel>();
            return donationsData.Select(d => (d.Key, d.Object)).ToList();
        }

        public async Task<DonationModel> GetDonationByMPaymentId(string mPaymentId)
        {
            var donations = await _firebaseClient.Child("Donations").OnceAsync<DonationModel>();
            var record = donations.FirstOrDefault(d => d.Object.M_PaymentId == mPaymentId);
            return record?.Object;
        }

        public async Task UpdateDonation(DonationModel donation)
        {
            var data = new Dictionary<string, object>
            {
                { "DonorName", donation.DonorName ?? "" },
                { "Email", donation.Email ?? "" },
                { "Amount", donation.Amount },
                { "Status", donation.Status ?? "Pending" },
                { "PayFastPaymentId", donation.PayFastPaymentId ?? "" },
                { "Timestamp", donation.Timestamp.ToString("o") },
                { "FirstName", donation.FirstName ?? "" },
                { "LastName", donation.LastName ?? "" },
                { "M_PaymentId", donation.M_PaymentId ?? "" },
                { "PaymentReference", donation.PaymentReference ?? "" }
            };

            var donations = await _firebaseClient.Child("Donations").OnceAsync<DonationModel>();
            var existing = donations.FirstOrDefault(d => d.Object.M_PaymentId == donation.M_PaymentId);

            if (existing != null)
                await _firebaseClient.Child("Donations").Child(existing.Key).PatchAsync(data);
            else
                await SaveDonation(donation);
        }

        // ===================== ADMINS =====================
        public async Task SaveAdmin(string uid, string email, string firstName, string lastName, string phoneNumber, string role)
        {
            var adminData = new
            {
                Uid = uid,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                Role = role,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };

            await _firebaseClient.Child("Admins").Child(uid).PutAsync(adminData);
        }
    }
}
