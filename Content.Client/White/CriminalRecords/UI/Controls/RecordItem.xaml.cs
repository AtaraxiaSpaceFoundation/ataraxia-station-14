using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.White.CriminalRecords.UI.Controls;

[GenerateTypedNameReferences, Virtual]
public partial class RecordItem : Control
{
    public RecordItem()
    {
        RobustXamlLoader.Load(this);
    }
}
