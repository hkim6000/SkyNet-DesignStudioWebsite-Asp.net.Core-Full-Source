using System.Globalization;
using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class AdminPages : WebPage
    {
        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle("Pages | " + site.Brand + " staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminPagesJs.expired();");
            }
            return c;
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminPagesJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("pg-brand", Ui.Esc(site.Brand));
            response.SetElementContents("pg-menu", await Auth.Menu("pg", "AdminPages", c));
            response.SetElementContents("pg-who", Auth.Who("pg", c));
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
            response.SetElementContents("pg-main", MainHtml());
            response.ExecuteScript("AdminPagesJs.loaded();");
            return response;
        }

        private static string Opts(IEnumerable<(string Key, string Name)> list, string cur)
        {
            return string.Concat(list.Select(x => "<option value=\"" + Ui.Esc(x.Key) + "\"" + (x.Key == cur ? " selected" : string.Empty) + ">" + Ui.Esc(x.Name) + "</option>"));
        }

        private static string CE(string f, string value, string ph, string cls, bool multi = false)
        {
            return "<div class=\"" + cls + (multi ? " pg-ml" : string.Empty) + "\" data-f=\"" + f + "\" contenteditable=\"plaintext-only\" data-ph=\"" + Ui.Esc(ph) + "\"" + (multi ? " data-ml=\"1\"" : string.Empty) + ">" + Ui.Esc(value) + "</div>";
        }

        private static string CI(string f, string value, string ph, bool multi)
        {
            return "<div data-i=\"" + f + "\" contenteditable=\"plaintext-only\" data-ph=\"" + Ui.Esc(ph) + "\"" + (multi ? " data-ml=\"1\"" : string.Empty) + ">" + Ui.Esc(value) + "</div>";
        }

        private static string RichEd(string html, string ph)
        {
            return "<div class=\"pg-er\" data-f=\"text\" contenteditable=\"true\" data-ph=\"" + Ui.Esc(ph) + "\">" + html + "</div>";
        }

        private static string MediaEd(List<MediaItem> media, string src, string allow, string ph)
        {
            MediaItem? m = media.FirstOrDefault(x => x.Path == src);
            bool vid = Blocks.IsVideo(src);
            string prev = src == string.Empty ? string.Empty : vid ? "<video src=\"" + Ui.Esc(src) + "#t=0.1\" muted preload=\"metadata\" playsinline></video>" : "<img src=\"" + Ui.Esc(m != null && m.Thumb.Length > 0 ? m.Thumb : src) + "\" alt=\"\">";
            return "<div class=\"pg-em\" data-f=\"media\" data-src=\"" + Ui.Esc(src) + "\" data-allow=\"" + allow + "\" data-ph=\"" + Ui.Esc(ph) + "\"><button type=\"button\" class=\"pg-emb\" onclick=\"AdminPagesJs.media(this)\" aria-label=\"" + Ui.Esc(ph) + "\">" + (prev != string.Empty ? prev + (vid ? "<span class=\"pg-emv\">Video</span>" : string.Empty) + "<span class=\"pg-emc\">Change</span>" : "<span>" + Ui.Esc(ph) + "</span>") + "</button><button type=\"button\" class=\"pg-emx\" onclick=\"AdminPagesJs.noMedia(this)\"" + (prev == string.Empty ? " hidden" : string.Empty) + ">Remove</button></div>";
        }

        private static string BtnEd(string n, string label, string link, string title)
        {
            return "<div class=\"pg-ebtn\"><span>" + title + "</span><input type=\"text\" data-f=\"label" + n + "\" maxlength=\"40\" value=\"" + Ui.Esc(label) + "\" placeholder=\"Button text\"><input type=\"text\" data-f=\"link" + n + "\" maxlength=\"300\" value=\"" + Ui.Esc(link) + "\" placeholder=\"Link: Projects, Contact or https://\"></div>";
        }

        private static string ItemEd(List<MediaItem> media, Block b, PartSpec spec, BlockItem it)
        {
            bool icon = spec.ItemFields.Contains("icon");
            bool link = spec.ItemFields.Contains("link");
            bool pic = spec.ItemFields.Contains("media");
            (string tp, string xp) = b.Type switch
            {
                "hero" => ("Caption, for example Entrance hall", string.Empty),
                "steps" => ("Step", "What happens in this step"),
                "stats" => ("Number, like 14", "What it counts"),
                "people" => ("Name", "Role or title"),
                "boxes" => ("Box title", "One item per line"),
                "faq" => ("Question", "Answer"),
                "cards" => ("Card title", "Short text"),
                _ => ("Title", "Text")
            };
            StringBuilder sb = new StringBuilder("<div class=\"pg-eit\">");
            if (icon)
            {
                sb.Append("<select data-i=\"icon\" aria-label=\"Icon\"><option value=\"\">No icon</option>" + Opts(Blocks.Icons, it.Icon) + "</select>");
            }
            if (pic)
            {
                sb.Append(MediaEd(media, it.Media, "photo", "Add a photo").Replace("data-f=\"media\"", "data-i=\"media\"").Replace("class=\"pg-em\"", "class=\"pg-em pg-emi\""));
            }
            sb.Append(CI("title", it.Title, tp, false));
            if (xp.Length > 0)
            {
                sb.Append(CI("text", it.Text, xp, true));
            }
            if (link)
            {
                sb.Append("<div class=\"pg-elink\"><input type=\"text\" data-i=\"label\" maxlength=\"40\" value=\"" + Ui.Esc(it.Label) + "\" placeholder=\"Link text\"><input type=\"text\" data-i=\"link\" maxlength=\"300\" value=\"" + Ui.Esc(it.Link) + "\" placeholder=\"Link: Projects, Contact or https://\"></div>");
            }
            sb.Append("<div class=\"pg-ictl\"><button type=\"button\" aria-label=\"Move earlier\" onclick=\"AdminPagesJs.itemMove(this,-1)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 15l6-6 6 6\"/></svg></button><button type=\"button\" aria-label=\"Move later\" onclick=\"AdminPagesJs.itemMove(this,1)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 9l6 6 6-6\"/></svg></button><button type=\"button\" aria-label=\"Remove\" onclick=\"AdminPagesJs.itemDrop(this)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3\"/></svg></button></div></div>");
            return sb.ToString();
        }

        private static string Ed(List<MediaItem> lib, Block b, string page)
        {
            PartSpec? spec = Blocks.Spec(b);
            if (spec == null)
            {
                return string.Empty;
            }
            bool data = b.Type == "data";
            string x = data ? "data" : b.Type;
            StringBuilder sb = new StringBuilder("<div class=\"pg-blk pg-x-" + x + (b.Hidden ? " pg-hid" : string.Empty) + "\" data-id=\"" + Ui.Esc(b.Id) + "\" data-type=\"" + b.Type + "\" data-src=\"" + Ui.Esc(b.Source) + "\" data-hidden=\"" + (b.Hidden ? "1" : "0") + "\"><header class=\"pg-bh\">");
            sb.Append("<button type=\"button\" class=\"pg-grip\" aria-label=\"Drag to move\" title=\"Drag to move\" onpointerdown=\"AdminPagesJs.grab(event,this)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"currentColor\" aria-hidden=\"true\"><circle cx=\"9\" cy=\"6\" r=\"1.6\"/><circle cx=\"15\" cy=\"6\" r=\"1.6\"/><circle cx=\"9\" cy=\"12\" r=\"1.6\"/><circle cx=\"15\" cy=\"12\" r=\"1.6\"/><circle cx=\"9\" cy=\"18\" r=\"1.6\"/><circle cx=\"15\" cy=\"18\" r=\"1.6\"/></svg></button>");
            sb.Append("<b>" + Ui.Esc(spec.Name) + "</b>" + (data ? "<span class=\"pg-src\">From your data</span>" : string.Empty) + "<span class=\"pg-bsp\"></span>");
            sb.Append("<button type=\"button\" class=\"pg-hb\" aria-pressed=\"" + (b.Hidden ? "true" : "false") + "\" onclick=\"AdminPagesJs.hide(this)\" title=\"Hide on the website without deleting\"><span>" + (b.Hidden ? "Show" : "Hide") + "</span></button><button type=\"button\" class=\"pg-hb\" aria-label=\"Move up\" title=\"Move up\" onclick=\"AdminPagesJs.move(this,-1)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 15l6-6 6 6\"/></svg></button><button type=\"button\" class=\"pg-hb\" aria-label=\"Move down\" title=\"Move down\" onclick=\"AdminPagesJs.move(this,1)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 9l6 6 6-6\"/></svg></button>" + (data ? string.Empty : "<button type=\"button\" class=\"pg-hb pg-del\" aria-label=\"Remove block\" title=\"Remove block\" onclick=\"AdminPagesJs.drop(this)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3\"/></svg></button>"));
            sb.Append("</header><div class=\"pg-bb\">");
            if (data)
            {
                sb.Append("<div class=\"pg-dinfo\">" + Ui.Esc(spec.Desc) + "</div>");
            }
            string kick = Blocks.Has(spec, "kicker") ? CE("kicker", b.Kicker, "Small label above the title", "pg-ek") : string.Empty;
            string title = Blocks.Has(spec, "title") ? CE("title", b.Title, "Title", "pg-et") : string.Empty;
            string text = Blocks.Has(spec, "text") ? RichEd(b.Text, "Write here") : string.Empty;
            string media = Blocks.Has(spec, "media") ? MediaEd(lib, b.Media, b.Type == "quote" ? "photo" : string.Empty, b.Type == "quote" ? "Add a photo" : "Add a picture or video") : string.Empty;
            string btns = Blocks.Has(spec, "buttons") ? "<div class=\"pg-ebtns\">" + BtnEd(string.Empty, b.Label, b.Link, "Main button") + BtnEd("2", b.Label2, b.Link2, "Second button") + "</div>" : Blocks.Has(spec, "button") ? "<div class=\"pg-ebtns\">" + BtnEd(string.Empty, b.Label, b.Link, "Button") + "</div>" : string.Empty;
            string extra = string.Empty;
            if (Blocks.Has(spec, "flip"))
            {
                extra = "<label class=\"pg-chk\"><input type=\"checkbox\" data-f=\"flip\"" + (b.Flip ? " checked" : string.Empty) + " onchange=\"this.closest('.pg-bb').querySelector('.pg-two').classList.toggle('pg-flip',this.checked)\"> Picture on the right</label>";
            }
            switch (b.Type)
            {
                case "hero":
                    sb.Append("<div class=\"pg-two\"><div class=\"pg-col\">" + kick + title + text + btns + "</div>" + media + "</div>");
                    break;
                case "split":
                    sb.Append("<div class=\"pg-two" + (b.Flip ? " pg-flip" : string.Empty) + "\">" + media + "<div class=\"pg-col\">" + kick + title + text + btns + "</div></div>" + extra);
                    break;
                case "quote":
                    sb.Append(media + CE("text", b.Text, "The quote", "pg-eq", true) + CE("title", b.Title, "Who said it, for example A happy client", "pg-et"));
                    break;
                case "media":
                    sb.Append(media + CE("text", b.Text, "Caption (optional)", "pg-hint2"));
                    break;
                default:
                    sb.Append(kick + title + text + btns);
                    break;
            }
            if (Blocks.Has(spec, "items") && (b.Type != "hero" || page == "home"))
            {
                bool slides = b.Type == "hero";
                sb.Append(slides ? "<div class=\"pg-slides\"><b>Slides</b><p class=\"pg-hint2\">The picture above is the first slide. Add more pictures from the Media library and they change every 7 seconds. The caption shows in the corner of each picture.</p><div class=\"pg-eitems pg-sitems\" data-f=\"items\">" : "<div><div class=\"pg-eitems\" data-f=\"items\">");
                foreach (BlockItem it in b.Items)
                {
                    sb.Append(ItemEd(lib, b, spec, it));
                }
                sb.Append("</div><button type=\"button\" class=\"pg-eadd\" onclick=\"AdminPagesJs.addItem(this)\">+ Add " + (b.Type switch { "steps" => "a step", "faq" => "a question", "boxes" => "a box", "cards" => "a card", "stats" => "a number", "people" => "a person", "hero" => "a slide", _ => "a line" }) + "</button><template>" + ItemEd(lib, b, spec, new BlockItem()) + "</template></div>");
            }
            sb.Append("</div></div>");
            return sb.ToString();
        }

        private static string Tabs(string cur)
        {
            return string.Concat(Blocks.Pages.Select(p => "<button type=\"button\" class=\"pg-chip" + (p.Key == cur ? " pg-act" : string.Empty) + "\" aria-pressed=\"" + (p.Key == cur ? "true" : "false") + "\" onclick=\"AdminPagesJs.tab('" + p.Key + "')\">" + Ui.Esc(p.Name) + "</button>"));
        }

        private static string MainHtml()
        {
            return "<div class=\"pg-ph\"><div><h1>Pages</h1><p>" + "Click any text to change it. Drag a block by its handle, or use the arrows, to move it. Use {name} to show the studio name from Settings. Pictures and videos are chosen from the Media library. Changes go live when you save." + "</p></div></div><div class=\"pg-ptools\"><div class=\"pg-ptabs\" id=\"pg-ptabs\">" + Tabs("home") + "</div><div class=\"pg-pacts\"><span class=\"pg-dirty\" id=\"pg-dirty\"></span><button type=\"button\" class=\"pg-btn pg-btn-sm pg-btn-ghost\" onclick=\"AdminPagesJs.versions()\">Versions</button><button type=\"button\" class=\"pg-btn pg-btn-sm pg-btn-ghost\" id=\"pg-viewlink\" data-href=\"Home\" onclick=\"AdminPagesJs.view()\">View page</button>" + "<button type=\"button\" class=\"pg-btn pg-btn-sm\" id=\"pg-savebtn\" onclick=\"AdminPagesJs.save()\">Save page</button>" + "</div></div><div class=\"pg-canvas\" id=\"pg-canvas\"></div><div id=\"pg-holder\" hidden></div><div class=\"pg-rt\" id=\"pg-rt\" role=\"toolbar\" aria-label=\"Text style\"><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminPagesJs.fmt('bold')\" title=\"Bold\"><b>B</b></button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminPagesJs.fmt('italic')\" title=\"Italic\"><i>I</i></button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminPagesJs.fmt('link')\" title=\"Link\">Link</button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminPagesJs.fmt('insertUnorderedList')\" title=\"List\">&bull; List</button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminPagesJs.fmt('clear')\" title=\"Clear formatting\">Clear</button></div>";
        }

        private static string Canvas(SiteData site, List<MediaItem> media, PageSpec p)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Block b in site.Pages[p.Key].Blocks)
            {
                sb.Append(Ed(media, b, p.Key));
            }
            sb.Append("<div class=\"pg-addp\" id=\"pg-addp\"><b>Add a block</b><div class=\"pg-addl\">" + string.Concat(Blocks.Types.Select(t => "<button type=\"button\" onclick=\"AdminPagesJs.addBlock('" + t.Key + "')\"><b>" + Ui.Esc(t.Name) + "</b><small>" + Ui.Esc(t.Desc) + "</small></button>")) + "</div><p class=\"pg-hint2\">Blocks marked From your data (projects, services, team, journal, inquiry form) cannot be removed, but you can move them, change their titles or hide them.</p></div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> Show()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            List<MediaItem> media = await Media.Load();
            PageSpec p = Blocks.PageOf(GetDataValue("page")) ?? Blocks.Pages[0];
            response.SetElementContents("pg-canvas", Canvas(site, media, p));
            response.SetElementContents("pg-ptabs", Tabs(p.Key));
            response.ExecuteScript("AdminPagesJs.shown('" + p.Key + "','" + p.Href + "');");
            return response;
        }

        public async Task<ApiResponse> AddBlock()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            PartSpec? t = Blocks.Types.FirstOrDefault(x => x.Key == GetDataValue("btype"));
            if (t == null)
            {
                return response;
            }
            List<MediaItem> media = await Media.Load();
            Block b = new Block { Id = Blocks.NewId(), Type = t.Key };
            if (t.Key is "cards" or "steps" or "boxes" or "faq" or "stats" or "people")
            {
                b.Items.Add(new BlockItem());
                b.Items.Add(new BlockItem());
                b.Items.Add(new BlockItem());
            }
            response.SetElementContents("pg-holder", Ed(media, b, Blocks.PageOf(GetDataValue("page"))?.Key ?? string.Empty));
            response.ExecuteScript("AdminPagesJs.added();");
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
            PageSpec? p = Blocks.PageOf(GetDataValue("page"));
            if (p == null)
            {
                return response;
            }
            List<MediaItem> media = await Media.Load();
            string err = string.Empty;
            SiteData? done = null;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                (List<Block>? list, string e) = Blocks.Clean(media, p, GetDataValue("blocks"));
                if (list == null)
                {
                    err = e;
                }
                else
                {
                    PageDoc doc = site.Pages[p.Key];
                    doc.Versions.Insert(0, new PageVersion { When = doc.Saved == default ? DateTime.UtcNow.AddMinutes(-1) : doc.Saved, By = doc.By, Blocks = doc.Blocks });
                    if (doc.Versions.Count > 5)
                    {
                        doc.Versions.RemoveRange(5, doc.Versions.Count - 5);
                    }
                    doc.Blocks = list;
                    doc.Saved = DateTime.UtcNow;
                    doc.By = c.U.Id;
                    await Store.SaveSite(site);
                    done = site;
                }
            }
            if (done == null)
            {
                response.ExecuteScript("AdminPagesJs.fail('" + Ui.Js(err) + "');");
                return response;
            }
            await Store.Log(c.U.Id, "save page", p.Name, true);
            response.SetElementContents("pg-canvas", Canvas(done, media, p));
            response.ExecuteScript("AdminPagesJs.shown('" + p.Key + "','" + p.Href + "');AdminPagesJs.saved('" + Ui.Js(p.Name + " saved. It is live on the website.") + "');");
            return response;
        }

        public async Task<ApiResponse> Versions()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            PageSpec? p = Blocks.PageOf(GetDataValue("page"));
            if (p == null)
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            PageDoc doc = site.Pages[p.Key];
            List<Staff> staff = await Store.LoadStaff();
            string Who(string id) => id == c.U.Id ? c.U.Name : staff.FirstOrDefault(s => s.Id == id)?.Name ?? "Admin";
            StringBuilder sb = new StringBuilder("<p class=\"pg-hint\">The last five saved versions are kept. Restoring one makes it live right away.</p><div class=\"pg-vlist\"><div class=\"pg-vrow\"><div><b>Live now</b><small>" + (doc.Saved != default ? "Saved " + Ui.Esc(Ui.When(doc.Saved)) + (doc.By != string.Empty ? " by " + Ui.Esc(Who(doc.By)) : string.Empty) : "Sample content") + " &middot; " + Ui.Count(doc.Blocks.Count, "block", "blocks") + "</small></div></div>");
            if (doc.Versions.Count == 0)
            {
                sb.Append("<p class=\"pg-hint\">No earlier versions yet. They appear here after you save.</p>");
            }
            for (int i = 0; i < doc.Versions.Count; i++)
            {
                PageVersion v = doc.Versions[i];
                string when = Ui.When(v.When);
                sb.Append("<div class=\"pg-vrow\"><div><b>" + Ui.Esc(when) + "</b><small>" + (v.By != string.Empty ? Ui.Esc(Who(v.By)) + " &middot; " : string.Empty) + Ui.Count(v.Blocks.Count, "block", "blocks") + "</small></div>" + "<button type=\"button\" class=\"pg-btn pg-btn-sm pg-btn-ghost\" onclick=\"AdminPagesJs.restore(" + i + ",'" + Ui.Js(when) + "')\">Restore</button>" + "</div>");
            }
            sb.Append("</div><div class=\"pg-acts\"><button type=\"button\" class=\"pg-btn pg-btn-ghost\" onclick=\"AdminPagesJs.closeModal()\">Close</button></div>");
            response.SetElementContents("pg-mbody", sb.ToString());
            response.ExecuteScript("AdminPagesJs.opened('" + Ui.Js(p.Name + ": versions") + "');");
            return response;
        }

        public async Task<ApiResponse> Restore()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            PageSpec? p = Blocks.PageOf(GetDataValue("page"));
            int i = int.TryParse(GetDataValue("vi"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv) ? iv : -1;
            SiteData? done = null;
            if (p != null)
            {
                using (await Store.Hold())
                {
                    SiteData site = await Store.LoadSite();
                    PageDoc doc = site.Pages[p.Key];
                    if (i >= 0 && i < doc.Versions.Count)
                    {
                        PageVersion v = doc.Versions[i];
                        doc.Versions.RemoveAt(i);
                        doc.Versions.Insert(0, new PageVersion { When = doc.Saved == default ? DateTime.UtcNow.AddMinutes(-1) : doc.Saved, By = doc.By, Blocks = doc.Blocks });
                        if (doc.Versions.Count > 5)
                        {
                            doc.Versions.RemoveRange(5, doc.Versions.Count - 5);
                        }
                        doc.Blocks = v.Blocks;
                        doc.Saved = DateTime.UtcNow;
                        doc.By = c.U.Id;
                        Blocks.Ensure(site);
                        await Store.SaveSite(site);
                        done = site;
                    }
                }
            }
            if (p == null || done == null)
            {
                return response;
            }
            await Store.Log(c.U.Id, "restore page", p.Name, true);
            List<MediaItem> media = await Media.Load();
            response.SetElementContents("pg-canvas", Canvas(done, media, p));
            response.ExecuteScript("AdminPagesJs.shown('" + p.Key + "','" + p.Href + "');AdminPagesJs.saved('Version restored. It is live on the website.');");
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
            await Media.PickReply(response, "pg", "AdminPagesJs", GetDataValue("pterm") ?? string.Empty, GetDataValue("pvid") == "1");
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
            await Media.UploadReply(response, "pg", "AdminPagesJs", c.U, GetDataValue("pvid") == "1");
            return response;
        }
    }
}
