using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class AdminServices : WebPage
    {
        private const int Size = 10;

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle("Services | " + site.Brand + " staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminServicesJs.expired();");
            }
            return c;
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminServicesJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("sa-brand", Ui.Esc(site.Brand));
            response.SetElementContents("sa-menu", await Auth.Menu("sa", "AdminServices", c));
            response.SetElementContents("sa-who", Auth.Who("sa", c));
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
            response.SetElementContents("sa-main", "<div class=\"sa-ph\"><div><h1>Services</h1><p>What the studio offers, shown on the Services page and as cards on the Home page. Order here is the order on the website.</p></div><button type=\"button\" class=\"sa-btn\" onclick=\"AdminServicesJs.edit('')\">Add a service</button></div><div class=\"sa-tools\"><input type=\"search\" id=\"sa-q\" placeholder=\"Search name or summary\" maxlength=\"40\" oninput=\"AdminServicesJs.type()\" aria-label=\"Search services\"></div><div id=\"sa-list\">" + ListHtml(site, string.Empty, 1) + "</div>");
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
            response.SetElementContents("sa-list", ListHtml(site, Ui.Clip(GetDataValue("term"), 40), Ui.Int(GetDataValue("pn"), 1, 1000, 1)));
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
            string id = Ui.Clip(GetDataValue("sid"), 60);
            Offer? s = id.Length == 0 ? new Offer() : site.Services.FirstOrDefault(x => x.Id == id);
            if (s == null)
            {
                response.ExecuteScript("AdminServicesJs.toast('That service was not found.');");
                return response;
            }
            response.SetElementContents("sa-mbody", FormHtml(media, s, id.Length == 0));
            response.ExecuteScript("AdminServicesJs.opened('" + Ui.Js(id.Length == 0 ? "Add a service" : s.Title) + "');");
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
            string id = Ui.Clip(GetDataValue("sid"), 60);
            List<MediaItem> media = await Media.Load();
            Dictionary<string, string> err = new Dictionary<string, string>();
            string title = Ui.Clip(GetDataValue("title"), 60);
            string icon = Ui.Pick(GetDataValue("icon"), Blocks.Icons.Select(i => i.Key));
            string summary = Ui.Clip(GetDataValue("summary"), 160);
            string text = Ui.Clip(GetDataValue("text"), 1500);
            List<string> points = Ui.Split(GetDataValue("points"), 6, 80);
            string duration = Ui.Clip(GetDataValue("duration"), 40);
            string image = Ui.Clip(GetDataValue("image"), 200);
            if (title.Length < 2)
            {
                err["title"] = "Please enter the service name.";
            }
            if (summary.Length < 10)
            {
                err["summary"] = "Write a one-line summary of at least 10 characters.";
            }
            if (image.Length > 0 && Media.Find(media, image, "photo") == null)
            {
                err["image"] = "That picture is no longer in the media library. Choose it again.";
            }
            foreach (string f in new[] { "title", "summary", "image" })
            {
                response.SetElementContents("sa-e-" + f, err.TryGetValue(f, out string? m) ? m : string.Empty);
            }
            if (err.Count > 0)
            {
                response.ExecuteScript("AdminServicesJs.failed();");
                return response;
            }
            bool created = false;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Offer? s = id.Length == 0 ? null : site.Services.FirstOrDefault(x => x.Id == id);
                if (s == null && id.Length > 0)
                {
                    response.ExecuteScript("AdminServicesJs.toast('That service was not found. It may have been deleted.');");
                    return response;
                }
                if (s == null)
                {
                    string slug = Ui.Slug(title);
                    if (slug.Length == 0)
                    {
                        slug = "service";
                    }
                    string unique = slug;
                    for (int n = 2; site.Services.Any(x => x.Id == unique); n++)
                    {
                        unique = slug + "-" + n;
                    }
                    s = new Offer { Id = unique };
                    site.Services.Add(s);
                    created = true;
                }
                s.Title = title;
                s.Icon = icon;
                s.Summary = summary;
                s.Text = text;
                s.Points = points;
                s.Duration = duration;
                s.Image = image;
                s.Published = GetDataValue("published") == "1";
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, created ? "add service" : "edit service", title, true);
            SiteData fresh = await Store.LoadSite();
            response.SetElementContents("sa-list", ListHtml(fresh, Ui.Clip(GetDataValue("fterm"), 40), Ui.Int(GetDataValue("fpn"), 1, 1000, 1)));
            response.ExecuteScript("AdminServicesJs.saved(" + (created ? "true" : "false") + ");");
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
            string id = Ui.Clip(GetDataValue("sid"), 60);
            int dir = GetDataValue("dir") == "-1" ? -1 : 1;
            string title = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                int at = site.Services.FindIndex(x => x.Id == id);
                int to = at + dir;
                if (at >= 0 && to >= 0 && to < site.Services.Count)
                {
                    Offer s = site.Services[at];
                    site.Services[at] = site.Services[to];
                    site.Services[to] = s;
                    title = s.Title;
                    await Store.SaveSite(site);
                }
            }
            if (title.Length > 0)
            {
                await Store.Log(c.U.Id, "move service", title, true);
            }
            response.SetElementContents("sa-list", ListHtml(await Store.LoadSite(), Ui.Clip(GetDataValue("fterm"), 40), Ui.Int(GetDataValue("fpn"), 1, 1000, 1)));
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
            string id = Ui.Clip(GetDataValue("sid"), 60);
            string title = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Offer? s = site.Services.FirstOrDefault(x => x.Id == id);
                if (s != null)
                {
                    title = s.Title;
                    site.Services.Remove(s);
                    await Store.SaveSite(site);
                }
            }
            if (title.Length == 0)
            {
                response.ExecuteScript("AdminServicesJs.toast('That service was not found.');");
                return response;
            }
            await Store.Log(c.U.Id, "delete service", title, true);
            response.SetElementContents("sa-list", ListHtml(await Store.LoadSite(), Ui.Clip(GetDataValue("fterm"), 40), Ui.Int(GetDataValue("fpn"), 1, 1000, 1)));
            response.ExecuteScript("AdminServicesJs.dropped();");
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
            await Media.PickReply(response, "sa", "AdminServicesJs", GetDataValue("pterm") ?? string.Empty);
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
            await Media.UploadReply(response, "sa", "AdminServicesJs", c.U);
            return response;
        }

        private static string ListHtml(SiteData site, string term, int pn)
        {
            bool filtered = term.Length > 0;
            List<Offer> rows = site.Services.Where(s => term.Length == 0 || Ui.Has(s.Title, term) || Ui.Has(s.Summary, term)).ToList();
            if (rows.Count == 0)
            {
                return "<div class=\"sa-panel sa-empty\">" + (site.Services.Count == 0 ? "No services yet. Add the first one." : "No services match.") + "</div>";
            }
            int pages = (rows.Count + Size - 1) / Size;
            pn = Math.Min(pn, pages);
            StringBuilder sb = new StringBuilder("<div class=\"sa-wrapt\"><table class=\"sa-table sa-stack\"><thead><tr><th>Service</th><th>Typical duration</th><th>Status</th><th></th></tr></thead><tbody>");
            foreach (Offer s in rows.Skip((pn - 1) * Size).Take(Size))
            {
                string id = Ui.Js(s.Id);
                int at = site.Services.IndexOf(s);
                sb.Append("<tr><td class=\"sa-first\"><div class=\"sa-ttl\">" + (s.Image.Length > 0 ? "<img class=\"sa-thumb\" src=\"" + Ui.Esc(Media.Thumb(s.Image)) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<span><b>" + Ui.Esc(s.Title) + "</b><small>" + Ui.Esc(s.Summary) + "</small></span></div></td><td data-l=\"Duration\">" + Ui.Esc(s.Duration.Length > 0 ? s.Duration : "Not stated") + "</td><td data-l=\"Status\"><span class=\"sa-pill " + (s.Published ? "sa-pill-go" : "sa-pill-off") + "\">" + (s.Published ? "Published" : "Hidden") + "</span></td>");
                sb.Append("<td class=\"sa-acts2\"><button type=\"button\" class=\"sa-btn sa-btn-ghost sa-btn-sm\" onclick=\"AdminServicesJs.edit('" + id + "')\">Edit</button>");
                if (!filtered)
                {
                    sb.Append("<button type=\"button\" class=\"sa-btn sa-btn-ghost sa-btn-sm\" aria-label=\"Move " + Ui.Esc(s.Title) + " up\"" + (at == 0 ? " disabled" : string.Empty) + " onclick=\"AdminServicesJs.move('" + id + "',-1)\">&uarr;</button><button type=\"button\" class=\"sa-btn sa-btn-ghost sa-btn-sm\" aria-label=\"Move " + Ui.Esc(s.Title) + " down\"" + (at == site.Services.Count - 1 ? " disabled" : string.Empty) + " onclick=\"AdminServicesJs.move('" + id + "',1)\">&darr;</button>");
                }
                sb.Append("<button type=\"button\" class=\"sa-btn sa-btn-danger sa-btn-sm\" onclick=\"AdminServicesJs.drop('" + id + "','" + Ui.Js(s.Title) + "')\">Delete</button></td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"sa-pager\"><span>Page " + pn + " of " + pages + " &middot; " + Ui.Count(rows.Count, "service", "services") + (filtered ? string.Empty : ". Order here is the order on the website.") + "</span><div>" + (pn > 1 ? "<button type=\"button\" class=\"sa-btn sa-btn-ghost sa-btn-sm\" onclick=\"AdminServicesJs.go(" + (pn - 1) + ")\">Previous</button>" : string.Empty) + (pn < pages ? "<button type=\"button\" class=\"sa-btn sa-btn-ghost sa-btn-sm\" onclick=\"AdminServicesJs.go(" + (pn + 1) + ")\">Next</button>" : string.Empty) + "</div></div>");
            return sb.ToString();
        }

        private static string FormHtml(List<MediaItem> media, Offer s, bool isNew)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<form class=\"sa-stack2\" id=\"sa-form\" onsubmit=\"return AdminServicesJs.save('" + Ui.Js(isNew ? string.Empty : s.Id) + "')\" novalidate>");
            sb.Append("<div class=\"sa-grid2\"><label class=\"sa-field\" for=\"sa-f-title\"><span>Service name</span><input type=\"text\" id=\"sa-f-title\" data-k=\"title\" maxlength=\"60\" value=\"" + Ui.Esc(s.Title) + "\"><em class=\"sa-err\" id=\"sa-e-title\"></em></label>");
            sb.Append("<label class=\"sa-field\" for=\"sa-f-icon\"><span>Icon</span><select id=\"sa-f-icon\" data-k=\"icon\"><option value=\"\">No icon</option>" + string.Concat(Blocks.Icons.Select(i => "<option value=\"" + i.Key + "\"" + (i.Key == s.Icon ? " selected" : string.Empty) + ">" + Ui.Esc(i.Name) + "</option>")) + "</select></label></div>");
            sb.Append("<label class=\"sa-field\" for=\"sa-f-summary\"><span>One-line summary</span><input type=\"text\" id=\"sa-f-summary\" data-k=\"summary\" maxlength=\"160\" value=\"" + Ui.Esc(s.Summary) + "\"><small>Shown on the Home page card and under the title.</small><em class=\"sa-err\" id=\"sa-e-summary\"></em></label>");
            sb.Append("<label class=\"sa-field\" for=\"sa-f-text\"><span>Description</span><textarea id=\"sa-f-text\" class=\"sa-textarea sa-tall\" data-k=\"text\" maxlength=\"1500\">" + Ui.Esc(s.Text) + "</textarea><small>Leave a blank line between paragraphs.</small></label>");
            sb.Append("<label class=\"sa-field\" for=\"sa-f-points\"><span>What is included</span><textarea id=\"sa-f-points\" class=\"sa-textarea\" data-k=\"points\" maxlength=\"500\">" + Ui.Esc(Ui.Join(s.Points)) + "</textarea><small>One point per line. Up to 6.</small></label>");
            sb.Append("<label class=\"sa-field\" for=\"sa-f-duration\"><span>Typical duration</span><input type=\"text\" id=\"sa-f-duration\" data-k=\"duration\" maxlength=\"40\" value=\"" + Ui.Esc(s.Duration) + "\"></label>");
            sb.Append("<div class=\"sa-sect\">" + Media.PicField(media, "sa", "AdminServicesJs", "image", s.Image, "Picture (optional)", false) + "<em class=\"sa-err\" id=\"sa-e-image\"></em></div>");
            sb.Append("<label class=\"sa-check\"><input type=\"checkbox\" data-k=\"published\"" + (s.Published ? " checked" : string.Empty) + "> Show on the website</label>");
            sb.Append("<div class=\"sa-acts\"><button type=\"submit\" class=\"sa-btn\" id=\"sa-save\">" + (isNew ? "Add service" : "Save service") + "</button><button type=\"button\" class=\"sa-btn sa-btn-ghost\" onclick=\"AdminServicesJs.closeModal()\">Cancel</button></div></form>");
            return sb.ToString();
        }
    }
}
