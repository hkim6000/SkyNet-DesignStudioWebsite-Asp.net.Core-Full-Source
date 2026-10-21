using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class AdminSettings : WebPage
    {
        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle("Settings | " + site.Brand + " staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminSettingsJs.expired();");
            }
            return c;
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminSettingsJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("se-brand", Ui.Esc(site.Brand));
            response.SetElementContents("se-menu", await Auth.Menu("se", "AdminSettings", c));
            response.SetElementContents("se-who", Auth.Who("se", c));
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
            response.SetElementContents("se-main", PageHtml(site));
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
            Dictionary<string, string> err = new Dictionary<string, string>();
            string brand = Ui.Clip(GetDataValue("brand"), 60);
            string tagline = Ui.Clip(GetDataValue("tagline"), 100);
            string phone = Ui.Clip(GetDataValue("phone"), 30);
            string email = Ui.Clip(GetDataValue("email"), 120);
            string address = Ui.Clip(GetDataValue("address"), 200);
            string hours = Ui.Clip(GetDataValue("hours"), 200);
            int video = Ui.Int(GetDataValue("video"), 0, 1000, 0);
            List<string> budgets = Ui.Split(GetDataValue("budgets"), 8, 40);
            List<string> timelines = Ui.Split(GetDataValue("timelines"), 8, 40);
            if (brand.Length < 2)
            {
                err["brand"] = "Please enter the studio name.";
            }
            if (phone.Length > 0 && !Ui.IsPhone(phone))
            {
                err["phone"] = "Please enter a valid phone number, or leave it empty.";
            }
            if (email.Length > 0 && !Ui.IsEmail(email))
            {
                err["email"] = "Please enter a valid email address, or leave it empty.";
            }
            if (video < 1 || video > Media.CeilingMb)
            {
                err["video"] = "Enter a whole number from 1 to " + Media.CeilingMb + ".";
            }
            if (budgets.Count == 0)
            {
                err["budgets"] = "Add at least one budget range, one per line.";
            }
            if (timelines.Count == 0)
            {
                err["timelines"] = "Add at least one start time, one per line.";
            }
            foreach (string f in new[] { "brand", "tagline", "phone", "email", "address", "hours", "video", "budgets", "timelines" })
            {
                response.SetElementContents("se-e-" + f, err.TryGetValue(f, out string? m) ? m : string.Empty);
            }
            if (err.Count > 0)
            {
                response.ExecuteScript("AdminSettingsJs.failed();");
                return response;
            }
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                site.Brand = brand;
                site.Tagline = tagline;
                site.Phone = phone;
                site.Email = email;
                site.Address = address;
                site.Hours = hours;
                site.MaxVideoMb = video;
                site.Budgets = budgets;
                site.Timelines = timelines;
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "save settings", brand, true);
            response.SetElementContents("se-brand", Ui.Esc(brand));
            response.ExecuteScript("AdminSettingsJs.saved();");
            return response;
        }

        public async Task<ApiResponse> Initialize()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            if (!Store.Demo())
            {
                return await Refuse(response, c, "initialize data", "settings", "The data is initialized once, on the first start in Production. Edit records in their own pages instead.");
            }
            if ((GetDataValue("word") ?? string.Empty).Trim() != "INITIALIZE")
            {
                response.ExecuteScript("AdminSettingsJs.toast('Type INITIALIZE to confirm.');");
                return response;
            }
            await Store.Log(c.U.Id, "initialize data", "requested from Settings", true);
            int removed = await Init.Run("Settings, by " + c.U.Name, false);
            response.ExecuteScript("AdminSettingsJs.initialized(" + removed + ");");
            return response;
        }

        private static string Field(string key, string label, string value, int max, string hint)
        {
            return "<label class=\"se-field\" for=\"se-f-" + key + "\"><span>" + Ui.Esc(label) + "</span><input type=\"text\" id=\"se-f-" + key + "\" data-k=\"" + key + "\" maxlength=\"" + max + "\" value=\"" + Ui.Esc(value) + "\">" + (hint.Length > 0 ? "<small>" + Ui.Esc(hint) + "</small>" : string.Empty) + "<em class=\"se-err\" id=\"se-e-" + key + "\"></em></label>";
        }

        private static string Area(string key, string label, string value, int max, string hint)
        {
            return "<label class=\"se-field\" for=\"se-f-" + key + "\"><span>" + Ui.Esc(label) + "</span><textarea id=\"se-f-" + key + "\" class=\"se-textarea\" data-k=\"" + key + "\" maxlength=\"" + max + "\">" + Ui.Esc(value) + "</textarea>" + (hint.Length > 0 ? "<small>" + Ui.Esc(hint) + "</small>" : string.Empty) + "<em class=\"se-err\" id=\"se-e-" + key + "\"></em></label>";
        }

        private static string PageHtml(SiteData site)
        {
            string html = "<div class=\"se-ph\"><div><h1>Settings</h1><p>Studio details shown in the footer and on the Contact page, and the choices in the inquiry form.</p></div></div>"
                + "<form class=\"se-stack\" id=\"se-form\" onsubmit=\"return AdminSettingsJs.save()\" novalidate><section class=\"se-panel\"><h2>Studio</h2><div class=\"se-grid2\">"
                + Field("brand", "Studio name", site.Brand, 60, "Used in the header, the footer and as {name} in page text.")
                + Field("tagline", "Tagline", site.Tagline, 100, string.Empty)
                + Field("phone", "Phone", site.Phone, 30, string.Empty)
                + Field("email", "Email", site.Email, 120, string.Empty)
                + Area("address", "Address", site.Address, 200, string.Empty)
                + Area("hours", "Opening hours", site.Hours, 200, string.Empty)
                + "</div></section><section class=\"se-panel\"><h2>Inquiry form</h2><div class=\"se-grid2\">"
                + Area("budgets", "Budget ranges", Ui.Join(site.Budgets), 400, "One per line. These are the choices in the Budget field.")
                + Area("timelines", "Start times", Ui.Join(site.Timelines), 400, "One per line. These are the choices in the start-time field.")
                + "</div></section><section class=\"se-panel\"><h2>Media library</h2><div class=\"se-grid2\">"
                + Field("video", "Largest video (MB)", site.MaxVideoMb.ToString(), 3, "Photos can be up to " + Media.PhotoMb + " MB. Videos are MP4 files up to this size, at most " + Media.CeilingMb + " MB.")
                + "</div></section><div class=\"se-acts\"><button type=\"submit\" class=\"se-btn\" id=\"se-save\">Save settings</button></div></form>"
                + "<section class=\"se-panel se-note\"><h2>Run mode</h2><p class=\"se-hint\">Run mode is set in appConfig/application.cfg.</p></section>";
            if (Store.Demo())
            {
                html += "<section class=\"se-panel se-danger\"><h2>Initialize data</h2><p class=\"se-hint\">Removes every message, the activity log and any staff account made so far. Projects, services, team, journal, pages and the media library stay. The next sign-in page shows a new first password.</p><div class=\"se-acts\"><button type=\"button\" class=\"se-btn se-btn-danger\" onclick=\"AdminSettingsJs.initialize()\">Initialize data</button></div></section>";
            }
            return html;
        }
    }
}
