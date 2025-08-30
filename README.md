

# 2D Platformer Prototype (C# WinForms)

A simple **2D side-scrolling platformer** built with **C# and Windows Forms**.
The player can run, jump, collect coins, avoid enemies, and explore an **infinite procedurally generated level**.

---

## Features

* **Player movement**: Run, jump, and collide with platforms.
* **Procedural generation**: Infinite platforms are generated using random rules with adjustable difficulty.
* **Enemies**: Patrolling enemies that move back and forth.
* **Collectibles**: Coins scattered across platforms.
* **Respawn system**:

  * If the player falls, they respawn at the last checkpoint.
  * Players can create custom checkpoints by spending **10 coins** (press `Space`).
* **Camera system**: Follows the player smoothly as they move forward.

---

## Controls

* `A` , `→`   → Move left
* `D` , `←` → Move right
* `w` , `↑` → Jump
* `Space` → Create checkpoint (if ≥ 10 coins)

---

## How It Works

* The game loop is driven by a `Timer` running at \~60 FPS.
* Collision detection ensures the player interacts with platforms correctly.
* Procedural level generation creates chunks dynamically as the player progresses.
* Old platforms, enemies, and coins are removed when they move too far behind the camera.

---

## Requirements

* Windows with .NET Framework (or .NET 6/7 with WinForms support).
* Visual Studio (or any C# IDE) to build and run the project.

---

## Getting Started

1. Clone the repository:

   ```sh
   git clone https://github.com/your-username/2d-platformer-winforms.git
   ```
2. Open the project in Visual Studio.
3. Build and run.

---

## Future Improvements

* Animated sprites for player and enemies.
* More platform types (moving, breaking, slippery).
* Background music and sound effects.
* Improved procedural generation using noise functions.

