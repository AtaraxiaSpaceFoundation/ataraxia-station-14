## Rev Head

roles-antag-rev-head-name = Глава Революции
roles-antag-rev-head-objective = Группа бунтовщиков тайком пробралась на станцию для того чтобы убить всех глав и захватить её. Они вербуют новых сторонников при помощи устройства-вспышки. Постарайтесь выследить и уничтожить их!

head-rev-role-greeting =
    Вы - Глава Революции.
        Вашей целью является убрать весь коммандный состав станции - убив их или изгнав.
        Синдикат снабдил вас вспышками, которые могут быть использованы для вербовки персонала станции на свою сторону.
        Однако, это не сработает на Службу Безопасности, Командный состав или на тех, кто носит защиту от вспышек.
        Да здравствует Революция! За старый космос

head-rev-briefing =
    Используйте вспышку чтобы вербовать людей на свою сторону.
        Убейте всех глав, чтобы захватить станцию.

head-rev-break-mindshield = Имплант защиты разума сломался!

## Rev

roles-antag-rev-name = Революционер
roles-antag-rev-objective = Ваша задача - обеспечить безопасность и выполнять приказы Глав Революции, а также убить весь командный состав станции.

rev-break-control = {$name} вспомнили о своей истинной преданности!

rev-role-greeting =
    Вы - революционер.
        Вам поручено захватить станцию и защитить Глав Революции.
        Уничтожьте весь командный состав.
        Да здравствует революция!

rev-briefing = Помогите Главам Революции убить весь командный состав, чтобы захватить станцию.

## General

rev-title = Революционеры
rev-description = Революционеры среди нас.

rev-not-enough-ready-players = Недостаточно игроков чтобы запустить режим. Готово {$readyPlayersCount} игроков из {$minimumPlayers} необходимых. Невозможно запустить режим Революция.
rev-no-one-ready = Нет готовых игроков! Невозможно запустить режим Революция.
rev-no-heads = Не удалось выбрать Глав Революции. Невозможно запустить режим Революция.

rev-all-heads-dead = Все главы мертвы, теперь прикончите остальных членов экипажа!

rev-won = Главы Революции выжили и убили весь командный состав.

rev-lost = Командный состав выжил и убили всех Глав Революции.

rev-stalemate = И Глав Революции, и командный состав погибли. Ничья.

rev-reverse-stalemate = Обе команды выжили. Ничья.

rev-headrev-count = {$initialCount ->
[one] Был один Глава Революции:
*[other] Было {$initialCount} Глав Революции:
}

rev-headrev-name-user = [color=#5e9cff]{$name}[/color] ([color=gray]{$username}[/color]) завербовал {$count} {$count ->
[one] человека
*[other] человек
}

rev-headrev-name = [color=#5e9cff]{$name}[/color] завербовал {$count} {$count ->
[one] человека
*[other] человек
}

## Deconverted window

rev-deconverted-title = Деконвертирован!
rev-deconverted-text =
    Поскольку последний Глава Революции умер, революция закончилась.

            Вы больше не революционер, так что будьте спокойны.
rev-deconverted-confirm = Принять
