using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class Projects : WebPage
    {
        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            List<MediaItem> media = await Media.Load();
            HtmlDoc.SetTitle("Projects | " + site.Brand);
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Houses, interiors and small public buildings by " + site.Brand + ".");
            kindNow = Ui.Pick(QueryValue("t"), Catalog.Kinds.Select(k => k.Key));
            yearNow = Ui.Year(QueryValue("y"));
            termNow = Ui.Clip(QueryValue("s"), 40);
            HtmlDoc.HtmlBodyText = Fill(HtmlDoc.HtmlBodyText, site).Replace("{plhd_blocks}", PageHtml(site, media, "projects"));
        }

        private string kindNow = string.Empty;
        private int yearNow;
        private string termNow = string.Empty;

        public async Task<ApiResponse> Filter()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await Store.LoadSite();
            string kind = Ui.Pick(GetDataValue("kind"), Catalog.Kinds.Select(k => k.Key));
            int yr = Ui.Year(GetDataValue("yr"));
            string term = Ui.Clip(GetDataValue("term"), 40);
            List<Work> rows = Match(site, kind, yr, term);
            response.SetElementContents("pj-results", ResultsHtml(rows));
            response.SetElementContents("pj-count", CountText(rows.Count, site.Projects.Count(w => w.Published)));
            response.ExecuteScript("ProjectsJs.filtered('" + kind + "'," + yr + ",'" + Ui.Js(term) + "');");
            return response;
        }

        private static List<Work> Match(SiteData site, string kind, int yr, string term)
        {
            return site.Projects.Where(w => w.Published && (kind == string.Empty || w.Kind == kind) && (yr == 0 || w.Year == yr) && (term == string.Empty || Ui.Has(w.Title, term) || Ui.Has(w.Location, term) || Ui.Has(w.Summary, term) || w.Scope.Any(s => Ui.Has(s, term)))).ToList();
        }

        private static string CountText(int n, int all)
        {
            return n == all ? Ui.Count(n, "project", "projects") : "Showing " + n + " of " + all;
        }

        private static string ResultsHtml(List<Work> rows)
        {
            if (rows.Count == 0)
            {
                return "<p class=\"pj-none\">No projects match. Try another type or year, or clear the search.</p>";
            }
            return "<div class=\"pj-pcards\">" + string.Concat(rows.Select(ProjectCard)) + "</div>";
        }

        private string DataPart(SiteData site, List<MediaItem> media, Block b, bool first)
        {
            if (b.Source != "projectlist")
            {
                return string.Empty;
            }
            List<Work> all = site.Projects.Where(w => w.Published).ToList();
            StringBuilder sb = new StringBuilder(PartHead(site, b, first));
            sb.Append("<div class=\"pj-filters\"><div class=\"pj-chips\" id=\"pj-chips\" role=\"group\" aria-label=\"Project type\"><button type=\"button\" class=\"pj-chip" + (kindNow == string.Empty ? " pj-act" : string.Empty) + "\" data-kind=\"\" aria-pressed=\"" + (kindNow == string.Empty ? "true" : "false") + "\" onclick=\"ProjectsJs.kind('')\">All</button>");
            foreach ((string key, string name) in Catalog.Kinds.Where(k => all.Any(w => w.Kind == k.Key)))
            {
                sb.Append("<button type=\"button\" class=\"pj-chip" + (kindNow == key ? " pj-act" : string.Empty) + "\" data-kind=\"" + key + "\" aria-pressed=\"" + (kindNow == key ? "true" : "false") + "\" onclick=\"ProjectsJs.kind('" + key + "')\">" + Ui.Esc(name) + "</button>");
            }
            sb.Append("</div><div class=\"pj-controls\"><label class=\"pj-fl\"><span>Year</span><select id=\"pj-yr\" onchange=\"ProjectsJs.change()\"><option value=\"0\">All years</option>");
            foreach (int y in all.Select(w => w.Year).Distinct().OrderByDescending(y => y))
            {
                sb.Append("<option value=\"" + y + "\"" + (yearNow == y ? " selected" : string.Empty) + ">" + y + "</option>");
            }
            sb.Append("</select></label><label class=\"pj-fl pj-fs\"><span>Search</span><input type=\"search\" id=\"pj-term\" maxlength=\"40\" placeholder=\"Name, place or scope\" value=\"" + Ui.Esc(termNow) + "\" oninput=\"ProjectsJs.type()\"></label></div></div>");
            List<Work> rows = Match(site, kindNow, yearNow, termNow);
            sb.Append("<p class=\"pj-count\" id=\"pj-count\" role=\"status\">" + CountText(rows.Count, all.Count) + "</p><div id=\"pj-results\">" + ResultsHtml(rows) + "</div>");
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
            return "<a class=\"pj-pc\" href=\"Project?id=" + Ui.Esc(w.Id) + "\"><span class=\"pj-pc-img\">" + (w.Cover.Length > 0 ? "<img src=\"" + Ui.Esc(Media.Thumb(w.Cover)) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<i class=\"pj-pc-tag\">" + Ui.Esc(Catalog.Kind(w.Kind)) + "</i>" + (w.Pairs.Count > 0 ? "<i class=\"pj-pc-ba\">Before and after</i>" : string.Empty) + "</span><span class=\"pj-pc-t\"><b>" + Ui.Esc(w.Title) + "</b><small>" + Ui.Esc(w.Location) + " &middot; " + w.Year + "</small><p>" + Ui.Esc(w.Summary) + "</p></span></a>";
        }

        private static string EntryCard(Entry e)
        {
            bool press = e.Kind == "press";
            string href = press ? e.Link : "Journal?id=" + e.Id;
            string img = e.Cover.Length > 0 ? "<span class=\"pj-pc-img\"><img src=\"" + Ui.Esc(Media.Thumb(e.Cover)) + "\" alt=\"\" loading=\"lazy\"></span>" : string.Empty;
            string inner = img + "<span class=\"pj-pc-t\"><small>" + (press ? "Press &middot; " + Ui.Esc(e.Source) : Ui.Esc(e.Category)) + " &middot; " + Ui.Esc(Ui.Day(e.Date)) + "</small><b>" + Ui.Esc(e.Title) + "</b><p>" + Ui.Esc(e.Summary) + "</p>" + (press ? "<em class=\"pj-ec-out\">Read the article</em>" : string.Empty) + "</span>";
            string cls = "pj-pc pj-ec" + (img.Length == 0 ? " pj-ec-text" : string.Empty);
            return href.Length == 0 ? "<div class=\"" + cls + "\">" + inner + "</div>" : "<a class=\"" + cls + "\" href=\"" + Ui.Esc(href) + "\"" + (press ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + inner + "</a>";
        }

        private static string PartHead(SiteData site, Block b, bool first)
        {
            string tag = first ? "h1" : "h2";
            string x = Kick(site, b) + (b.Title == string.Empty ? string.Empty : "<" + tag + " class=\"pj-bh2" + (first ? " pj-bh1" : string.Empty) + "\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">") + Rich(site, b.Text, "pj-blead");
            return x == string.Empty ? string.Empty : "<div class=\"pj-bhead\">" + x + "</div>";
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
            return label == string.Empty || link == string.Empty ? string.Empty : "<a class=\"pj-btn" + (main ? string.Empty : " pj-btn-ghost") + "\" href=\"" + Ui.Esc(link) + "\"" + (link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + Ui.Esc(Tok(site, label)) + "</a>";
        }

        private static string Btns(SiteData site, Block b)
        {
            string x = Btn(site, b.Label, b.Link, true) + Btn(site, b.Label2, b.Link2, false);
            return x == string.Empty ? string.Empty : "<div class=\"pj-bbtns\">" + x + "</div>";
        }

        private static string Kick(SiteData site, Block b)
        {
            return b.Kicker == string.Empty ? string.Empty : "<div class=\"pj-eyebrow\">" + Ui.Esc(Tok(site, b.Kicker)) + "</div>";
        }

        private static string H2(SiteData site, Block b, string tag = "h2")
        {
            return b.Title == string.Empty ? string.Empty : "<" + tag + " class=\"pj-bh2\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">";
        }

        private static string Rich(SiteData site, string html, string cls)
        {
            return html == string.Empty ? string.Empty : "<div class=\"pj-rich" + (cls == string.Empty ? string.Empty : " " + cls) + "\">" + TokH(site, html) + "</div>";
        }

        private static string Head(SiteData site, Block b, bool lead, string tag = "h2")
        {
            string x = Kick(site, b) + H2(site, b, tag) + (lead ? Rich(site, b.Text, "pj-blead") : string.Empty);
            return x == string.Empty ? string.Empty : "<div class=\"pj-bhead\">" + x + "</div>";
        }

        private static string Sec(Block b, string inner)
        {
            return "<section class=\"pj-wrap pj-sec\" id=\"" + Ui.Esc(b.Id) + "\">" + inner + "</section>";
        }

        private static string Hero(SiteData site, List<MediaItem> media, Block b, bool h1)
        {
            string tag = h1 ? "h1" : "h2";
            bool vid = Blocks.IsVideo(b.Media);
            string mv = Mv(media, b.Media, Tok(site, b.Title), vid, h1);
            return "<section class=\"pj-bhero" + (mv == string.Empty ? " pj-bplain" : string.Empty) + "\" id=\"" + Ui.Esc(b.Id) + "\">" + (mv != string.Empty ? "<div class=\"pj-bhero-p\">" + mv + "</div>" : string.Empty) + "<div class=\"pj-wrap pj-bhero-in\"><div class=\"pj-bhero-t\">" + Kick(site, b) + "<" + tag + " class=\"pj-btitle\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">" + Rich(site, b.Text, "pj-blead") + Btns(site, b) + "</div></div></section>";
        }

        private static string Split(SiteData site, List<MediaItem> media, Block b)
        {
            string btn = Btn(site, b.Label, b.Link, true);
            return Sec(b, "<div class=\"pj-bsplit" + (b.Flip ? " pj-bflip" : string.Empty) + (b.Media == string.Empty ? " pj-bnomedia" : string.Empty) + "\">" + (b.Media != string.Empty ? "<div class=\"pj-bsm\">" + Mv(media, b.Media, Tok(site, b.Title), false, false) + "</div>" : string.Empty) + "<div class=\"pj-bst\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + (btn != string.Empty ? "<div class=\"pj-bbtns\">" + btn + "</div>" : string.Empty) + "</div></div>");
        }

        private static string Cards(SiteData site, Block b)
        {
            int n = Math.Clamp(b.Items.Count, 1, 4);
            if (b.Items.Count == 6)
            {
                n = 3;
            }
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"pj-bcards pj-n" + n + "\">");
            foreach (BlockItem it in b.Items)
            {
                string ico = Icon(it.Icon);
                string inner = (ico != string.Empty ? "<span class=\"pj-wico\">" + ico + "</span>" : string.Empty) + "<b>" + Ui.Esc(Tok(site, it.Title)) + "</b>" + (it.Text != string.Empty ? "<p>" + Lines(Tok(site, it.Text)) + "</p>" : string.Empty);
                if (it.Link != string.Empty)
                {
                    sb.Append("<a class=\"pj-bcard\" href=\"" + Ui.Esc(it.Link) + "\">" + inner + "<span class=\"pj-bmore\">" + Ui.Esc(it.Label != string.Empty ? it.Label : "Learn more") + " &rarr;</span></a>");
                }
                else
                {
                    sb.Append("<div class=\"pj-bcard\">" + inner + "</div>");
                }
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Steps(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<ol class=\"pj-bsteps pj-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
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
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"pj-bstats pj-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
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
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"pj-bpeople pj-n" + Math.Clamp(b.Items.Count, 1, 4) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"pj-bperson\">" + (it.Media != string.Empty && !Blocks.IsVideo(it.Media) ? Mv(media, it.Media, it.Title, false, false) : "<span class=\"pj-bnoface\">" + Ui.Esc(Initials(it.Title)) + "</span>") + "<b>" + Ui.Esc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Boxes(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"pj-bboxes pj-n" + Math.Clamp(b.Items.Count, 1, 3) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"pj-bbox\">" + (it.Title != string.Empty ? "<h3>" + Ui.Esc(Tok(site, it.Title)) + "</h3>" : string.Empty) + "<ul class=\"pj-bticks\">");
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
            StringBuilder sb = new StringBuilder("<div class=\"pj-bfaq\">" + Head(site, b, false) + "<div class=\"pj-bfaqs\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<details class=\"pj-bq\"><summary>" + Ui.Esc(Tok(site, it.Title)) + "</summary><p>" + Lines(Tok(site, it.Text)) + "</p></details>");
            }
            sb.Append("</div></div>");
            return Sec(b, sb.ToString());
        }

        private static string Quote(SiteData site, List<MediaItem> media, Block b)
        {
            return Sec(b, "<figure class=\"pj-bquote\">" + (b.Media != string.Empty && !Blocks.IsVideo(b.Media) ? Mv(media, b.Media, b.Title, false, false) : string.Empty) + "<blockquote>" + Lines(Tok(site, b.Text)) + "</blockquote>" + (b.Title != string.Empty ? "<figcaption>" + Ui.Esc(Tok(site, b.Title)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Wide(SiteData site, List<MediaItem> media, Block b)
        {
            return b.Media == string.Empty ? string.Empty : Sec(b, "<figure class=\"pj-bmedia\">" + Mv(media, b.Media, b.Text, false, false) + (b.Text != string.Empty ? "<figcaption>" + Ui.Esc(Tok(site, b.Text)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Cta(SiteData site, Block b)
        {
            return Sec(b, "<div class=\"pj-bcta\"><div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "pj-blead") + "</div>" + Btns(site, b) + "</div>");
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
                    "text" => Sec(b, "<div class=\"pj-btext\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + "</div>"),
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
