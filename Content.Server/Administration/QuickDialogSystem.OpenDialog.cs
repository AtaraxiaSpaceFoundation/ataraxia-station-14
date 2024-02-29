using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Administration;

//res<IEntityManager>().System<MindSystem>().TryGetSession(new EntityUid(X), out var seks);res<IEntityManager>().System<QuickDialogSystem>().OpenDialog<string, bool, bool, int, VoidOption, VoidOption, Hex16>(seks, "Заголовок", "Серийный код твоей матери", "Селёдкой пахнет?", "Сосал?", "Сколько ванотян жрал хуёв:", "тыгыдык тыгыдык тыгыдык тыгыдык" ," ", "Вскрываем байты", (_,_,_,_,_,_,_)=>{});
//
//List<(Type, string, object)> entries = new();entries.Add((typeof(string), "shitass", "faggot"));entries.Add((typeof(int), "cunt", 2254))
//res<IEntityManager>().System<MindSystem>().TryGetSession(new EntityUid(X), out var seks);res<IEntityManager>().System<QuickDialogSystem>().OpenDialog(seks, "Заголовок", entries, (_)=>{});
public sealed partial class QuickDialogSystem
{
    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt">The prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1>(ICommonSession session, string title, string prompt, Action<T1> okAction,
        Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
                new("1", TypeToEntryType(typeof(T1)), prompt)
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1))
                    okAction.Invoke(v1);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The first prompt.</param>
    /// <param name="prompt2">The second prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the first input.</typeparam>
    /// <typeparam name="T2">Type of the second input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2>(ICommonSession session, string title, string prompt1, string prompt2,
        Action<T1, T2> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
                new("1", TypeToEntryType(typeof(T1)), prompt1),
                new("2", TypeToEntryType(typeof(T2)), prompt2)
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {

                if (TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                    TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2)
                    )
                    okAction.Invoke(v1, v2);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The first prompt.</param>
    /// <param name="prompt2">The second prompt.</param>
    /// <param name="prompt3">The third prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the first input.</typeparam>
    /// <typeparam name="T2">Type of the second input.</typeparam>
    /// <typeparam name="T3">Type of the third input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2, T3>(ICommonSession session, string title, string prompt1, string prompt2,
        string prompt3, Action<T1, T2, T3> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
                new("1", TypeToEntryType(typeof(T1)), prompt1),
                new("2", TypeToEntryType(typeof(T2)), prompt2),
                new("3", TypeToEntryType(typeof(T3)), prompt3)
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                    TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2) &&
                    TryParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"], out var v3)
                   )
                    okAction.Invoke(v1, v2, v3);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The first prompt.</param>
    /// <param name="prompt2">The second prompt.</param>
    /// <param name="prompt3">The third prompt.</param>
    /// <param name="prompt4">The fourth prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the first input.</typeparam>
    /// <typeparam name="T2">Type of the second input.</typeparam>
    /// <typeparam name="T3">Type of the third input.</typeparam>
    /// <typeparam name="T4">Type of the fourth input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2, T3, T4>(ICommonSession session, string title, string prompt1, string prompt2,
        string prompt3, string prompt4, Action<T1, T2, T3, T4> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
                new("1", TypeToEntryType(typeof(T1)), prompt1),
                new("2", TypeToEntryType(typeof(T2)), prompt2),
                new("3", TypeToEntryType(typeof(T3)), prompt3),
                new("4", TypeToEntryType(typeof(T4)), prompt4),
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                    TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2) &&
                    TryParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"], out var v3) &&
                    TryParseQuickDialog<T4>(TypeToEntryType(typeof(T4)), ev.Responses["4"], out var v4)
                   )
                    okAction.Invoke(v1, v2, v3, v4);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The first prompt.</param>
    /// <param name="prompt2">The second prompt.</param>
    /// <param name="prompt3">The third prompt.</param>
    /// <param name="prompt4">The fourth prompt.</param>
    /// <param name="prompt5">The fifth prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the first input.</typeparam>
    /// <typeparam name="T2">Type of the second input.</typeparam>
    /// <typeparam name="T3">Type of the third input.</typeparam>
    /// <typeparam name="T4">Type of the fourth input.</typeparam>
    /// <typeparam name="T5">Type of the fifth input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2, T3, T4, T5>(ICommonSession session, string title, string prompt1, string prompt2, string prompt3, string prompt4, string prompt5, Action<T1, T2, T3, T4, T5> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
              new("1", TypeToEntryType(typeof(T1)), prompt1),
              new("2", TypeToEntryType(typeof(T2)), prompt2),
              new("3", TypeToEntryType(typeof(T3)), prompt3),
              new("4", TypeToEntryType(typeof(T4)), prompt4),
              new("5", TypeToEntryType(typeof(T5)), prompt5),
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (
                      TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                      TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2) &&
                      TryParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"], out var v3) &&
                      TryParseQuickDialog<T4>(TypeToEntryType(typeof(T4)), ev.Responses["4"], out var v4) &&
                      TryParseQuickDialog<T5>(TypeToEntryType(typeof(T5)), ev.Responses["5"], out var v5)
                   )
                    okAction.Invoke(v1, v2, v3, v4, v5);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }


    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The Nth prompt.</param>
    /// <param name="prompt2">The Nth prompt.</param>
    /// <param name="prompt3">The Nth prompt.</param>
    /// <param name="prompt4">The Nth prompt.</param>
    /// <param name="prompt5">The Nth prompt.</param>
    /// <param name="prompt6">The Nth prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the Nth input.</typeparam>
    /// <typeparam name="T2">Type of the Nth input.</typeparam>
    /// <typeparam name="T3">Type of the Nth input.</typeparam>
    /// <typeparam name="T4">Type of the Nth input.</typeparam>
    /// <typeparam name="T5">Type of the Nth input.</typeparam>
    /// <typeparam name="T6">Type of the Nth input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2, T3, T4, T5, T6>(ICommonSession session, string title, string prompt1, string prompt2, string prompt3, string prompt4, string prompt5, string prompt6, Action<T1, T2, T3, T4, T5, T6> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
          new("1", TypeToEntryType(typeof(T1)), prompt1),
          new("2", TypeToEntryType(typeof(T2)), prompt2),
          new("3", TypeToEntryType(typeof(T3)), prompt3),
          new("4", TypeToEntryType(typeof(T4)), prompt4),
          new("5", TypeToEntryType(typeof(T5)), prompt5),
          new("6", TypeToEntryType(typeof(T6)), prompt6),
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (
                      TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                      TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2) &&
                      TryParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"], out var v3) &&
                      TryParseQuickDialog<T4>(TypeToEntryType(typeof(T4)), ev.Responses["4"], out var v4) &&
                      TryParseQuickDialog<T5>(TypeToEntryType(typeof(T5)), ev.Responses["5"], out var v5) &&
                      TryParseQuickDialog<T6>(TypeToEntryType(typeof(T6)), ev.Responses["6"], out var v6)
                   )
                    okAction.Invoke(v1, v2, v3, v4, v5, v6);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }


    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The Nth prompt.</param>
    /// <param name="prompt2">The Nth prompt.</param>
    /// <param name="prompt3">The Nth prompt.</param>
    /// <param name="prompt4">The Nth prompt.</param>
    /// <param name="prompt5">The Nth prompt.</param>
    /// <param name="prompt6">The Nth prompt.</param>
    /// <param name="prompt7">The Nth prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the Nth input.</typeparam>
    /// <typeparam name="T2">Type of the Nth input.</typeparam>
    /// <typeparam name="T3">Type of the Nth input.</typeparam>
    /// <typeparam name="T4">Type of the Nth input.</typeparam>
    /// <typeparam name="T5">Type of the Nth input.</typeparam>
    /// <typeparam name="T6">Type of the Nth input.</typeparam>
    /// <typeparam name="T7">Type of the Nth input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2, T3, T4, T5, T6, T7>(ICommonSession session, string title, string prompt1, string prompt2, string prompt3, string prompt4, string prompt5, string prompt6, string prompt7, Action<T1, T2, T3, T4, T5, T6, T7> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
          new("1", TypeToEntryType(typeof(T1)), prompt1),
          new("2", TypeToEntryType(typeof(T2)), prompt2),
          new("3", TypeToEntryType(typeof(T3)), prompt3),
          new("4", TypeToEntryType(typeof(T4)), prompt4),
          new("5", TypeToEntryType(typeof(T5)), prompt5),
          new("6", TypeToEntryType(typeof(T6)), prompt6),
          new("7", TypeToEntryType(typeof(T7)), prompt7),
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (
                      TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                      TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2) &&
                      TryParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"], out var v3) &&
                      TryParseQuickDialog<T4>(TypeToEntryType(typeof(T4)), ev.Responses["4"], out var v4) &&
                      TryParseQuickDialog<T5>(TypeToEntryType(typeof(T5)), ev.Responses["5"], out var v5) &&
                      TryParseQuickDialog<T6>(TypeToEntryType(typeof(T6)), ev.Responses["6"], out var v6) &&
                      TryParseQuickDialog<T7>(TypeToEntryType(typeof(T7)), ev.Responses["7"], out var v7)
                   )
                    okAction.Invoke(v1, v2, v3, v4, v5, v6, v7);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }


    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The Nth prompt.</param>
    /// <param name="prompt2">The Nth prompt.</param>
    /// <param name="prompt3">The Nth prompt.</param>
    /// <param name="prompt4">The Nth prompt.</param>
    /// <param name="prompt5">The Nth prompt.</param>
    /// <param name="prompt6">The Nth prompt.</param>
    /// <param name="prompt7">The Nth prompt.</param>
    /// <param name="prompt8">The Nth prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the Nth input.</typeparam>
    /// <typeparam name="T2">Type of the Nth input.</typeparam>
    /// <typeparam name="T3">Type of the Nth input.</typeparam>
    /// <typeparam name="T4">Type of the Nth input.</typeparam>
    /// <typeparam name="T5">Type of the Nth input.</typeparam>
    /// <typeparam name="T6">Type of the Nth input.</typeparam>
    /// <typeparam name="T7">Type of the Nth input.</typeparam>
    /// <typeparam name="T8">Type of the Nth input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2, T3, T4, T5, T6, T7, T8>(ICommonSession session, string title, string prompt1, string prompt2, string prompt3, string prompt4, string prompt5, string prompt6, string prompt7, string prompt8, Action<T1, T2, T3, T4, T5, T6, T7, T8> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
          new("1", TypeToEntryType(typeof(T1)), prompt1),
          new("2", TypeToEntryType(typeof(T2)), prompt2),
          new("3", TypeToEntryType(typeof(T3)), prompt3),
          new("4", TypeToEntryType(typeof(T4)), prompt4),
          new("5", TypeToEntryType(typeof(T5)), prompt5),
          new("6", TypeToEntryType(typeof(T6)), prompt6),
          new("7", TypeToEntryType(typeof(T7)), prompt7),
          new("8", TypeToEntryType(typeof(T8)), prompt8),
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (
                      TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                      TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2) &&
                      TryParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"], out var v3) &&
                      TryParseQuickDialog<T4>(TypeToEntryType(typeof(T4)), ev.Responses["4"], out var v4) &&
                      TryParseQuickDialog<T5>(TypeToEntryType(typeof(T5)), ev.Responses["5"], out var v5) &&
                      TryParseQuickDialog<T6>(TypeToEntryType(typeof(T6)), ev.Responses["6"], out var v6) &&
                      TryParseQuickDialog<T7>(TypeToEntryType(typeof(T7)), ev.Responses["7"], out var v7) &&
                      TryParseQuickDialog<T8>(TypeToEntryType(typeof(T8)), ev.Responses["8"], out var v8)
                   )
                    okAction.Invoke(v1, v2, v3, v4, v5, v6, v7, v8);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The Nth prompt.</param>
    /// <param name="prompt2">The Nth prompt.</param>
    /// <param name="prompt3">The Nth prompt.</param>
    /// <param name="prompt4">The Nth prompt.</param>
    /// <param name="prompt5">The Nth prompt.</param>
    /// <param name="prompt6">The Nth prompt.</param>
    /// <param name="prompt7">The Nth prompt.</param>
    /// <param name="prompt8">The Nth prompt.</param>
    /// <param name="prompt9">The Nth prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the Nth input.</typeparam>
    /// <typeparam name="T2">Type of the Nth input.</typeparam>
    /// <typeparam name="T3">Type of the Nth input.</typeparam>
    /// <typeparam name="T4">Type of the Nth input.</typeparam>
    /// <typeparam name="T5">Type of the Nth input.</typeparam>
    /// <typeparam name="T6">Type of the Nth input.</typeparam>
    /// <typeparam name="T7">Type of the Nth input.</typeparam>
    /// <typeparam name="T8">Type of the Nth input.</typeparam>
    /// <typeparam name="T9">Type of the Nth input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ICommonSession session, string title, string prompt1, string prompt2, string prompt3, string prompt4, string prompt5, string prompt6, string prompt7, string prompt8, string prompt9, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
          new("1", TypeToEntryType(typeof(T1)), prompt1),
          new("2", TypeToEntryType(typeof(T2)), prompt2),
          new("3", TypeToEntryType(typeof(T3)), prompt3),
          new("4", TypeToEntryType(typeof(T4)), prompt4),
          new("5", TypeToEntryType(typeof(T5)), prompt5),
          new("6", TypeToEntryType(typeof(T6)), prompt6),
          new("7", TypeToEntryType(typeof(T7)), prompt7),
          new("8", TypeToEntryType(typeof(T8)), prompt8),
          new("9", TypeToEntryType(typeof(T9)), prompt9),
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (
                      TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                      TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2) &&
                      TryParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"], out var v3) &&
                      TryParseQuickDialog<T4>(TypeToEntryType(typeof(T4)), ev.Responses["4"], out var v4) &&
                      TryParseQuickDialog<T5>(TypeToEntryType(typeof(T5)), ev.Responses["5"], out var v5) &&
                      TryParseQuickDialog<T6>(TypeToEntryType(typeof(T6)), ev.Responses["6"], out var v6) &&
                      TryParseQuickDialog<T7>(TypeToEntryType(typeof(T7)), ev.Responses["7"], out var v7) &&
                      TryParseQuickDialog<T8>(TypeToEntryType(typeof(T8)), ev.Responses["8"], out var v8) &&
                      TryParseQuickDialog<T9>(TypeToEntryType(typeof(T9)), ev.Responses["9"], out var v9)
                   )
                    okAction.Invoke(v1, v2, v3, v4, v5, v6, v7, v8, v9);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The Nth prompt.</param>
    /// <param name="prompt2">The Nth prompt.</param>
    /// <param name="prompt3">The Nth prompt.</param>
    /// <param name="prompt4">The Nth prompt.</param>
    /// <param name="prompt5">The Nth prompt.</param>
    /// <param name="prompt6">The Nth prompt.</param>
    /// <param name="prompt7">The Nth prompt.</param>
    /// <param name="prompt8">The Nth prompt.</param>
    /// <param name="prompt9">The Nth prompt.</param>
    /// <param name="prompt10">The Nth prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the Nth input.</typeparam>
    /// <typeparam name="T2">Type of the Nth input.</typeparam>
    /// <typeparam name="T3">Type of the Nth input.</typeparam>
    /// <typeparam name="T4">Type of the Nth input.</typeparam>
    /// <typeparam name="T5">Type of the Nth input.</typeparam>
    /// <typeparam name="T6">Type of the Nth input.</typeparam>
    /// <typeparam name="T7">Type of the Nth input.</typeparam>
    /// <typeparam name="T8">Type of the Nth input.</typeparam>
    /// <typeparam name="T9">Type of the Nth input.</typeparam>
    /// <typeparam name="T10">Type of the Nth input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ICommonSession session, string title, string prompt1, string prompt2, string prompt3, string prompt4, string prompt5, string prompt6, string prompt7, string prompt8, string prompt9, string prompt10, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
          new("1", TypeToEntryType(typeof(T1)), prompt1),
          new("2", TypeToEntryType(typeof(T2)), prompt2),
          new("3", TypeToEntryType(typeof(T3)), prompt3),
          new("4", TypeToEntryType(typeof(T4)), prompt4),
          new("5", TypeToEntryType(typeof(T5)), prompt5),
          new("6", TypeToEntryType(typeof(T6)), prompt6),
          new("7", TypeToEntryType(typeof(T7)), prompt7),
          new("8", TypeToEntryType(typeof(T8)), prompt8),
          new("9", TypeToEntryType(typeof(T9)), prompt9),
          new("10", TypeToEntryType(typeof(T10)), prompt10),
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (
                      TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                      TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2) &&
                      TryParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"], out var v3) &&
                      TryParseQuickDialog<T4>(TypeToEntryType(typeof(T4)), ev.Responses["4"], out var v4) &&
                      TryParseQuickDialog<T5>(TypeToEntryType(typeof(T5)), ev.Responses["5"], out var v5) &&
                      TryParseQuickDialog<T6>(TypeToEntryType(typeof(T6)), ev.Responses["6"], out var v6) &&
                      TryParseQuickDialog<T7>(TypeToEntryType(typeof(T7)), ev.Responses["7"], out var v7) &&
                      TryParseQuickDialog<T8>(TypeToEntryType(typeof(T8)), ev.Responses["8"], out var v8) &&
                      TryParseQuickDialog<T9>(TypeToEntryType(typeof(T9)), ev.Responses["9"], out var v9) &&
                      TryParseQuickDialog<T10>(TypeToEntryType(typeof(T10)), ev.Responses["10"], out var v10)
                   )
                    okAction.Invoke(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }


    /// <summary>
    /// Opens a dialog for the given client, with any amount of entries, allowing them to enter in the desired data. For the truly unhinged.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="dialogEntries">List of tuples, not QuickDialogEntries.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <remarks>
    /// Tuple structure for dialogEntries argument:
    ///     Type - int/float/string/LongString/Hex16/VoidOption/null (VoidOption)
    ///     string - prompt text
    ///     object - default value. No checks are performed whether or not it matches the specified type.
    /// </remarks>

    [PublicAPI]
    public void OpenDialog(ICommonSession session, string title, List<(Type, string, object)> dialogEntries, Action<object[]> okAction, Action? cancelAction = null)
    {
        List<QuickDialogEntry> _dialogEntries = new();

        for(int i = 0; i < dialogEntries.Count; i++)
        {
            var (type, prompt, defaultValue) = dialogEntries[i];
            _dialogEntries.Add(new QuickDialogEntry((i+1).ToString(), TypeToEntryType(type), prompt??" ", null, defaultValue));
        }                                         // ^^^ these "indexes" start with 1, for some reason


        OpenDialogInternal(
            session,
            title,
            _dialogEntries,
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (TryParseQuickDialogList(_dialogEntries, ev.Responses, out var results))
                {
                    okAction.Invoke(results!);
                }
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }
}
