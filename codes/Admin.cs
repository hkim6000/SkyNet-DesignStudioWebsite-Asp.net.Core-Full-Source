using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class Admin : WebPage
    {
        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle("Dashboard | " + site.Brand + " staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminJs.expired();");
            }
            return c;
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("db-brand", Ui.Esc(site.Brand));
            response.SetElementContents("db-menu", await Auth.Menu("db", "Admin", c));
            response.SetElementContents("db-who", Auth.Who("db", c));
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c != null)
            {
                await Auth.Close(s => s.Token == c.Token);
                await Store.Log(c.U.Id, "sign out", c.U.Login, true);
            }
            return response;
        }


        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            await Shell(response, c, site);
            List<Inquiry> inquiries = await Store.LoadInquiries();
            List<ActivityEntry> log = await Store.LoadActivity();
            response.SetElementContents("db-main", MainHtml(site, inquiries, log, c));
            return response;
        }

        private static string Tile(string href, string label, string value, string note, bool hot)
        {
            return "<a class=\"db-tile" + (hot ? " db-hot" : string.Empty) + "\" href=\"" + href + "\"><span>" + Ui.Esc(label) + "</span><b>" + Ui.Esc(value) + "</b><small>" + Ui.Esc(note) + "</small></a>";
        }

        private static string MainHtml(SiteData site, List<Inquiry> inquiries, List<ActivityEntry> log, Ctx c)
        {
            int fresh = inquiries.Count(m => m.Status == "new");
            int live = site.Projects.Count(p => p.Published);
            StringBuilder sb = new StringBuilder();
            sb.Append("<div class=\"db-ph\"><div><h1>Welcome back, " + Ui.Esc(c.U.Name.Split(' ')[0]) + "</h1><p>Here is what is happening on the " + Ui.Esc(site.Brand) + " website.</p></div></div>");
            sb.Append("<div class=\"db-tiles\">");
            sb.Append(Tile("AdminMessages", "New messages", fresh.ToString(), fresh == 0 ? "All caught up" : "Waiting for a reply", fresh > 0));
            sb.Append(Tile("AdminProjects", "Projects", live.ToString(), site.Projects.Count - live == 0 ? "All published" : (site.Projects.Count - live) + " hidden", false));
            sb.Append(Tile("AdminServices", "Services", site.Services.Count(s => s.Published).ToString(), "Published on the website", false));
            sb.Append(Tile("AdminTeam", "Team", site.Team.Count(t => t.Published).ToString(), "People on the Studio page", false));
            sb.Append(Tile("AdminJournal", "Journal and press", site.Journal.Count(e => e.Published).ToString(), Ui.Count(site.Journal.Count(e => e.Published && e.Kind == "press"), "press piece", "press pieces"), false));
            sb.Append("</div><div class=\"db-cols\"><section class=\"db-panel\"><div class=\"db-ph2\"><h2>Latest inquiries</h2><a href=\"AdminMessages\">Open inbox</a></div>");
            List<Inquiry> latest = inquiries.OrderByDescending(m => m.Created).Take(4).ToList();
            if (latest.Count == 0)
            {
                sb.Append("<p class=\"db-empty\">No inquiries yet. They appear here when someone uses the form on the Contact page.</p>");
            }
            foreach (Inquiry m in latest)
            {
                sb.Append("<a class=\"db-row\" href=\"AdminMessages?id=" + Ui.Esc(m.Id) + "\"><span><b>" + Ui.Esc(m.Name) + "</b><small>" + Ui.Esc(Catalog.Kind(m.Kind).Length > 0 ? Catalog.Kind(m.Kind) : "Inquiry") + " &middot; " + Ui.Esc(Ui.When(m.Created)) + "</small></span><i class=\"db-pill db-pill-" + m.Status + "\">" + Ui.Esc(Catalog.Status(m.Status)) + "</i></a>");
            }
            sb.Append("</section><section class=\"db-panel\"><div class=\"db-ph2\"><h2>Recent activity</h2><a href=\"AdminActivity\">Open log</a></div>");
            if (log.Count == 0)
            {
                sb.Append("<p class=\"db-empty\">Nothing has been logged yet.</p>");
            }
            foreach (ActivityEntry a in log.Take(5))
            {
                sb.Append("<div class=\"db-row\"><span><b>" + Ui.Esc(a.Action) + (a.Target.Length > 0 ? ": " + Ui.Esc(a.Target) : string.Empty) + "</b><small>" + Ui.Esc(a.Who) + " &middot; " + Ui.Esc(Ui.When(a.When)) + "</small></span>" + (a.Ok ? string.Empty : "<i class=\"db-pill db-pill-bad\">Refused</i>") + "</div>");
            }
            sb.Append("</section></div><section class=\"db-panel db-quick\"><h2>Quick actions</h2><div class=\"db-actions\"><a class=\"db-btn\" href=\"AdminPages?page=home\">Edit the Home page</a><a class=\"db-btn db-btn-ghost\" href=\"AdminProjects?new=1\">Add a project</a><a class=\"db-btn db-btn-ghost\" href=\"AdminMedia\">Open the media library</a><a class=\"db-btn db-btn-ghost\" href=\"Home\" target=\"_blank\" rel=\"noopener\">View the website</a></div></section>");
            return sb.ToString();
        }
    }
}
