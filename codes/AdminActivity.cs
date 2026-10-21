using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class AdminActivity : WebPage
    {
        private const int Size = 15;

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle("Activity log | " + site.Brand + " staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminActivityJs.expired();");
            }
            return c;
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminActivityJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("ac-brand", Ui.Esc(site.Brand));
            response.SetElementContents("ac-menu", await Auth.Menu("ac", "AdminActivity", c));
            response.SetElementContents("ac-who", Auth.Who("ac", c));
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
            List<ActivityEntry> log = await Store.LoadActivity();
            response.SetElementContents("ac-main", PageHtml(log));
            response.SetElementContents("ac-list", ListHtml(log, string.Empty, string.Empty, 1));
            return response;
        }

        public async Task<ApiResponse> Rows()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            List<ActivityEntry> log = await Store.LoadActivity();
            string res = Ui.Pick(GetDataValue("res"), new[] { "ok", "bad" });
            response.SetElementContents("ac-list", ListHtml(log, res, Ui.Clip(GetDataValue("term"), 40), Ui.Int(GetDataValue("pn"), 1, 1000, 1)));
            return response;
        }

        private static string PageHtml(List<ActivityEntry> log)
        {
            return "<div class=\"ac-ph\"><div><h1>Activity log</h1><p>Every change made in the staff area, newest first. The latest 5,000 entries are kept.</p></div></div><div class=\"ac-tools\"><div class=\"ac-chips\" id=\"ac-chips\"><button type=\"button\" class=\"ac-chip ac-act\" data-res=\"\" onclick=\"AdminActivityJs.result('')\">All <i>" + log.Count + "</i></button><button type=\"button\" class=\"ac-chip\" data-res=\"ok\" onclick=\"AdminActivityJs.result('ok')\">Done <i>" + log.Count(a => a.Ok) + "</i></button><button type=\"button\" class=\"ac-chip\" data-res=\"bad\" onclick=\"AdminActivityJs.result('bad')\">Refused <i>" + log.Count(a => !a.Ok) + "</i></button></div><input type=\"search\" id=\"ac-q\" placeholder=\"Search action or target\" maxlength=\"40\" oninput=\"AdminActivityJs.type()\" aria-label=\"Search the log\"></div><div id=\"ac-list\"></div>";
        }

        private static string ListHtml(List<ActivityEntry> log, string res, string term, int pn)
        {
            List<ActivityEntry> rows = log.Where(a => (res.Length == 0 || (res == "ok") == a.Ok) && (term.Length == 0 || Ui.Has(a.Action, term) || Ui.Has(a.Target, term) || Ui.Has(a.Who, term))).ToList();
            if (rows.Count == 0)
            {
                return "<div class=\"ac-panel ac-empty\">" + (log.Count == 0 ? "Nothing has been logged yet." : "No entries match.") + "</div>";
            }
            int pages = (rows.Count + Size - 1) / Size;
            pn = Math.Min(pn, pages);
            StringBuilder sb = new StringBuilder("<div class=\"ac-wrapt\"><table class=\"ac-table ac-stack\"><thead><tr><th>When</th><th>Who</th><th>Action</th><th>Target</th><th>Result</th></tr></thead><tbody>");
            foreach (ActivityEntry a in rows.Skip((pn - 1) * Size).Take(Size))
            {
                sb.Append("<tr><td class=\"ac-first\"><b>" + Ui.Esc(Ui.When(a.When)) + "</b></td><td data-l=\"Who\">" + Ui.Esc(a.Who) + "</td><td data-l=\"Action\">" + Ui.Esc(a.Action) + "</td><td data-l=\"Target\">" + Ui.Esc(a.Target) + "</td><td data-l=\"Result\"><span class=\"ac-pill " + (a.Ok ? "ac-pill-go" : "ac-pill-bad") + "\">" + (a.Ok ? "Done" : "Refused") + "</span></td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"ac-pager\"><span>Page " + pn + " of " + pages + " &middot; " + Ui.Count(rows.Count, "entry", "entries") + "</span><div>" + (pn > 1 ? "<button type=\"button\" class=\"ac-btn ac-btn-ghost ac-btn-sm\" onclick=\"AdminActivityJs.go(" + (pn - 1) + ")\">Previous</button>" : string.Empty) + (pn < pages ? "<button type=\"button\" class=\"ac-btn ac-btn-ghost ac-btn-sm\" onclick=\"AdminActivityJs.go(" + (pn + 1) + ")\">Next</button>" : string.Empty) + "</div></div>");
            return sb.ToString();
        }
    }
}
