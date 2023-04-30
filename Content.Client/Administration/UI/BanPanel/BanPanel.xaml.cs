using System.Linq;
using System.Net;
using System.Net.Sockets;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.BanPanel;

[GenerateTypedNameReferences]
public sealed partial class BanPanel : DefaultWindow
{
    public event Action<string?, (IPAddress, int)?, bool, byte[]?, bool, uint, string, NoteSeverity, bool, bool>? BanSubmitted;
    public event Action<string>? PlayerChanged;
    private string? PlayerUsername { get; set; }
    private (IPAddress, int)? IpAddress { get; set; }
    private byte[]? Hwid { get; set; }
    private double TimeEntered { get; set; }
    private uint Multiplier { get; set; }
    private bool HasBanFlag { get; set; }
    private TimeSpan? ButtonResetOn { get; set; }

    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private enum TabNumbers
    {
        BasicInfo,
        //Text,
        Players
    }

    private enum Multipliers
    {
        Minutes,
        Hours,
        Days,
        Weeks,
        Months,
        Years,
        Permanent
    }

    public BanPanel()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        PlayerList.OnSelectionChanged += OnPlayerSelectionChanged;
        PlayerNameLine.OnFocusExit += _ => OnPlayerNameChanged();
        PlayerCheckbox.OnPressed += _ =>
        {
            PlayerNameLine.Editable = PlayerCheckbox.Pressed;
            PlayerNameLine.ModulateSelfOverride = null;
        };
        TimeLine.OnTextChanged += OnMinutesChanged;
        MultiplierOption.OnItemSelected += args =>
        {
            MultiplierOption.SelectId(args.Id);
            OnMultiplierChanged();
        };
        IpLine.OnFocusExit += _ => OnIpChanged();
        IpCheckbox.OnPressed += _ =>
        {
            IpLine.Editable = IpCheckbox.Pressed;
            OnIpChanged();
        };
        HwidLine.OnFocusExit += _ => OnHwidChanged();
        HwidCheckbox.OnPressed += _ =>
        {
            HwidLine.Editable = HwidCheckbox.Pressed;
            OnHwidChanged();
        };
        LastConnCheckbox.OnPressed += args =>
        {
            IpLine.ModulateSelfOverride = null;
            HwidLine.ModulateSelfOverride = null;
            OnIpChanged();
            OnHwidChanged();
        };
        SubmitButton.OnPressed += SubmitButtonOnOnPressed;

        MultiplierOption.AddItem(Loc.GetString("ban-panel-minutes"), (int) Multipliers.Minutes);
        MultiplierOption.AddItem(Loc.GetString("ban-panel-hours"), (int) Multipliers.Hours);
        MultiplierOption.AddItem(Loc.GetString("ban-panel-days"), (int) Multipliers.Days);
        MultiplierOption.AddItem(Loc.GetString("ban-panel-weeks"), (int) Multipliers.Weeks);
        MultiplierOption.AddItem(Loc.GetString("ban-panel-months"), (int) Multipliers.Months);
        MultiplierOption.AddItem(Loc.GetString("ban-panel-years"), (int) Multipliers.Years);
        MultiplierOption.AddItem(Loc.GetString("ban-panel-permanent"), (int) Multipliers.Permanent);
        MultiplierOption.SelectId((int) Multipliers.Minutes);
        OnMultiplierChanged();

        Tabs.SetTabTitle((int) TabNumbers.BasicInfo, Loc.GetString("ban-panel-tabs-basic"));
        //Tabs.SetTabTitle((int) TabNumbers.Text, Loc.GetString("ban-panel-tabs-reason"));
        Tabs.SetTabTitle((int) TabNumbers.Players, Loc.GetString("ban-panel-tabs-players"));

        ReasonTextEdit.Placeholder = new Rope.Leaf(Loc.GetString("ban-panel-reason"));
    }

    public void UpdateBanFlag(bool newFlag)
    {
        HasBanFlag = newFlag;
        SubmitButton.Visible = HasBanFlag;
        ModulateSelfOverride = HasBanFlag ? Color.Red : null;
    }

    public void UpdatePlayerData(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            PlayerNameLine.ModulateSelfOverride = Color.Red;
            ErrorLevel |= ErrorLevelEnum.PlayerName;
            UpdateSubmitEnabled();
            return;
        }
        PlayerNameLine.ModulateSelfOverride = null;
        ErrorLevel &= ~ErrorLevelEnum.PlayerName;
        UpdateSubmitEnabled();
        PlayerUsername = playerName;
        PlayerNameLine.Text = playerName;
    }

    [Flags]
    private enum ErrorLevelEnum : byte
    {
        None = 0,
        Minutes = 1 << 0,
        PlayerName = 1 << 1,
        IpAddress = 1 << 2,
        Hwid = 1 << 3,
    }

    private ErrorLevelEnum ErrorLevel { get; set; }

    private void OnMinutesChanged(LineEdit.LineEditEventArgs args)
    {
        TimeLine.Text = args.Text;
        if (!double.TryParse(args.Text, out var result))
        {
            ExpiresLabel.Text = "err";
            ErrorLevel |= ErrorLevelEnum.Minutes;
            TimeLine.ModulateSelfOverride = Color.Red;
            UpdateSubmitEnabled();
            return;
        }

        ErrorLevel &= ~ErrorLevelEnum.Minutes;
        TimeLine.ModulateSelfOverride = null;
        TimeEntered = result;
        UpdateSubmitEnabled();
        UpdateExpiresLabel();
    }

    private void OnMultiplierChanged()
    {
        TimeLine.Editable = MultiplierOption.SelectedId != (int) Multipliers.Permanent;
        Multiplier = MultiplierOption.SelectedId switch
        {
            (int) Multipliers.Minutes => 1,
            (int) Multipliers.Hours => 60,
            (int) Multipliers.Days => 60 * 24,
            (int) Multipliers.Weeks => 60 * 24 * 7,
            (int) Multipliers.Months => 60 * 24 * 30,
            (int) Multipliers.Years => 60 * 24 * 365,
            (int) Multipliers.Permanent => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(MultiplierOption.SelectedId), "Multiplier out of range")
        };
        UpdateExpiresLabel();
    }

    private void UpdateExpiresLabel()
    {
        var minutes = (uint) (TimeEntered * Multiplier);
        ExpiresLabel.Text = minutes == 0
            ? $"{Loc.GetString("admin-note-editor-expiry-label")} {Loc.GetString("server-ban-string-never")}"
            : $"{Loc.GetString("admin-note-editor-expiry-label")} {DateTime.Now + TimeSpan.FromMinutes(minutes):yyyy/MM/dd HH:mm:ss}";
    }

    private void OnIpChanged()
    {
        if (LastConnCheckbox.Pressed && IpAddress is null || !IpCheckbox.Pressed)
        {
            IpAddress = null;
            ErrorLevel &= ~ErrorLevelEnum.IpAddress;
            IpLine.ModulateSelfOverride = null;
            UpdateSubmitEnabled();
            return;
        }
        var ip = IpLine.Text;
        var hid = "0";
        if (ip.Contains('/'))
        {
            var split = ip.Split('/');
            ip = split[0];
            hid = split[1];
        }

        if (!IPAddress.TryParse(ip, out var parsedIp) || !byte.TryParse(hid, out var hidInt) || hidInt > 128 || hidInt > 32 && parsedIp.AddressFamily == AddressFamily.InterNetwork)
        {
            ErrorLevel |= ErrorLevelEnum.IpAddress;
            IpLine.ModulateSelfOverride = Color.Red;
            UpdateSubmitEnabled();
            return;
        }

        if (hidInt == 0)
            hidInt = (byte) (parsedIp.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32);
        IpAddress = (parsedIp, hidInt);
        ErrorLevel &= ~ErrorLevelEnum.IpAddress;
        IpLine.ModulateSelfOverride = null;
        UpdateSubmitEnabled();
    }

    private void OnHwidChanged()
    {
        var hwidString = HwidLine.Text;
        var length = 3 * (hwidString.Length / 4) - hwidString.TakeLast(2).Count(c => c == '=');
        Hwid = new byte[length];
        if (HwidCheckbox.Pressed && !(string.IsNullOrEmpty(hwidString) && LastConnCheckbox.Pressed) && !Convert.TryFromBase64String(hwidString, Hwid, out _))
        {
            ErrorLevel |= ErrorLevelEnum.Hwid;
            HwidLine.ModulateSelfOverride = Color.Red;
            UpdateSubmitEnabled();
            return;
        }

        ErrorLevel &= ~ErrorLevelEnum.Hwid;
        HwidLine.ModulateSelfOverride = null;
        UpdateSubmitEnabled();

        if (LastConnCheckbox.Pressed || !HwidCheckbox.Pressed)
        {
            Hwid = null;
            return;
        }
        Hwid = Convert.FromHexString(hwidString);
    }

    private void UpdateSubmitEnabled()
    {
        SubmitButton.Disabled = ErrorLevel != ErrorLevelEnum.None;
    }

    private void OnPlayerNameChanged()
    {
        if (PlayerUsername == PlayerNameLine.Text)
            return;
        PlayerUsername = PlayerNameLine.Text;
        if (!PlayerCheckbox.Pressed)
            return;
        if (string.IsNullOrWhiteSpace(PlayerUsername))
            ErrorLevel |= ErrorLevelEnum.PlayerName;
        else
            ErrorLevel &= ~ErrorLevelEnum.PlayerName;

        UpdateSubmitEnabled();
        PlayerChanged?.Invoke(PlayerUsername);
    }

    public void OnPlayerSelectionChanged(PlayerInfo? player)
    {
        PlayerNameLine.Text = player?.Username ?? string.Empty;
        OnPlayerNameChanged();
    }

    private void ResetTextEditor(GUIBoundKeyEventArgs _)
    {
        ReasonTextEdit.ModulateSelfOverride = null;
        ReasonTextEdit.OnKeyBindDown -= ResetTextEditor;
    }

    private void SubmitButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
    {
        var reason = Rope.Collapse(ReasonTextEdit.TextRope);
        if (string.IsNullOrWhiteSpace(reason))
        {
            //Tabs.CurrentTab = (int) TabNumbers.Text;
            Tabs.CurrentTab = (int) TabNumbers.BasicInfo;
            ReasonTextEdit.GrabKeyboardFocus();
            ReasonTextEdit.ModulateSelfOverride = Color.Red;
            ReasonTextEdit.OnKeyBindDown += ResetTextEditor;
            return;
        }

        if (ButtonResetOn is null)
        {
            ButtonResetOn = _gameTiming.CurTime.Add(TimeSpan.FromSeconds(3));
            SubmitButton.ModulateSelfOverride = Color.Red;
            SubmitButton.Text = Loc.GetString("ban-panel-confirm");
            return;
        }

        var player = PlayerCheckbox.Pressed ? PlayerUsername : null;
        var useLastIp = IpCheckbox.Pressed && LastConnCheckbox.Pressed && IpAddress is null;
        var useLastHwid = HwidCheckbox.Pressed && LastConnCheckbox.Pressed && Hwid is null;
        var erase = EraseCheckbox.Pressed;
        var isGlobalBan = GlobalBanCheckbox.Pressed;

        BanSubmitted?.Invoke(player, IpAddress, useLastIp, Hwid, useLastHwid, (uint) (TimeEntered * Multiplier), reason, NoteSeverity.Medium, erase, isGlobalBan);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // This checks for null for free, do not invert it as null always produces a false value
        if (_gameTiming.CurTime > ButtonResetOn)
        {
            ButtonResetOn = null;
            SubmitButton.ModulateSelfOverride = null;
            SubmitButton.Text = Loc.GetString("ban-panel-submit");
        }
    }
}
