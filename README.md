# Avoid the dark
Game during Brussels Game Jam 2025

## 1. Team

## 2. Core Concept

## 3. Virtual part

## 4. Physical integration
### 4.1. Switches


### 4.2. Launchpad
#### 4.2.1. Get signal
To use Launchpad, we use the Unity package: MidiJack.\
You can download it from [GitHub](https://github.com/keijiro/MidiJack?tab=readme-ov-file).\
Install it via the package manager (custom package).

#### 4.2.2 Send signal
To use the LEDs on the Launchpad, you need the RtMidi.core in the Unity project.\
Use NuGetForUnity (available from the Asset Store)\
Or download the library manually and import it

Enable support for native plugins in the Unity player settings\
    In `Edit` → `Project Settings` → `Player` → `Other Settings`, make sure that ‘Allow “unsafe” Code’ is enabled
