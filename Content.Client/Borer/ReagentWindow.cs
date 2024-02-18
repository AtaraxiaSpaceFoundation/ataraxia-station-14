﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Borer;

[GenerateTypedNameReferences]
public sealed partial class ReagentWindow : DefaultWindow
{
    public ReagentWindow()
    {
        RobustXamlLoader.Load(this);
    }
}
