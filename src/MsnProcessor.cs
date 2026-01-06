using System.Text;

namespace ii.Ascend;

public class MsnProcessor
{
    // Standard MSN (Descent 1) keys
    private static readonly HashSet<string> StandardD1Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "type",
        "briefing",
        "hog",
        "num_levels",
        "num_secrets"
    };

    // Standard MN2 (Descent 2) keys
    private static readonly HashSet<string> StandardD2Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "xname",      // alternative to name (unknown effect)
        "zname",      // alternative to name (enables Vertigo Series mode)
        "type",
        "num_levels",
        "num_secrets"
    };

    // MNX extension keys for Descent 1
    private static readonly HashSet<string> MnxExtensionD1Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        "author",
        "email",
        "web_site",
        "revision",
        "date",
        "build_time",
        "normal",
        "anarchy",
        "robo_anarchy",
        "coop",
        "custom_music"
    };

    // MNX extension keys for Descent 2
    private static readonly HashSet<string> MnxExtensionD2Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        "author",
        "email",
        "web_site",
        "revision",
        "date",
        "build_time",
        "normal",
        "anarchy",
        "robo_anarchy",
        "coop",
        "custom_music",
        "capture_flag",
        "hoard",
        "custom_robots",
        "custom_textures"
    };

    public List<string> CustomValidKeys { get; set; } = [];

    public MsnFile Read(string filename)
    {
        var content = File.ReadAllText(filename, Encoding.ASCII);
        return ReadInternal(content);
    }

    public MsnFile Read(byte[] fileData)
    {
        var content = Encoding.ASCII.GetString(fileData);
        return ReadInternal(content);
    }

    private MsnFile ReadInternal(string content)
    {
        var result = new MsnFile();

        var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Action<string>? listAdder = null;
        int itemsRemaining = 0;

        foreach (var line in lines)
        {
            if (TryParseProperty(line, out var prop))
            {
                result.Properties.Add(prop);
                switch (prop.Key.ToLowerInvariant())
                {
                    case "num_levels" when int.TryParse(prop.Value, out var count):
                        listAdder = s => result.LevelFilenames.Add(s);
                        itemsRemaining = count;
                        break;

                    case "num_secrets" when int.TryParse(prop.Value, out var count):
                        if (itemsRemaining == 0)
                        {
                            listAdder = s => result.SecretEntries.Add(s);
                            itemsRemaining = count;
                        }
                        break;

                    default:
                        listAdder = null;
                        break;
                }
            }
            else if (listAdder != null && itemsRemaining > 0)
            {
                listAdder(line);
                itemsRemaining--;

                if (itemsRemaining == 0)
                {
                    listAdder = null;
                }
            }
        }

        return result;
    }

    private bool TryParseProperty(string line, out MsnProperty property)
    {
        property = null!;
        var equalsIndex = line.IndexOf('=');

        // Must be a valid key=value pair (key cannot be empty)
        if (equalsIndex <= 0) 
            return false;

        var key = line.Substring(0, equalsIndex).Trim();
        var valueAndComment = line.Substring(equalsIndex + 1);

        string value;
        string comment = string.Empty;

        var commentIndex = valueAndComment.IndexOf(';');
        if (commentIndex >= 0)
        {
            value = valueAndComment.Substring(0, commentIndex).Trim();
            comment = valueAndComment.Substring(commentIndex + 1).Trim();
        }
        else
        {
            value = valueAndComment.Trim();
        }

        property = new MsnProperty
        {
            Key = key,
            Value = value,
            Comment = comment
        };

        return true;
    }

    public bool Validate(MsnFile msnFile, bool validateD1 = true, bool validateD2 = false, bool includeMnxExtensions = false)
    {
        var validKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        if (validateD1)
        {
            foreach (var key in StandardD1Keys)
            {
                validKeys.Add(key);
            }

            if (includeMnxExtensions)
            {
                foreach (var key in MnxExtensionD1Keys)
                {
                    validKeys.Add(key);
                }
            }
        }

        if (validateD2)
        {
            foreach (var key in StandardD2Keys)
            {
                validKeys.Add(key);
            }

            if (includeMnxExtensions)
            {
                foreach (var key in MnxExtensionD2Keys)
                {
                    validKeys.Add(key);
                }
            }
        }

        foreach (var customKey in CustomValidKeys)
        {
            validKeys.Add(customKey);
        }

        foreach (var property in msnFile.Properties)
        {
            if (!validKeys.Contains(property.Key))
            {
                return false;
            }
        }

        return true;
    }
}