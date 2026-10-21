namespace DesignStudio.Core
{
    public static class Catalog
    {
        public static readonly (string Key, string Name)[] Kinds =
        {
            ("house", "Houses"),
            ("interior", "Interiors"),
            ("commercial", "Cafés and workplaces"),
            ("extension", "Extensions")
        };

        public static readonly (string Key, string Name)[] Statuses =
        {
            ("new", "New"),
            ("read", "Read"),
            ("replied", "Replied"),
            ("archived", "Archived")
        };

        public static string Kind(string key)
        {
            foreach ((string k, string n) in Kinds)
            {
                if (k == key)
                {
                    return n;
                }
            }
            return key == "other" ? "Not sure yet" : string.Empty;
        }

        public static string Status(string key)
        {
            foreach ((string k, string n) in Statuses)
            {
                if (k == key)
                {
                    return n;
                }
            }
            return key;
        }
    }
}
