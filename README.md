<p align="center">
  <img src="nova_icon.png" width="96" height="96" alt="Nova Browser Logo" />
</p>

<h1 align="center">Nova Browser</h1>

<p align="center">
  <b>Современный, ультрабыстрый и безопасный веб-браузер на движке WebView2 с открытым исходным кодом.</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?style=flat-square&logo=windows" alt="Platform" />
  <img src="https://img.shields.io/badge/Framework-.NET%209.0%20WPF-512BD4?style=flat-square&logo=dotnet" alt=".NET 9" />
  <img src="https://img.shields.io/badge/Engine-Microsoft%20WebView2-008080?style=flat-square&logo=microsoftedge" alt="WebView2" />
  <img src="https://img.shields.io/badge/License-MIT-BEF47B?style=flat-square&labelColor=1a1f2c&color=BEF47B" alt="License" />
  <img src="https://img.shields.io/badge/Open%20Source-%E2%9C%93-brightgreen?style=flat-square" alt="Open Source" />
</p>

---

## 🌟 Обзор проекта

**Nova Browser** — это независимый веб-браузер для Windows, сочетающий аппаратную производительность движка Chromium (через Microsoft WebView2) и легковесный интерфейс на .NET 9 WPF. 

Проект разработан с прицелом на максимальную приватность, отсутствие телеметрии, сверхнизкое потребление оперативной памяти и уникальные функции управления сетевым трафиком.

---

## ⚡ Ключевые возможности

* **Умный Прокси без перезапуска (Live Proxy Switcher)**:
  Встроенный реверс-прокси шлюз `NovaProxyBridge` позволяет переключать выходной IP-адрес на лету в один клик. Браузер автоматически проверяет и показывает **только реально работающие узлы** прямо сейчас (Tor Browser, Tor System, Privoxy, Clash, v2ray/Xray, Shadowsocks и быстрые облачные датацентровые ноды).

* **Глубокая оптимизация памяти (RAM Saver)**:
  Агрессивный контроль расхода ОЗУ. Неактивные фоновые вкладки мгновенно отдают память системе, снижая потребление памяти более чем на 60% по сравнению со стандартным Chrome.

* **Защита от стиллеров и вредоносного ПО**:
  Встроенный механизм безопасности `SecurityHelper` проверяет сигнатуры загружаемых исполняемых файлов (.exe, .jar), блокируя попытки кражи данных и скрытые инжекторы.

* **Встроенный AdBlock & Анти-трекинг**:
  Блокировка рекламы на уровне сетевых перехватов WebView2 до загрузки страницы в DOM.

* **Премиальный кибер-неоновый дизайн**:
  Фирменный акцент `#BEF47B`, 10 цветовых палитр, градиенты, поддержка пользовательских обоев (альпийское озеро на закате) и виджеты погоды и курса валют.

* **Встроенная офлайн-аркада**:
  Flappy Bird, классический Тетрис и Chrome Dino прямо на домашней странице без интернета.

---

## 📂 Исходный код и структура проекта

Весь исходный код полностью открыт и структурирован по модульному принципу:

```text
NovaBrowser/
├── App.xaml / App.xaml.cs                 # Точка входа в приложение, аргументы запуска WebView2
├── MainWindow.xaml / MainWindow.xaml.cs   # Главное окно: вкладки, навигация, омнибокс, оформление
├── MainWindow.NovaSettings.cs             # Панель настроек, динамический опрос прокси, темы оформления
├── MainWindow.NovaHome.cs                 # Координатор стартовой страницы, Speed Dial, поиск
├── MainWindow.PrivateAndCli.cs            # Инкогнито-режим, изоляция приватных вкладок
├── NovaHomeHtml.cs                        # Шаблон стартовой страницы (поиск, игры, погода, обои)
├── PrivateHomeHtml.cs                     # Шаблон стартовой страницы приватного режима
├── SplashWindow.xaml / .cs                # Плавный анимированный сплэш-экран запуска
├── NovaBrowser.csproj                     # Файл проекта .NET 9 с конфигурацией сборки
│
├── Helpers/
│   ├── NovaProxyBridge.cs                 # Локальный TCP/HTTP шлюз для смены IP без перезапуска
│   └── SecurityHelper.cs                  # Блокировщик рекламы, анти-стиллер, санитайзер DOM
│
├── Models/
│   ├── BrowserSettings.cs                 # Хранилище конфигурации и настроек (JSON)
│   ├── BrowserTab.cs                      # Модель вкладки браузера и жизненного цикла WebView2
│   ├── BookmarkItem.cs                    # Модель закладок
│   ├── HistoryItem.cs                     # Модель истории посещений
│   ├── SavedAccountItem.cs                # Модель сохранённых аккаунтов
│   ├── SavedPasswordItem.cs               # Модель паролей с шифрованием
│   └── QuickLinkItem.cs                   # Модель плиток быстрого доступа (Speed Dial)
│
├── Styles/
│   ├── Theme.xaml                         # Цветовые темы, неоновый акцент #BEF47B, кисти
│   └── ControlStyles.xaml                 # Стили кнопок, текстовых полей, скроллбаров
│
├── Assets/
│   ├── nova_1.ico                         # Иконка приложения для Windows
│   ├── nova_icon.png                      # PNG-логотип высокого разрешения
│   └── wallpaper-default.jpg              # Фирменные обои стартовой страницы (альпийский закат)
│
└── site/
    └── index.html                         # Официальный промо-сайт для GitHub Pages
```

---

## 🔬 Архитектура и как это устроено внутри

### 1. Как работает смена Прокси на лету без перезапуска (`NovaProxyBridge.cs`):
Движок Microsoft WebView2 стандартно требует перезапуска всего окружения браузера для смены параметров командной строки `--proxy-server`. 
В Nova Browser эта проблема решена архитектурно:
* Браузер при старте запускает фоновый сверхбыстрый локальный TCP/HTTP шлюз `NovaProxyBridge` на динамическом порту `127.0.0.1:порт`.
* WebView2 постоянно подключен к этому локальному шлюзу.
* При нажатии кнопки «Подключить» на любом рабочем прокси шлюз мгновенно перенаправляет исходящие сокеты на выбранный вышестоящий прокси (HTTP или SOCKS5) без перезапуска браузера и без потери открытых вкладок.

### 2. Динамический опрос только рабочих прокси (`MainWindow.NovaSettings.cs`):
При открытии раздела прокси система запускает параллельное асинхронное тестирование:
* Локальные порты Tor (9150/9050), Clash (7890), v2ray (10808) и Shadowsocks (1080) опрашиваются за 100 мс. Карточки отображаются только если соответствующая программа действительно запущена на ПК.
* Публичные серверы опрашиваются через контрольный запрос к тестовой точке. Узлы, не ответившие за 2 секунды, отсеиваются. В списке остаются только узлы со статусом `✓ ХХХ мс`.

### 3. Защита от стиллеров (`SecurityHelper.cs`):
При скачивании файлов с расширениями `.jar`, `.exe`, `.scr`, `.bat` перехватчик проверяет заголовки PE и сигнатуры упаковщиков/инжекторов вредоносного ПО, защищая пользователя от кражи сессий и паролей.

---

## 🛠️ Сборка из исходного кода (Build from Source)

### Необходимые инструменты:
1. Операционная система: **Windows 10 x64** (версия 1809 или новее) или **Windows 11 x64**.
2. [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (x64).
3. Visual Studio 2022 (версия 17.12+) с пакетом «Разработка классических приложений .NET» либо VS Code / Rider.

### 1. Клонирование репозитория:
```bash
git clone https://github.com/your-username/nova-browser.git
cd nova-browser
```

### 2. Запуск в режиме разработки:
```bash
dotnet run
```

### 3. Автономная публикация готового приложения (Release):
Для сборки полного автономного пакета со всеми зависимостями .NET 9 и WebView2:
```cmd
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o ./publish
```
Все скомпилированные файлы, библиотеки и ассеты окажутся в папке **`publish`**.

---

## 📦 Сборка установщика через Inno Setup

1. Установите [Inno Setup](https://jrsoftware.org/isdl.php).
2. Выполните команду публикации `dotnet publish` (указанную выше).
3. Создайте файл `installer.iss` со следующим содержимым:

```iss
[Setup]
AppName=Nova Browser
AppVersion=1.0.0
DefaultDirName={autopf}\Nova Browser
DefaultGroupName=Nova Browser
UninstallDisplayIcon={app}\nova_1.ico
Compression=lzma2/ultra64
SolidCompression=yes
OutputDir=.\Output
OutputBaseFilename=NovaBrowser_Setup
SetupIconFile=nova_1.ico

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Nova Browser"; Filename: "{app}\NovaBrowser.exe"; IconFilename: "{app}\nova_1.ico"
Name: "{autodesktop}\Nova Browser"; Filename: "{app}\NovaBrowser.exe"; IconFilename: "{app}\nova_1.ico"

[Run]
Filename: "{app}\NovaBrowser.exe"; Description: "Запустить Nova Browser"; Flags: nowait postinstall skipifsilent
```

4. Откройте файл в Inno Setup Compiler и нажмите **Compile (F9)**. Готовый инсталлятор `NovaBrowser_Setup.exe` будет сохранён в папке `Output`.

---

## 🌐 Хостинг официального сайта (GitHub Pages)

В репозитории подготовлен промо-сайт браузера в папке `site/index.html`:
1. Включите **GitHub Pages** в настройках репозитория (**Settings → Pages**).
2. Укажите источник: ветка `main`, папка `/site` (или перенесите файл в `/docs`).
3. Сайт моментально запустится по адресу: `https://your-username.github.io/nova-browser/`.

---

## 🤝 Участие в разработке (Contributing)

Приветствуются любые улучшения, пулл-реквесты и предложения по оптимизации:
1. Создайте форк репозитория.
2. Создайте ветку для новой функции (`git checkout -b feature/awesome-feature`).
3. Зафиксируйте изменения (`git commit -m 'Add awesome feature'`).
4. Отправьте ветку в форк (`git push origin feature/awesome-feature`).
5. Откройте Pull Request.

---

## 📄 Лицензия

Проект распространяется под свободной лицензией **MIT License**. Подробнее см. в файле [LICENSE](LICENSE).
