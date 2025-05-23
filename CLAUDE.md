# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Exodyssey 2D is a sci-fi turn-based strategy roguelike built in Unity 6000.0.39f1. It's a 2D survival game where players scavenge resources while being hunted by enemies on procedurally generated maps.

## Development Commands

**Unity Project**:
- Open in Unity 6000.0.39f1 or compatible version
- Main scene: `Assets/Scenes/Main.unity`
- Build settings configured for Mac/Windows standalone

**No package manager** - This is a standard Unity project without npm/package.json

## Architecture Overview

### Core Systems
- **Singleton Pattern**: `GameManager` is the central controller, `SoundManager` handles audio globally
- **Turn-Based Flow**: Player and enemy turns managed by `GameManager` with `TurnTimer` enforcement
- **Energy System**: Actions consume energy, limiting moves per turn based on profession

### Key Components
- **GameManager**: Central hub coordinating all systems, level progression, turn flow, UI management
- **Player**: Health/energy management, inventory, A* movement, vehicle interaction  
- **Enemy**: AI pathfinding, turn-based movement, stun mechanics, range attacks
- **Inventory**: Drag-and-drop system with rarity-based items and weapon durability
- **MapGen**: Template-based procedural wall generation in quadrants
- **AStar**: Pathfinding algorithm used by all moving entities

### Data Architecture
- **Info Classes**: `ItemInfo`, `EnemyInfo`, `VehicleInfo` contain stats separate from behavior
- **Profession System**: Affects player abilities and energy/health bonuses
- **Weighted Rarity**: `WeightedRarityGeneration` drives procedural item spawning

### File Organization
- `Assets/Scripts/`: All C# game logic
- `Assets/Prefabs/`: GameObject templates for items, enemies, vehicles
- `Assets/Resources/Sprites/`: All game sprites and UI elements
- `Assets/Animations/`: Animation controllers and clips for player/enemies
- `Assets/Audio/`: Sound effects and ambient audio

## Development Notes

### Making Changes
- **Scripts**: Follow existing patterns - Info classes for data, MonoBehaviours for logic
- **Prefabs**: Items use `Item.cs` component with `ItemInfo` scriptable objects
- **Movement**: All entity movement goes through `AStar` pathfinding system
- **Turn System**: Actions must respect energy costs and turn timer limits
- **UI**: Inventory uses drag-and-drop system in `InventoryUI.cs`

### Tilemap System
- Ground tiles placed procedurally with random variants
- Wall generation uses template-based quadrant system
- Tile areas show movement range visually
- Collision detection uses tilemap bounds

### Audio Integration
- `SoundManager` singleton handles all audio playback
- Audio clips referenced in prefabs and triggered by game events
- Ambient audio managed separately from sound effects