using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class AdminMessages : WebPage
    {
        private const int Size = 10;

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle("Messages | " + site.Brand + " staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMessagesJs.expired();");
            }
            return c;
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminMessagesJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("ms-brand", Ui.Esc(site.Brand));
            response.SetElementContents("ms-menu", await Auth.Menu("ms", "AdminMessages", c));
            response.SetElementContents("ms-who", Auth.Who("ms", c));
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
            List<Inquiry> all = await Store.LoadInquiries();
            response.SetElementContents("ms-main", PageHtml(all, site));
            response.SetElementContents("ms-list", ListHtml(all, string.Empty, string.Empty, 1));
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
            List<Inquiry> all = await Store.LoadInquiries();
            string st = Ui.Pick(GetDataValue("st"), Catalog.Statuses.Select(s => s.Key));
            response.SetElementContents("ms-list", ListHtml(all, st, Ui.Clip(GetDataValue("term"), 40), Ui.Int(GetDataValue("pn"), 1, 1000, 1)));
            response.SetElementContents("ms-menu", await Auth.Menu("ms", "AdminMessages", c));
            return response;
        }

        public async Task<ApiResponse> Open()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            string id = Ui.Clip(GetDataValue("mid"), 20);
            Inquiry? m;
            using (await Store.Hold())
            {
                List<Inquiry> all = await Store.LoadInquiries();
                m = all.FirstOrDefault(x => x.Id == id);
                if (m != null && m.Status == "new")
                {
                    m.Status = "read";
                    await Store.SaveInquiries(all);
                }
            }
            if (m == null)
            {
                response.ExecuteScript("AdminMessagesJs.toast('That message was not found. It may have been deleted.');");
                return response;
            }
            response.SetElementContents("ms-mbody", FormHtml(m));
            response.ExecuteScript("AdminMessagesJs.opened('" + Ui.Js(m.Name) + "');");
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
            string id = Ui.Clip(GetDataValue("mid"), 20);
            string status = Ui.Pick(GetDataValue("status"), Catalog.Statuses.Select(s => s.Key));
            string note = Ui.Clip(GetDataValue("note"), 600);
            if (status.Length == 0)
            {
                return await Refuse(response, c, "update message", id, "Choose a status for the message.");
            }
            string name = string.Empty;
            using (await Store.Hold())
            {
                List<Inquiry> all = await Store.LoadInquiries();
                Inquiry? m = all.FirstOrDefault(x => x.Id == id);
                if (m != null)
                {
                    m.Status = status;
                    m.Note = note;
                    name = m.Name;
                    await Store.SaveInquiries(all);
                }
            }
            if (name.Length == 0)
            {
                response.ExecuteScript("AdminMessagesJs.toast('That message was not found.');");
                return response;
            }
            await Store.Log(c.U.Id, "update message", name + " · " + Catalog.Status(status), true);
            response.ExecuteScript("AdminMessagesJs.saved();");
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
            string id = Ui.Clip(GetDataValue("mid"), 20);
            string name = string.Empty;
            using (await Store.Hold())
            {
                List<Inquiry> all = await Store.LoadInquiries();
                Inquiry? m = all.FirstOrDefault(x => x.Id == id);
                if (m != null)
                {
                    name = m.Name;
                    all.Remove(m);
                    await Store.SaveInquiries(all);
                }
            }
            if (name.Length == 0)
            {
                response.ExecuteScript("AdminMessagesJs.toast('That message was not found.');");
                return response;
            }
            await Store.Log(c.U.Id, "delete message", name, true);
            response.ExecuteScript("AdminMessagesJs.dropped();");
            return response;
        }

        private static string PageHtml(List<Inquiry> all, SiteData site)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<div class=\"ms-ph\"><div><h1>Messages</h1><p>Project inquiries sent from the Contact page. Replies go by email or phone; set the status here to keep track.</p></div></div>");
            sb.Append("<div class=\"ms-tools\"><div class=\"ms-chips\" id=\"ms-chips\"><button type=\"button\" class=\"ms-chip ms-act\" data-st=\"\" onclick=\"AdminMessagesJs.status('')\">All <i>" + all.Count + "</i></button>");
            foreach ((string key, string name) in Catalog.Statuses)
            {
                sb.Append("<button type=\"button\" class=\"ms-chip\" data-st=\"" + key + "\" onclick=\"AdminMessagesJs.status('" + key + "')\">" + Ui.Esc(name) + " <i>" + all.Count(m => m.Status == key) + "</i></button>");
            }
            sb.Append("</div><input type=\"search\" id=\"ms-q\" placeholder=\"Search name, email or message\" maxlength=\"40\" oninput=\"AdminMessagesJs.type()\" aria-label=\"Search messages\"></div><div id=\"ms-list\"></div>");
            return sb.ToString();
        }

        private static string ListHtml(List<Inquiry> all, string st, string term, int pn)
        {
            List<Inquiry> rows = all.Where(m => (st.Length == 0 || m.Status == st) && (term.Length == 0 || Ui.Has(m.Name, term) || Ui.Has(m.Email, term) || Ui.Has(m.Message, term) || Ui.Has(m.Place, term))).OrderByDescending(m => m.Created).ToList();
            if (rows.Count == 0)
            {
                return "<div class=\"ms-panel ms-empty\">" + (all.Count == 0 ? "No messages yet. They appear here when someone uses the form on the Contact page." : "No messages match.") + "</div>";
            }
            int pages = (rows.Count + Size - 1) / Size;
            pn = Math.Min(pn, pages);
            StringBuilder sb = new StringBuilder("<div class=\"ms-wrapt\"><table class=\"ms-table ms-stack\"><thead><tr><th>From</th><th>Project</th><th>Received</th><th>Status</th></tr></thead><tbody>");
            foreach (Inquiry m in rows.Skip((pn - 1) * Size).Take(Size))
            {
                string kind = Catalog.Kind(m.Kind);
                sb.Append("<tr class=\"ms-row\" tabindex=\"0\" onclick=\"AdminMessagesJs.open('" + Ui.Js(m.Id) + "')\" onkeydown=\"if(event.key==='Enter'){AdminMessagesJs.open('" + Ui.Js(m.Id) + "')}\"><td class=\"ms-first\"><b" + (m.Status == "new" ? " class=\"ms-unread\"" : string.Empty) + ">" + Ui.Esc(m.Name) + "</b><small>" + Ui.Esc(m.Email) + "</small></td><td data-l=\"Project\">" + Ui.Esc(kind.Length > 0 ? kind : "Not stated") + (m.Place.Length > 0 ? "<small>" + Ui.Esc(m.Place) + "</small>" : string.Empty) + "</td><td data-l=\"Received\">" + Ui.Esc(Ui.When(m.Created)) + "</td><td data-l=\"Status\"><span class=\"ms-pill ms-pill-" + m.Status + "\">" + Ui.Esc(Catalog.Status(m.Status)) + "</span></td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"ms-pager\"><span>Page " + pn + " of " + pages + " &middot; " + Ui.Count(rows.Count, "message", "messages") + "</span><div>" + (pn > 1 ? "<button type=\"button\" class=\"ms-btn ms-btn-ghost ms-btn-sm\" onclick=\"AdminMessagesJs.go(" + (pn - 1) + ")\">Previous</button>" : string.Empty) + (pn < pages ? "<button type=\"button\" class=\"ms-btn ms-btn-ghost ms-btn-sm\" onclick=\"AdminMessagesJs.go(" + (pn + 1) + ")\">Next</button>" : string.Empty) + "</div></div>");
            return sb.ToString();
        }

        private static string FormHtml(Inquiry m)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<form class=\"ms-stack2\" id=\"ms-form\" onsubmit=\"return AdminMessagesJs.save('" + Ui.Js(m.Id) + "')\" novalidate>");
            sb.Append("<dl class=\"ms-kv\"><dt>From</dt><dd>" + Ui.Esc(m.Name) + "</dd><dt>Email</dt><dd><a href=\"mailto:" + Ui.Esc(m.Email) + "\">" + Ui.Esc(m.Email) + "</a></dd>");
            if (m.Phone.Length > 0)
            {
                sb.Append("<dt>Phone</dt><dd><a href=\"tel:" + Ui.Esc(new string(m.Phone.Where(ch => char.IsDigit(ch) || ch == '+').ToArray())) + "\">" + Ui.Esc(m.Phone) + "</a></dd>");
            }
            sb.Append("<dt>Received</dt><dd>" + Ui.Esc(Ui.When(m.Created)) + "</dd><dt>Reference</dt><dd>" + Ui.Esc(m.Id) + "</dd>");
            sb.Append("<dt>Project type</dt><dd>" + Ui.Esc(Catalog.Kind(m.Kind).Length > 0 ? Catalog.Kind(m.Kind) : "Not stated") + "</dd><dt>Where</dt><dd>" + Ui.Esc(m.Place.Length > 0 ? m.Place : "Not stated") + "</dd><dt>Budget</dt><dd>" + Ui.Esc(m.Budget.Length > 0 ? m.Budget : "Not stated") + "</dd><dt>Start</dt><dd>" + Ui.Esc(m.Timeline.Length > 0 ? m.Timeline : "Not stated") + "</dd></dl>");
            sb.Append("<div class=\"ms-msg\">" + Ui.Lines(m.Message) + "</div>");
            sb.Append("<label class=\"ms-field\" for=\"ms-f-status\"><span>Status</span><select id=\"ms-f-status\" data-k=\"status\">" + string.Concat(Catalog.Statuses.Select(s => "<option value=\"" + s.Key + "\"" + (s.Key == m.Status ? " selected" : string.Empty) + ">" + Ui.Esc(s.Name) + "</option>")) + "</select></label>");
            sb.Append("<label class=\"ms-field\" for=\"ms-f-note\"><span>Private note (never shown to the visitor)</span><textarea id=\"ms-f-note\" class=\"ms-textarea\" data-k=\"note\" maxlength=\"600\">" + Ui.Esc(m.Note) + "</textarea></label>");
            sb.Append("<div class=\"ms-acts\"><button type=\"submit\" class=\"ms-btn\">Save</button><button type=\"button\" class=\"ms-btn ms-btn-ghost\" onclick=\"AdminMessagesJs.closeModal()\">Close</button><button type=\"button\" class=\"ms-btn ms-btn-danger ms-push\" onclick=\"AdminMessagesJs.drop('" + Ui.Js(m.Id) + "','" + Ui.Js(m.Name) + "')\">Delete message</button></div></form>");
            return sb.ToString();
        }
    }
}
