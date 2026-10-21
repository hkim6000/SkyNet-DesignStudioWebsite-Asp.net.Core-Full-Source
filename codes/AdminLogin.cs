using System.Text;
using DesignStudio.Core;
using DesignStudio.Models;
using SkyNet;

namespace DesignStudio.codes
{
    public class AdminLogin : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Staff sign in");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            await Init.OnStart();
            await Store.EnsureFirstManager();
            SiteData site = await Store.LoadSite();
            bool demo = Store.Demo();
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                .Replace("{plhd_brand}", Ui.Esc(site.Brand))
                .Replace("{plhd_first}", !demo && Local.Request() ? FirstBox(Store.InitialPassword()) : string.Empty)
                .Replace("{plhd_demo}", demo ? "<div class=\"lg-note\"><b>Demo mode</b><p>Any user name and password signs in as the demo manager. The form is already filled in, so press Sign in.</p></div>" : string.Empty)
                .Replace("{plhd_login}", demo ? "demo" : string.Empty)
                .Replace("{plhd_pass}", demo ? "demo1234" : string.Empty);
        }

        private static string FirstBox(string pass)
        {
            if (pass.Length == 0)
            {
                return string.Empty;
            }
            return "<div class=\"lg-first\"><b>First sign-in</b><dl><dt>User name</dt><dd>admin</dd><dt>Password</dt><dd>" + Ui.Esc(pass) + "</dd></dl><p>Shown only on this computer, until you choose your own password. Also in appConfig/initial-admin.txt.</p></div>";
        }

        public async Task<ApiResponse> SignIn()
        {
            ApiResponse response = new ApiResponse();
            string login = Ui.Clip(GetDataValue("login"), 60).ToLowerInvariant();
            string pass = GetDataValue("pass") ?? string.Empty;
            response.SetElementContents("lg-err", string.Empty);
            if (Auth.Paused(login))
            {
                response.SetElementContents("lg-err", "Too many wrong passwords. Please wait five minutes and try again.");
                response.ExecuteScript("LoginJs.failed();");
                return response;
            }
            Staff? user = (await Store.LoadStaff()).FirstOrDefault(s => s.Login.ToLowerInvariant() == login && s.Hash.Length > 0);
            bool right = user != null && Access.Verify(pass, user.Hash);
            if (!right && Store.Demo())
            {
                string tok = await Auth.Open("u-demo");
                await Store.Log("u-demo", "sign in", "demo", true);
                response.ExecuteScript("LoginJs.signedIn('" + tok + "');");
                return response;
            }
            if (!right || user == null)
            {
                Auth.Missed(login);
                await Store.Log("system", "sign in refused", login.Length == 0 ? "(empty)" : login, false);
                response.SetElementContents("lg-err", "The user name or password is not right.");
                response.ExecuteScript("LoginJs.failed();");
                return response;
            }
            Auth.Cleared(login);
            if (user.MustChange)
            {
                response.ExecuteScript("LoginJs.choose();");
                return response;
            }
            string token = await Auth.Open(user.Id);
            await Store.Log(user.Id, "sign in", user.Login, true);
            response.ExecuteScript("LoginJs.signedIn('" + token + "');");
            return response;
        }

        public async Task<ApiResponse> SetPassword()
        {
            ApiResponse response = new ApiResponse();
            string login = Ui.Clip(GetDataValue("login"), 60).ToLowerInvariant();
            string pass = GetDataValue("pass") ?? string.Empty;
            string next = GetDataValue("npass") ?? string.Empty;
            string again = GetDataValue("npass2") ?? string.Empty;
            response.SetElementContents("lg-err", string.Empty);
            string error = string.Empty;
            string token = string.Empty;
            string userId = string.Empty;
            using (await Store.Hold())
            {
                List<Staff> staff = await Store.LoadStaff();
                Staff? user = staff.FirstOrDefault(s => s.Login.ToLowerInvariant() == login && s.Hash.Length > 0 && s.MustChange);
                if (user == null || !Access.Verify(pass, user.Hash))
                {
                    error = "The user name or current password is not right.";
                }
                else if (next.Length < 8)
                {
                    error = "Use at least 8 characters for the new password.";
                }
                else if (next == pass)
                {
                    error = "Choose a password different from the first one.";
                }
                else if (next != again)
                {
                    error = "The two new passwords are not the same.";
                }
                else
                {
                    user.Hash = Access.HashPassword(next);
                    user.MustChange = false;
                    await Store.SaveStaff(staff);
                    userId = user.Id;
                }
            }
            if (error.Length > 0)
            {
                response.SetElementContents("lg-err", error);
                response.ExecuteScript("LoginJs.failed();");
                return response;
            }
            if (File.Exists(Store.InitialFile))
            {
                File.Delete(Store.InitialFile);
            }
            token = await Auth.Open(userId);
            await Store.Log(userId, "choose password", login, true);
            response.ExecuteScript("LoginJs.signedIn('" + token + "');");
            return response;
        }
    }
}
