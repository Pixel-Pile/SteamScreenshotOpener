# SteamScreenshotViewer

This is a small app to open the screenshot directories of any Steam application.
**Screenshots cannot be viewed directly inside the app.**
(I originally planned to implement this, hence the name.)

## Why?

Steam's builtin screenshot manager started crashing a lot for me after they overhauled the UI.
And every time it crashes, it takes all of Steam down with it.

## How?

When started for the first time, you are prompted to enter the full path to the screenshot directory of any steam app.
To do so, just open the Steam screenshot manager, select any game, and click the folder icon at the top to open the
screenshot directory.
Copy the full path of that directory into this application.

Once supplied with a path, the program will search for other screenshot folders.
The path cannot be hardcoded because it contains your Steam UserID.
Also steam's directory structure is very weird in general.
For me, all screenshots are contained in subdirectories of a folder named `760`.
No idea if that is true for everyone.

After finding all screenshot folders the program tries to translate the folder names, which are app ids, to the apps'
names.
This is done using Steam's appdetails api (<https://store.steampowered.com/api/appdetails>).
The filter `packages` is used to reduce network usage when possible.
If it fails, another request using the filter `basic` is started.
For more details on this see [Why is the packages filter used?](#why-is-the-packages-filter-used).

If any apps cannot be resolved or multiple apps have the same name, you are prompted to resolve these apps manually.

## State of this Repository

The app is deemed feature-complete.
I am not planning to add any major features.

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

Unfortunately, using `packages` has 2 major drawbacks.

1. It can result in wrong names rarely.
   This occurs if an app's first purchase option is not `Buy <app name>`.
   An example for this is GTAV, whose name will be resolved as `Shark Cash Cards`.
   To fix such issues, see [Troubleshooting](#Troubleshooting).
2. It does not work for free apps. In such cases, a second request using `filters=basic` is done.

## Troubleshooting

### App List Empty

This is most likely related to the game-specific screenshot directory path being faulty.
Delete the file `config.json` (located in a directory called `storage` next to the `.exe`).
Start the app and it should prompt you to enter a path again.

### Wrong App Name

If an app name is resolved incorrectly (e.g. GTAV is called "Shark Cash Cards")  you can edit `cache.json`.
The file maps app ids to names.
The file is located in a directory called `storage` next to the `.exe`.
Close the app before editing `cache.json`.

If you resolved a name conflict manually but want to change the app name, you can edit `cache.json` (see above).
If you want to delete all manually set names, delete the `cache.json` file entirely.

### Other Problems

You're welcome to open an issue in this repository.
_It might take a long time for me to reply, and not all issue will necessarily get fixed._
If you want to take things into your own hands, this project is using the MIT license - you can fork and freely develop
it.

## Used Technologies

- [.Net (Core)](https://github.com/dotnet)
- [WPF](https://github.com/dotnet/wpf)
- [Material Design in XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- [Serilog](https://github.com/serilog/serilog)

## License

This program is released under the [MIT license](LICENSE).
