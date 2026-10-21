using System.Globalization;
using System.Net;
using System.Text;

namespace DesignStudio.Core
{
    public static class Ui
    {
        public static string Esc(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        public static string Lines(string? value)
        {
            return Esc(value).Replace("\r", string.Empty).Replace("\n", "<br>");
        }

        public static string Paras(string? value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string p in (value ?? string.Empty).Replace("\r", string.Empty).Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
            {
                string t = p.Trim();
                if (t.Length > 0)
                {
                    sb.Append("<p>" + Esc(t).Replace("\n", "<br>") + "</p>");
                }
            }
            return sb.ToString();
        }

        public static string Js(string? value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in value ?? string.Empty)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\'': sb.Append("\\'"); break;
                    case '"': sb.Append("\\u0022"); break;
                    case '<': sb.Append("\\u003C"); break;
                    case '>': sb.Append("\\u003E"); break;
                    case '&': sb.Append("\\u0026"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    default: sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }

        public static string Clip(string? value, int max)
        {
            string v = (value ?? string.Empty).Trim();
            return v.Length > max ? v.Substring(0, max) : v;
        }

        public static string Count(int n, string one, string many)
        {
            return n + " " + (n == 1 ? one : many);
        }

        public static bool Has(string? text, string term)
        {
            return term.Length > 0 && (text ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        public static string When(DateTime utc)
        {
            DateTime d = utc.Kind == DateTimeKind.Unspecified ? utc : utc.ToLocalTime();
            return d.ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
        }

        public static string Day(DateTime d)
        {
            return d.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        }

        public static string Pick(string? value, IEnumerable<string> allowed)
        {
            string v = (value ?? string.Empty).Trim();
            return allowed.Contains(v) ? v : string.Empty;
        }

        public static int Int(string? value, int min, int max, int fallback)
        {
            return int.TryParse((value ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? Math.Clamp(n, min, max) : fallback;
        }

        public static int Year(string? value)
        {
            int n = Int(value, 0, 2100, 0);
            return n < 1900 ? 0 : n;
        }

        public static bool IsEmail(string v)
        {
            if (v.Length < 5 || v.Length > 120 || v.Contains(' '))
            {
                return false;
            }
            int at = v.IndexOf('@');
            int dot = v.LastIndexOf('.');
            return at > 0 && at == v.LastIndexOf('@') && dot > at + 1 && dot < v.Length - 1;
        }

        public static bool IsPhone(string v)
        {
            return v.Length >= 6 && v.Length <= 30 && v.All(c => char.IsDigit(c) || c == '+' || c == ' ' || c == '-' || c == '(' || c == ')' || c == '.') && v.Count(char.IsDigit) >= 6;
        }

        public static string Slug(string? value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in (value ?? string.Empty).ToLowerInvariant())
            {
                if (ch >= 'a' && ch <= 'z' || ch >= '0' && ch <= '9')
                {
                    sb.Append(ch);
                }
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-')
                {
                    sb.Append('-');
                }
            }
            string s = sb.ToString().Trim('-');
            return s.Length > 40 ? s.Substring(0, 40).Trim('-') : s;
        }

        public static string Join(IEnumerable<string> list)
        {
            return string.Join("\n", list);
        }

        public static List<string> Split(string? value, int max, int each)
        {
            return (value ?? string.Empty).Replace("\r", string.Empty).Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).Select(x => x.Length > each ? x.Substring(0, each) : x).Take(max).ToList();
        }
    }
}
