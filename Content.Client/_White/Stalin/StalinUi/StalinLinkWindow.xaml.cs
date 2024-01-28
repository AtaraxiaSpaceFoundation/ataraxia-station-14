﻿using Content.Client.UserInterface.Controls;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._White.Stalin.StalinUi;

[GenerateTypedNameReferences]
public sealed partial class StalinLinkWindow : FancyWindow
{
    [Dependency] private readonly IUriOpener _uriOpener = default!;

    public StalinLinkWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public void SetUri(string uri)
    {
        OpenInBrowserButton.OnPressed += args =>
        {
            _uriOpener.OpenUri(uri);
        };

        StalinLinkText.Text = uri;
    }
}
