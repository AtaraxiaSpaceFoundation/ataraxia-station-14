# Ataraxia

<p style='text-align: center;'><img alt="ASF" src="https://raw.githubusercontent.com/AtaraxiaSpaceFoundation/ataraxia-station-14/master/Resources/Textures/Logo/ataraxia.png" width="512px" /></p>

---

Ataraxia - это форк [Space Wizards](https://github.com/space-wizards/space-station-14), ориентирующийся на идеи [СтароTG](https://github.com/tgstation/tgstation) и [Shiptest](https://github.com/shiptest-ss13/Shiptest) из Space Station 13.

Space Station 14 - это ремейк SS13, который работает на собственном движке [Robust Toolbox](https://github.com/space-wizards/Robust-Toolbox), собственном игровом движке, написанном на C#.

## Сообщество ASF

[<img src="https://i.imgur.com/XiS9QP5.png" alt="ASF" width="150" align="left">](https://github.com/AtaraxiaSpaceFoundation)
**Ataraxia Space Foundation**<br>Специализируемся на разработке этого билда.

[<img src="https://i.imgur.com/xMzKtYK.png" alt="Discord" width="150" align="left">](https://discord.gg/2Jz7yrHAAw)
**Discord Server**<br>В космосе вас никто не услышит.

## Сборка

Следуйте гайду от [Space Wizards](https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html) по настройке рабочей среды, но учитывайте, что репозитории отличаются друг от друга и некоторые вещи могут отличаться.
Ниже перечислены скрипты и методы облегчающие работу с билдом.

### Windows

> 1. Склонируйте данный репозиторий.
> 2. Запустите `git submodule update --init --recursive` в командной строке, чтобы скачать движок игры.
> 3. Запускайте `Scripts/bat/buildAllDebug.bat` после любых изменений в коде проекта.
> 4. Запустите `Scripts/bat/runQuickAll.bat`, чтобы запустить клиент и сервер.
> 5. Подключитесь к локальному серверу и играйте.

### Linux

> 1. Склонируйте данный репозиторий.
> 2. Запустите `git submodule update --init --recursive` в командной строке, чтобы скачать движок игры.
> 3. Запускайте `Scripts/sh/buildAllDebug.sh` после любых изменений в коде проекта.
> 4. Запустите `Scripts/sh/runQuickAll.sh`, чтобы запустить клиент и сервер.
> 5. Подключитесь к локальному серверу и играйте.

### MacOS

> Предположительно, также, как и на Линуксе, сами разберётесь.

## Лицензия

Содержимое, добавленное в этот репозиторий после коммита NULL (`30 August 2024 23:00:00 UTC`), распространяется по лицензии GNU Affero General Public License версии 3.0, если не указано иное.
См. [LICENSE-AGPLv3](./LICENSE-AGPLv3.txt).

Содержимое, добавленное в этот репозиторий до коммита NULL (`30 August 2024 23:00:00 UTC`) распространяется по лицензии MIT, если не указано иное.
См. [LICENSE-MIT](./LICENSE-MIT.txt).

Большинство ресурсов лицензировано под [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/), если не указано иное. Лицензия и авторские права на ресурсах указаны в файле метаданных.
[Example](./Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).

Обратите внимание, что некоторые активы лицензированы под некоммерческой [CC-BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/) или аналогичной некоммерческой лицензией и должны быть удалены, если вы хотите использовать этот проект в коммерческих целях.
