# CSpect Plugin - Socket

This is a [CSpect](http://www.cspect.org) plugin that establishes a listenign socket.
DeZog will connect this socket when a debug session is started.


# State

Currently the plugin does connect the UART to a socket in RX and TX direction.
But it has not been tested yet.
I.e. this is an early development state. Don't expect it to work.


# Plugin Installation

The plugin can be compiled with Visual Studio (19). It has been built with VS on a Mac.
Most probably this will work on Windows as well but has not been tested.

Place the DeZogpPlugin.dll (and the DeZogpPlugin.dll.config) in the root directory of CSpect (i.e. at the same level as the CSpect.exe program).
Once you start CSpect it will automatically start the plugin.
If everything works well you will see a message in the console: "DeZogp plugin started."


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




# Acknowledgements

The plugin is based on the example plugin from Mike Dailly's [CSpect](http://www.cspect.org).
It also uses some code from Threetwosevensixseven's [CSpectPlugins](https://github.com/Threetwosevensixseven/CSpectPlugins).



