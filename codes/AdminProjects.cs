using System.Text;
using System.Text.Json;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class AdminProjects : WebPage
    {
        private const int Size = 10;
        private const int MaxGallery = 12;
        private const int MaxPairs = 4;

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle("Projects | " + site.Brand + " staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminProjectsJs.expired();");
            }
            return c;
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminProjectsJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("pr-brand", Ui.Esc(site.Brand));
            response.SetElementContents("pr-menu", await Auth.Menu("pr", "AdminProjects", c));
            response.SetElementContents("pr-who", Auth.Who("pr", c));
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
            response.SetElementContents("pr-main", PageHtml(site));
            response.SetElementContents("pr-list", ListHtml(site, string.Empty, string.Empty, 1));
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
            response.SetElementContents("pr-list", ListHtml(site, Ui.Pick(GetDataValue("kind"), Catalog.Kinds.Select(k => k.Key)), Ui.Clip(GetDataValue("term"), 40), Ui.Int(GetDataValue("pn"), 1, 1000, 1)));
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
            string id = Ui.Clip(GetDataValue("pid"), 60);
            Work? w = id.Length == 0 ? new Work { Year = DateTime.Today.Year, Kind = "house" } : site.Projects.FirstOrDefault(x => x.Id == id);
            if (w == null)
            {
                response.ExecuteScript("AdminProjectsJs.toast('That project was not found.');");
                return response;
            }
            response.SetElementContents("pr-mbody", FormHtml(media, w, id.Length == 0));
            response.ExecuteScript("AdminProjectsJs.opened('" + Ui.Js(id.Length == 0 ? "Add a project" : w.Title) + "');");
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
            string id = Ui.Clip(GetDataValue("pid"), 60);
            List<MediaItem> media = await Media.Load();
            HashSet<string> photos = media.Where(m => m.Kind == "photo").Select(m => m.Path).ToHashSet(StringComparer.Ordinal);
            Dictionary<string, string> err = new Dictionary<string, string>();
            string title = Ui.Clip(GetDataValue("title"), 80);
            string kind = Ui.Pick(GetDataValue("kind"), Catalog.Kinds.Select(k => k.Key));
            string location = Ui.Clip(GetDataValue("location"), 80);
            int year = Ui.Int(GetDataValue("year"), 0, 2100, 0);
            int size = Ui.Int(GetDataValue("size"), 0, 1000000, 0);
            List<string> scope = Ui.Split(GetDataValue("scope"), 6, 40);
            string summary = Ui.Clip(GetDataValue("summary"), 160);
            string story = Ui.Clip(GetDataValue("story"), 3000);
            string cover = Ui.Clip(GetDataValue("cover"), 200);
            if (title.Length < 2)
            {
                err["title"] = "Please enter the project name.";
            }
            if (kind.Length == 0)
            {
                err["kind"] = "Choose a type.";
            }
            if (year < 1900)
            {
                err["year"] = "Enter the year, for example 2025.";
            }
            if (summary.Length < 10)
            {
                err["summary"] = "Write a one-line summary of at least 10 characters.";
            }
            if (!photos.Contains(cover))
            {
                err["cover"] = "Choose a cover picture from the media library.";
            }
            List<Pic> gallery = new List<Pic>();
            List<Duo> pairs = new List<Duo>();
            try
            {
                JsonSerializerOptions opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                foreach (Pic g in JsonSerializer.Deserialize<List<Pic>>(GetDataValue("gallery") ?? "[]", opts) ?? new List<Pic>())
                {
                    if (photos.Contains(g.Media ?? string.Empty) && gallery.Count < MaxGallery)
                    {
                        gallery.Add(new Pic { Media = g.Media!, Caption = Ui.Clip(g.Caption, 80) });
                    }
                }
                foreach (Duo d in JsonSerializer.Deserialize<List<Duo>>(GetDataValue("pairs") ?? "[]", opts) ?? new List<Duo>())
                {
                    if (photos.Contains(d.Before ?? string.Empty) && photos.Contains(d.After ?? string.Empty) && pairs.Count < MaxPairs)
                    {
                        pairs.Add(new Duo { Before = d.Before!, After = d.After!, Caption = Ui.Clip(d.Caption, 100) });
                    }
                    else if (pairs.Count < MaxPairs && ((d.Before ?? string.Empty).Length > 0 || (d.After ?? string.Empty).Length > 0))
                    {
                        err["pairs"] = "Each before and after pair needs both pictures from the media library.";
                    }
                }
            }
            catch (JsonException)
            {
                err["pairs"] = "The pictures could not be read. Reload the page and try again.";
            }
            foreach (string f in new[] { "title", "kind", "year", "summary", "cover", "pairs" })
            {
                response.SetElementContents("pr-e-" + f, err.TryGetValue(f, out string? m) ? m : string.Empty);
            }
            if (err.Count > 0)
            {
                response.ExecuteScript("AdminProjectsJs.failed();");
                return response;
            }
            bool created = false;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Work? w = id.Length == 0 ? null : site.Projects.FirstOrDefault(x => x.Id == id);
                if (w == null && id.Length > 0)
                {
                    response.ExecuteScript("AdminProjectsJs.toast('That project was not found. It may have been deleted.');");
                    return response;
                }
                if (w == null)
                {
                    string slug = Ui.Slug(title);
                    if (slug.Length == 0)
                    {
                        slug = "project";
                    }
                    string unique = slug;
                    for (int n = 2; site.Projects.Any(x => x.Id == unique); n++)
                    {
                        unique = slug + "-" + n;
                    }
                    w = new Work { Id = unique };
                    site.Projects.Add(w);
                    created = true;
                }
                w.Title = title;
                w.Kind = kind;
                w.Location = location;
                w.Year = year;
                w.Size = size;
                w.Scope = scope;
                w.Summary = summary;
                w.Story = story;
                w.Cover = cover;
                w.Gallery = gallery;
                w.Pairs = pairs;
                w.Published = GetDataValue("published") == "1";
                w.Featured = GetDataValue("featured") == "1";
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, created ? "add project" : "edit project", title, true);
            SiteData fresh = await Store.LoadSite();
            response.SetElementContents("pr-list", ListHtml(fresh, Ui.Pick(GetDataValue("fkind"), Catalog.Kinds.Select(k => k.Key)), Ui.Clip(GetDataValue("fterm"), 40), Ui.Int(GetDataValue("fpn"), 1, 1000, 1)));
            response.ExecuteScript("AdminProjectsJs.saved(" + (created ? "true" : "false") + ");");
            return response;
        }

        public async Task<ApiResponse> Move()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            string id = Ui.Clip(GetDataValue("pid"), 60);
            int dir = GetDataValue("dir") == "-1" ? -1 : 1;
            string title = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                int at = site.Projects.FindIndex(x => x.Id == id);
                int to = at + dir;
                if (at >= 0 && to >= 0 && to < site.Projects.Count)
                {
                    Work w = site.Projects[at];
                    site.Projects[at] = site.Projects[to];
                    site.Projects[to] = w;
                    title = w.Title;
                    await Store.SaveSite(site);
                }
            }
            if (title.Length > 0)
            {
                await Store.Log(c.U.Id, "move project", title, true);
            }
            SiteData fresh = await Store.LoadSite();
            response.SetElementContents("pr-list", ListHtml(fresh, string.Empty, string.Empty, Ui.Int(GetDataValue("pn"), 1, 1000, 1)));
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
            string id = Ui.Clip(GetDataValue("pid"), 60);
            string title = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Work? w = site.Projects.FirstOrDefault(x => x.Id == id);
                if (w != null)
                {
                    title = w.Title;
                    site.Projects.Remove(w);
                    await Store.SaveSite(site);
                }
            }
            if (title.Length == 0)
            {
                response.ExecuteScript("AdminProjectsJs.toast('That project was not found.');");
                return response;
            }
            await Store.Log(c.U.Id, "delete project", title, true);
            SiteData fresh = await Store.LoadSite();
            response.SetElementContents("pr-list", ListHtml(fresh, Ui.Pick(GetDataValue("fkind"), Catalog.Kinds.Select(k => k.Key)), Ui.Clip(GetDataValue("fterm"), 40), 1));
            response.ExecuteScript("AdminProjectsJs.dropped();");
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
            await Media.PickReply(response, "pr", "AdminProjectsJs", GetDataValue("pterm") ?? string.Empty);
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
            await Media.UploadReply(response, "pr", "AdminProjectsJs", c.U);
            return response;
        }

        private static string PageHtml(SiteData site)
        {
            StringBuilder sb = new StringBuilder("<div class=\"pr-ph\"><div><h1>Projects</h1><p>The portfolio on the Projects page. Pictures come from the media library.</p></div><button type=\"button\" class=\"pr-btn\" onclick=\"AdminProjectsJs.edit('')\">Add a project</button></div>");
            sb.Append("<div class=\"pr-tools\"><div class=\"pr-chips\" id=\"pr-chips\"><button type=\"button\" class=\"pr-chip pr-act\" data-kind=\"\" onclick=\"AdminProjectsJs.kind('')\">All <i>" + site.Projects.Count + "</i></button>");
            foreach ((string key, string name) in Catalog.Kinds)
            {
                sb.Append("<button type=\"button\" class=\"pr-chip\" data-kind=\"" + key + "\" onclick=\"AdminProjectsJs.kind('" + key + "')\">" + Ui.Esc(name) + " <i>" + site.Projects.Count(p => p.Kind == key) + "</i></button>");
            }
            sb.Append("</div><input type=\"search\" id=\"pr-q\" placeholder=\"Search name, place or scope\" maxlength=\"40\" oninput=\"AdminProjectsJs.type()\" aria-label=\"Search projects\"></div><div id=\"pr-list\"></div>");
            return sb.ToString();
        }

        private static string ListHtml(SiteData site, string kind, string term, int pn)
        {
            bool filtered = kind.Length > 0 || term.Length > 0;
            List<Work> rows = site.Projects.Where(w => (kind.Length == 0 || w.Kind == kind) && (term.Length == 0 || Ui.Has(w.Title, term) || Ui.Has(w.Location, term) || w.Scope.Any(s => Ui.Has(s, term)))).ToList();
            if (rows.Count == 0)
            {
                return "<div class=\"pr-panel pr-empty\">" + (site.Projects.Count == 0 ? "No projects yet. Add the first one." : "No projects match.") + "</div>";
            }
            int pages = (rows.Count + Size - 1) / Size;
            pn = Math.Min(pn, pages);
            StringBuilder sb = new StringBuilder("<div class=\"pr-wrapt\"><table class=\"pr-table pr-stack\"><thead><tr><th>Project</th><th>Type</th><th>Year</th><th>Status</th><th></th></tr></thead><tbody>");
            foreach (Work w in rows.Skip((pn - 1) * Size).Take(Size))
            {
                string id = Ui.Js(w.Id);
                int at = site.Projects.IndexOf(w);
                sb.Append("<tr><td class=\"pr-first\"><div class=\"pr-ttl\">" + (w.Cover.Length > 0 ? "<img class=\"pr-thumb\" src=\"" + Ui.Esc(Media.Thumb(w.Cover)) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<span><b>" + Ui.Esc(w.Title) + "</b><small>" + Ui.Esc(w.Location) + "</small></span></div></td>");
                sb.Append("<td data-l=\"Type\">" + Ui.Esc(Catalog.Kind(w.Kind)) + "</td><td data-l=\"Year\">" + w.Year + "</td><td data-l=\"Status\"><span class=\"pr-pill " + (w.Published ? "pr-pill-go" : "pr-pill-off") + "\">" + (w.Published ? "Published" : "Hidden") + "</span>" + (w.Featured ? " <span class=\"pr-pill pr-pill-new\">On Home</span>" : string.Empty) + "</td>");
                sb.Append("<td class=\"pr-acts2\"><button type=\"button\" class=\"pr-btn pr-btn-ghost pr-btn-sm\" onclick=\"AdminProjectsJs.edit('" + id + "')\">Edit</button>");
                if (!filtered)
                {
                    sb.Append("<button type=\"button\" class=\"pr-btn pr-btn-ghost pr-btn-sm\" aria-label=\"Move " + Ui.Esc(w.Title) + " up\"" + (at == 0 ? " disabled" : string.Empty) + " onclick=\"AdminProjectsJs.move('" + id + "',-1)\">&uarr;</button><button type=\"button\" class=\"pr-btn pr-btn-ghost pr-btn-sm\" aria-label=\"Move " + Ui.Esc(w.Title) + " down\"" + (at == site.Projects.Count - 1 ? " disabled" : string.Empty) + " onclick=\"AdminProjectsJs.move('" + id + "',1)\">&darr;</button>");
                }
                sb.Append("<button type=\"button\" class=\"pr-btn pr-btn-danger pr-btn-sm\" onclick=\"AdminProjectsJs.drop('" + id + "','" + Ui.Js(w.Title) + "')\">Delete</button></td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"pr-pager\"><span>Page " + pn + " of " + pages + " &middot; " + Ui.Count(rows.Count, "project", "projects") + (filtered ? string.Empty : ". Order here is the order on the website.") + "</span><div>" + (pn > 1 ? "<button type=\"button\" class=\"pr-btn pr-btn-ghost pr-btn-sm\" onclick=\"AdminProjectsJs.go(" + (pn - 1) + ")\">Previous</button>" : string.Empty) + (pn < pages ? "<button type=\"button\" class=\"pr-btn pr-btn-ghost pr-btn-sm\" onclick=\"AdminProjectsJs.go(" + (pn + 1) + ")\">Next</button>" : string.Empty) + "</div></div>");
            return sb.ToString();
        }

        private static string Input(string key, string label, string value, int max, string type, string hint)
        {
            return "<label class=\"pr-field\" for=\"pr-f-" + key + "\"><span>" + Ui.Esc(label) + "</span><input type=\"" + type + "\" id=\"pr-f-" + key + "\" data-k=\"" + key + "\" maxlength=\"" + max + "\" value=\"" + Ui.Esc(value) + "\">" + (hint.Length > 0 ? "<small>" + Ui.Esc(hint) + "</small>" : string.Empty) + "<em class=\"pr-err\" id=\"pr-e-" + key + "\"></em></label>";
        }

        private static string GalleryItem(List<MediaItem> media, Pic g)
        {
            MediaItem? m = media.FirstOrDefault(x => x.Path == g.Media);
            string thumb = m == null ? g.Media : (m.Thumb.Length > 0 ? m.Thumb : m.Path);
            return "<div class=\"pr-gi\" data-src=\"" + Ui.Esc(g.Media) + "\" data-thumb=\"" + Ui.Esc(thumb) + "\"><img src=\"" + Ui.Esc(thumb) + "\" alt=\"\"><input type=\"text\" class=\"pr-gcap\" maxlength=\"80\" value=\"" + Ui.Esc(g.Caption) + "\" placeholder=\"Caption (optional)\" aria-label=\"Caption\"><div class=\"pr-gctl\"><button type=\"button\" aria-label=\"Move earlier\" onclick=\"AdminProjectsJs.galMove(this,-1)\">&larr;</button><button type=\"button\" aria-label=\"Move later\" onclick=\"AdminProjectsJs.galMove(this,1)\">&rarr;</button><button type=\"button\" aria-label=\"Remove picture\" onclick=\"AdminProjectsJs.galDrop(this)\">&times;</button></div></div>";
        }

        private static string PairItem(List<MediaItem> media, Duo d)
        {
            return "<div class=\"pr-pair\"><div class=\"pr-pairpics\">" + Media.PicField(media, "pr", "AdminProjectsJs", "before", d.Before, "Before", false) + Media.PicField(media, "pr", "AdminProjectsJs", "after", d.After, "After", false) + "</div><label class=\"pr-field\"><span>Caption (optional)</span><input type=\"text\" class=\"pr-pcap\" maxlength=\"100\" value=\"" + Ui.Esc(d.Caption) + "\"></label><button type=\"button\" class=\"pr-btn pr-btn-danger pr-btn-sm\" onclick=\"AdminProjectsJs.pairDrop(this)\">Remove this pair</button></div>";
        }

        private static string FormHtml(List<MediaItem> media, Work w, bool isNew)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<form class=\"pr-stack2\" id=\"pr-form\" onsubmit=\"return AdminProjectsJs.save('" + Ui.Js(isNew ? string.Empty : w.Id) + "')\" novalidate>");
            sb.Append("<div class=\"pr-grid2\">" + Input("title", "Project name", w.Title, 80, "text", string.Empty));
            sb.Append("<label class=\"pr-field\" for=\"pr-f-kind\"><span>Type</span><select id=\"pr-f-kind\" data-k=\"kind\">" + string.Concat(Catalog.Kinds.Select(k => "<option value=\"" + k.Key + "\"" + (k.Key == w.Kind ? " selected" : string.Empty) + ">" + Ui.Esc(k.Name) + "</option>")) + "</select><em class=\"pr-err\" id=\"pr-e-kind\"></em></label>");
            sb.Append(Input("location", "Location", w.Location, 80, "text", string.Empty) + Input("year", "Year", w.Year == 0 ? string.Empty : w.Year.ToString(), 4, "text", string.Empty) + Input("size", "Size in square metres", w.Size == 0 ? string.Empty : w.Size.ToString(), 7, "text", "Leave empty if it does not apply.") + "</div>");
            sb.Append("<label class=\"pr-field\" for=\"pr-f-scope\"><span>Scope of work</span><textarea id=\"pr-f-scope\" class=\"pr-textarea\" data-k=\"scope\" maxlength=\"300\">" + Ui.Esc(Ui.Join(w.Scope)) + "</textarea><small>One item per line, for example New build or Joinery. Up to 6.</small></label>");
            sb.Append(Input("summary", "One-line summary", w.Summary, 160, "text", "Shown on the project card and under the title."));
            sb.Append("<label class=\"pr-field\" for=\"pr-f-story\"><span>About the project</span><textarea id=\"pr-f-story\" class=\"pr-textarea pr-tall\" data-k=\"story\" maxlength=\"3000\">" + Ui.Esc(w.Story) + "</textarea><small>Leave a blank line between paragraphs.</small></label>");
            sb.Append("<div class=\"pr-sect\"><h3>Cover picture</h3>" + Media.PicField(media, "pr", "AdminProjectsJs", "cover", w.Cover, "Cover picture", false) + "<em class=\"pr-err\" id=\"pr-e-cover\"></em></div>");
            sb.Append("<div class=\"pr-sect\"><h3>Gallery</h3><p class=\"pr-hint\">Pictures shown below the story. Use the arrows to change the order.</p><div class=\"pr-gal\" id=\"pr-gal\">" + string.Concat(w.Gallery.Select(g => GalleryItem(media, g))) + "</div><button type=\"button\" class=\"pr-btn pr-btn-ghost pr-btn-sm\" onclick=\"AdminProjectsJs.galAdd()\">Choose gallery pictures</button></div>");
            sb.Append("<div class=\"pr-sect\"><h3>Before and after</h3><p class=\"pr-hint\">Pairs of the same view before and after the work. Visitors compare them with a slider. Up to " + MaxPairs + ".</p><div id=\"pr-pairs\">" + string.Concat(w.Pairs.Select(d => PairItem(media, d))) + "</div><em class=\"pr-err\" id=\"pr-e-pairs\"></em><button type=\"button\" class=\"pr-btn pr-btn-ghost pr-btn-sm\" onclick=\"AdminProjectsJs.pairAdd()\">Add a before and after pair</button><template id=\"pr-pairtpl\">" + PairItem(media, new Duo()) + "</template></div>");
            sb.Append("<div class=\"pr-sect pr-checks\"><label class=\"pr-check\"><input type=\"checkbox\" data-k=\"published\"" + (w.Published ? " checked" : string.Empty) + "> Show on the website</label><label class=\"pr-check\"><input type=\"checkbox\" data-k=\"featured\"" + (w.Featured ? " checked" : string.Empty) + "> Show on the Home page</label></div>");
            sb.Append("<div class=\"pr-acts\"><button type=\"submit\" class=\"pr-btn\" id=\"pr-save\">" + (isNew ? "Add project" : "Save project") + "</button><button type=\"button\" class=\"pr-btn pr-btn-ghost\" onclick=\"AdminProjectsJs.closeModal()\">Cancel</button>" + (isNew ? string.Empty : "<a class=\"pr-btn pr-btn-ghost pr-push\" href=\"Project?id=" + Ui.Esc(w.Id) + "\" target=\"_blank\" rel=\"noopener\">View on the website</a>") + "</div></form>");
            return sb.ToString();
        }
    }
}
