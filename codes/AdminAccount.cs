using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class AdminAccount : WebPage
    {
        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle("My account | " + site.Brand + " staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminAccountJs.expired();");
            }
            return c;
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminAccountJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("ak-brand", Ui.Esc(site.Brand));
            response.SetElementContents("ak-menu", await Auth.Menu("ak", "AdminAccount", c));
            response.SetElementContents("ak-who", Auth.Who("ak", c));
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
            response.SetElementContents("ak-main", PageHtml(c));
            return response;
        }

        public async Task<ApiResponse> SaveName()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            if (c.U.Id == "u-demo")
            {
                return await Refuse(response, c, "edit account", c.U.Login, "The demo account cannot be renamed. Sign in with the admin account to change its details.");
            }
            string name = Ui.Clip(GetDataValue("name"), 60);
            string title = Ui.Clip(GetDataValue("title"), 60);
            response.SetElementContents("ak-e-name", name.Length < 2 ? "Please enter your name." : string.Empty);
            if (name.Length < 2)
            {
                return response;
            }
            using (await Store.Hold())
            {
                List<Staff> staff = await Store.LoadStaff();
                Staff? me = staff.FirstOrDefault(s => s.Id == c.U.Id);
                if (me != null)
                {
                    me.Name = name;
                    me.Title = title;
                    await Store.SaveStaff(staff);
                }
            }
            await Store.Log(c.U.Id, "edit account", name, true);
            response.SetElementContents("ak-who", Auth.Who("ak", new Ctx { U = new Staff { Id = c.U.Id, Name = name, Title = title }, Token = c.Token }));
            response.ExecuteScript("AdminAccountJs.nameSaved();");
            return response;
        }

        public async Task<ApiResponse> Password()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            if (c.U.Id == "u-demo")
            {
                return await Refuse(response, c, "change password", c.U.Login, "The demo account has no password to change.");
            }
            string cur = GetDataValue("cur") ?? string.Empty;
            string next = GetDataValue("next") ?? string.Empty;
            string again = GetDataValue("again") ?? string.Empty;
            Dictionary<string, string> err = new Dictionary<string, string>();
            if (!Access.Verify(cur, c.U.Hash))
            {
                err["cur"] = "The current password is not right.";
            }
            if (next.Length < 8)
            {
                err["next"] = "Use at least 8 characters.";
            }
            else if (next == cur)
            {
                err["next"] = "Choose a password different from the current one.";
            }
            if (next != again)
            {
                err["again"] = "The two new passwords are not the same.";
            }
            foreach (string f in new[] { "cur", "next", "again" })
            {
                response.SetElementContents("ak-e-" + f, err.TryGetValue(f, out string? m) ? m : string.Empty);
            }
            if (err.Count > 0)
            {
                if (err.ContainsKey("cur"))
                {
                    await Store.Log(c.U.Id, "change password", c.U.Login, false);
                }
                return response;
            }
            using (await Store.Hold())
            {
                List<Staff> staff = await Store.LoadStaff();
                Staff? me = staff.FirstOrDefault(s => s.Id == c.U.Id);
                if (me != null)
                {
                    me.Hash = Access.HashPassword(next);
                    await Store.SaveStaff(staff);
                }
            }
            await Auth.Close(s => s.UserId == c.U.Id && s.Token != c.Token);
            await Store.Log(c.U.Id, "change password", c.U.Login, true);
            response.ExecuteScript("AdminAccountJs.passwordSaved();");
            return response;
        }

        private static string PageHtml(Ctx c)
        {
            bool demo = c.U.Id == "u-demo";
            string d = demo ? " disabled" : string.Empty;
            return "<div class=\"ak-ph\"><div><h1>My account</h1><p>Your name and password for the staff area.</p></div></div>"
                + (demo ? "<div class=\"ak-panel ak-note\"><b>You are signed in with the demo account.</b><p>It has no password of its own, so nothing here can be changed. The admin account is created on the sign-in page the first time the site runs in Production.</p></div>" : string.Empty)
                + "<div class=\"ak-cols\"><section class=\"ak-panel\"><h2>Details</h2><form class=\"ak-stack\" id=\"ak-nform\" onsubmit=\"return AdminAccountJs.saveName()\" novalidate>"
                + "<label class=\"ak-field\" for=\"ak-name\"><span>Name</span><input type=\"text\" id=\"ak-name\" data-k=\"name\" maxlength=\"60\" value=\"" + Ui.Esc(c.U.Name) + "\"" + d + "><em class=\"ak-err\" id=\"ak-e-name\"></em></label>"
                + "<label class=\"ak-field\" for=\"ak-title\"><span>Role shown in the header</span><input type=\"text\" id=\"ak-title\" data-k=\"title\" maxlength=\"60\" value=\"" + Ui.Esc(c.U.Title) + "\"" + d + "></label>"
                + "<dl class=\"ak-kv\"><dt>User name</dt><dd>" + Ui.Esc(c.U.Login) + "</dd></dl>"
                + "<div class=\"ak-acts\"><button type=\"submit\" class=\"ak-btn\"" + d + ">Save details</button></div></form></section>"
                + "<section class=\"ak-panel\"><h2>Password</h2><form class=\"ak-stack\" id=\"ak-pform\" onsubmit=\"return AdminAccountJs.savePassword()\" novalidate>"
                + "<label class=\"ak-field\" for=\"ak-cur\"><span>Current password</span><input type=\"password\" id=\"ak-cur\" data-k=\"cur\" autocomplete=\"current-password\" maxlength=\"100\"" + d + "><em class=\"ak-err\" id=\"ak-e-cur\"></em></label>"
                + "<label class=\"ak-field\" for=\"ak-next\"><span>New password</span><input type=\"password\" id=\"ak-next\" data-k=\"next\" autocomplete=\"new-password\" maxlength=\"100\"" + d + "><small>At least 8 characters.</small><em class=\"ak-err\" id=\"ak-e-next\"></em></label>"
                + "<label class=\"ak-field\" for=\"ak-again\"><span>New password again</span><input type=\"password\" id=\"ak-again\" data-k=\"again\" autocomplete=\"new-password\" maxlength=\"100\"" + d + "><em class=\"ak-err\" id=\"ak-e-again\"></em></label>"
                + "<div class=\"ak-acts\"><button type=\"submit\" class=\"ak-btn\"" + d + ">Change password</button></div><p class=\"ak-hint\">Changing your password signs you out of every other browser.</p></form></section></div>";
        }
    }
}
