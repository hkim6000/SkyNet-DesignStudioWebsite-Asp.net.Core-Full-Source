using System.Globalization;
using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class AdminJournal : WebPage
    {
        private const int Size = 10;

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle("Journal and press | " + site.Brand + " staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminJournalJs.expired();");
            }
            return c;
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminJournalJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("jr-brand", Ui.Esc(site.Brand));
            response.SetElementContents("jr-menu", await Auth.Menu("jr", "AdminJournal", c));
            response.SetElementContents("jr-who", Auth.Who("jr", c));
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
            response.SetElementContents("jr-main", PageHtml(site));
            response.SetElementContents("jr-list", ListHtml(site, string.Empty, string.Empty, string.Empty, 1));
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
            SiteData site = await Store.LoadSite();
            response.SetElementContents("jr-list", ListHtml(site, Ui.Pick(GetDataValue("kind"), new[] { "journal", "press" }), Ui.Pick(GetDataValue("st"), new[] { "published", "hidden" }), Ui.Clip(GetDataValue("term"), 40), Ui.Int(GetDataValue("pn"), 1, 1000, 1)));
            return response;
        }

        public async Task<ApiResponse> Edit()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            List<MediaItem> media = await Media.Load();
            string id = Ui.Clip(GetDataValue("eid"), 80);
            string kind = Ui.Pick(GetDataValue("newkind"), new[] { "journal", "press" });
            Entry? e = id.Length == 0 ? new Entry { Kind = kind.Length > 0 ? kind : "journal", Date = DateTime.Today, Category = kind == "press" ? "Press" : string.Empty } : site.Journal.FirstOrDefault(x => x.Id == id);
            if (e == null)
            {
                response.ExecuteScript("AdminJournalJs.toast('That entry was not found.');");
                return response;
            }
            response.SetElementContents("jr-mbody", FormHtml(media, site, e, id.Length == 0));
            response.ExecuteScript("AdminJournalJs.opened('" + Ui.Js(id.Length == 0 ? (e.Kind == "press" ? "Add a press piece" : "Add a journal entry") : e.Title) + "');");
            return response;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            string id = Ui.Clip(GetDataValue("eid"), 80);
            List<MediaItem> media = await Media.Load();
            Dictionary<string, string> err = new Dictionary<string, string>();
            string kind = Ui.Pick(GetDataValue("kind"), new[] { "journal", "press" });
            string title = Ui.Clip(GetDataValue("title"), 120);
            string category = Ui.Clip(GetDataValue("category"), 30);
            string summary = Ui.Clip(GetDataValue("summary"), 220);
            string body = Ui.Clip(GetDataValue("body"), 6000);
            string source = Ui.Clip(GetDataValue("source"), 80);
            string link = Ui.Clip(GetDataValue("link"), 300);
            string cover = Ui.Clip(GetDataValue("cover"), 200);
            bool dated = DateTime.TryParseExact(Ui.Clip(GetDataValue("date"), 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
            if (kind.Length == 0)
            {
                err["kind"] = "Choose journal entry or press piece.";
            }
            if (title.Length < 3)
            {
                err["title"] = "Please enter a title.";
            }
            if (!dated)
            {
                err["date"] = "Choose a date.";
            }
            if (summary.Length < 10)
            {
                err["summary"] = "Write a short summary of at least 10 characters.";
            }
            if (kind == "journal" && body.Length < 20)
            {
                err["body"] = "Write the entry text, at least a couple of sentences.";
            }
            if (kind == "press" && !(Uri.TryCreate(link, UriKind.Absolute, out Uri? address) && (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp) && !link.Contains(' ') && !link.Contains('<') && !link.Contains('"')))
            {
                err["link"] = "Enter the full address of the article, starting with https://";
            }
            if (kind == "press" && source.Length < 2)
            {
                err["source"] = "Enter the publication's name.";
            }
            if (cover.Length > 0 && Media.Find(media, cover, "photo") == null)
            {
                err["cover"] = "That picture is no longer in the media library. Choose it again.";
            }
            foreach (string f in new[] { "kind", "title", "date", "summary", "body", "source", "link", "cover" })
            {
                response.SetElementContents("jr-e-" + f, err.TryGetValue(f, out string? m) ? m : string.Empty);
            }
            if (err.Count > 0)
            {
                response.ExecuteScript("AdminJournalJs.failed();");
                return response;
            }
            bool created = false;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Entry? e = id.Length == 0 ? null : site.Journal.FirstOrDefault(x => x.Id == id);
                if (e == null && id.Length > 0)
                {
                    response.ExecuteScript("AdminJournalJs.toast('That entry was not found. It may have been deleted.');");
                    return response;
                }
                if (e == null)
                {
                    string slug = Ui.Slug(title);
                    if (slug.Length == 0)
                    {
                        slug = "entry";
                    }
                    string unique = slug;
                    for (int n = 2; site.Journal.Any(x => x.Id == unique); n++)
                    {
                        unique = slug + "-" + n;
                    }
                    e = new Entry { Id = unique };
                    site.Journal.Add(e);
                    created = true;
                }
                e.Kind = kind;
                e.Title = title;
                e.Date = date;
                e.Category = kind == "press" ? "Press" : (category.Length > 0 ? category : "Notes");
                e.Summary = summary;
                e.Body = kind == "journal" ? body : string.Empty;
                e.Source = kind == "press" ? source : string.Empty;
                e.Link = kind == "press" ? link : string.Empty;
                e.Cover = cover;
                e.Published = GetDataValue("published") == "1";
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, created ? "add journal entry" : "edit journal entry", title, true);
            SiteData fresh = await Store.LoadSite();
            response.SetElementContents("jr-list", ListHtml(fresh, Ui.Pick(GetDataValue("fkind"), new[] { "journal", "press" }), Ui.Pick(GetDataValue("fst"), new[] { "published", "hidden" }), Ui.Clip(GetDataValue("fterm"), 40), Ui.Int(GetDataValue("fpn"), 1, 1000, 1)));
            response.ExecuteScript("AdminJournalJs.saved(" + (created ? "true" : "false") + ");");
            return response;
        }

        public async Task<ApiResponse> Drop()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            string id = Ui.Clip(GetDataValue("eid"), 80);
            string title = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Entry? e = site.Journal.FirstOrDefault(x => x.Id == id);
                if (e != null)
                {
                    title = e.Title;
                    site.Journal.Remove(e);
                    await Store.SaveSite(site);
                }
            }
            if (title.Length == 0)
            {
                response.ExecuteScript("AdminJournalJs.toast('That entry was not found.');");
                return response;
            }
            await Store.Log(c.U.Id, "delete journal entry", title, true);
            SiteData fresh = await Store.LoadSite();
            response.SetElementContents("jr-list", ListHtml(fresh, Ui.Pick(GetDataValue("fkind"), new[] { "journal", "press" }), Ui.Pick(GetDataValue("fst"), new[] { "published", "hidden" }), Ui.Clip(GetDataValue("fterm"), 40), Ui.Int(GetDataValue("fpn"), 1, 1000, 1)));
            response.ExecuteScript("AdminJournalJs.dropped();");
            return response;
        }

        public async Task<ApiResponse> Pick()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            await Media.PickReply(response, "jr", "AdminJournalJs", GetDataValue("pterm") ?? string.Empty);
            return response;
        }

        public async Task<ApiResponse> Upload()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            await Media.UploadReply(response, "jr", "AdminJournalJs", c.U);
            return response;
        }

        private static string PageHtml(SiteData site)
        {
            return "<div class=\"jr-ph\"><div><h1>Journal and press</h1><p>Essays from the studio and articles that mention it, shown together on the Journal page, newest first.</p></div><div class=\"jr-acts\"><button type=\"button\" class=\"jr-btn\" onclick=\"AdminJournalJs.edit('','journal')\">Add a journal entry</button><button type=\"button\" class=\"jr-btn jr-btn-ghost\" onclick=\"AdminJournalJs.edit('','press')\">Add a press piece</button></div></div>"
                + "<div class=\"jr-tools\"><div class=\"jr-chips\" id=\"jr-chips\"><button type=\"button\" class=\"jr-chip jr-act\" data-kind=\"\" onclick=\"AdminJournalJs.kind('')\">All <i>" + site.Journal.Count + "</i></button><button type=\"button\" class=\"jr-chip\" data-kind=\"journal\" onclick=\"AdminJournalJs.kind('journal')\">Journal <i>" + site.Journal.Count(e => e.Kind == "journal") + "</i></button><button type=\"button\" class=\"jr-chip\" data-kind=\"press\" onclick=\"AdminJournalJs.kind('press')\">Press <i>" + site.Journal.Count(e => e.Kind == "press") + "</i></button></div>"
                + "<select id=\"jr-st\" onchange=\"AdminJournalJs.status()\" aria-label=\"Status\"><option value=\"\">All statuses</option><option value=\"published\">Published</option><option value=\"hidden\">Hidden</option></select><input type=\"search\" id=\"jr-q\" placeholder=\"Search title or topic\" maxlength=\"40\" oninput=\"AdminJournalJs.type()\" aria-label=\"Search entries\"></div><div id=\"jr-list\"></div>";
        }

        private static string ListHtml(SiteData site, string kind, string status, string term, int pn)
        {
            List<Entry> rows = site.Journal.Where(e => (kind.Length == 0 || e.Kind == kind) && (status.Length == 0 || e.Published == (status == "published")) && (term.Length == 0 || Ui.Has(e.Title, term) || Ui.Has(e.Category, term) || Ui.Has(e.Source, term))).OrderByDescending(e => e.Date).ToList();
            if (rows.Count == 0)
            {
                return "<div class=\"jr-panel jr-empty\">" + (site.Journal.Count == 0 ? "Nothing here yet. Add the first entry." : "No entries match.") + "</div>";
            }
            int pages = (rows.Count + Size - 1) / Size;
            pn = Math.Min(pn, pages);
            StringBuilder sb = new StringBuilder("<div class=\"jr-wrapt\"><table class=\"jr-table jr-stack\"><thead><tr><th>Entry</th><th>Kind</th><th>Date</th><th>Status</th><th></th></tr></thead><tbody>");
            foreach (Entry e in rows.Skip((pn - 1) * Size).Take(Size))
            {
                string id = Ui.Js(e.Id);
                sb.Append("<tr><td class=\"jr-first\"><div class=\"jr-ttl\">" + (e.Cover.Length > 0 ? "<img class=\"jr-thumb\" src=\"" + Ui.Esc(Media.Thumb(e.Cover)) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<span><b>" + Ui.Esc(e.Title) + "</b><small>" + Ui.Esc(e.Kind == "press" ? e.Source : e.Category) + "</small></span></div></td><td data-l=\"Kind\">" + (e.Kind == "press" ? "Press" : "Journal") + "</td><td data-l=\"Date\">" + Ui.Esc(Ui.Day(e.Date)) + "</td><td data-l=\"Status\"><span class=\"jr-pill " + (e.Published ? "jr-pill-go" : "jr-pill-off") + "\">" + (e.Published ? "Published" : "Hidden") + "</span></td>");
                sb.Append("<td class=\"jr-acts2\"><button type=\"button\" class=\"jr-btn jr-btn-ghost jr-btn-sm\" onclick=\"AdminJournalJs.edit('" + id + "','')\">Edit</button><button type=\"button\" class=\"jr-btn jr-btn-danger jr-btn-sm\" onclick=\"AdminJournalJs.drop('" + id + "','" + Ui.Js(e.Title) + "')\">Delete</button></td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"jr-pager\"><span>Page " + pn + " of " + pages + " &middot; " + Ui.Count(rows.Count, "entry", "entries") + "</span><div>" + (pn > 1 ? "<button type=\"button\" class=\"jr-btn jr-btn-ghost jr-btn-sm\" onclick=\"AdminJournalJs.go(" + (pn - 1) + ")\">Previous</button>" : string.Empty) + (pn < pages ? "<button type=\"button\" class=\"jr-btn jr-btn-ghost jr-btn-sm\" onclick=\"AdminJournalJs.go(" + (pn + 1) + ")\">Next</button>" : string.Empty) + "</div></div>");
            return sb.ToString();
        }

        private static string FormHtml(List<MediaItem> media, SiteData site, Entry e, bool isNew)
        {
            bool press = e.Kind == "press";
            StringBuilder sb = new StringBuilder();
            sb.Append("<form class=\"jr-stack2\" id=\"jr-form\" onsubmit=\"return AdminJournalJs.save('" + Ui.Js(isNew ? string.Empty : e.Id) + "')\" novalidate>");
            sb.Append("<input type=\"hidden\" id=\"jr-f-kind\" data-k=\"kind\" value=\"" + e.Kind + "\"><em class=\"jr-err\" id=\"jr-e-kind\"></em>");
            sb.Append("<label class=\"jr-field\" for=\"jr-f-title\"><span>Title</span><input type=\"text\" id=\"jr-f-title\" data-k=\"title\" maxlength=\"120\" value=\"" + Ui.Esc(e.Title) + "\"><em class=\"jr-err\" id=\"jr-e-title\"></em></label>");
            sb.Append("<div class=\"jr-grid2\"><label class=\"jr-field\" for=\"jr-f-date\"><span>Date</span><input type=\"date\" id=\"jr-f-date\" data-k=\"date\" value=\"" + e.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "\"><em class=\"jr-err\" id=\"jr-e-date\"></em></label>");
            if (!press)
            {
                sb.Append("<label class=\"jr-field\" for=\"jr-f-category\"><span>Topic</span><input type=\"text\" id=\"jr-f-category\" data-k=\"category\" maxlength=\"30\" list=\"jr-topics\" value=\"" + Ui.Esc(e.Category) + "\"><datalist id=\"jr-topics\">" + string.Concat(site.Journal.Where(x => x.Kind == "journal" && x.Category.Length > 0).Select(x => x.Category).Distinct().Select(x => "<option value=\"" + Ui.Esc(x) + "\">")) + "</datalist><small>Entries with the same topic share a filter on the Journal page.</small></label>");
            }
            sb.Append("</div><label class=\"jr-field\" for=\"jr-f-summary\"><span>Short summary</span><input type=\"text\" id=\"jr-f-summary\" data-k=\"summary\" maxlength=\"220\" value=\"" + Ui.Esc(e.Summary) + "\"><small>Shown on the cards.</small><em class=\"jr-err\" id=\"jr-e-summary\"></em></label>");
            if (press)
            {
                sb.Append("<div class=\"jr-grid2\"><label class=\"jr-field\" for=\"jr-f-source\"><span>Publication</span><input type=\"text\" id=\"jr-f-source\" data-k=\"source\" maxlength=\"80\" value=\"" + Ui.Esc(e.Source) + "\"><em class=\"jr-err\" id=\"jr-e-source\"></em></label><label class=\"jr-field\" for=\"jr-f-link\"><span>Address of the article</span><input type=\"text\" id=\"jr-f-link\" data-k=\"link\" maxlength=\"300\" value=\"" + Ui.Esc(e.Link) + "\" placeholder=\"https://\"><em class=\"jr-err\" id=\"jr-e-link\"></em></label></div>");
            }
            else
            {
                sb.Append("<label class=\"jr-field\" for=\"jr-f-body\"><span>Entry text</span><textarea id=\"jr-f-body\" class=\"jr-textarea jr-tall\" data-k=\"body\" maxlength=\"6000\">" + Ui.Esc(e.Body) + "</textarea><small>Leave a blank line between paragraphs.</small><em class=\"jr-err\" id=\"jr-e-body\"></em></label>");
            }
            sb.Append("<div class=\"jr-sect\">" + Media.PicField(media, "jr", "AdminJournalJs", "cover", e.Cover, "Cover picture (optional)", false) + "<em class=\"jr-err\" id=\"jr-e-cover\"></em></div>");
            sb.Append("<label class=\"jr-check\"><input type=\"checkbox\" data-k=\"published\"" + (e.Published ? " checked" : string.Empty) + "> Show on the website</label>");
            sb.Append("<div class=\"jr-acts\"><button type=\"submit\" class=\"jr-btn\" id=\"jr-save\">" + (isNew ? "Add entry" : "Save entry") + "</button><button type=\"button\" class=\"jr-btn jr-btn-ghost\" onclick=\"AdminJournalJs.closeModal()\">Cancel</button>" + (isNew || press ? string.Empty : "<a class=\"jr-btn jr-btn-ghost jr-push\" href=\"Journal?id=" + Ui.Esc(e.Id) + "\" target=\"_blank\" rel=\"noopener\">View on the website</a>") + "</div></form>");
            return sb.ToString();
        }
    }
}
