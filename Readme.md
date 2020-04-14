﻿# DeZog Cspect Plugin

The Dezog plugin allows to connect [DeZog](https://github.com/maziac/DeZog) with [CSpect](http://www.cspect.org).
I.e. you can use the DeZog IDE and run/debug your program in CSpect.

This plugin establishes a listening socket (default port 11000).
DeZog will connect to this socket when a debug session is started.


# State

The plugin is working with CSpect v2.12.17.
The state is: it is working but still experimental.

What should work is:
- Continue/StepInto/StepOver/StepOut (see "Known Problems")
- Lite reverse stepping
- Memory display
- Register display
- Setting breakpoints

What's not working/not tested:
- Breakpoint conditions (not tested)
- Watchpoints
- Sprite display


# Plugin Installation

The plugin can be compiled with Visual Studio (19). It has been built with VS on a Mac.
Most probably this will work on Windows as well but has not been tested.

You can find precompiled DLLs [here](https://github.com/maziac/DeZogPlugin/releases).

Place the DeZogPlugin.dll (and the DeZogPlugin.dll.config) in the root directory of CSpect (i.e. at the same level as the CSpect.exe program).
Once you start CSpect it will automatically start the plugin.
If everything works well you will see a message in the console: "DeZog plugin started."

For the DeZog configuration see [DeZog](https://github.com/maziac/DeZog).
Basically you need to create a launch.json with and set the port (if different from default).

You can start CSpect without any (Z80) program. The program is being transferred by DeZog when the debug session is started.


# Build

Each new version of CSpect the new Plugin.dll needs to be referenced.
In the Cpect directory a link can be made (ln) to the dll, so it is not required each time to copy the dll.
(Note: macOS link via desktop is not working, use commandline "ln -s".)


# Socket Usage

## Socket Configuration

The DeZog plugin starts to listen for a socket connection on startup at port 11000.
You can change the used port by providing a different port number in the DeZogPlugin.dll.config file.


## Socket Protocol

Please see [DZRP-DeZog Remote Protocol](https://github.com/maziac/DeZog/blob/master/design/DeZogProtocol.md).


# Known Problems

- StepOut:
  - A break/pause during StepOut is not working.
  - Does not break if running over a breakout.
  - Flickering of CSpect display.
- Break reasons not displayed.


# Acknowledgements

The plugin is based on the example plugin from Mike Dailly's [CSpect](http://www.cspect.org).
It also uses some code from Threetwosevensixseven's [CSpectPlugins](https://github.com/Threetwosevensixseven/CSpectPlugins).



