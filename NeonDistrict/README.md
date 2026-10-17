# NEON DISTRICT — Procedural Cyberpunk City

Unity **Built-in Render Pipeline** project (Unity **2022.3 LTS** recommended). Open the `NeonDistrict` folder as the project root.

## Open & Play

1. Install **Unity Hub** + **Unity 2022.3.x** (Built-in RP; no URP/HDRP package required).
2. **Add** → open this folder: `NeonDistrict/`
3. In the menu bar: **Tools → Neon District → Build City Scene**  
   (creates/updates `Assets/Scenes/NeonDistrict.unity`, wires refs, regenerates without duplicating the city)
4. Open `Assets/Scenes/NeonDistrict.unity` if it is not already open.
5. Press **Play**.

If the scene is missing systems, `CityBootstrap` creates camera/lighting/city at runtime.

## Controls

| Input | Action |
|-------|--------|
| WASD / Arrows | Move |
| Mouse | Look (cursor locked) |
| Space / Ctrl | Up / Down |
| Shift | Sprint |
| Mouse wheel | Adjust move speed |
| R | Reset camera |
| Escape | Toggle cursor lock |
| F | Toggle rain / wet streets |
| L | Toggle neon emissives |
| N | Snap day ↔ night |
| T | Pause / resume day-night time |

HUD title: **NEON DISTRICT** (phase, rain, neon, time status).

## Scripts

| Script | Role |
|--------|------|
| `CityBootstrap` | Runtime / editor wiring; generate on Awake |
| `CityGenerator` | ~200×200 city, ~12×12 blocks, roads, sidewalks, landmarks |
| `BuildingGenerator` | Towers, rooftops, batched emissive windows |
| `TrafficController` | Lane-loop traffic spawner |
| `ProceduralVehicle` | Waypoint follower; emissive head/taillights |
| `RainController` | Rain particles, wet roads, lightning flash |
| `DayNightController` | Fog, sun, ambient; N/T/L |
| `FreeFlyCamera` | Classic Input Manager fly cam |
| `CityHUD` | OnGUI overlay |
| `NeonDistrictMenu` | **Tools → Neon District → Build City Scene** |

## Notes

- Deterministic seed default: `1337` (on `CityBootstrap` / `CityGenerator`).
- Shared materials only; window glow is emissive geometry (no per-window Lights).
- Bloom is not added (no Post Processing stack packaged); enable in your editor if you already have it.
- No paid assets, downloads, or third-party packages.
