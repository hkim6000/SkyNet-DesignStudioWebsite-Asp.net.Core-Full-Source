using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class AdminTeam : WebPage
    {
        private const int Size = 12;

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle("Team | " + site.Brand + " staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminTeamJs.expired();");
            }
            return c;
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminTeamJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("tm-brand", Ui.Esc(site.Brand));
            response.SetElementContents("tm-menu", await Auth.Menu("tm", "AdminTeam", c));
            response.SetElementContents("tm-who", Auth.Who("tm", c));
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
            response.SetElementContents("tm-main", "<div class=\"tm-ph\"><div><h1>Team</h1><p>The people shown on the Studio page, in this order. These are website profiles, not sign-in accounts.</p></div><button type=\"button\" class=\"tm-btn\" onclick=\"AdminTeamJs.edit('')\">Add a person</button></div><div class=\"tm-tools\"><input type=\"search\" id=\"tm-q\" placeholder=\"Search name or role\" maxlength=\"40\" oninput=\"AdminTeamJs.type()\" aria-label=\"Search team\"></div><div id=\"tm-list\">" + ListHtml(site, string.Empty, 1) + "</div>");
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
            response.SetElementContents("tm-list", ListHtml(site, Ui.Clip(GetDataValue("term"), 40), Ui.Int(GetDataValue("pn"), 1, 1000, 1)));
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
            string id = Ui.Clip(GetDataValue("mid"), 60);
            TeamMember? p = id.Length == 0 ? new TeamMember() : site.Team.FirstOrDefault(x => x.Id == id);
            if (p == null)
            {
                response.ExecuteScript("AdminTeamJs.toast('That person was not found.');");
                return response;
            }
            response.SetElementContents("tm-mbody", FormHtml(media, p, id.Length == 0));
            response.ExecuteScript("AdminTeamJs.opened('" + Ui.Js(id.Length == 0 ? "Add a person" : p.Name) + "');");
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
            string id = Ui.Clip(GetDataValue("mid"), 60);
            List<MediaItem> media = await Media.Load();
            Dictionary<string, string> err = new Dictionary<string, string>();
            string name = Ui.Clip(GetDataValue("name"), 60);
            string role = Ui.Clip(GetDataValue("role"), 80);
            string bio = Ui.Clip(GetDataValue("bio"), 400);
            string photo = Ui.Clip(GetDataValue("photo"), 200);
            if (name.Length < 2)
            {
                err["name"] = "Please enter the person's name.";
            }
            if (role.Length < 2)
            {
                err["role"] = "Please enter a role, for example Project architect.";
            }
            if (photo.Length > 0 && Media.Find(media, photo, "photo") == null)
            {
                err["photo"] = "That picture is no longer in the media library. Choose it again.";
            }
            foreach (string f in new[] { "name", "role", "photo" })
            {
                response.SetElementContents("tm-e-" + f, err.TryGetValue(f, out string? m) ? m : string.Empty);
            }
            if (err.Count > 0)
            {
                response.ExecuteScript("AdminTeamJs.failed();");
                return response;
            }
            bool created = false;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                TeamMember? p = id.Length == 0 ? null : site.Team.FirstOrDefault(x => x.Id == id);
                if (p == null && id.Length > 0)
                {
                    response.ExecuteScript("AdminTeamJs.toast('That person was not found. They may have been deleted.');");
                    return response;
                }
                if (p == null)
                {
                    string slug = Ui.Slug(name);
                    if (slug.Length == 0)
                    {
                        slug = "member";
                    }
                    string unique = slug;
                    for (int n = 2; site.Team.Any(x => x.Id == unique); n++)
                    {
                        unique = slug + "-" + n;
                    }
                    p = new TeamMember { Id = unique };
                    site.Team.Add(p);
                    created = true;
                }
                p.Name = name;
                p.Role = role;
                p.Bio = bio;
                p.Photo = photo;
                p.Published = GetDataValue("published") == "1";
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, created ? "add team member" : "edit team member", name, true);
            response.SetElementContents("tm-list", ListHtml(await Store.LoadSite(), Ui.Clip(GetDataValue("fterm"), 40), Ui.Int(GetDataValue("fpn"), 1, 1000, 1)));
            response.ExecuteScript("AdminTeamJs.saved(" + (created ? "true" : "false") + ");");
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
            string id = Ui.Clip(GetDataValue("mid"), 60);
            int dir = GetDataValue("dir") == "-1" ? -1 : 1;
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                int at = site.Team.FindIndex(x => x.Id == id);
                int to = at + dir;
                if (at >= 0 && to >= 0 && to < site.Team.Count)
                {
                    TeamMember p = site.Team[at];
                    site.Team[at] = site.Team[to];
                    site.Team[to] = p;
                    name = p.Name;
                    await Store.SaveSite(site);
                }
            }
            if (name.Length > 0)
            {
                await Store.Log(c.U.Id, "move team member", name, true);
            }
            response.SetElementContents("tm-list", ListHtml(await Store.LoadSite(), Ui.Clip(GetDataValue("fterm"), 40), Ui.Int(GetDataValue("fpn"), 1, 1000, 1)));
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
            string id = Ui.Clip(GetDataValue("mid"), 60);
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                TeamMember? p = site.Team.FirstOrDefault(x => x.Id == id);
                if (p != null)
                {
                    name = p.Name;
                    site.Team.Remove(p);
                    await Store.SaveSite(site);
                }
            }
            if (name.Length == 0)
            {
                response.ExecuteScript("AdminTeamJs.toast('That person was not found.');");
                return response;
            }
            await Store.Log(c.U.Id, "delete team member", name, true);
            response.SetElementContents("tm-list", ListHtml(await Store.LoadSite(), Ui.Clip(GetDataValue("fterm"), 40), Ui.Int(GetDataValue("fpn"), 1, 1000, 1)));
            response.ExecuteScript("AdminTeamJs.dropped();");
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
            await Media.PickReply(response, "tm", "AdminTeamJs", GetDataValue("pterm") ?? string.Empty);
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
            await Media.UploadReply(response, "tm", "AdminTeamJs", c.U);
            return response;
        }

        private static string ListHtml(SiteData site, string term, int pn)
        {
            bool filtered = term.Length > 0;
            List<TeamMember> rows = site.Team.Where(p => term.Length == 0 || Ui.Has(p.Name, term) || Ui.Has(p.Role, term)).ToList();
            if (rows.Count == 0)
            {
                return "<div class=\"tm-panel tm-empty\">" + (site.Team.Count == 0 ? "No team members yet. Add the first person." : "No team members match.") + "</div>";
            }
            int pages = (rows.Count + Size - 1) / Size;
            pn = Math.Min(pn, pages);
            StringBuilder sb = new StringBuilder("<div class=\"tm-wrapt\"><table class=\"tm-table tm-stack\"><thead><tr><th>Person</th><th>Role</th><th>Status</th><th></th></tr></thead><tbody>");
            foreach (TeamMember p in rows.Skip((pn - 1) * Size).Take(Size))
            {
                string id = Ui.Js(p.Id);
                int at = site.Team.IndexOf(p);
                sb.Append("<tr><td class=\"tm-first\"><div class=\"tm-ttl\">" + (p.Photo.Length > 0 ? "<img class=\"tm-thumb tm-face\" src=\"" + Ui.Esc(Media.Thumb(p.Photo)) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<span><b>" + Ui.Esc(p.Name) + "</b></span></div></td><td data-l=\"Role\">" + Ui.Esc(p.Role) + "</td><td data-l=\"Status\"><span class=\"tm-pill " + (p.Published ? "tm-pill-go" : "tm-pill-off") + "\">" + (p.Published ? "Shown" : "Hidden") + "</span></td>");
                sb.Append("<td class=\"tm-acts2\"><button type=\"button\" class=\"tm-btn tm-btn-ghost tm-btn-sm\" onclick=\"AdminTeamJs.edit('" + id + "')\">Edit</button>");
                if (!filtered)
                {
                    sb.Append("<button type=\"button\" class=\"tm-btn tm-btn-ghost tm-btn-sm\" aria-label=\"Move " + Ui.Esc(p.Name) + " up\"" + (at == 0 ? " disabled" : string.Empty) + " onclick=\"AdminTeamJs.move('" + id + "',-1)\">&uarr;</button><button type=\"button\" class=\"tm-btn tm-btn-ghost tm-btn-sm\" aria-label=\"Move " + Ui.Esc(p.Name) + " down\"" + (at == site.Team.Count - 1 ? " disabled" : string.Empty) + " onclick=\"AdminTeamJs.move('" + id + "',1)\">&darr;</button>");
                }
                sb.Append("<button type=\"button\" class=\"tm-btn tm-btn-danger tm-btn-sm\" onclick=\"AdminTeamJs.drop('" + id + "','" + Ui.Js(p.Name) + "')\">Delete</button></td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"tm-pager\"><span>Page " + pn + " of " + pages + " &middot; " + Ui.Count(rows.Count, "person", "people") + (filtered ? string.Empty : ". Order here is the order on the Studio page.") + "</span><div>" + (pn > 1 ? "<button type=\"button\" class=\"tm-btn tm-btn-ghost tm-btn-sm\" onclick=\"AdminTeamJs.go(" + (pn - 1) + ")\">Previous</button>" : string.Empty) + (pn < pages ? "<button type=\"button\" class=\"tm-btn tm-btn-ghost tm-btn-sm\" onclick=\"AdminTeamJs.go(" + (pn + 1) + ")\">Next</button>" : string.Empty) + "</div></div>");
            return sb.ToString();
        }

        private static string FormHtml(List<MediaItem> media, TeamMember p, bool isNew)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<form class=\"tm-stack2\" id=\"tm-form\" onsubmit=\"return AdminTeamJs.save('" + Ui.Js(isNew ? string.Empty : p.Id) + "')\" novalidate>");
            sb.Append("<div class=\"tm-grid2\"><label class=\"tm-field\" for=\"tm-f-name\"><span>Name</span><input type=\"text\" id=\"tm-f-name\" data-k=\"name\" maxlength=\"60\" value=\"" + Ui.Esc(p.Name) + "\"><em class=\"tm-err\" id=\"tm-e-name\"></em></label>");
            sb.Append("<label class=\"tm-field\" for=\"tm-f-role\"><span>Role</span><input type=\"text\" id=\"tm-f-role\" data-k=\"role\" maxlength=\"80\" value=\"" + Ui.Esc(p.Role) + "\"><em class=\"tm-err\" id=\"tm-e-role\"></em></label></div>");
            sb.Append("<label class=\"tm-field\" for=\"tm-f-bio\"><span>Short bio</span><textarea id=\"tm-f-bio\" class=\"tm-textarea\" data-k=\"bio\" maxlength=\"400\">" + Ui.Esc(p.Bio) + "</textarea><small>Up to 400 characters.</small></label>");
            sb.Append("<div class=\"tm-sect\">" + Media.PicField(media, "tm", "AdminTeamJs", "photo", p.Photo, "Portrait (optional)", true) + "<em class=\"tm-err\" id=\"tm-e-photo\"></em></div>");
            sb.Append("<label class=\"tm-check\"><input type=\"checkbox\" data-k=\"published\"" + (p.Published ? " checked" : string.Empty) + "> Show on the Studio page</label>");
            sb.Append("<div class=\"tm-acts\"><button type=\"submit\" class=\"tm-btn\" id=\"tm-save\">" + (isNew ? "Add person" : "Save person") + "</button><button type=\"button\" class=\"tm-btn tm-btn-ghost\" onclick=\"AdminTeamJs.closeModal()\">Cancel</button></div></form>");
            return sb.ToString();
        }
    }
}
