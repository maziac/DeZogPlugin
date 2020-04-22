# Changelog

## 0.6.0
- Getting sprites clip window fixed.
- Added watchpoints.

## 0.5.0
- Using CSpect 2.12.22.

## 0.4.0
- CMD_GET_CONFIG changed to CMD_INIT
- CMD_SET_BORDER added.

## 0.3.0
- GetSpritesPalette functionality added.
- Corrected break reason.
- Handling of breakpoint ID corrected.

## 0.2.0
- Changed to new CSpect API 2.12.20.
- New config parameter "CSpectDebuggerVisible".
- Sprite access added.
- Corrected sending. Length was off by 1.
- Removed one byte from pause notification.

## 0.1.0
Initial version.
The plugin is working with CSpect v2.12.17.
The state is: it is working but still experimental.

What should work is:
- Continue/StepInto/StepOver/StepOut (see known problems)
- Lite reverse stepping
- Memory display
- Register display
- Setting breakpoints

What's not working/not tested:
- Breakpoint conditions (not tested)
- Watchpoints
- Sprite display
