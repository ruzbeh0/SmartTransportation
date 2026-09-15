# SmartTransportation

SmartTransportation is a Cities: Skylines II mod that automates transit route scaling and fares so routes can adapt to demand instead of staying static.

## What the mod does

- Adjusts ticket prices and number of vehicles for transit routes.
- Responds to peak and off-peak demand by increasing/decreasing service intensity over time.
- Lets you set behavior per transit mode and add custom routing rules through an in-game UI.

## Features by section

### Transit section

- **Per-mode controls** for Bus, Tram, Subway, and Train.
- **Ticket price automation**  
  Fares move up when vehicles are full (to reduce excess demand) and down when vehicles are empty (to encourage ridership).
- **Target occupancy**  
  The mod changes vehicle counts and fares to try to hit a target occupancy. If target values are not realistic for route length/capacity limits, actual occupancy may vary from the target.
- **Mode-level disable toggle**  
  Keep fare and vehicle automation off for a mode while leaving others active.
- **Min/Max vehicle scaling**  
  Adjust base game vehicle count limits by percentage above or below defaults.

### Settings section

- **Waiting Time Weight**  
  Uses waiting passengers and expected deboarding to estimate route pressure and occupancy.
- **Target Occupancy Threshold**  
  Controls tolerance around target occupancy before adjustments happen. Default is `10%`.
- **Write transit info to log**  
  Logs occupancy and vehicle-count decisions for troubleshooting.

### Custom rules

Available in the in-game SmartTransportation UI (top-left button). You can:

- View existing rules
- Add new custom rules
- See routes and assign rules
- Remove rules from the rule list

## Compatibility notes

- **Not compatible with Transport Policy Adjuster** (it adjusts min/max vehicles in a different way).
- Built to work with Cities: Skylines II in the 1.6.* series.

## Version and links

- **Mod ID:** 90264
- **Current mod version:** 1.0.6
- **Forum:** [Smart Transportation mod thread](https://forum.paradoxplaza.com/forum/threads/smart-transportation-mod.1704128/)
- **GitHub:** [github.com/ruzbeh0/SmartTransportation](https://github.com/ruzbeh0/SmartTransportation)
- **Discord:** [Channel link](https://discord.com/channels/1024242828114673724/1285954236647211130)
