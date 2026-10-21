namespace DesignStudio.Models
{
    public class SiteData
    {
        public string Brand { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Hours { get; set; } = string.Empty;
        public int MaxVideoMb { get; set; } = 30;
        public List<string> Budgets { get; set; } = new List<string>();
        public List<string> Timelines { get; set; } = new List<string>();
        public Dictionary<string, PageDoc> Pages { get; set; } = new Dictionary<string, PageDoc>();
        public List<Work> Projects { get; set; } = new List<Work>();
        public List<Offer> Services { get; set; } = new List<Offer>();
        public List<TeamMember> Team { get; set; } = new List<TeamMember>();
        public List<Entry> Journal { get; set; } = new List<Entry>();
    }

    public class Work
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Size { get; set; }
        public List<string> Scope { get; set; } = new List<string>();
        public string Summary { get; set; } = string.Empty;
        public string Story { get; set; } = string.Empty;
        public string Cover { get; set; } = string.Empty;
        public List<Pic> Gallery { get; set; } = new List<Pic>();
        public List<Duo> Pairs { get; set; } = new List<Duo>();
        public bool Featured { get; set; }
        public bool Published { get; set; } = true;
    }

    public class Pic
    {
        public string Media { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
    }

    public class Duo
    {
        public string Before { get; set; } = string.Empty;
        public string After { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
    }

    public class Offer
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string> Points { get; set; } = new List<string>();
        public string Duration { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public bool Published { get; set; } = true;
    }

    public class TeamMember
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Photo { get; set; } = string.Empty;
        public bool Published { get; set; } = true;
    }

    public class Entry
    {
        public string Id { get; set; } = string.Empty;
        public string Kind { get; set; } = "journal";
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Cover { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public bool Published { get; set; } = true;
    }

    public class Inquiry
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Budget { get; set; } = string.Empty;
        public string Timeline { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "new";
        public string Note { get; set; } = string.Empty;
    }

    public class Staff
    {
        public string Id { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public bool MustChange { get; set; }
    }

    public class Ctx
    {
        public Staff U { get; init; } = new Staff();
        public string Token { get; init; } = string.Empty;
    }

    public class Session
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
    }

    public class ActivityEntry
    {
        public DateTime When { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Who { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public bool Ok { get; set; } = true;
    }

    public class MediaItem
    {
        public string Id { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Thumb { get; set; } = string.Empty;
        public string Kind { get; set; } = "photo";
        public string Name { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public long Bytes { get; set; }
        public DateTime Added { get; set; }
        public string By { get; set; } = string.Empty;
    }

    public class PageDoc
    {
        public List<Block> Blocks { get; set; } = new List<Block>();
        public List<PageVersion> Versions { get; set; } = new List<PageVersion>();
        public DateTime Saved { get; set; }
        public string By { get; set; } = string.Empty;
    }

    public class PageVersion
    {
        public DateTime When { get; set; }
        public string By { get; set; } = string.Empty;
        public List<Block> Blocks { get; set; } = new List<Block>();
    }

    public class Block
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public bool Hidden { get; set; }
        public string Kicker { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Media { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Label2 { get; set; } = string.Empty;
        public string Link2 { get; set; } = string.Empty;
        public bool Flip { get; set; }
        public List<BlockItem> Items { get; set; } = new List<BlockItem>();
    }

    public class BlockItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Media { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
