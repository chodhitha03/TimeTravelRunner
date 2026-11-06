# Time Traveller Runner

A 3D endless runner developed in **Unity 6 (6000.0+)** utilizing the **Universal Render Pipeline (URP)**. Players sprint across distinct eras—ranging from the high-tech Neo-Cyber City to ancient Earth and Desert frontiers—dodging obstacles, collecting energy coins, and activating sci-fi gadgets.

---

## Gameplay & Features

- **Dynamic 3-Lane Movement**: High-responsiveness lane switching, jumping, and sliding with responsive physics and coyote-time jump buffering.
- **Cyberpunk Transit Obstacles**:
  - **Maglev Bullet Trains**: High-speed oncoming obstacles with angled rear ramps that allow jumping onto the roof to sprint across carriages.
  - **Holographic Jump Barriers**: Laser barricades requiring timed leaps.
  - **Overhead Scan Gantries**: Low-clearance security scanners requiring slide maneuvers.
  - **Induction Pits & Anti-Grav Launch Pads**: Hazards that test reflexes and launch pads that catapult the player over hazards.
- **Sci-Fi Gadgets**:
  - **Sky Jet**: Lane-wide aerial flight path with sky-high coin chains.
  - **Phase Board**: Anti-gravity hoverboard granting double jump and kinetic shield protection.
  - **Guardian Mech**: Autonomous escort unit with automated hazard detection and clearing.
- **Multi-Era Environments**:
  - **Future Cyber City**: Maglev hyperloop roadbed flanked by skyscrapers, floating holographic billboards, and overhead skyways.
  - **Prehistoric Earth**: Lush primeval terrain and stone structures.
  - **Desert Outpost**: Arid canyons and industrial frontier installations.
- **Audio & VFX**: Procedural audio synthesizer (BGM and SFX without external dependencies), bloom emissive lighting, speed lines, and landing dust particles.

---

## Controls

| Action | Primary Key | Secondary Key |
| :--- | :--- | :--- |
| **Move Left / Right** | `A` / `D` | `Left` / `Right Arrow` |
| **Jump** | `Space` | `Up Arrow` / `W` |
| **Slide** | `S` | `Down Arrow` |
| **Cycle Gadget** | `G` | UI Button |
| **Pause / Resume** | `P` / `Escape` | Pause Button |

---

## Project Structure

```text
Assets/
└── _Game/
    ├── Scenes/
    │   └── TimeTravellerRunner.unity     # Main game scene
    ├── Scripts/
    │   ├── RunnerGame.cs                 # Main game loop, state, score & speed
    │   ├── RunnerWorld.cs                # Endless track chunk pooling & recycling
    │   ├── RunnerAvatar.cs               # Player physics, animations & lane steering
    │   ├── RunnerUI.cs                   # Responsive HUD, pause menu & game over screens
    │   ├── RunnerAudio.cs                # Procedural audio generator for BGM & SFX
    │   ├── RunnerArt.cs                  # Procedural mesh and material factory
    │   ├── Obstacle.cs                   # Obstacle metadata and collider component
    │   ├── CoinItem.cs                   # Rotating collectible pickup component
    │   ├── CyberCityBuilder.cs           # City track generation & architecture layout
    │   ├── CyberObstacleFactory.cs       # Obstacle geometry generation
    │   ├── CyberGadgets.cs               # Gadget abilities & mounted visual models
    │   ├── CyberRobotAI.cs               # Guardian Mech steering & evasion logic
    │   ├── CyberAtmosphere.cs            # Skybox, fog, and environmental lighting
    │   ├── CyberPhysicsFx.cs             # Speed lines, jump sparks & landing dust
    │   ├── CyberCharacterCatalog.cs      # Selectable runner character skins
    │   └── Editor/
    │       ├── AutoBuildGameScene.cs     # Scene auto-setup & build settings verification
    │       ├── CityPrefabGenerator.cs    # Prefab & material creation tool
    │       └── OneClickBuild.cs          # Standalone build automation scripts
    ├── Materials/                        # Standard URP Lit and Emissive PBR materials
    └── Prefabs/
        └── CyberCity/                    # Modular obstacle, building, and prop prefabs
```

---

## Getting Started

1. Open the project in **Unity 6 (6000.6.0f1 or newer)**.
2. In the Project window, navigate to `Assets/_Game/Scenes/TimeTravellerRunner.unity` and double-click to open it.
3. Press the **Play** button in the Unity Editor toolbar to start the game.
4. To regenerate modular prefabs and materials at any time, select `Tools > Generate City Prefabs & Materials` from the top menu.
# TimeTravelRunner
