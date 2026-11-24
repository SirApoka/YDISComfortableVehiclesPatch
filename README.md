# You Drive, I Sleep + Comfortable Vehicles Compatibility Patch

![You Drive, I Sleep + Comfortable Vehicles Compatibility Patch](About/Preview.png)

Lets pawns inside vehicles regain sleep while a vehicle caravan is stopped, when [You Drive, I Sleep](https://steamcommunity.com/sharedfiles/filedetails/?id=3324430833) and [Comfortable Vehicles](https://steamcommunity.com/sharedfiles/filedetails/?id=3677424409) are both active.

Unofficial patch, not affiliated with or endorsed by the authors of those mods. Preview art adapted from the previews of You Drive, I Sleep by SpaceMoth and Comfortable Vehicles by Eclair155.

## Why this exists

On long trips with my vehicle caravans, my colonists never regained sleep while the caravan was stopped for the night, the driver included. With Comfortable Vehicles alone, drivers and gunners already miss out; with You Drive, I Sleep also loaded, nobody inside a vehicle rests.

## What it does

- Comfortable Vehicles patches `Caravan_NeedsTracker.TrySatisfyRestNeed` to skip vanilla caravan rest for every pawn inside a vehicle, and handles rest itself. Its own rest logic steps aside for caravan members when You Drive, I Sleep is active, and never rests drivers or gunners.
- You Drive, I Sleep only gives rest while the caravan is moving, and never to the driver.
- Together, nobody inside a vehicle rests while the caravan is stopped.

This mod adds a Harmony postfix to `Caravan_NeedsTracker.TrySatisfyRestNeed`. When the caravan is stopped and the pawn is inside a vehicle, it applies the same rest vanilla would (bedroll effectiveness if one is assigned), unless another mod already rested that pawn on the same tick. While the caravan is moving it does nothing.

If You Drive, I Sleep or Comfortable Vehicles is not active, the patch disables itself and logs a warning. Without You Drive, I Sleep, Comfortable Vehicles already rests passengers, and this patch would stack on top of it.

## AI disclosure

This mod was coded with AI assistance. I directed the work, made the design decisions and tested it in my own game with about 500 mods.

## Requirements

- RimWorld 1.7
- Harmony
- Vehicle Framework
- You Drive, I Sleep
- Comfortable Vehicles

## Saves

Safe to add to or remove from an existing save. This mod stores no data.

## Building

Requires the .NET 10 SDK. From `Source/`:

```
dotnet build -c Release
```

The `net48` target is written to `Assemblies/`. The `net10.0` target only checks the API against RimWorld 1.7.

## License

[Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)](LICENSE). You can share and adapt this mod for non-commercial purposes, as long as you credit SirApoka and indicate any changes.
