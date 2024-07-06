# Ghost Respawn

ghost-respawn-time-left = Минут осталось до возможности вернуться в раунд - { $time }.
ghost-respawn-max-players = Функция недоступна, игроков на сервере должно быть меньше { $players }.
ghost-respawn-window-title = Правила возвращения в раунд
ghost-respawn-window-request-button-timer = Принять ({ $time }сек.)
ghost-respawn-window-request-button = Принять
ghost-respawn-window-rules-footer = Пользуясь это функцией, вы [color=#ff7700]обязуетесь[/color] [color=#ff0000]не переносить[/color] знания своего прошлого персонажа в нового, [color=#ff0000]не метамстить[/color]. Каждый новый персонаж - [color=#ff7700]чистый уникальный лист[/color], который никак не связан с предыдущим. Поэтому не забудьте [color=#ff7700]поменять персонажа[/color] перед заходом, а также помните, что за нарушение пункта, указанного здесь, следует [color=#ff0000]бан в размере от 3ех дней[/color].
ghost-respawn-bug = Нет времени смерти. Установлено стандартное значение.
ghost-respawn-same-character = Нельзя заходить в раунд за того же персонажа. Поменяйте его в настройках персонажей.
ghost-respawn-character-almost-same = Игрок { $player } { $try ->
    [true] зашёл
    *[false] попытался зайти
    } в раунд после респауна с похожим именем. Прошлое имя: { $oldName }, текущее: { $newName }.
ghost-respawn-same-character-slightly-changed-name = Попытка обойти запрет входа в раунд тем же персонажем. Ваши действия будут переданы администрации!

# Egun
ent-WeaponEgun = Энергетическая винтовка
    .desc = Стандартное вооружение службы безопасности Nanotransen, поддерживающее стрельбу несколькими видами снарядов.


gun-twomode-mode-examine = Выбран [color={ $color }]{ $mode }[/color] тип снарядов
gun-twomode-Stun = станящий
gun-twomode-Laser = лазерный

ent-CrateArmoryEgun = ящик с энергетическими винтовками

# Timer

signal-timer-menu-title = Таймер
signal-timer-menu-label = Пометка:
signal-timer-menu-delay = Время:
signal-timer-menu-start = Запустить
timer-start-announcement = Таймер был установлен. Пометка: { $Label }, время: { $Time }
timer-end-announcement = Таймер истек, заключенный будет автоматически освобожден. Пометка: { $Label }
timer-suffer-end = Таймер принудительно остановлен. Пометка: { $Label }
label-none = отсутствует

# Petting

petting-success-cat = Вы гладите { $target } по { POSS-ADJ($target) } маленькой пушистой голове.
petting-success-cat-others = { CAPITALIZE($user) } гладит { $target } по { POSS-ADJ($target) } маленькой пушистой голов…

# Expedition

salvage-expedition-difficulty-Minimal = Минимальная
salvage-expedition-difficulty-Minor = Незначительная

# Double toy sword

ent-ToySwordDouble = двойной игрушечный меч


