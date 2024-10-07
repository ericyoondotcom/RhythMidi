# RhythMidi
An SDK for making Rhythm games in Unity.

## Getting Started
Download the `RhythMidi` folder and open it in Unity. Open the example scene in `RhythMidi/Assets/Example_DDR/Scenes/Example_DDR.unity`.

## Basic Usage
1. Create a new GameObject in the scene and attach the `RhythMidiController` script.
2. Create your own script, similar to `DDRLoader` in the example project.
3. Place your charts in `StreamingAssets/Charts`. `RhythMidiController` will automatically load all charts in this folder. If you don't want to automatically load charts, unset the `chartsPath` variable in Inspector and call `LoadChart` manually. 
4. Set up Note Notifiers, depending on your use case. Call `CreateNoteNotifier`, and add a callback to the returned `OnNote` Unity Action.
5. Call `PrepareChart` with the name of the chart you want to play.
6. Call `PlayChart` to start the game.

### Note Notifiers
A note notifier is a wrapper around a `UnityAction`. The Unity Action will be called `timeInAdvance` seconds before a note is supposed to be hit.

The most basic use of note notifiers is to set `timeInAdvance` to 0. The corresponding Unity Action will be called exactly when a Midi note is triggered.

Say you want to instantiate a falling note sprite, _x_ seconds before the note reaches the hit zone. You would set `timeInAdvance` to _x_, and instantiate the note whenever the Unity Action is called.

You can use note notifiers to account for "coyote time" of a rhythm game. However, you can use the `HitWindow` class instead to handle this in a much simpler way.

### Hit Window
A common use case is if you want to detect if the player performed an input while a note was in a "valid window," i.e. between _x_ millis before it triggers and _x_ millis after it triggers.

You can use the `HitWindow` class for this. Attach your hit window behavior to a component and supply it with a reference to the `RhythMidiController`.

When the user performs an input, call `CheckHit(int noteNum)`. The note number should correspond to the midi note number of the note you want to check. It will also, by default, delete the note from the valid window, assuming it has been consumed (to prevent double-hits).

If you want to punish players for missing notes, attach a callback to the `OnNoteMissed` Unity Action. This will be called whenever a note exits this hit window without being consumed.
