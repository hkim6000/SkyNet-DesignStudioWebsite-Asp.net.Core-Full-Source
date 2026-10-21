using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class Services : WebPage
    {
        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            List<MediaItem> media = await Media.Load();
            HtmlDoc.SetTitle("Services | " + site.Brand);
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Architecture, interior design, renovation and feasibility studies from " + site.Brand + ".");
            HtmlDoc.HtmlBodyText = Fill(HtmlDoc.HtmlBodyText, site).Replace("{plhd_blocks}", PageHtml(site, media, "services"));
        }

        private static string DataPart(SiteData site, List<MediaItem> media, Block b, bool first)
        {
            if (b.Source != "servicelist")
            {
                return string.Empty;
            }
            List<Offer> list = site.Services.Where(s => s.Published).ToList();
            StringBuilder sb = new StringBuilder(PartHead(site, b, first));
            if (list.Count == 0)
            {
                sb.Append("<p class=\"sv-none\">Services will be listed here soon.</p>");
            }
            int i = 0;
            foreach (Offer s in list)
            {
                sb.Append("<article class=\"sv-row" + (i++ % 2 == 1 ? " sv-flip" : string.Empty) + "\" id=\"" + Ui.Esc(s.Id) + "\">");
                if (s.Image.Length > 0)
                {
                    sb.Append("<div class=\"sv-pic\"><img src=\"" + Ui.Esc(Media.Thumb(s.Image)) + "\" alt=\"\" loading=\"lazy\"></div>");
                }
                sb.Append("<div class=\"sv-txt\"><span class=\"sv-ico\">" + Icon(s.Icon) + "</span><h2 class=\"sv-h\">" + Ui.Esc(s.Title) + "</h2><p class=\"sv-sum\">" + Ui.Esc(s.Summary) + "</p>" + Ui.Paras(s.Text));
                if (s.Points.Count > 0)
                {
                    sb.Append("<ul class=\"sv-pts\">" + string.Concat(s.Points.Select(p => "<li>" + Icon("check") + "<span>" + Ui.Esc(p) + "</span></li>")) + "</ul>");
                }
                sb.Append("<div class=\"sv-foot\">" + (s.Duration.Length > 0 ? "<span class=\"sv-dur\">Typical duration <b>" + Ui.Esc(s.Duration) + "</b></span>" : string.Empty) + "<a class=\"sv-btn\" href=\"Contact?s=" + Ui.Esc(s.Id) + "\">Ask about this service</a></div></div></article>");
            }
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
            return "<a class=\"sv-pc\" href=\"Project?id=" + Ui.Esc(w.Id) + "\"><span class=\"sv-pc-img\">" + (w.Cover.Length > 0 ? "<img src=\"" + Ui.Esc(Media.Thumb(w.Cover)) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<i class=\"sv-pc-tag\">" + Ui.Esc(Catalog.Kind(w.Kind)) + "</i>" + (w.Pairs.Count > 0 ? "<i class=\"sv-pc-ba\">Before and after</i>" : string.Empty) + "</span><span class=\"sv-pc-t\"><b>" + Ui.Esc(w.Title) + "</b><small>" + Ui.Esc(w.Location) + " &middot; " + w.Year + "</small><p>" + Ui.Esc(w.Summary) + "</p></span></a>";
        }

        private static string EntryCard(Entry e)
        {
            bool press = e.Kind == "press";
            string href = press ? e.Link : "Journal?id=" + e.Id;
            string img = e.Cover.Length > 0 ? "<span class=\"sv-pc-img\"><img src=\"" + Ui.Esc(Media.Thumb(e.Cover)) + "\" alt=\"\" loading=\"lazy\"></span>" : string.Empty;
            string inner = img + "<span class=\"sv-pc-t\"><small>" + (press ? "Press &middot; " + Ui.Esc(e.Source) : Ui.Esc(e.Category)) + " &middot; " + Ui.Esc(Ui.Day(e.Date)) + "</small><b>" + Ui.Esc(e.Title) + "</b><p>" + Ui.Esc(e.Summary) + "</p>" + (press ? "<em class=\"sv-ec-out\">Read the article</em>" : string.Empty) + "</span>";
            string cls = "sv-pc sv-ec" + (img.Length == 0 ? " sv-ec-text" : string.Empty);
            return href.Length == 0 ? "<div class=\"" + cls + "\">" + inner + "</div>" : "<a class=\"" + cls + "\" href=\"" + Ui.Esc(href) + "\"" + (press ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + inner + "</a>";
        }

        private static string PartHead(SiteData site, Block b, bool first)
        {
            string tag = first ? "h1" : "h2";
            string x = Kick(site, b) + (b.Title == string.Empty ? string.Empty : "<" + tag + " class=\"sv-bh2" + (first ? " sv-bh1" : string.Empty) + "\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">") + Rich(site, b.Text, "sv-blead");
            return x == string.Empty ? string.Empty : "<div class=\"sv-bhead\">" + x + "</div>";
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
            return label == string.Empty || link == string.Empty ? string.Empty : "<a class=\"sv-btn" + (main ? string.Empty : " sv-btn-ghost") + "\" href=\"" + Ui.Esc(link) + "\"" + (link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + Ui.Esc(Tok(site, label)) + "</a>";
        }

        private static string Btns(SiteData site, Block b)
        {
            string x = Btn(site, b.Label, b.Link, true) + Btn(site, b.Label2, b.Link2, false);
            return x == string.Empty ? string.Empty : "<div class=\"sv-bbtns\">" + x + "</div>";
        }

        private static string Kick(SiteData site, Block b)
        {
            return b.Kicker == string.Empty ? string.Empty : "<div class=\"sv-eyebrow\">" + Ui.Esc(Tok(site, b.Kicker)) + "</div>";
        }

        private static string H2(SiteData site, Block b, string tag = "h2")
        {
            return b.Title == string.Empty ? string.Empty : "<" + tag + " class=\"sv-bh2\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">";
        }

        private static string Rich(SiteData site, string html, string cls)
        {
            return html == string.Empty ? string.Empty : "<div class=\"sv-rich" + (cls == string.Empty ? string.Empty : " " + cls) + "\">" + TokH(site, html) + "</div>";
        }

        private static string Head(SiteData site, Block b, bool lead, string tag = "h2")
        {
            string x = Kick(site, b) + H2(site, b, tag) + (lead ? Rich(site, b.Text, "sv-blead") : string.Empty);
            return x == string.Empty ? string.Empty : "<div class=\"sv-bhead\">" + x + "</div>";
        }

        private static string Sec(Block b, string inner)
        {
            return "<section class=\"sv-wrap sv-sec\" id=\"" + Ui.Esc(b.Id) + "\">" + inner + "</section>";
        }

        private static string Hero(SiteData site, List<MediaItem> media, Block b, bool h1)
        {
            string tag = h1 ? "h1" : "h2";
            bool vid = Blocks.IsVideo(b.Media);
            string mv = Mv(media, b.Media, Tok(site, b.Title), vid, h1);
            return "<section class=\"sv-bhero" + (mv == string.Empty ? " sv-bplain" : string.Empty) + "\" id=\"" + Ui.Esc(b.Id) + "\">" + (mv != string.Empty ? "<div class=\"sv-bhero-p\">" + mv + "</div>" : string.Empty) + "<div class=\"sv-wrap sv-bhero-in\"><div class=\"sv-bhero-t\">" + Kick(site, b) + "<" + tag + " class=\"sv-btitle\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">" + Rich(site, b.Text, "sv-blead") + Btns(site, b) + "</div></div></section>";
        }

        private static string Split(SiteData site, List<MediaItem> media, Block b)
        {
            string btn = Btn(site, b.Label, b.Link, true);
            return Sec(b, "<div class=\"sv-bsplit" + (b.Flip ? " sv-bflip" : string.Empty) + (b.Media == string.Empty ? " sv-bnomedia" : string.Empty) + "\">" + (b.Media != string.Empty ? "<div class=\"sv-bsm\">" + Mv(media, b.Media, Tok(site, b.Title), false, false) + "</div>" : string.Empty) + "<div class=\"sv-bst\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + (btn != string.Empty ? "<div class=\"sv-bbtns\">" + btn + "</div>" : string.Empty) + "</div></div>");
        }

        private static string Cards(SiteData site, Block b)
        {
            int n = Math.Clamp(b.Items.Count, 1, 4);
            if (b.Items.Count == 6)
            {
                n = 3;
            }
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"sv-bcards sv-n" + n + "\">");
            foreach (BlockItem it in b.Items)
            {
                string ico = Icon(it.Icon);
                string inner = (ico != string.Empty ? "<span class=\"sv-wico\">" + ico + "</span>" : string.Empty) + "<b>" + Ui.Esc(Tok(site, it.Title)) + "</b>" + (it.Text != string.Empty ? "<p>" + Lines(Tok(site, it.Text)) + "</p>" : string.Empty);
                if (it.Link != string.Empty)
                {
                    sb.Append("<a class=\"sv-bcard\" href=\"" + Ui.Esc(it.Link) + "\">" + inner + "<span class=\"sv-bmore\">" + Ui.Esc(it.Label != string.Empty ? it.Label : "Learn more") + " &rarr;</span></a>");
                }
                else
                {
                    sb.Append("<div class=\"sv-bcard\">" + inner + "</div>");
                }
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Steps(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<ol class=\"sv-bsteps sv-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
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
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"sv-bstats sv-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
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
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"sv-bpeople sv-n" + Math.Clamp(b.Items.Count, 1, 4) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"sv-bperson\">" + (it.Media != string.Empty && !Blocks.IsVideo(it.Media) ? Mv(media, it.Media, it.Title, false, false) : "<span class=\"sv-bnoface\">" + Ui.Esc(Initials(it.Title)) + "</span>") + "<b>" + Ui.Esc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Boxes(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"sv-bboxes sv-n" + Math.Clamp(b.Items.Count, 1, 3) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"sv-bbox\">" + (it.Title != string.Empty ? "<h3>" + Ui.Esc(Tok(site, it.Title)) + "</h3>" : string.Empty) + "<ul class=\"sv-bticks\">");
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
            StringBuilder sb = new StringBuilder("<div class=\"sv-bfaq\">" + Head(site, b, false) + "<div class=\"sv-bfaqs\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<details class=\"sv-bq\"><summary>" + Ui.Esc(Tok(site, it.Title)) + "</summary><p>" + Lines(Tok(site, it.Text)) + "</p></details>");
            }
            sb.Append("</div></div>");
            return Sec(b, sb.ToString());
        }

        private static string Quote(SiteData site, List<MediaItem> media, Block b)
        {
            return Sec(b, "<figure class=\"sv-bquote\">" + (b.Media != string.Empty && !Blocks.IsVideo(b.Media) ? Mv(media, b.Media, b.Title, false, false) : string.Empty) + "<blockquote>" + Lines(Tok(site, b.Text)) + "</blockquote>" + (b.Title != string.Empty ? "<figcaption>" + Ui.Esc(Tok(site, b.Title)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Wide(SiteData site, List<MediaItem> media, Block b)
        {
            return b.Media == string.Empty ? string.Empty : Sec(b, "<figure class=\"sv-bmedia\">" + Mv(media, b.Media, b.Text, false, false) + (b.Text != string.Empty ? "<figcaption>" + Ui.Esc(Tok(site, b.Text)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Cta(SiteData site, Block b)
        {
            return Sec(b, "<div class=\"sv-bcta\"><div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "sv-blead") + "</div>" + Btns(site, b) + "</div>");
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
                    "text" => Sec(b, "<div class=\"sv-btext\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + "</div>"),
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
