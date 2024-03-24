using Content.Client.UserInterface.Controls;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._White.AuthPanel;

[GenerateTypedNameReferences]
public sealed partial class AuthPanelMenu : FancyWindow
{

    public void OnRedButtonPressed(Action<BaseButton.ButtonEventArgs> func)
    {
        RedButton.OnPressed += func;
    }

    public void OnAccessButtonPressed(Action<BaseButton.ButtonEventArgs> func)
    {
        AccessButton.OnPressed += func;
    }

    public void OnBluespaceWeaponButtonPressed(Action<BaseButton.ButtonEventArgs> func)
    {
        BluespaceWeaponButton.OnPressed += func;
    }

    public void SetCount(Label label,int conf, int maxconf)
    {
        label.Visible = conf != 0;
        label.Text = conf + "/" + maxconf;
    }

    public void SetRedCount(int conf, int maxconf)
    {
        SetCount(RedCount,conf,maxconf);
        RedButton.Disabled = conf >= maxconf;
        AccessContainer.Visible = false;
        BluespaceWeaponContainer.Visible = false;
    }

    public void SetAccessCount(int conf, int maxconf)
    {
        SetCount(AccessCount,conf,maxconf);
        AccessButton.Disabled = conf >= maxconf;
        RedContainer.Visible = false;
        BluespaceWeaponContainer.Visible = false;
    }

    public void SetWeaponCount(int conf, int maxconf)
    {
        SetCount(BluespaceWeaponCount,conf,maxconf);
        BluespaceWeaponButton.Disabled = conf >= maxconf;
        RedContainer.Visible = false;
        AccessContainer.Visible = false;
    }

    public string GetReason()
    {
        return Reason.Text;
    }

    public void SetReason(string reason)
    {
        Reason.Text = reason;
        Reason.Editable = false;
    }
}