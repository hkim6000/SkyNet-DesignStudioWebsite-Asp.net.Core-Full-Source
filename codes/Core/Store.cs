using System.Text.Json;
using DesignStudio.Models;

namespace DesignStudio.Core
{
    public static class Store
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static readonly JsonSerializerOptions ReadOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions WriteOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        private static DateTime configStamp = DateTime.MinValue;
        private static bool demoMode = true;

        public sealed class Releaser : IDisposable
        {
            private int done;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref done, 1) == 0)
                {
                    Gate.Release();
                }
            }
        }

        public static string Root => Directory.GetCurrentDirectory();

        public static string DataDir => Path.Combine(Root, "data");

        public static string ConfigFile => Path.Combine(Root, "appConfig", "application.cfg");

        public static string SessionFile => Path.Combine(Root, "temp", "admin", "sessions.json");

        public static string InitialFile => Path.Combine(Root, "appConfig", "initial-admin.txt");

        public static async Task<Releaser> Hold()
        {
            await Gate.WaitAsync();
            return new Releaser();
        }

        public static async Task<T> Read<T>(string file, Func<T> empty)
        {
            if (!File.Exists(file))
            {
                return empty();
            }
            try
            {
                using FileStream s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                T? value = await JsonSerializer.DeserializeAsync<T>(s, ReadOpts);
                return value == null ? empty() : value;
            }
            catch (JsonException)
            {
                return empty();
            }
        }

        public static async Task Write<T>(string file, T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? Root);
            string tmp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(value, WriteOpts));
            File.Move(tmp, file, true);
        }

        public static async Task<SiteData> LoadSite()
        {
            SiteData site = await Read(Path.Combine(DataDir, "site.json"), () => new SiteData());
            Blocks.Ensure(site);
            return site;
        }

        public static Task SaveSite(SiteData site)
        {
            return Write(Path.Combine(DataDir, "site.json"), site);
        }

        public static Task<List<Staff>> LoadStaff()
        {
            return Read(Path.Combine(DataDir, "staff.json"), () => new List<Staff>());
        }

        public static Task SaveStaff(List<Staff> list)
        {
            return Write(Path.Combine(DataDir, "staff.json"), list);
        }

        public static Task<List<Inquiry>> LoadInquiries()
        {
            return Read(Path.Combine(DataDir, "messages.json"), () => new List<Inquiry>());
        }

        public static Task SaveInquiries(List<Inquiry> list)
        {
            return Write(Path.Combine(DataDir, "messages.json"), list);
        }

        public static Task<List<ActivityEntry>> LoadActivity()
        {
            return Read(Path.Combine(DataDir, "activity.json"), () => new List<ActivityEntry>());
        }

        public static string NewCode(int n)
        {
            const string letters = "abcdefghijkmnpqrstuvwxyz23456789";
            char[] c = new char[n];
            for (int i = 0; i < n; i++)
            {
                c[i] = letters[System.Security.Cryptography.RandomNumberGenerator.GetInt32(letters.Length)];
            }
            return new string(c);
        }

        public static async Task Log(string userId, string action, string target, bool ok)
        {
            string who = userId == "system" ? "System" : userId == "u-demo" ? "Demo manager" : string.Empty;
            if (who.Length == 0)
            {
                Staff? s = (await LoadStaff()).FirstOrDefault(x => x.Id == userId);
                who = s == null ? userId : s.Name;
            }
            using (await Hold())
            {
                List<ActivityEntry> list = await LoadActivity();
                list.Insert(0, new ActivityEntry { When = DateTime.UtcNow, UserId = userId, Who = who, Action = action, Target = Ui.Clip(target, 160), Ok = ok });
                if (list.Count > 5000)
                {
                    list.RemoveRange(5000, list.Count - 5000);
                }
                await Write(Path.Combine(DataDir, "activity.json"), list);
            }
        }

        public static bool Demo()
        {
            if (!File.Exists(ConfigFile))
            {
                return true;
            }
            DateTime stamp = File.GetLastWriteTimeUtc(ConfigFile);
            if (stamp != configStamp)
            {
                string line = File.ReadLines(ConfigFile).Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith("app.app.runmode", StringComparison.Ordinal)) ?? string.Empty;
                string[] part = line.Split('|', 2);
                demoMode = part.Length == 2 && part[1].Trim() == "0";
                configStamp = stamp;
            }
            return demoMode;
        }

        public static Staff DemoManager()
        {
            return new Staff { Id = "u-demo", Login = "demo", Name = "Demo manager", Title = "Manager" };
        }

        public static async Task EnsureFirstManager()
        {
            if (Demo())
            {
                return;
            }
            using (await Hold())
            {
                List<Staff> staff = await LoadStaff();
                if (staff.Any(u => !u.MustChange && u.Hash != string.Empty))
                {
                    return;
                }
                if (File.Exists(InitialFile) && staff.Any(u => u.MustChange))
                {
                    return;
                }
                string pass = Access.NewPassword();
                Staff? a = staff.FirstOrDefault(x => x.Login == "admin");
                if (a == null)
                {
                    a = new Staff { Id = "u-admin", Login = "admin", Name = "Administrator", Title = "Studio administrator" };
                    staff.Insert(0, a);
                }
                a.MustChange = true;
                a.Hash = Access.HashPassword(pass);
                await SaveStaff(staff);
                Directory.CreateDirectory(Path.GetDirectoryName(InitialFile) ?? Root);
                await File.WriteAllTextAsync(InitialFile, "user: admin" + Environment.NewLine + "password: " + pass + Environment.NewLine);
            }
        }

        public static string InitialPassword()
        {
            if (!File.Exists(InitialFile))
            {
                return string.Empty;
            }
            string line = File.ReadAllLines(InitialFile).FirstOrDefault(l => l.StartsWith("password: ", StringComparison.Ordinal)) ?? string.Empty;
            return line.Length > 10 ? line.Substring(10).Trim() : string.Empty;
        }
    }
}
