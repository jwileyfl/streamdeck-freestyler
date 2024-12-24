# streamdeck-freestyler ![](https://www.freestylersupport.com/wiki/_media/fs_logo.gif)
An unofficial Freestyler DMX plugin for the Elgato Stream Deck

![](https://img.shields.io/static/v1?label=&message=Unofficial&color=red&style=plastic)</br>
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey?style=plastic)</br>
![Last Commit](https://img.shields.io/github/last-commit/jwileyfl/streamdeck-freestyler?style=plastic)</br>
![Contributers](https://img.shields.io/github/contributors/jwileyfl/streamdeck-freestyler?style=plastic)</br>
![Open Issues](https://img.shields.io/github/issues-raw/jwileyfl/streamdeck-freestyler?style=plastic)

# Purpose
To provide a solution that allows a user to control dmx lighting via Freestyler dmx with a StreamDeck device.

# References
Link to Freestyler Home Page: https://www.freestylerdmx.be</br>
Link to online reference for external control of freestyler: https://www.freestylersupport.com/wiki/external_control

Link to Elgato Website: https://www.elgato.com</br>
Link to online reference for stream deck plugin development: https://developer.elgato.com/documentation/stream-deck/sdk/overview/

# Dependencies

[streamdeck-client-csharp](https://github.com/TyrenDe/streamdeck-client-csharp)
[![NuGet version (streamdeck-client-csharp)](https://img.shields.io/nuget/v/streamdeck-client-csharp.svg?style=plastic-square)](https://www.nuget.org/packages/streamdeck-client-csharp)

[CommandLine Parser](https://github.com/commandlineparser/commandline)
[![NuGet version (CommandLineParser)](https://img.shields.io/nuget/v/CommandLineParser.svg?style=plastic-square)](https://www.nuget.org/packages/CommandLineParser)

[Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
[![NuGet version (Newtonsoft.Json)](https://img.shields.io/nuget/v/Newtonsoft.Json.svg?style=plastic-square)](https://www.nuget.org/packages/Newtonsoft.Json)

[System.Drawing.Common](https://www.nuget.org/packages/System.Drawing.Common/9.0.0)
[![NuGet version (System.Drawing.Common)](https://img.shields.io/nuget/v/System.Drawing.Common.svg?style=plastic-square)](https://www.nuget.org/packages/System.Drawing.Common)

# Installation
### Windows Commandline:
1. Download the MSI *(Install.msi)*
2. Open a console window and navigate to the folder where the MSI file resides
3. Execute the following command:
   ```bat
   msiexec /i Install.msi
   ```
### Windows Explorer:

1. Download the MSI *(Install.msi)*
2. Navigate to the folder where the MSI file resides, and double click to install

### Alternatively:

1. Download the streamdeck plugin file *(com.resnexsoft.freestyler.remote.streamDeckPlugin)*
2. Navigate to the folder where the streamdeck plugin file is located, and double click to install

# Screenshot
![StreamDeck Screenshot](https://github.com/jwileyfl/streamdeck-freestyler/blob/main/StreamDeckScreenshot.png?raw=true)
