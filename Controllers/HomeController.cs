using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Security;
using TimeManagementApp.Models;
using TimeManagementApp.Models.ViewModel;
using PagedList;

namespace TimeManagementApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly TMDatabaseEntities db = new TMDatabaseEntities();

        public ActionResult Index(string searchV, string sortO, string currentFilter, int? page)
        {
            int? userId = db.Users.Where(x => x.Email == System.Web.HttpContext.Current.User.Identity.Name).FirstOrDefault()?.Id;

            if (userId != null)
            {
                ViewBag.userId = userId;
            }

            var tasks = db.Tasks.Include(t => t.User);


            if (searchV != null)
            {
                page = 1;
            }
            else
            {
                searchV = currentFilter;
            }
            ViewBag.CurrentFilter = searchV;

            if (!String.IsNullOrEmpty(searchV))
            {
                tasks = tasks.Where(s => s.TaskName.Contains(searchV) || s.Priority.Contains(searchV));
            }

            ViewBag.CurrentSort = sortO;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortO) ? "name_desc" : "";
            ViewBag.DateSortParm = sortO == "Date" ? "date_desc" : "Date";
            ViewBag.DeadlineSortParm = sortO == "endDate" ? "endDate_desc" : "endDate";

            switch (sortO)
            {
                case "name_desc":
                    tasks = tasks.OrderByDescending(s => s.TaskName);
                    break;
                case "Date":
                    tasks = tasks.OrderBy(s => s.StartTime);
                    break;
                case "date_desc":
                    tasks = tasks.OrderByDescending(s => s.StartTime);
                    break;
                case "endDate":
                    tasks = tasks.OrderBy(s => s.EndTime);
                    break;
                case "endDate_desc":
                    tasks = tasks.OrderByDescending(s => s.EndTime);
                    break;
                default:
                    tasks = tasks.OrderBy(t => t.TaskName);
                    break;
            }

            var model = tasks.Where(x => x.UserId == userId);

            int pageSize = 6;
            int pageNumber = (page ?? 1);

            return View(model.ToPagedList(pageNumber, pageSize));

        }


        public ActionResult About()
        {
            return View();
        }

        public ActionResult Gantt()
        {
            return View();
        }

        public ActionResult Calendar()
        {
            return View();
        }

        public ActionResult SignUp()
        {
            return View();
        }

        readonly Converter helper = new Converter();

        [HttpPost]
        public ActionResult SaveSignUp(SignUp details)
        {
            string salt = helper.CreateSalt(5);
            string hashedPassword = helper.GenerateSha256Hash(details.Password , salt);
            hashedPassword = hashedPassword.Replace("-", string.Empty).Substring(0, 16);

            if (ModelState.IsValid)
            {
                using (var dbContext = new TMDatabaseEntities())
                {
                    User user = new User
                    {
                        Firstname = details.Firstname,
                        Surname = details.Surname,
                        Email = details.Email,
                        Password = hashedPassword,
                        Salt = salt
                    };

                    dbContext.Users.Add(user);
                    dbContext.SaveChanges();
                }

                ViewBag.Message = "Successfully Registered";
                return View("SignUp");
            }
            else
            {
                return View("SignUp", details);
            }
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var isValid = IsValid(model);

                if (isValid != null)
                {
                    FormsAuthentication.SetAuthCookie(model.Email, false);
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("Error", "Incorrect Email & Password Combination");
                    return View();
                }

            }
            else
            {
                return View(model);
            }

        }

        public User IsValid(LoginModel model)
        {

            using (var dbContext = new TMDatabaseEntities())
            {
                User user;

                user = dbContext.Users.Where(x => x.Email.Equals(model.Email)).SingleOrDefault();
                
                string hashedPassword = helper.GenerateSha256Hash(model.Password, user.Salt);
                hashedPassword = hashedPassword.Replace("-", string.Empty).Substring(0, 16);

                if (user.Password == hashedPassword)
                {
                    if (user == null)
                    {
                        return null;
                    }
                    else
                    {
                        return user;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            return RedirectToAction("Index");
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Task task = db.Tasks.Find(id);
            if (task == null)
            {
                return HttpNotFound();
            }
            return View(task);
        }

        public ActionResult Create()
        {
            ViewBag.UserId = new SelectList(db.Users, "Id", "Firstname");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,TaskName,Description,StartTime,EndTime,Priority, UserId")] Task task)
        {
            var userId = db.Users.Where(x => x.Email == System.Web.HttpContext.Current.User.Identity.Name).FirstOrDefault().Id;

            if (ModelState.IsValid)
            {
                task.UserId = userId;
                db.Tasks.Add(task);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.UserId = new SelectList(db.Users, "Id", "Firstname", task.UserId);
            return View(task);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Task task = db.Tasks.Find(id);
            if (task == null)
            {
                return HttpNotFound();
            }
            ViewBag.UserId = new SelectList(db.Users, "Id", "Firstname", task.UserId);
            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,TaskName,Description,StartTime,EndTime,Priority,UserId")] Task task) 
        {
            var userId = db.Users.Where(x => x.Email == System.Web.HttpContext.Current.User.Identity.Name).FirstOrDefault().Id;

            if (ModelState.IsValid)
            {
                task.UserId = userId;
                db.Entry(task).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.UserId = new SelectList(db.Users, "Id", "Firstname", task.UserId);
            return View(task);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Task task = db.Tasks.Find(id);
            if (task == null)
            {
                return HttpNotFound();
            }
            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Task task = db.Tasks.Find(id);
            db.Tasks.Remove(task);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}