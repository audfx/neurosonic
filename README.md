[![dev & player chat](https://discordapp.com/api/guilds/594032849804591114/widget.png?style=shield)](https://discord.gg/pQkF5HM)

[![Standard Gameplay](https://img.youtube.com/vi/3X5OVUh50rA/2.jpg)](https://www.youtube.com/watch?v=3X5OVUh50rA)
[![Running on .NET Core](https://img.youtube.com/vi/I5wOUcjP_MQ/2.jpg)](https://www.youtube.com/watch?v=I5wOUcjP_MQ)

# NeuroSonic
A high intensity song-effecting rhythm game based on a popular arcade rhythm gaming experience.

# Running
You'll need to grab [.NET Core 3.0](https://dotnet.microsoft.com/download) if you don't already have it; it should come with the latest releases of Windows as of 9/23/2019 and is available for Linux/MacOS as well.

The game is currently only able to run on Windows; it's designed in a cross-platform way but hasn't yet made the commitment to targeting Linux and Mac officially.

Worth noting: all of the debug menus can be navigated with a controller (except the binding menu). Most of the bindings are displayed in the lower corners of the screen, but some actions are notably missing. To cover all bases, here's a general list of what buttons you can expect to do what:
- BT-A and BT-B: navigate up/down the menu options.
- BT-C and BT-D: adjust selected options left/right.
- BT-C in File Select: navigate up a directory.
- Start: select an option or enter a menu.
- Back (if applicable to your controller): navigate up a menu or out of a selection.
- Hold FX-L: for some menus, increase/decrease the stepping of BT inputs. This happens in the File Browser to navigate more than one folder up or down at a time and in the configuration menu to change values faster or slower.

When you run the game for the first time, here's what you should do to get it set up for yourself:
- Go to `Input Method` and select how you'd like to play. Gamepad controllers are listed by name, controllers in keyboard/mouse modes will require the keyboard/mouse option instead.
- Go to `Input Configuration` to set up your bindings and then select `Configure Controller Bindings`, the only option. Use the arrow keys to navigate the displayed binding options. For button inputs, press Enter/Return on a binding and then the respective keyboard key or gamepad button to bind the primary slot. For mouse or analog inputs, press Enter/Return on a binding and either move the respective analog input, press X or Y on your keybord, or press 0 or 1 on your keyboard; moving the analog input will detect just that axis, but to avoid accidentally moving a mouse during binding pressing X/Y or 0/1 is currently required to set mouse axes.
- Go to `Configuration` to change some common settings. You can navigate this menu with your controller using BT-C/BT-D to select values for your configurations. Select your preferred HiSpeed method (Constant Mod is currently not a constant due to some ongoing changes,) select the HiSpeed value you expect to be comfortable with, ignore Video and Input offset for the time being, and adjust the laser colors if you choose.
- Enter the `Timing Calibration` menu thru `Configuration` to generate rough values for your Video and Input offsets. Press any BT on your controller or tap the Spacebar to the sound of the clicks to calibrate your Input offset and to the passing of the bars to calibrate your Video offset. Pressing FX-L/R or Left/Right arrows selects which of the two you're currently calibrating. You will likely need to adjust your offsets slightly as the calibrator isn't quite the same as playing the game for real. In future, the calibration will mirror gameplay much closer making the values it gives more accurate to your genuine play experience. Press Start or Enter/Return to confirm your calibration or Back or Escape to cancel without saving.

Between choosing your `Input Method` and actually configuring it, you might need to restart the game if you selected a gamepad input device. NeuroSonic might have an issue actually using the gamepad when you switch to it for reasons I'm not 100% of yet. A game restart fixes the issue.

After you're configured, go to the `Chart Management` menu and select `Convert KSH Charts and Open Selected`. This name might be kind of cryptic, but what it does is ask you to browse for a .ksh file you'd like to play. It will then convert that entire folder, all .ksh charts inside it and their associated files, into the format that NeuroSonic understands. The converted chart files are placed inside the NeuroSonic installation directory by default and the process does not alter the original charts. This is currently done because there is no complete chart selection screen as you'd expect from a game of this nature. For the time being, you must navigate to each chart individually in the file browser and select the chart file to play it.

The chart selection menu is one of the next things in development! Some gameplay UI and the results screen are taking priority as easier things to work on that develop the internals better for creating the chart selection menu afterwards. Please be patient :)

Issues you may run into that are being worked on:
- Chart stops do not function correctly at all, the chart will pop in pretty strangely as they pass.
- BPM changes are not sudden, they're fluid. This might even be considered a feature, though it was unintented and will likely become a toggle option in the future.
- The built-in file browser may not retain the last folder you visited accurately. I'm not sure why, actually.
- If you try to select a chart which uses the same audio file as the last chart you played, if you did so too quickly it might crash the game because the operating system thinks the game is still using it and wont allow it to open twice. This is probably an easy fix, but happens infrequentyly enough that it hasn't been fixed yet.

# Building
NeuroSonic targets .NET Standard 2.1 / .NET Core 3.0.

## Windows
Download [Visual Studio Community 2019](https://visualstudio.microsoft.com/) version 16.3. When installing, make sure you have the .NET Core cross-platform development workload and .NET Core 3.0 SDK individual components checked.

With a properly setup Visual Studio Preview installed, simply open the solution and build or run the desired projects.

## Non-Windows
I'm currently only a Windows-familiar developer myself so all I can say is do your best to set up a .NET Core 3.0 compatible development environment and go to town. Feel free to create a pull request with valid OS setup instructions for non-Windows builds!

Maybe JetBrains Rider? Does that support the preview stuff that Visual Studio does? Are there releases of .NET Core 3.0 outside of Visual Studio?
