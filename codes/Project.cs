using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class Project : WebPage
    {
        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            List<MediaItem> media = await Media.Load();
            string id = Ui.Clip(QueryValue("id"), 60);
            List<Work> live = site.Projects.Where(w => w.Published).ToList();
            Work? work = live.FirstOrDefault(w => w.Id == id);
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            if (work == null)
            {
                HtmlDoc.SetTitle("Project not found | " + site.Brand);
                HtmlDoc.AddMetaElement("robots", "noindex");
                HtmlDoc.HtmlBodyText = Fill(HtmlDoc.HtmlBodyText, site).Replace("{plhd_project}", "<section class=\"pd-wrap pd-sec pd-nf\"><h1>We could not find that project</h1><p>It may have been renamed or taken offline. You can browse every project from the list.</p><a class=\"pd-btn\" href=\"Projects\">See all projects</a></section>");
                return;
            }
            HtmlDoc.SetTitle(work.Title + " | " + site.Brand);
            HtmlDoc.AddMetaElement("description", work.Summary);
            HtmlDoc.HtmlBodyText = Fill(HtmlDoc.HtmlBodyText, site).Replace("{plhd_project}", ProjectHtml(live, media, work));
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

        private static string Alt(List<MediaItem> media, string path, string fallback)
        {
            MediaItem? m = media.FirstOrDefault(x => x.Path == path);
            return m != null && m.Alt.Length > 0 ? m.Alt : fallback;
        }

        private static string Facts(Work w)
        {
            StringBuilder sb = new StringBuilder("<aside class=\"pd-facts\"><h2>Project facts</h2><dl>");
            sb.Append("<dt>Type</dt><dd>" + Ui.Esc(Catalog.Kind(w.Kind)) + "</dd>");
            if (w.Location.Length > 0)
            {
                sb.Append("<dt>Location</dt><dd>" + Ui.Esc(w.Location) + "</dd>");
            }
            if (w.Year > 0)
            {
                sb.Append("<dt>Year</dt><dd>" + w.Year + "</dd>");
            }
            if (w.Size > 0)
            {
                sb.Append("<dt>Size</dt><dd>" + w.Size.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " m&sup2;</dd>");
            }
            if (w.Scope.Count > 0)
            {
                sb.Append("<dt>Scope</dt><dd><ul class=\"pd-scope\">" + string.Concat(w.Scope.Select(s => "<li>" + Ui.Esc(s) + "</li>")) + "</ul></dd>");
            }
            sb.Append("</dl><a class=\"pd-btn\" href=\"Contact?p=" + Ui.Esc(w.Id) + "\">Start a similar project</a></aside>");
            return sb.ToString();
        }

        private static string Lens(string full, string thumb, string alt, string cap, string cls)
        {
            return "<button type=\"button\" class=\"pd-lbitem " + cls + "\" data-full=\"" + Ui.Esc(full) + "\" data-cap=\"" + Ui.Esc(cap) + "\" data-alt=\"" + Ui.Esc(alt) + "\" onclick=\"ProjectJs.open(this)\" aria-label=\"Open picture: " + Ui.Esc(cap.Length > 0 ? cap : alt) + "\"><img src=\"" + Ui.Esc(thumb) + "\" alt=\"" + Ui.Esc(alt) + "\" loading=\"lazy\">" + (cap.Length > 0 ? "<span>" + Ui.Esc(cap) + "</span>" : string.Empty) + "</button>";
        }

        private static string Compare(List<MediaItem> media, Duo d, int n)
        {
            string altB = Alt(media, d.Before, "Before the work");
            string altA = Alt(media, d.After, "After the work");
            return "<figure class=\"pd-ba\"><div class=\"pd-ba-stage\"><img class=\"pd-ba-img\" src=\"" + Ui.Esc(d.After) + "\" alt=\"" + Ui.Esc(altA) + "\" loading=\"lazy\"><img class=\"pd-ba-img pd-ba-before\" src=\"" + Ui.Esc(d.Before) + "\" alt=\"" + Ui.Esc(altB) + "\" loading=\"lazy\" style=\"clip-path: inset(0 50% 0 0)\"><input type=\"range\" min=\"0\" max=\"100\" value=\"50\" class=\"pd-ba-range\" aria-label=\"Compare before and after, pair " + n + "\" oninput=\"ProjectJs.compare(this)\"><span class=\"pd-ba-line\" style=\"left: 50%\"></span><span class=\"pd-ba-knob\" style=\"left: 50%\" aria-hidden=\"true\"><svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M8 7l-5 5 5 5M16 7l5 5-5 5\"/></svg></span><i class=\"pd-ba-tag pd-ba-l\">Before</i><i class=\"pd-ba-tag pd-ba-r\">After</i></div>" + (d.Caption.Length > 0 ? "<figcaption>" + Ui.Esc(d.Caption) + "</figcaption>" : string.Empty) + "</figure>";
        }

        private static string Card(Work w)
        {
            return "<a class=\"pd-pc\" href=\"Project?id=" + Ui.Esc(w.Id) + "\"><span class=\"pd-pc-img\">" + (w.Cover.Length > 0 ? "<img src=\"" + Ui.Esc(Media.Thumb(w.Cover)) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<i class=\"pd-pc-tag\">" + Ui.Esc(Catalog.Kind(w.Kind)) + "</i></span><span class=\"pd-pc-t\"><b>" + Ui.Esc(w.Title) + "</b><small>" + Ui.Esc(w.Location) + " &middot; " + w.Year + "</small></span></a>";
        }

        private static string ProjectHtml(List<Work> live, List<MediaItem> media, Work w)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<section class=\"pd-wrap pd-top\"><nav class=\"pd-crumbs\" aria-label=\"Breadcrumb\"><a href=\"Projects\">Projects</a><span aria-hidden=\"true\">/</span><span>" + Ui.Esc(w.Title) + "</span></nav>");
            sb.Append("<div class=\"pd-hd\"><div class=\"pd-eyebrow\">" + Ui.Esc(Catalog.Kind(w.Kind)) + "</div><h1 class=\"pd-h1\">" + Ui.Esc(w.Title) + "</h1><p class=\"pd-lead\">" + Ui.Esc(w.Summary) + "</p></div>");
            sb.Append("<div class=\"pd-grid\"><div class=\"pd-cover\">" + (w.Cover.Length > 0 ? Lens(w.Cover, w.Cover, Alt(media, w.Cover, w.Title), w.Title, "pd-lbcover") : string.Empty) + "</div>" + Facts(w) + "</div></section>");
            if (w.Story.Length > 0)
            {
                sb.Append("<section class=\"pd-wrap pd-sec\"><div class=\"pd-story\"><h2>About the project</h2>" + Ui.Paras(w.Story) + "</div></section>");
            }
            if (w.Pairs.Count > 0)
            {
                sb.Append("<section class=\"pd-wrap pd-sec\"><h2 class=\"pd-h2\">Before and after</h2><p class=\"pd-hint\">Drag the handle, or use the arrow keys, to compare the room before and after the work.</p><div class=\"pd-bas\">");
                int n = 1;
                foreach (Duo d in w.Pairs)
                {
                    sb.Append(Compare(media, d, n++));
                }
                sb.Append("</div></section>");
            }
            if (w.Gallery.Count > 0)
            {
                sb.Append("<section class=\"pd-wrap pd-sec\"><h2 class=\"pd-h2\">Gallery</h2><div class=\"pd-gal\">");
                foreach (Pic g in w.Gallery)
                {
                    sb.Append(Lens(g.Media, Media.Thumb(g.Media), Alt(media, g.Media, w.Title), g.Caption, "pd-gi"));
                }
                sb.Append("</div></section>");
            }
            List<Work> rel = live.Where(x => x.Id != w.Id).OrderBy(x => x.Kind == w.Kind ? 0 : 1).Take(3).ToList();
            if (rel.Count > 0)
            {
                sb.Append("<section class=\"pd-wrap pd-sec\"><div class=\"pd-sech\"><h2 class=\"pd-h2\">More projects</h2><a class=\"pd-more\" href=\"Projects\">See all projects</a></div><div class=\"pd-rel\">" + string.Concat(rel.Select(Card)) + "</div></section>");
            }
            sb.Append("<section class=\"pd-wrap pd-sec\"><div class=\"pd-cta\"><div><div class=\"pd-eyebrow\">Start a project</div><h2 class=\"pd-h2\">Have something like this in mind?</h2><p>Tell us about your place and what you would like to change. The first conversation is free.</p></div><a class=\"pd-btn\" href=\"Contact?p=" + Ui.Esc(w.Id) + "\">Send an inquiry</a></div></section>");
            sb.Append("<div class=\"pd-lb\" id=\"pd-lb\" hidden role=\"dialog\" aria-modal=\"true\" aria-label=\"Picture viewer\"><button type=\"button\" class=\"pd-lb-x\" aria-label=\"Close viewer\" onclick=\"ProjectJs.close()\"><svg viewBox=\"0 0 24 24\" width=\"24\" height=\"24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M6 6l12 12M18 6L6 18\"/></svg></button><button type=\"button\" class=\"pd-lb-a pd-lb-p\" aria-label=\"Previous picture\" onclick=\"ProjectJs.step(-1)\"><svg viewBox=\"0 0 24 24\" width=\"26\" height=\"26\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M15 6l-6 6 6 6\"/></svg></button><figure class=\"pd-lb-f\"><img id=\"pd-lb-img\" src=\"\" alt=\"\"><figcaption id=\"pd-lb-cap\"></figcaption></figure><button type=\"button\" class=\"pd-lb-a pd-lb-n\" aria-label=\"Next picture\" onclick=\"ProjectJs.step(1)\"><svg viewBox=\"0 0 24 24\" width=\"26\" height=\"26\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M9 6l6 6-6 6\"/></svg></button><div class=\"pd-lb-c\" id=\"pd-lb-c\"></div></div>");
            return sb.ToString();
        }
    }
}
