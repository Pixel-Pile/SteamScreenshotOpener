# SteamScreenshotOpener

This is a small app to quickly open the screenshot directory of any Steam application in file explorer.
**Screenshots cannot be viewed directly inside the app.**

This repository was formerly named _SteamScreenshotViewer_. 
I renamed it because I found it to be somewhat misleading.
The .exe, solution and project files are still named SteamScreenshotViewer.

## Why don't you use Steam's Screenshot Manager?

Steam's built-in screenshot manager started crashing a lot for me after they overhauled the UI.
And every time it crashes, it takes all of Steam down with it.
Eventually, I grew tired of it and created this.

## Why would I use this?

You might be thinking:
> "Why would I use this? 
> I could just save the path to the directory steam stores all screenshots in and open it directly in file explorer."  

And, well you could, but the screenshot folders are named by app id, not name.
(e.g. your Terraria screenshots are stored inside a folder with the name `105600`).
Unless you want to learn all those ids by heart you need a different solution.
This app is one of these many possible solutions.

Additionally, I made sure that using this app is fast and streamlined.
When started, the search box will immediately be focused.
Just start typing and hit enter as soon as the searched app hits the top of the search results.

**The app has to complete a small setup when started for the very first time.**
You have to provide it with the screenshot path of some game (so it knows where to look for other games' screenshots).
Also, the app has to resolve the names of all apps you ever took a screenshot in when it is started for the first time.
This can take a few seconds.

![Main View](readme%20files/AppView.png)


## Features

- Light & Dark Mode
  - enabled by [Material Design in XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- Automatic Name Resolution
  - Steam puts all screenshots into directories called by app ID (e.g. your Terraria screenshots should be inside a folder with the name `105600`)
  - this app automatically translates these IDs into names using Steam's API
- Search
  - as opposed to Steam's built-in Screenshot Manager, this app lets you search your screenshot directories by name 
- Keyboard Navigation
  - Press enter while searching to open the first screenshot directory
  - Focus and navigate the list using arrow keys
- Name Caching
  - resolved app names are stored locally on your computer (`cache.json`)
  - API requests are only made once for each app
- Manual Resolution View
  - separate view that opens if multiple apps have the same name or an app name cannot be resolved
  - enables you to identify and manually resolve such apps

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
