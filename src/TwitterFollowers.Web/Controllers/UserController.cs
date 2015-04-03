using System;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using TwitterFollowers.Web.DAL;
using TwitterFollowers.Web.Models;
using TwitterFollowers.Web.Services;
using TwitterOAuth.RestAPI.Models;

namespace TwitterFollowers.Web.Controllers
{
    public class UserController : Controller
    {
        private static Twitter _twitter;

        public UserController()
        {
            if (_twitter == null)
                _twitter = new Twitter();
        }

        public async Task<ActionResult> Find(long? id)
        {
            await AddUpdateUsersFromIds("user_id", id.ToString(), false, true, false);

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Load(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = db.Users.Find(id);
            var userLookup = await _twitter.UserAsync(id.ToString());

            _twitter.MapTwitterUserToUser(user, userLookup);

            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> RegenerateFriends()
        {
            await AddUpdateUsersFromIds("screen_name", _twitter.UserName, true, false);

            return RedirectToAction("Index");
        }

        private async Task AddUpdateUsersFromIds(string parameter, string userNameOrId, bool following, bool autoAdded, bool updateIfExists = true)
        {
            var friendsIdsModel = await _twitter.FriendsIdsAsync(parameter, userNameOrId);
            var ids = friendsIdsModel.ids.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToList();

            var count = (ids.Count / 100) + 1;

            for (var i = 0; i < count; i++)
            {
                var list = ids.Skip(100 * i).Take(100).ToList();
                var usersTwitter = await _twitter.UsersAsync(list);

                foreach (var userLookup in usersTwitter)
                {
                    var user = db.Users.Find(userLookup.id);
                    if (user != null)
                    {
                        if (!updateIfExists) continue;

                        _twitter.MapTwitterUserToUser(user, userLookup);

                        user.Following = following;

                        db.Entry(user).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        user = new User
                        {
                            AutoAdded = autoAdded,
                            Following = following
                        };

                        _twitter.MapTwitterUserToUser(user, userLookup);

                        db.Users.Add(user);
                        db.SaveChanges();
                    }
                }
            }
        }

        // Default methods
        private TwitterFollowersContext db = new TwitterFollowersContext();

        // GET: Users

        public ActionResult Index(string sortOrder, string filter)
        {
            ViewBag.IdSortParm = String.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewBag.UserSortParm = sortOrder == "User" ? "user_desc" : "User";
            ViewBag.FollowersSortParm = sortOrder == "Followers" ? "followers_desc" : "Followers";
            ViewBag.FollowingSortParm = sortOrder == "Following" ? "following_asc" : "Following";

            var users = from u in db.Users
                        select u;

            switch (sortOrder)
            {
                case "id_desc":
                    users = users.OrderByDescending(u => u.Id);
                    break;
                case "User":
                    users = users.OrderBy(u => u.Name);
                    break;
                case "user_desc":
                    users = users.OrderByDescending(u => u.Name);
                    break;
                case "Followers":
                    users = users.OrderBy(u => u.FollowersCount);
                    break;
                case "followers_desc":
                    users = users.OrderByDescending(u => u.FollowersCount);
                    break;
                case "Following":
                    users = users.OrderByDescending(u => u.Following);
                    break;
                case "following_asc":
                    users = users.OrderBy(u => u.Following);
                    break;
                default:
                    users = users.OrderBy(u => u.Id);
                    break;
            }

            return View(users.ToList());
        }

        // GET: Users/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // GET: Users/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,ScreenName,ProfileImageUrl,CreatedAt,Location,FollowRequestSent,IsTranslator,IdStr,Url,ProfileImageUrlHttps,UtcOffset,ProfileUseBackgroundImage,ListedCount,Lang,FollowersCount,Protected,Notifications,Verified,GeoEnabled,TimeZone,Description,DefaultProfileImage,ProfileBackgroundImageUrl,StatusesCount,FriendsCount,Following,IsTranslationEnabled,ProfileBannerUrl")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // GET: Users/Edit/5
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,ScreenName,ProfileImageUrl,CreatedAt,Location,FollowRequestSent,IsTranslator,IdStr,Url,ProfileImageUrlHttps,UtcOffset,ProfileUseBackgroundImage,ListedCount,Lang,FollowersCount,Protected,Notifications,Verified,GeoEnabled,TimeZone,Description,DefaultProfileImage,ProfileBackgroundImageUrl,StatusesCount,FriendsCount,Following,IsTranslationEnabled,ProfileBannerUrl")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
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
