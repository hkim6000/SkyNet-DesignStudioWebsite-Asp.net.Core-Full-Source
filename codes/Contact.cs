using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class Contact : WebPage
    {
        private static readonly Dictionary<string, List<DateTime>> Recent = new Dictionary<string, List<DateTime>>();
        private string startText = string.Empty;
        private string startKind = string.Empty;

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            List<MediaItem> media = await Media.Load();
            HtmlDoc.SetTitle("Contact | " + site.Brand);
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Tell " + site.Brand + " about your project. We reply within two working days.");
            Work? w = site.Projects.FirstOrDefault(x => x.Published && x.Id == Ui.Clip(QueryValue("p"), 60));
            Offer? s = site.Services.FirstOrDefault(x => x.Published && x.Id == Ui.Clip(QueryValue("s"), 60));
            if (w != null)
            {
                startText = "I saw " + w.Title + " and would like to talk about something similar.";
                startKind = w.Kind;
            }
            else if (s != null)
            {
                startText = "I would like to talk about " + s.Title.ToLowerInvariant() + ".";
            }
            HtmlDoc.HtmlBodyText = Fill(HtmlDoc.HtmlBodyText, site).Replace("{plhd_blocks}", PageHtml(site, media, "contact"));
        }

        public async Task<ApiResponse> Send()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await Store.LoadSite();
            Dictionary<string, string> err = new Dictionary<string, string>();
            string name = Ui.Clip(GetDataValue("name"), 80);
            string email = Ui.Clip(GetDataValue("email"), 120);
            string phone = Ui.Clip(GetDataValue("phone"), 30);
            string kind = Ui.Pick(GetDataValue("kind"), Catalog.Kinds.Select(k => k.Key).Append("other"));
            string budget = Ui.Pick(GetDataValue("budget"), site.Budgets);
            string timeline = Ui.Pick(GetDataValue("timeline"), site.Timelines);
            string place = Ui.Clip(GetDataValue("place"), 120);
            string message = Ui.Clip(GetDataValue("message"), 2000);
            string hp = Ui.Clip(GetDataValue("hp"), 40);
            if (name.Length < 2)
            {
                err["name"] = "Please enter your name.";
            }
            if (!Ui.IsEmail(email))
            {
                err["email"] = "Please enter a valid email address.";
            }
            if (phone.Length > 0 && !Ui.IsPhone(phone))
            {
                err["phone"] = "Please enter a phone number we can reach you on, or leave it empty.";
            }
            if (message.Length < 10)
            {
                err["message"] = "Please tell us a little about the project, at least a sentence.";
            }
            if (GetDataValue("agree") != "1")
            {
                err["agree"] = "Please tick the box so we may contact you about your inquiry.";
            }
            foreach (string f in new[] { "name", "email", "phone", "kind", "budget", "timeline", "place", "message", "agree" })
            {
                response.SetElementContents("ct-e-" + f, err.TryGetValue(f, out string? m) ? m : string.Empty);
            }
            if (err.Count > 0)
            {
                response.ExecuteScript("ContactJs.failed();");
                return response;
            }
            string id = "iq-" + Store.NewCode(6);
            if (hp.Length == 0)
            {
                if (TooMany(Local.Caller()))
                {
                    response.SetElementContents("ct-e-message", "Too many inquiries from this connection. Please try again in a few minutes, or call the studio.");
                    response.ExecuteScript("ContactJs.failed();");
                    return response;
                }
                using (await Store.Hold())
                {
                    List<Inquiry> list = await Store.LoadInquiries();
                    list.Insert(0, new Inquiry { Id = id, Created = DateTime.UtcNow, Name = name, Email = email, Phone = phone, Kind = kind, Budget = budget, Timeline = timeline, Place = place, Message = message, Status = "new" });
                    if (list.Count > 1000)
                    {
                        list.RemoveRange(1000, list.Count - 1000);
                    }
                    await Store.SaveInquiries(list);
                }
            }
            response.SetElementContents("ct-done", "<div class=\"ct-ok\"><svg viewBox=\"0 0 24 24\" width=\"30\" height=\"30\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg></div><h2>Thank you, " + Ui.Esc(name) + "</h2><p>We have your inquiry. We read every one personally and reply by email" + (phone.Length > 0 ? " or phone" : string.Empty) + " within two working days to arrange a first conversation.</p><p class=\"ct-ref\">Your reference: <b>" + id + "</b></p><button type=\"button\" class=\"ct-btn ct-btn-ghost\" onclick=\"ContactJs.again()\">Send another inquiry</button>");
            response.ExecuteScript("ContactJs.sent();");
            return response;
        }

        private static bool TooMany(string who)
        {
            lock (Recent)
            {
                if (!Recent.TryGetValue(who, out List<DateTime>? list))
                {
                    list = new List<DateTime>();
                    Recent[who] = list;
                }
                list.RemoveAll(t => t < DateTime.UtcNow.AddMinutes(-10));
                if (list.Count >= 5)
                {
                    return true;
                }
                list.Add(DateTime.UtcNow);
                return false;
            }
        }

        private static string Opts(IEnumerable<(string Key, string Name)> list, string cur, string first)
        {
            StringBuilder sb = new StringBuilder("<option value=\"\">" + Ui.Esc(first) + "</option>");
            foreach ((string k, string n) in list)
            {
                sb.Append("<option value=\"" + Ui.Esc(k) + "\"" + (k == cur ? " selected" : string.Empty) + ">" + Ui.Esc(n) + "</option>");
            }
            return sb.ToString();
        }

        private string DataPart(SiteData site, List<MediaItem> media, Block b, bool first)
        {
            if (b.Source != "contactform")
            {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder(PartHead(site, b, first));
            sb.Append("<div class=\"ct-grid\"><div class=\"ct-info\"><h2>Visit or call</h2><ul class=\"ct-list\">");
            if (site.Address.Length > 0)
            {
                sb.Append("<li><span class=\"ct-ico\">" + Icon("pin") + "</span><span>" + Ui.Lines(site.Address) + "</span></li>");
            }
            if (site.Phone.Length > 0)
            {
                sb.Append("<li><span class=\"ct-ico\">" + Icon("phone") + "</span><a href=\"tel:" + Ui.Esc(new string(site.Phone.Where(ch => char.IsDigit(ch) || ch == '+').ToArray())) + "\">" + Ui.Esc(site.Phone) + "</a></li>");
            }
            if (site.Email.Length > 0)
            {
                sb.Append("<li><span class=\"ct-ico\">" + Icon("mail") + "</span><a href=\"mailto:" + Ui.Esc(site.Email) + "\">" + Ui.Esc(site.Email) + "</a></li>");
            }
            if (site.Hours.Length > 0)
            {
                sb.Append("<li><span class=\"ct-ico\">" + Icon("cal") + "</span><span>" + Ui.Lines(site.Hours) + "</span></li>");
            }
            sb.Append("</ul><p class=\"ct-note\">Your details are used only to reply to your inquiry. We do not add you to any mailing list.</p></div>");
            sb.Append("<div class=\"ct-card\"><form id=\"ct-form\" onsubmit=\"return ContactJs.send()\" novalidate><h2>Project inquiry</h2><div class=\"ct-two\">");
            sb.Append("<label class=\"ct-f\" for=\"ct-name\"><span>Your name</span><input type=\"text\" id=\"ct-name\" maxlength=\"80\" autocomplete=\"name\"><em class=\"ct-err\" id=\"ct-e-name\"></em></label>");
            sb.Append("<label class=\"ct-f\" for=\"ct-email\"><span>Email</span><input type=\"email\" id=\"ct-email\" maxlength=\"120\" autocomplete=\"email\"><em class=\"ct-err\" id=\"ct-e-email\"></em></label>");
            sb.Append("<label class=\"ct-f\" for=\"ct-phone\"><span>Phone (optional)</span><input type=\"tel\" id=\"ct-phone\" maxlength=\"30\" autocomplete=\"tel\"><em class=\"ct-err\" id=\"ct-e-phone\"></em></label>");
            sb.Append("<label class=\"ct-f\" for=\"ct-place\"><span>Where is the project?</span><input type=\"text\" id=\"ct-place\" maxlength=\"120\"><em class=\"ct-err\" id=\"ct-e-place\"></em></label>");
            sb.Append("<label class=\"ct-f\" for=\"ct-kind\"><span>Type of project</span><select id=\"ct-kind\">" + Opts(Catalog.Kinds.Append(("other", "Not sure yet")), startKind, "Choose a type") + "</select><em class=\"ct-err\" id=\"ct-e-kind\"></em></label>");
            sb.Append("<label class=\"ct-f\" for=\"ct-budget\"><span>Budget</span><select id=\"ct-budget\">" + Opts(site.Budgets.Select(x => (x, x)), string.Empty, "Choose a range") + "</select><em class=\"ct-err\" id=\"ct-e-budget\"></em></label>");
            sb.Append("<label class=\"ct-f\" for=\"ct-timeline\"><span>When would you like to start?</span><select id=\"ct-timeline\">" + Opts(site.Timelines.Select(x => (x, x)), string.Empty, "Choose a time") + "</select><em class=\"ct-err\" id=\"ct-e-timeline\"></em></label></div>");
            sb.Append("<label class=\"ct-f\" for=\"ct-message\"><span>Tell us about the project</span><textarea id=\"ct-message\" rows=\"6\" maxlength=\"2000\">" + Ui.Esc(startText) + "</textarea><em class=\"ct-err\" id=\"ct-e-message\"></em></label>");
            sb.Append("<div class=\"ct-hp\" aria-hidden=\"true\"><label for=\"ct-hp\">Leave this field empty</label><input type=\"text\" id=\"ct-hp\" tabindex=\"-1\" autocomplete=\"off\"></div>");
            sb.Append("<label class=\"ct-chk\" for=\"ct-agree\"><input type=\"checkbox\" id=\"ct-agree\"><span>You may contact me about this inquiry.</span></label><em class=\"ct-err\" id=\"ct-e-agree\"></em>");
            sb.Append("<button type=\"submit\" class=\"ct-btn\" id=\"ct-submit\">Send inquiry</button></form><div class=\"ct-done\" id=\"ct-done\" role=\"status\" aria-live=\"polite\" hidden></div></div></div>");
            return Sec(b, sb.ToString());
        }

        private static string FootContact(SiteData site)
        {
            StringBuilder sb = new StringBuilder();
            if (site.Address.Length > 0)
            {
                sb.Append("<li>" + Ui.Esc(site.Address) + "</li>");
            }
            if (site.Phone.Length > 0)
            {
                sb.Append("<li><a href=\"tel:" + Ui.Esc(new string(site.Phone.Where(ch => char.IsDigit(ch) || ch == '+').ToArray())) + "\">" + Ui.Esc(site.Phone) + "</a></li>");
            }
            if (site.Email.Length > 0)
            {
                sb.Append("<li><a href=\"mailto:" + Ui.Esc(site.Email) + "\">" + Ui.Esc(site.Email) + "</a></li>");
            }
            if (site.Hours.Length > 0)
            {
                sb.Append("<li>" + Ui.Esc(site.Hours) + "</li>");
            }
            return sb.ToString();
        }

        private static string Fill(string body, SiteData site)
        {
            return body.Replace("{plhd_brand}", Ui.Esc(site.Brand)).Replace("{plhd_tag}", Ui.Esc(site.Tagline)).Replace("{plhd_fcontact}", FootContact(site)).Replace("{plhd_year}", DateTime.Today.Year.ToString());
        }

        private static string ProjectCard(Work w)
        {
            return "<a class=\"ct-pc\" href=\"Project?id=" + Ui.Esc(w.Id) + "\"><span class=\"ct-pc-img\">" + (w.Cover.Length > 0 ? "<img src=\"" + Ui.Esc(Media.Thumb(w.Cover)) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<i class=\"ct-pc-tag\">" + Ui.Esc(Catalog.Kind(w.Kind)) + "</i>" + (w.Pairs.Count > 0 ? "<i class=\"ct-pc-ba\">Before and after</i>" : string.Empty) + "</span><span class=\"ct-pc-t\"><b>" + Ui.Esc(w.Title) + "</b><small>" + Ui.Esc(w.Location) + " &middot; " + w.Year + "</small><p>" + Ui.Esc(w.Summary) + "</p></span></a>";
        }

        private static string EntryCard(Entry e)
        {
            bool press = e.Kind == "press";
            string href = press ? e.Link : "Journal?id=" + e.Id;
            string img = e.Cover.Length > 0 ? "<span class=\"ct-pc-img\"><img src=\"" + Ui.Esc(Media.Thumb(e.Cover)) + "\" alt=\"\" loading=\"lazy\"></span>" : string.Empty;
            string inner = img + "<span class=\"ct-pc-t\"><small>" + (press ? "Press &middot; " + Ui.Esc(e.Source) : Ui.Esc(e.Category)) + " &middot; " + Ui.Esc(Ui.Day(e.Date)) + "</small><b>" + Ui.Esc(e.Title) + "</b><p>" + Ui.Esc(e.Summary) + "</p>" + (press ? "<em class=\"ct-ec-out\">Read the article</em>" : string.Empty) + "</span>";
            string cls = "ct-pc ct-ec" + (img.Length == 0 ? " ct-ec-text" : string.Empty);
            return href.Length == 0 ? "<div class=\"" + cls + "\">" + inner + "</div>" : "<a class=\"" + cls + "\" href=\"" + Ui.Esc(href) + "\"" + (press ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + inner + "</a>";
        }

        private static string PartHead(SiteData site, Block b, bool first)
        {
            string tag = first ? "h1" : "h2";
            string x = Kick(site, b) + (b.Title == string.Empty ? string.Empty : "<" + tag + " class=\"ct-bh2" + (first ? " ct-bh1" : string.Empty) + "\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">") + Rich(site, b.Text, "ct-blead");
            return x == string.Empty ? string.Empty : "<div class=\"ct-bhead\">" + x + "</div>";
        }

        private static string Tok(SiteData site, string v)
        {
            return v.Replace("{name}", site.Brand);
        }

        private static string TokH(SiteData site, string v)
        {
            return v.Replace("{name}", Ui.Esc(site.Brand));
        }

        private static string Lines(string v)
        {
            return Ui.Esc(v).Replace("\n", "<br>");
        }

        private static string Icon(string k)
        {
            string a = "<svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">";
            return k switch
            {
                "home" => a + "<path d=\"M4 11l8-6.5L20 11M6 9.5V19h12V9.5M10 19v-5h4v5\"/></svg>",
                "ruler" => a + "<path d=\"M3.5 15.5l12-12 5 5-12 12zM7 12l2 2M10 9l2 2M13 6l2 2\"/></svg>",
                "pencil" => a + "<path d=\"M4 20l1-4.5L16.5 4a2 2 0 0 1 3 3L8 18.5zM14.5 6l3.5 3.5\"/></svg>",
                "compass" => a + "<circle cx=\"12\" cy=\"12\" r=\"8.5\"/><path d=\"M15.5 8.5l-2 5-5 2 2-5z\"/></svg>",
                "layers" => a + "<path d=\"M12 4l9 5-9 5-9-5zM3 14l9 5 9-5\"/></svg>",
                "leaf" => a + "<path d=\"M5 19c0-8 5-13 14-13-0 9-5 14-14 13zM5 19c3-4 6-7 10-9\"/></svg>",
                "sun" => a + "<circle cx=\"12\" cy=\"12\" r=\"3.8\"/><path d=\"M12 3v2.5M12 18.5V21M3 12h2.5M18.5 12H21M5.6 5.6l1.8 1.8M16.6 16.6l1.8 1.8M18.4 5.6l-1.8 1.8M7.4 16.6l-1.8 1.8\"/></svg>",
                "key" => a + "<circle cx=\"8\" cy=\"15\" r=\"3.5\"/><path d=\"M10.5 12.5L20 3M16 7l3 3M13.5 9.5l2 2\"/></svg>",
                "tool" => a + "<path d=\"M14.5 5.5a4 4 0 0 0-5 5L4 16l4 4 5.5-5.5a4 4 0 0 0 5-5l-2.5 2.5-2.5-.5-.5-2.5z\"/></svg>",
                "pin" => a + "<path d=\"M12 21s-6.5-6-6.5-11a6.5 6.5 0 0 1 13 0c0 5-6.5 11-6.5 11z\"/><circle cx=\"12\" cy=\"10\" r=\"2.4\"/></svg>",
                "cal" => a + "<rect x=\"3.5\" y=\"5\" width=\"17\" height=\"15\" rx=\"2\"/><path d=\"M3.5 10h17M8 3v4M16 3v4\"/></svg>",
                "users" => a + "<circle cx=\"9\" cy=\"8\" r=\"3.2\"/><path d=\"M3 19c0-3.3 2.7-5.5 6-5.5s6 2.2 6 5.5\"/><circle cx=\"17\" cy=\"9\" r=\"2.4\"/><path d=\"M16 13.6c2.8 0 5 1.8 5 4.9\"/></svg>",
                "phone" => a + "<path d=\"M6.5 3.5h3l1.5 4.5-2 1.5a11 11 0 0 0 5.5 5.5l1.5-2 4.5 1.5v3a2 2 0 0 1-2 2A16 16 0 0 1 4.5 5.5a2 2 0 0 1 2-2z\"/></svg>",
                "mail" => a + "<rect x=\"3.5\" y=\"5.5\" width=\"17\" height=\"13\" rx=\"2\"/><path d=\"M4 7l8 6 8-6\"/></svg>",
                "star" => a + "<path d=\"M12 3.5l2.6 5.4 5.9.8-4.3 4.1 1 5.8L12 16.9l-5.2 2.7 1-5.8-4.3-4.1 5.9-.8z\"/></svg>",
                "check" => "<svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg>",
                _ => string.Empty
            };
        }

        private static string Mv(List<MediaItem> media, string src, string alt, bool bg, bool eager)
        {
            if (src == string.Empty)
            {
                return string.Empty;
            }
            MediaItem? m = media.FirstOrDefault(x => x.Path == src);
            if (Blocks.IsVideo(src))
            {
                return "<video src=\"" + Ui.Esc(src) + "\" playsinline " + (bg ? "autoplay muted loop aria-hidden=\"true\"" : "controls preload=\"metadata\" aria-label=\"" + Ui.Esc(alt) + "\"") + "></video>";
            }
            return "<img src=\"" + Ui.Esc(src) + "\" alt=\"" + Ui.Esc(m != null && m.Alt != string.Empty ? m.Alt : alt) + "\"" + (eager ? string.Empty : " loading=\"lazy\"") + ">";
        }

        private static string Btn(SiteData site, string label, string link, bool main)
        {
            return label == string.Empty || link == string.Empty ? string.Empty : "<a class=\"ct-btn" + (main ? string.Empty : " ct-btn-ghost") + "\" href=\"" + Ui.Esc(link) + "\"" + (link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + Ui.Esc(Tok(site, label)) + "</a>";
        }

        private static string Btns(SiteData site, Block b)
        {
            string x = Btn(site, b.Label, b.Link, true) + Btn(site, b.Label2, b.Link2, false);
            return x == string.Empty ? string.Empty : "<div class=\"ct-bbtns\">" + x + "</div>";
        }

        private static string Kick(SiteData site, Block b)
        {
            return b.Kicker == string.Empty ? string.Empty : "<div class=\"ct-eyebrow\">" + Ui.Esc(Tok(site, b.Kicker)) + "</div>";
        }

        private static string H2(SiteData site, Block b, string tag = "h2")
        {
            return b.Title == string.Empty ? string.Empty : "<" + tag + " class=\"ct-bh2\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">";
        }

        private static string Rich(SiteData site, string html, string cls)
        {
            return html == string.Empty ? string.Empty : "<div class=\"ct-rich" + (cls == string.Empty ? string.Empty : " " + cls) + "\">" + TokH(site, html) + "</div>";
        }

        private static string Head(SiteData site, Block b, bool lead, string tag = "h2")
        {
            string x = Kick(site, b) + H2(site, b, tag) + (lead ? Rich(site, b.Text, "ct-blead") : string.Empty);
            return x == string.Empty ? string.Empty : "<div class=\"ct-bhead\">" + x + "</div>";
        }

        private static string Sec(Block b, string inner)
        {
            return "<section class=\"ct-wrap ct-sec\" id=\"" + Ui.Esc(b.Id) + "\">" + inner + "</section>";
        }

        private static string Hero(SiteData site, List<MediaItem> media, Block b, bool h1)
        {
            string tag = h1 ? "h1" : "h2";
            bool vid = Blocks.IsVideo(b.Media);
            string mv = Mv(media, b.Media, Tok(site, b.Title), vid, h1);
            return "<section class=\"ct-bhero" + (mv == string.Empty ? " ct-bplain" : string.Empty) + "\" id=\"" + Ui.Esc(b.Id) + "\">" + (mv != string.Empty ? "<div class=\"ct-bhero-p\">" + mv + "</div>" : string.Empty) + "<div class=\"ct-wrap ct-bhero-in\"><div class=\"ct-bhero-t\">" + Kick(site, b) + "<" + tag + " class=\"ct-btitle\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">" + Rich(site, b.Text, "ct-blead") + Btns(site, b) + "</div></div></section>";
        }

        private static string Split(SiteData site, List<MediaItem> media, Block b)
        {
            string btn = Btn(site, b.Label, b.Link, true);
            return Sec(b, "<div class=\"ct-bsplit" + (b.Flip ? " ct-bflip" : string.Empty) + (b.Media == string.Empty ? " ct-bnomedia" : string.Empty) + "\">" + (b.Media != string.Empty ? "<div class=\"ct-bsm\">" + Mv(media, b.Media, Tok(site, b.Title), false, false) + "</div>" : string.Empty) + "<div class=\"ct-bst\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + (btn != string.Empty ? "<div class=\"ct-bbtns\">" + btn + "</div>" : string.Empty) + "</div></div>");
        }

        private static string Cards(SiteData site, Block b)
        {
            int n = Math.Clamp(b.Items.Count, 1, 4);
            if (b.Items.Count == 6)
            {
                n = 3;
            }
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"ct-bcards ct-n" + n + "\">");
            foreach (BlockItem it in b.Items)
            {
                string ico = Icon(it.Icon);
                string inner = (ico != string.Empty ? "<span class=\"ct-wico\">" + ico + "</span>" : string.Empty) + "<b>" + Ui.Esc(Tok(site, it.Title)) + "</b>" + (it.Text != string.Empty ? "<p>" + Lines(Tok(site, it.Text)) + "</p>" : string.Empty);
                if (it.Link != string.Empty)
                {
                    sb.Append("<a class=\"ct-bcard\" href=\"" + Ui.Esc(it.Link) + "\">" + inner + "<span class=\"ct-bmore\">" + Ui.Esc(it.Label != string.Empty ? it.Label : "Learn more") + " &rarr;</span></a>");
                }
                else
                {
                    sb.Append("<div class=\"ct-bcard\">" + inner + "</div>");
                }
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Steps(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<ol class=\"ct-bsteps ct-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
            int i = 1;
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<li><i>" + i++ + "</i><b>" + Ui.Esc(Tok(site, it.Title)) + "</b><p>" + Lines(Tok(site, it.Text)) + "</p></li>");
            }
            sb.Append("</ol>");
            return Sec(b, sb.ToString());
        }

        private static string StatRow(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"ct-bstats ct-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div><b>" + Ui.Esc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Initials(string name)
        {
            string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? "?" : string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
        }

        private static string People(SiteData site, List<MediaItem> media, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"ct-bpeople ct-n" + Math.Clamp(b.Items.Count, 1, 4) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"ct-bperson\">" + (it.Media != string.Empty && !Blocks.IsVideo(it.Media) ? Mv(media, it.Media, it.Title, false, false) : "<span class=\"ct-bnoface\">" + Ui.Esc(Initials(it.Title)) + "</span>") + "<b>" + Ui.Esc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Boxes(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"ct-bboxes ct-n" + Math.Clamp(b.Items.Count, 1, 3) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"ct-bbox\">" + (it.Title != string.Empty ? "<h3>" + Ui.Esc(Tok(site, it.Title)) + "</h3>" : string.Empty) + "<ul class=\"ct-bticks\">");
                foreach (string line in it.Text.Split('\n').Select(x => x.Trim()).Where(x => x != string.Empty))
                {
                    sb.Append("<li>" + Icon("check") + "<span>" + Ui.Esc(Tok(site, line)) + "</span></li>");
                }
                sb.Append("</ul></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Faq(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder("<div class=\"ct-bfaq\">" + Head(site, b, false) + "<div class=\"ct-bfaqs\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<details class=\"ct-bq\"><summary>" + Ui.Esc(Tok(site, it.Title)) + "</summary><p>" + Lines(Tok(site, it.Text)) + "</p></details>");
            }
            sb.Append("</div></div>");
            return Sec(b, sb.ToString());
        }

        private static string Quote(SiteData site, List<MediaItem> media, Block b)
        {
            return Sec(b, "<figure class=\"ct-bquote\">" + (b.Media != string.Empty && !Blocks.IsVideo(b.Media) ? Mv(media, b.Media, b.Title, false, false) : string.Empty) + "<blockquote>" + Lines(Tok(site, b.Text)) + "</blockquote>" + (b.Title != string.Empty ? "<figcaption>" + Ui.Esc(Tok(site, b.Title)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Wide(SiteData site, List<MediaItem> media, Block b)
        {
            return b.Media == string.Empty ? string.Empty : Sec(b, "<figure class=\"ct-bmedia\">" + Mv(media, b.Media, b.Text, false, false) + (b.Text != string.Empty ? "<figcaption>" + Ui.Esc(Tok(site, b.Text)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Cta(SiteData site, Block b)
        {
            return Sec(b, "<div class=\"ct-bcta\"><div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "ct-blead") + "</div>" + Btns(site, b) + "</div>");
        }

        private string PageHtml(SiteData site, List<MediaItem> media, string key)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            if (!site.Pages.TryGetValue(key, out PageDoc? doc))
            {
                return string.Empty;
            }
            foreach (Block b in doc.Blocks.Where(x => !x.Hidden))
            {
                string html = b.Type switch
                {
                    "hero" => Hero(site, media, b, first),
                    "text" => Sec(b, "<div class=\"ct-btext\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + "</div>"),
                    "split" => Split(site, media, b),
                    "cards" => Cards(site, b),
                    "steps" => Steps(site, b),
                    "stats" => StatRow(site, b),
                    "people" => People(site, media, b),
                    "boxes" => Boxes(site, b),
                    "faq" => Faq(site, b),
                    "quote" => Quote(site, media, b),
                    "media" => Wide(site, media, b),
                    "cta" => Cta(site, b),
                    "data" => DataPart(site, media, b, first),
                    _ => string.Empty
                };
                if (html.Length > 0 && (b.Type == "hero" || b.Type == "data"))
                {
                    first = false;
                }
                sb.Append(html);
            }
            return sb.ToString();
        }
    }
}
