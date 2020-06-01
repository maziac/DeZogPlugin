# Changelog


## 1.2.0
- Changed to DZRP 1.2.0: CMD_SET_SLOT.
- Experimental message parsing of GetSpritePatterns in Tick().

## 1.1.0
- Assembly info updated.
- Clear any pending interrupt at startup.
- Changed to DZRP 1.1.0.


## 1.0.0
- Supports CSpect 2.12.26 ClearAllBreakpoints.
- Changed to DZRP 1.0.0.

## 0.10.0
- Registers I and R can be changed now.
- Corrected retrieving of register HL', I, R and IM.
- 'CSpectDebuggerVisible' setting removed.

## 0.9.0
- DZRP v0.4.0: Changes to CMD_CONTINUE.
- Lock introduced which seems to solve the "run-away" problem.

## 0.8.0
- Fixed a mix of response and notification problem (asynchronous problem)
- DZRP v0.3.0: Changed commands to obtain the clip window/sprite priority.
- Reset breakpoints when connection is lost.

## 0.7.0
- DZRP v0.2.0: PAUSE notification returns breakpoint address instead of breakpoint ID.

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
