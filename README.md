# Avoid the dark
Game during Brussels Game Jam 2025

## 1. Team
Team Of 5 people :\
Git & Tech Manager : Antonin De Breuck [Website](https://antodb.be)\
Game Design & Programer : Joe Nammour [mail](mailto:joeduliban79@gmail.com)\
Sound Design : Loïs Lajarreti [itch.io](https://billy_hell.itch.io)\
Musician : Adam Courtiol [linktree](https://linktr.ee/Terdix)\
3D Artist : Shegan Tavares Das Neves | [Artstation](https://www.artstation.com/demsheg)\
## 2. Core Concept
Make a game were you remember your childhood (not always pleasant...)
## 3. Virtual part
Game Engine : Unity 6
Versioner : Github
3D Software : Blender
Animation website : Mixamo
## 4. Physical integration
Launchpad => [here](https://novationmusic.com/products/launchpad-mini-mk3)

### 4.1. Switches
Esp 32 switch => [here](https://www.amazon.com.be/-/nl/Espressive-Development-Bluetooth-WROOM32-NodeMCU/dp/B07K68RQTS/ref=asc_df_B07K68RQTS/?tag=begogshpadd0d-21&linkCode=df0&hvadid=714430882267&hvpos=&hvnetw=g&hvrand=4144347134565506489&hvpone=&hvptwo=&hvqmt=&hvdev=c&hvdvcmdl=&hvlocint=&hvlocphy=9195606&hvtargid=pla-862519322535&psc=1&mcid=3907c0ab5c033b51848cae096e865b8c&gad_source=1)
#### 4.1.1. MQTT Brocker
For MQTT, we wanted Unity to be the brocker (server) but that's not possible, so we wanted to host the brocker on a computer and have Unity and the ESP32s with the switches connect to it.\
Antonin, the dev for this part, had a computer with firewalls that were too powerful, so the end nodes couldn't connect to it. 

##### Solution:
We switched to a remote Raspberry Pi 5 (pinging Antonin's house).

##### Technique:
We wanted everyone to be able to reproduce the project so we decided that the brocker could be hosted.\
We tested [Paho](https://pypi.org/project/paho-mqtt/) with a python code that is [here](https://github.com/AntoDB/avoid_the_dark/tree/main/PhysicalIntegration/MQTT%20brocker/paho-mqtt-broker.py) but causing too mush issues so we move to [Mosquitto](https://mosquitto.org/).\
_If you realy want to use a python script with Paho, first you need to install python on your ‘server’ (a computer can act as a server). After that, read the libraries part._

You don't need to run both at the same time (script with Paho and Mosquitto), and it wouldn't be possible anyway because they're both trying to use port 1883.\
If you continue to use Mosquitto, you can simply ignore the Python script. Mosquitto is much more reliable, powerful and well maintained as an MQTT broker.\
Another advantage is that Mosquitto will start automatically when you restart your Raspberry Pi if you haven't disabled it, so you can be sure that the MQTT service is always available.\

##### Here are the libraries to install:
[Paho (local)](https://pypi.org/project/paho-mqtt/) : `pip install paho-mqtt` or pip3 for python3\
Paho (remotely, which is what we had to do): `sudo apt install python3-paho-mqtt`\
Paho was causing problems, so we switched to Mosquitto for the brocker.
[Mosquitto](https://mosquitto.org/) : `sudo apt install mosquitto mosquitto-clients`

##### Mosquitto configuration:
```bash
sudo nano /etc/mosquitto/mosquitto.conf

# Add these lines at the end of the file
listener 1883 0.0.0.0
allow_anonymous true

# Save (CTRL + X, Y, ENTER) and restart Mosquitto
sudo systemctl restart mosquitto
```

#### 4.1.2. MQTT ESP32-1 Client
ESP32s are microcontrollers. They are mini programmable electronic cards. If you're familiar with Arduino Genuino, ESPs are the same thing, but more powerful and with a WIFI and Bluetooth chip.\
We use them as physical integration with switches in real life.

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
