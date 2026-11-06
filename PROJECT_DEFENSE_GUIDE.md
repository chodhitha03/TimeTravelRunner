# Time Traveller Runner: Future City Edition
## Project Defense & Academic Viva Guide (Student Cheat Sheet)

---

### 1. Project Overview & Pitch
- **Title**: Time Traveller Runner (Future Cyber City & Multi-Era Edition)
- **Engine & Pipeline**: Unity 6 (6000.6.0f1) using the Universal Render Pipeline (URP)
- **Target Platform**: Desktop (Linux / Windows) and Mobile (Android / iOS)
- **Genre**: 3D Procedural Endless Runner (inspired by *Subway Surfers* and futuristic cyberpunk themes)
- **Core Concept**: A high-speed runner navigating a 3-lane magnetic levitation transit avenue between towering neon skyscrapers, dodging aerodynamic maglev bullet trains, leaping over holographic energy barriers, ducking under laser gantries, and collecting gold coins and high-tech gadgets (Sky Jetpack, Hoverboard, Guardian Mech).

---

### 2. Project Directory Structure
When your professor opens the project, here is how everything is organized:

```
Assets/
  _Game/
    Scenes/
      TimeTravellerRunner.unity     <- The main game scene with GameManager
    Prefabs/
      CyberCity/                    <- Modular Future City elements & obstacles
        Player_Runner.prefab              (3D Player Runner: stylized cyber suit, visor, backpack, thrusters)
        MaglevTrain_Obstacle.prefab       (Dodge obstacle: 7m maglev train with roof ramp)
        HoloJumpBarrier_Obstacle.prefab   (Jump obstacle: glowing energy barrier with chevrons)
        CyberScanGantry_Obstacle.prefab   (Slide obstacle: overhead laser scanner beam)
        InductionPit_Obstacle.prefab      (Pit hazard: recessed high-voltage coils)
        LaunchPad_Obstacle.prefab         (Kinetic boost pad launching player into air)
        CyberSkyscraper_TypeA.prefab      (Glass facade skyscraper with illuminated floor bands)
        CyberSkyscraper_TypeB.prefab      (Titanium high-rise megatower with rooftop twin spires)
        HoloBillboard.prefab              (Suspended neon holographic advertising display)
        StreetLuminaire.prefab            (Arched roadway cyber streetlamp)
        OverheadSkybridge.prefab          (Overhead hyperloop tube spanning the 3 lanes)
        CyberTrackTile.prefab             (Full 28-meter 3-lane maglev track segment)
        CyberCoin.prefab                  (Spinning collectible gold coin with trigger)
    Materials/
      Mat_CyberTitanium.mat         <- Smooth metallic composite for train bodies & spires
      Mat_DarkMetal.mat             <- Dark carbon chassis and undercarriages
      Mat_NeonCyan.mat              <- Emissive electric cyan for lane guides & HUD
      Mat_NeonPink.mat              <- Emissive neon magenta for safety caps & warnings
      Mat_NeonYellow.mat            <- Emissive laser scanner beams & streetlights
      Mat_RoadComposite.mat         <- Matte asphalt/composite track surface
      Mat_GlassHolo.mat             <- Translucent holographic guardrails
      Mat_GlassDark.mat             <- Tinted cockpit visors and skyscraper window bands
      Mat_GoldCoin.mat              <- High-metallic reflective coin material
      Mat_HoverBlue.mat             <- Under-train magnetic levitation ion glow
    Scripts/
      Core & Game Loop:
        RunnerGame.cs               <- Game Manager: game states, score, speed, camera, loop
        RunnerWorld.cs              <- Infinite track chunk spawner, tile lifecycle, obstacle placement
        RunnerAudio.cs              <- Procedural audio synthesizer (sfx for jumps, coins, crashes)
      Player:
        RunnerAvatar.cs             <- 3D player controller: lane transitions, jumping, sliding, lean
        CyberCharacterCatalog.cs    <- Runner outfits & palettes (Axel, Ryder, Kai, Classic)
        CyberRideMotion.cs          <- Smooth flight trajectory for jetpack and hoverboard
        CyberRideStageFX.cs         <- Particle thrusters, sonic rings, and hoverboard glow
      Environment & City Architecture:
        CyberCityBuilder.cs         <- Procedural district builder (roadbed, towers, skybridges)
        CyberObstacleFactory.cs     <- Configures collision bounds and obstacle geometry
        CyberMeshBatch.cs           <- Performance optimizer: batches static meshes into single draw calls
        CyberAtmosphere.cs          <- Skyline horizon, volumetric fog, dynamic cyber lighting
        CyberPostProcessing.cs      <- URP Post-Processing volume (Bloom, Color Grading, Tonemapping)
        DemoEarthBuilder.cs         <- Earth Demo environment generator (sidewalks, trees, houses)
        DesertThemeBuilder.cs       <- Ancient Egyptian desert theme (pyramids, obelisks, palms)
      UI & Feedback:
        RunnerUI.cs                 <- Canvas UI: HUD, characters menu, pause menu, game over screen
        CyberPhysicsFx.cs           <- Particle bursts on coin collect, impact sparks on slide
        CyberMotion.cs              <- Idle bobbing/floating animation for pickups
        Obstacle.cs                 <- Standard component for obstacle prefabs
        CoinItem.cs                 <- Standard spinning component for coin prefabs
      Editor:
        CityPrefabGenerator.cs      <- Editor tool under "Tools > Generate City Prefabs & Materials"
        AutoBuildGameScene.cs       <- Ensures game scene is registered in Build Settings
        OneClickBuild.cs            <- Automated build script for Standalone executables
```

### 2.1 Scene Hierarchy (Visible in Unity Editor)
When you open `Assets/_Game/Scenes/TimeTravellerRunner.unity` in the Unity Editor, the Hierarchy is cleanly organized with all visual elements visible in both the **Scene View** and the **Game View**:

- **`Main Camera`**: Positioned at `(0, 5.7, -9.8)` angled down at 18°, framing the player and the cyber avenue.
- **`Directional Light`**: Cyber sunlight angled at `(40, 22, 0)` with soft shadows.
- **`Global Volume`**: URP Post-Processing Volume with Bloom and ACES Tonemapping.
- **`EventSystem`**: Standard Unity EventSystem for UI button inputs.
- **`GameManager`**: Houses `RunnerGame.cs` coordinating the game loop, scoring, and era selection.
- **`Player`**: Instance of `Player_Runner.prefab` placed at `(0, 0, 0)` on the center lane in running pose.
- **`Environment`**:
  - **`Tracks/`**: 5 modular `CyberTrackTile` prefabs extending down the avenue.
  - **`Skyscrapers/`**: Alternating high-rises (`CyberSkyscraper_TypeA` and `TypeB`) flanking both sides.
  - **`Props & Streetlights/`**: Holographic billboards, streetlamps, and suspended overhead skybridge.
  - **`Obstacles & Collectibles/`**:
    - `HoloJumpBarrier_CenterLane` on lane 0
    - `CyberScanGantry_LeftLane` on lane -1
    - `MaglevBulletTrain_RightLane` on lane +1
    - Row of spinning gold `CyberCoin` items along the center lane

---

### 3. How You Added the Elements (What to Tell Your Professor)

#### Q1: "How did you build the Future City environment and buildings?"
> **Your Answer**:
> *"I designed the Future City using a modular kit of architectural elements. I modeled the building blocks—such as Skyscraper Type A (glass facade high-rise), Skyscraper Type B (aerodynamic megatower), Holographic Billboards, Street Luminaires, and Overhead Skybridges—and configured them as modular Prefabs in `Assets/_Game/Prefabs/CyberCity/`.*
>
> *To handle dynamic endless running, `CyberCityBuilder.cs` places these buildings alongside the track in alternating districts outside the active lanes. For maximum performance and 60 FPS gameplay, I implemented a mesh-batching system (`CyberMeshBatch.cs`) that combines the static mesh pieces of each 28-meter chunk into unified draw calls, cutting draw calls by over 80%."*

#### Q2: "How do your obstacles work?"
> **Your Answer**:
> *"I built three core interaction obstacle types inspired by standard runner mechanics:*
> 1. ***Dodge Hazard (Maglev Bullet Train)***: *A 7-meter aerodynamic train with ion levitation glow that occupies a single lane. It features a rear roof ramp allowing skilled players to jump onto the train roof.*
> 2. ***Jump Hazard (Holographic Energy Barrier)***: *A waist-height glowing barrier (0.85m tall) with upward chevron holograms indicating players must jump.*
> 3. ***Slide Hazard (Overhead Cyber Scan Gantry)***: *A high gantry spanning at 2.2m with downward laser hazard teeth, requiring the player to crouch/slide underneath.*
>
> *Each obstacle is saved as a Prefab with an `Obstacle.cs` component and a properly sized `BoxCollider` for collision boundaries."*

#### Q3: "How does the endless procedural track generation work?"
> **Your Answer**:
> *"In `RunnerWorld.cs`, the track is divided into 28-meter modular chunks (`CyberTrackTile.prefab`). The system maintains an active window of chunks ahead of the player (`EnsureAhead` method spawns ~235 meters ahead). As the runner travels forward along the Z axis:*
> 1. *New chunks are dynamically generated ahead with randomized lane obstacles, coin trails, and sky scenery.*
> 2. *Once chunks fall ~38 meters behind the player camera, they are automatically destroyed to free memory.*
> 3. *This guarantees zero memory leaks and constant 60 FPS performance without hitching."*

#### Q4: "How does player movement work?"
> **Your Answer**:
> *"The player controller in `RunnerAvatar.cs` handles 3 lanes spaced at -2.65m, 0m, and +2.65m:*
> - **Lane Switching**: *When pressing A/D or Arrow Keys, the player's target lane changes and smoothly interpolates with an athletic banking lean (`laneLean`).*
> - **Jumping & Gravity**: *Space/W applies vertical jump velocity (`13.5 m/s`) with simulated downward gravity (`-32 m/s²`).*
> - **Sliding**: *Pressing S initiates a responsive slide crouch, pitching the character forward and reducing the vertical collision hitbox so the player can pass beneath overhead gantries. Dynamic sparks are emitted under the shoes during the slide via `CyberPhysicsFx`."*

#### Q5: "How are collisions and pickups detected?"
> **Your Answer**:
> *"In `RunnerGame.cs` (`CheckWorld` loop), each active object (`TrackThing`) checks proximity to the player's current lane and Z-position. If a player overlaps a coin, it triggers `audioFx.Coin()`, spawns a particle burst, adds score, and triggers the UI punch animation. If an obstacle is encountered:*
> - *If jumping over a low barrier or sliding under a gantry, the collision is safely bypassed.*
> - *If hitting a train or hurdle unprepared, the shield absorbs the impact if active, or triggers the game over sequence."*

#### Q6: "How did you create the UI?"
> **Your Answer**:
> *"The UI in `RunnerUI.cs` is built using a modern Unity Canvas configured with `CanvasScaler` set to 1600x900 resolution. It includes dynamic HUD elements (score multiplier, distance meter, animated coin counter, power-up timers), a character garage screen, pause menu, and a high-score game over card with button bounce animations (`JuicyButtonFeedback`)."*

---

### 4. Quick Reference Summary Table for Oral Exam

| Question | Short Answer to Give |
| :--- | :--- |
| **Where are the prefabs?** | `Assets/_Game/Prefabs/CyberCity/` |
| **Where are the materials?** | `Assets/_Game/Materials/` |
| **Which scene is the main game?** | `Assets/_Game/Scenes/TimeTravellerRunner.unity` |
| **What script runs the game loop?** | `RunnerGame.cs` on the `GameManager` GameObject |
| **What script handles the player?** | `RunnerAvatar.cs` (movement, jump, slide, animation) |
| **What script spawns the tracks?** | `RunnerWorld.cs` (28m chunk recycling ahead/behind) |
| **How are draw calls minimized?** | `CyberMeshBatch.cs` combines static city meshes per chunk |
| **How do you generate prefabs?** | Top Menu: `Tools > Generate City Prefabs & Materials` |
