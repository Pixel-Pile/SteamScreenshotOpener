# SteamScreenshotOpener

Quickly open the screenshot directory of any Steam application in file explorer.
**Screenshots cannot be viewed directly inside the app.**

This repository was formerly named _SteamScreenshotViewer_. 
I renamed it because I found it to be somewhat misleading.
The .exe, solution and project files are still named SteamScreenshotViewer.

![Main View](readme%20files/AppView.png)

## Features

- Streamlined Search and Keyboard Navigation
  - Screenshot directories can be searched by app name (which for some reason is not possible in Steam's built-in Screenshot Manager)
  - Search box is automatically focused when program is started
  - Press enter while typing to open the top list item (you can also use double-click)
  - Focus and navigate the list using arrow keys
- Light & Dark Mode
  - Enabled by [Material Design in XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- Automatic Name Resolution
  - Steam puts all screenshots into directories called by app ID (e.g. your Terraria screenshots should be inside a folder with the name `105600`)
  - When started for the first time, the app automatically  translates all these ID's into names using Steam's API
  - Multiple requests are started simultaneously to speed things up.
    (For me, resolution rate is roughly 30-60 apps/second.)
  - Resolved names are stored locally on your computer (in a file called `cache.json`).
  Internet access is only required to resolve new apps.
  Steam Screenshot Viewer checks for new apps every time it is started.
- Manual Resolution View
  - Separate view that opens if multiple apps have the same name or an app name cannot be resolved
  - Enables you to identify and manually resolve such apps

**The app has to complete a small setup when started for the very first time.**
You have to provide it with the screenshot path of some game (so it knows where to look for other games' screenshots).
The app then has to resolve the names of all apps you have ever taken a screenshot in when it is started for the first time.
This can take a few seconds.

## Why does this exist?

Steam's built-in screenshot manager started crashing a lot for me after they overhauled the UI.
And it took all of steam down with it on every crash.
Eventually, I grew tired of it and created this application.

## Which .exe?

Every release contains 2 .exe files:

- Selfcontained_SteamScreenshotViewer
    - Use this if you have no .Net runtime installed.
- DotnetRuntimeRequired_SteamScreenshotViewer
    - Use this if you have a .Net runtime installed. If you encounter any issues, try the self-contained version
      instead.

## State of this Repository

The app is deemed feature-complete.
I plan on maintaining the app and providing bug-fixes.
(Please open an issue if you found a bug.)

## How does it work?

When started for the first time, you are prompted to enter the full path to the screenshot directory of any steam app.
To do so, just open the Steam screenshot manager, select any game, and click the folder icon at the top to open the
screenshot directory.
Copy the full path of that directory into this application.

Once supplied with a path, the program will search for other screenshot folders.
The path cannot be hardcoded because it contains your Steam UserID.
Also steam's directory structure is very weird in general.
For me, all screenshots are contained in subdirectories of a folder named `760`.
No idea if that is true for everyone.

After finding all screenshot folders the program tries to translate the folder names, which are app IDs, to the apps'
names.
This is done using Steam's appdetails api (<https://store.steampowered.com/api/appdetails>).
The filter `packages` is used to reduce network usage when possible.
If it fails, another request using the filter `basic` is started.
For more details on this see [Why is the packages filter used?](#why-is-the-packages-filter-used).

If any apps cannot be resolved or multiple apps have the same name, you are prompted to resolve these apps manually.

## Supported Platforms

Due to using WPF this application unfortunately only supports Windows.

## Why is the packages filter used?

The appdetails api does not support requests for name only.
The smallest filter containing the app name is `basic`.
Unfortunately, `basic` contains lots of information, such as the full app description.
From my testing, requests using this filter return anywhere from 4-12 KiloBytes.

Requests using `packages` however return as little as 500 Bytes to around 1 KiloByte, resulting in up to 20 times less
bandwidth usage in extreme cases.
(Although probably more around 10 times on average.)

Unfortunately, using `packages` does not work for free apps,
which are resolved by starting a second request, using `filters=basic`.

## Troubleshooting

### Logs

Logs can be found in a folder called `logs` next to the executable.
A separate log is written every time the app is started.
Log files are named by date and time of the app start,
e.g. `2024-04-02_14-36-47.log` logs all events for an app run that started on the 2nd of april 2024 at 14:36:47.


### App List Empty

This is most likely related to the game-specific screenshot directory path being faulty.
Delete the file `config.json` (located in a directory called `storage` next to the `.exe`).
Start the app and it should prompt you to enter a path again.

### Wrong App Name

If an app name is resolved incorrectly you can edit `cache.json`.
The file maps app ids to names.
The file is located in a directory called `storage` next to the `.exe`.
Close the app before editing `cache.json`.

If you resolved a name conflict manually but want to change the app name, you can edit `cache.json` (see above).
If you want to delete all manually set names, delete the `cache.json` file entirely.

### Other Problems

You're welcome to open an issue in this repository.
_It might take a long time for me to reply._

If you want to take things into your own hands, this project is using the MIT license - you can fork and freely develop
it.

## Used Technologies

- [.Net (Core)](https://github.com/dotnet)
- [WPF](https://github.com/dotnet/wpf)
- [Community Toolkit (MVVM)](https://github.com/CommunityToolkit/dotnet)
- [Material Design in XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- [Serilog](https://github.com/serilog/serilog)

## License

This program is released under the [MIT license](LICENSE).
