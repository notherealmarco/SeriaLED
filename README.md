# SeriaLED
Drive an RGBW LED strip attached to an Arduino connected via USB (or serial) and control it from Home Assistant.
#### For now it supports RGBW (and RGBWW) only, support for RGB will be added soon.

RGBW pins are: 3 (red), 5 (green), 6 (blue), 9 (white)

## Installation
Just flash the sketch, plug the Arduino via USB to the PC and run the software (for now only Windows is supported, if you have MAC OS or Linux you should run it with mono).
A console will appear and ask you for:
- Home Assistant MQTT broker's address
- Home Assistant MQTT broker's username
- Home Assistant MQTT broker's password
- MQTT Topic to use
- COM Port of the Arduino (you can easily find it in Arduino IDE).

## Home Assistant configuration
Add this in your configuration.yaml
```
light:
  - platform: mqtt
    name: '_any name for Home Assistant_'
    state_topic: "stat/_your MQTT topic_/POWER"
    command_topic: "cmnd/_your MQTT topic_/POWER"
    rgb_state_topic: "stat/_your MQTT topic_/COLOR"
    rgb_command_topic: "cmnd/_your MQTT topic_/COLOR"
    rgb_command_template: "{{ '%02x%02x%02x' | format(red, green, blue) }}"
    brightness_state_topic: "stat/_your MQTT topic_/DIMMER"
    brightness_value_template: "{{ value_json.brightness }}"
    brightness_command_topic: "cmnd/_your MQTT topic_/DIMMER"
    brightness_scale: 100
    color_temp_command_topic: "cmnd/_your MQTT topic_/CT"
    effect_command_topic: "cmnd/_your MQTT topic_/EFFECT"
    effect_state_topic: "cmnd/_your MQTT topic_/EFFECT"
    effect_list:
      - "None"
      - "Fade"
    qos: 1
    payload_on: "ON"
    payload_off: "OFF"
    retain: true
```
