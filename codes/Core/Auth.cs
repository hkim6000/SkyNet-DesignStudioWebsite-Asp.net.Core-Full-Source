using System.Security.Cryptography;
using System.Text;
using DesignStudio.Models;

namespace DesignStudio.Core
{
    public static class Auth
    {
        private const int Hours = 8;
        private static readonly Dictionary<string, List<DateTime>> Misses = new Dictionary<string, List<DateTime>>();

        public static async Task<Ctx?> Check(string? token)
        {
            string tok = (token ?? string.Empty).Trim();
            if (tok.Length < 16 || tok.Length > 64)
            {
                return null;
            }
            Session? found;
            using (await Store.Hold())
            {
                List<Session> list = await Store.Read(Store.SessionFile, () => new List<Session>());
                found = list.FirstOrDefault(s => s.Token == tok && s.Expires > DateTime.UtcNow);
                if (found != null && found.Expires < DateTime.UtcNow.AddHours(Hours - 1))
                {
                    found.Expires = DateTime.UtcNow.AddHours(Hours);
                    list.RemoveAll(s => s.Expires <= DateTime.UtcNow);
                    await Store.Write(Store.SessionFile, list);
                }
            }
            if (found == null)
            {
                return null;
            }
            Staff? user = await Find(found.UserId);
            return user == null ? null : new Ctx { U = user, Token = tok };
        }

        public static async Task<Staff?> Find(string id)
        {
            if (id == "u-demo")
            {
                return Store.Demo() ? Store.DemoManager() : null;
            }
            return (await Store.LoadStaff()).FirstOrDefault(s => s.Id == id && !s.MustChange && s.Hash != string.Empty);
        }

        public static async Task<string> Open(string userId)
        {
            string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            using (await Store.Hold())
            {
                List<Session> list = await Store.Read(Store.SessionFile, () => new List<Session>());
                list.RemoveAll(s => s.Expires <= DateTime.UtcNow);
                list.Add(new Session { Token = token, UserId = userId, Expires = DateTime.UtcNow.AddHours(Hours) });
                await Store.Write(Store.SessionFile, list);
            }
            return token;
        }

        public static async Task Close(Func<Session, bool> which)
        {
            using (await Store.Hold())
            {
                List<Session> list = await Store.Read(Store.SessionFile, () => new List<Session>());
                if (list.RemoveAll(s => which(s)) > 0)
                {
                    await Store.Write(Store.SessionFile, list);
                }
            }
        }

        public static bool Paused(string login)
        {
            lock (Misses)
            {
                if (!Misses.TryGetValue(login, out List<DateTime>? list))
                {
                    return false;
                }
                list.RemoveAll(t => t < DateTime.UtcNow.AddMinutes(-5));
                return list.Count >= 6;
            }
        }

        public static void Missed(string login)
        {
            lock (Misses)
            {
                if (!Misses.TryGetValue(login, out List<DateTime>? list))
                {
                    list = new List<DateTime>();
                    Misses[login] = list;
                }
                list.Add(DateTime.UtcNow);
            }
        }

        public static void Cleared(string login)
        {
            lock (Misses)
            {
                Misses.Remove(login);
            }
        }

        public static async Task<int> Unread()
        {
            return (await Store.LoadInquiries()).Count(m => m.Status == "new");
        }

        public static readonly (string Page, string Name)[] Areas =
        {
            ("Admin", "Dashboard"),
            ("AdminPages", "Pages"),
            ("AdminMedia", "Media library"),
            ("AdminProjects", "Projects"),
            ("AdminServices", "Services"),
            ("AdminTeam", "Team"),
            ("AdminJournal", "Journal and press"),
            ("AdminMessages", "Messages"),
            ("AdminSettings", "Settings"),
            ("AdminActivity", "Activity log"),
            ("AdminAccount", "My account")
        };

        public static async Task<string> Menu(string prefix, string current, Ctx c)
        {
            int unread = await Unread();
            StringBuilder sb = new StringBuilder();
            foreach ((string page, string name) in Areas)
            {
                sb.Append("<a class=\"" + prefix + "-mi" + (page == current ? " " + prefix + "-on" : string.Empty) + "\" href=\"" + page + "\"><span>" + Ui.Esc(name) + "</span>" + (page == "AdminMessages" && unread > 0 ? "<i class=\"" + prefix + "-badge\">" + unread + "</i>" : string.Empty) + "</a>");
            }
            return sb.ToString();
        }

        public static string Who(string prefix, Ctx c)
        {
            return "<b>" + Ui.Esc(c.U.Name) + "</b><small>" + Ui.Esc(c.U.Title.Length > 0 ? c.U.Title : "Staff") + "</small>";
        }
    }
}
