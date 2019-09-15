[![dev & player chat](https://discordapp.com/api/guilds/594032849804591114/widget.png?style=shield)](https://discord.gg/pQkF5HM)

[![Standard Gameplay](https://www.youtube.com/vi/3X5OVUh50rA/2.jpg)](https://www.youtube.com/watch?v=3X5OVUh50rA)
[![Running on .NET Core](https://img.youtube.com/vi/I5wOUcjP_MQ/2.jpg)](https://www.youtube.com/watch?v=I5wOUcjP_MQ)

# NeuroSonic
A high intensity song-effecting rhythm game based on a popular arcade rhythm gaming experience.

# Running
NeuroSonic makes use of .NET development tools which aren't in full releases yet. Follow the **Building** guide below for information on seting up that development environment. For this reason pre-built versions will not be distributed from this repo until the dev tools are in full releases which the average consumer should have installed on their end.

# Building
NeuroSonic targets .NET Standard 2.1 / .NET Core 3.0 so you'll need an up-to-date dev environment for those; the code occasionally makes use of modern features which aren't supported on .NET Framework versions or older .NET Core and Standard versions.

## Windows
On Windows as of August 2019 you'll need to download [Visual Studio 2019 Preview](https://visualstudio.microsoft.com/vs/preview/) which can be installed alongside current Visual Studio installations.
When installing, make sure you have the .NET Core cross-platform development workload and .NET Core 3.0 SDK individual components checked.

With a properly setup Visual Studio Preview installed, simply open the solution and build or run the desired projects.

## Non-Windows
I'm currently only a Windows-familiar developer myself so all I can say is do your best to set up a .NET Core 3.0 compatible development environment and go to town. Feel free to create a pull request with valid OS setup instructions for non-Windows builds!

Maybe JetBrains Rider? Does that support the preview stuff that Visual Studio does? Are there releases of .NET Core 3.0 outside of Visual Studio?
