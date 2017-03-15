# Pi Pie Rube Goldberg Device
By [Ayzenberg](http://www.ayzenberg.com)

<img src="https://raw.githubusercontent.com/brandsoulmates/pi-pie/master/photos/rube_ui.jpg" style="width:100%">

# Parts list

* Pi 3, Power supply, SD card, Windows 10 IOT Core
* Adafruit 16-Channel 12-bit PWM/Servo Driver - I2C interface https://www.adafruit.com/product/815
* Wire or servo extension cables
* Small push-pull solenoid
* 1N4001 Diode (for solenoid circuit)
* TIP120 Power Darlington Transistor (for solenoid circuit)
* 9 VDC 1000mA regulated switching power adapter for solenoid (would recommend higher, up to 24V)
* Female DC Power adapter 
* Arcade Button
* Standard servo
* PowerSwitch Tail II PN 80136 Normally Open http://www.powerswitchtail.com/Pages/default.aspx
* 5V power supply or 4 AA battery holder (6V for servo)
* Two half-size breadboards
* Two 5mm LEDs
* Two 220 Ohm resistors and extension wire
* Optional: 5mm Chromed Metal Wide LED Holders
* 16 x 32 RGB panel https://www.adafruit.com/product/420

Misc:
* HDMI Monitor
* USB Keyboard
* Boxes
* Dominos
* Mousetrap
* Stands & Hardware
* Pinball
* Marble rail kit
* Fishing line
* Paper airplane
* Desktop fan
* Whipped cream
* Raspberry Pie

# Setup
<img src="https://raw.githubusercontent.com/brandsoulmates/pi-pie/master/photos/wiring.jpg" style="width:100%">

You can set up Windows IOT Core with a pi connected to the internet and an SD card with NOOBS on it. https://www.raspberrypi.org/downloads/noobs/
Install Visual Studio and Windows IoT Core Dashboard with the instructions here: https://developer.microsoft.com/en-us/windows/iot/GetStarted

LED 1 is connected to Broadcom GPIO pin 5 and ground. LED 2 is connected to pin 6 and ground. The Solenoid is connected to pin 17 and ground.
Solenoid circuit: http://playground.arduino.cc/uploads/Learning/solenoid_driver.pdf

The button is connected to pin 27 and ground. The PowerSwitch relay is connected to pin 22 and ground. And the Arduino is connected to pin 26 and ground.
The Arduino code is in [Arduino/happypiday/happypiday.ino](Arduino/happypiday/happypiday.ino) and is just modified sample code from Adafruit. The sign circuit can be found here: https://learn.adafruit.com/32x16-32x32-rgb-led-matrix/overview

The MCP3002 is connected to the proximity sensor. The MCP3002 is connected to CE0 and the pi's SPI pins.
MCP3002 ADC circuit:
http://www.learningaboutelectronics.com/Articles/MCP3002-analog-to-digital-converter-ADC-to-Raspberry-Pi.php

The servo driver is connected to the pi via the I2C pins. We use the 16th channel (channel 15) on the servo driver to connect to the motor.

Open the Rube2.sln file in visual studio choose the ARM architecture in the dropdown and Remote machine to compile and deploy. Your pi should show up as a remote machine if it's connected to the same network and running Windows 10 IoT core. 

<img src="https://raw.githubusercontent.com/brandsoulmates/pi-pie/master/photos/remotemachine.png" style="width:100%">

The pi will display a point-and-click UI via its HDMI output for starting and stopping the device, along with statuses for the solenoid, button, proximity sensor, and servo.





