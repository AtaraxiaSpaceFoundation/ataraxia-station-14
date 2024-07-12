using System.Linq;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

namespace Content.Client._White.Chat;

public sealed class ChatAbbreviationManager : IChatAbbreviationManager
{
    [Dependency] private readonly IResourceCache _resources = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    public readonly string SuggestSeparator = "%";
    public List<AbbreviatedWord> Worlds = new List<AbbreviatedWord>();

    public void Initialize()
    {
        LoadWords(new ResPath("/White/ChatFilters/slang.yml"));
    }

    public void LoadWords(ResPath resPath)
    {
        try
        {
            var yaml = _resources.ContentFileReadYaml(resPath);
            var node = yaml.Documents[0].RootNode.ToDataNodeCast<MappingDataNode>();
            var data = _serializationManager.Read<Dictionary<string, string>>(node, notNullableOverride : false);
            Worlds = data.Select(s => new AbbreviatedWord(s.Key, s.Value)).ToList();
        }
        catch (Exception e)
        {
            Logger.Error($"Shit happened!: {e}");
        }
    }

    public (List<AbbreviatedWord>, string) GetSuggestWord(string input)
    {
        var splited = input.Split(SuggestSeparator);
        if (splited.Length <= 1)
            return (new List<AbbreviatedWord>(), string.Empty);

        splited = splited.Last().Split(" ");
        if (splited.Length > 1)
            return (new List<AbbreviatedWord>(), string.Empty);

        var currentAbbreviation = splited[0];
        Logger.Debug($"Current shit: {currentAbbreviation}");
        return (Worlds.Where(a => a.Short.StartsWith(SuggestSeparator+currentAbbreviation)).ToList(),currentAbbreviation);
    }
}

public interface IChatAbbreviationManager
{
    public void Initialize();
    public void LoadWords(ResPath resPath);
    public (List<AbbreviatedWord>, string) GetSuggestWord(string input);
}

public record struct AbbreviatedWord(string Short, string Word);
