using System.Linq;
using System.Numerics;
using System.Text;
using Content.Client._White.Chat;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Chat.Widgets; // НЕ ТРОГАТЬ НАХУЙ! ФИЧА БЛЯТЬ!

#pragma warning disable RA0003
public partial class ChatBox
#pragma warning restore RA0003
{
    [Dependency] private readonly IChatAbbreviationManager _chatAbbreviationManager = default!;

    private readonly RichTextLabel _currentLabel = new();
    private readonly FormattedMessage _currentMessage = new();

    private readonly RichTextLabel _fullLabel = new();
    private readonly FormattedMessage _fullMessage = new();

    public void InitializeExtension()
    {
        ChatInput.Input.OnTextChanged += InputTextChanged;
        ChatInput.Input.OnFocusEnter += OnEnter;
        ChatInput.Input.OnFocusExit += OnExit;
        ChatInput.Input.OnTextEntered += InputTextEntered;

        _currentLabel.SetMessage(_currentMessage);
        _fullLabel.SetMessage(_fullMessage);

        ChatInput.AddChild(_currentLabel);
        ChatInput.AddChild(_fullLabel);
    }

    private void InputTextEntered(LineEdit.LineEditEventArgs obj)
    {
        _currentMessage.Clear();
        _fullMessage.Clear();
    }

    private void OnEnter(LineEdit.LineEditEventArgs obj)
    {
        _currentLabel.Visible = true;
        _fullLabel.Visible = true;
    }

    private void OnExit(LineEdit.LineEditEventArgs obj)
    {
        _currentLabel.Visible = false;
        _fullLabel.Visible = false;
    }

    private void InputTextChanged(LineEdit.LineEditEventArgs args)
    {
        _currentMessage.Clear();
        _fullMessage.Clear();
        _currentLabel.Visible = false;
        _fullLabel.Visible = false;

        var font = GetFont(args.Control);

        var cursorPosShift = UIScale * 0f;

        var runes = args.Text.EnumerateRunes().ToArray();
        var (words,suggest) = _chatAbbreviationManager.GetSuggestWord(args.Text);

        if (words.Count == 0)
            return;

        var count = 0;
        var posX = 0;

        foreach (var rune in runes)
        {
            if (!font.TryGetCharMetrics(rune, UIScale, out var metrics))
            {
                count += 1;
                continue;
            }

            posX += metrics.Advance;
            count += rune.Utf16SequenceLength;

            if (count == args.Control.CursorPosition)
            {
                cursorPosShift = posX;
            }
        }

        _currentLabel.Margin = new Thickness(ChatInput.Input.Position.X + cursorPosShift,0,0,0);
        _currentLabel.InvalidateMeasure();
        _fullLabel.Margin = new Thickness(ChatInput.Input.Position.X + cursorPosShift,0,0,0);
        _fullLabel.InvalidateMeasure();

        var limit = 0;

        foreach (var word in words)
        {
            limit++;

            if(limit > 4) continue;

            if (!_currentLabel.Visible)
                _currentLabel.Visible = true;
            if (!_fullLabel.Visible)
                _fullLabel.Visible = true;

            _currentMessage.AddMarkup($"[color=#aaaaaa]{word.Short.Substring(suggest.Length+1)}[/color]\r");
        }

        if (words.Count > 0)
        {
            _fullMessage.PushNewline();
            _fullMessage.AddMarkup($"[font size=8][color=#aaaaaa]{words[0].Word}[/color][/font]\r");
        }

    }

    private Font GetFont(Control element)
    {
        if (element.TryGetStyleProperty<Font>("font", out var font))
        {
            return font;
        }

        return UserInterfaceManager.ThemeDefaults.DefaultFont;
    }
}
