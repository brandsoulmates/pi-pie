// scrolltext demo for Adafruit RGBmatrixPanel library.
// Demonstrates double-buffered animation on our 16x32 RGB LED matrix:
// http://www.adafruit.com/products/420

// Written by Limor Fried/Ladyada & Phil Burgess/PaintYourDragon
// for Adafruit Industries.
// BSD license, all text above must be included in any redistribution.

#include <Adafruit_GFX.h>   // Core graphics library
#include <RGBmatrixPanel.h> // Hardware-specific library

// Similar to F(), but for PROGMEM string pointers rather than literals
#define F2(progmem_ptr) (const __FlashStringHelper *)progmem_ptr

#define CLK 8  // MUST be on PORTB! (Use pin 11 on Mega)
#define LAT A3
#define OE  9
#define A   A0
#define B   A1
#define C   A2
// Last parameter = 'true' enables double-buffering, for flicker-free,
// buttery smooth animation.  Note that NOTHING WILL SHOW ON THE DISPLAY
// until the first call to swapBuffers().  This is normal.
RGBmatrixPanel matrix(A, B, C, CLK, LAT, OE, true);
// Double-buffered mode consumes nearly all the RAM available on the
// Arduino Uno -- only a handful of free bytes remain.  Even the
// following string needs to go in PROGMEM:

const char str[] PROGMEM = "HAPPY PI DAY!";
int    textX   = matrix.width(),
       textMin = sizeof(str) * -12,
       hue     = 0;
       int j = 0;

static const uint16_t PROGMEM ballcolor[3] = {
  0x0080, // Green=1
  0x0002, // Blue=1
  0x1000  // Red=1
};

const int buttonPin = 10;
int buttonState = 0; 
int lastButtonState = LOW;
unsigned long lastDebounceTime = 0;  // the last time the output pin was toggled
unsigned long debounceDelay = 7000;    // the debounce time; increase if the output flickers
boolean messageShown = false;

void setup() {
  Serial.begin(9600);
  matrix.begin();
  matrix.setTextWrap(false); // Allow text to run off right edge
  matrix.setTextSize(2);
  pinMode(buttonPin, INPUT);

  byte i;
  //showMessage(); 
}

void showMessage() {
  for(j = 0; j < 370; j++)
  {
  // Clear background
  matrix.fillScreen(0);
  //draw a pi
  /*
  matrix.drawPixel(15,8, matrix.Color888(21,29,11));
  matrix.drawPixel(16,8, matrix.Color888(50,68,27));
  matrix.drawPixel(15,9, matrix.Color888(42,71,25));
  matrix.drawPixel(16,9, matrix.Color888(195,255,108));
  matrix.drawPixel(17,9, matrix.Color888(40,57,22));
  matrix.drawPixel(14,10, matrix.Color888(11,0,3));
  matrix.drawPixel(15,10, matrix.Color888(75,17,26));
  matrix.drawPixel(16,10, matrix.Color888(74,61,31));
  matrix.drawPixel(17,10, matrix.Color888(62,102,36));
  matrix.drawPixel(18,10, matrix.Color888(167,240,91));
  matrix.drawPixel(19,10, matrix.Color888(43,59,23));
  matrix.drawPixel(13,11, matrix.Color888(21,1,7));
  matrix.drawPixel(14,11, matrix.Color888(161,4,49));
  matrix.drawPixel(15,11, matrix.Color888(167,0,50));
  matrix.drawPixel(16,11, matrix.Color888(97,0,21));
  matrix.drawPixel(17,11, matrix.Color888(99,33,36));
  matrix.drawPixel(18,11, matrix.Color888(112,164,62));
  matrix.drawPixel(19,11, matrix.Color888(29,40,14));
  matrix.drawPixel(13,12, matrix.Color888(73,2,22));
  matrix.drawPixel(14,12, matrix.Color888(147,4,45));
  matrix.drawPixel(15,12, matrix.Color888(141,4,43));
  matrix.drawPixel(16,12, matrix.Color888(146,4,45));
  matrix.drawPixel(17,12, matrix.Color888(164,0,47));
  matrix.drawPixel(18,12, matrix.Color888(1,0,0));
  matrix.drawPixel(14,13, matrix.Color888(98,3,30));
  matrix.drawPixel(14,13, matrix.Color888(147,4,45));
  matrix.drawPixel(15,13, matrix.Color888(149,4,46));
  matrix.drawPixel(16,13, matrix.Color888(131,3,40));
  matrix.drawPixel(17,13, matrix.Color888(162,1,47));
  matrix.drawPixel(18,13, matrix.Color888(7,2,3));
  matrix.drawPixel(13,14, matrix.Color888(3,0,1));
  matrix.drawPixel(14,14, matrix.Color888(147,4,45));
  matrix.drawPixel(15,14, matrix.Color888(187,4,57));
  matrix.drawPixel(16,14, matrix.Color888(145,3,45));
  matrix.drawPixel(17,14, matrix.Color888(76,2,23));
  matrix.drawPixel(18,14, matrix.Color888(3,0,1));
  matrix.drawPixel(14,15, matrix.Color888(30,1,9));
  matrix.drawPixel(15,15, matrix.Color888(36,1,11));
  matrix.drawPixel(16,15, matrix.Color888(63,2,19));

  //fill
  matrix.drawPixel(13,13, matrix.Color888(131,3,40));
  matrix.drawPixel(13,14, matrix.Color888(131,3,40));
  */

  // Draw big scrolly text on top
  matrix.setTextColor(matrix.ColorHSV(hue, 255, 255, true));
  matrix.setCursor(textX, 1);
  matrix.print(F2(str));

  // Move text left (w/wrap), increase hue
  if((--textX) < textMin) textX = matrix.width();
  //hue += 7;
  //if(hue >= 1536) hue -= 1536;

  // Update display
  matrix.swapBuffers(false);
  delay(15);
  j++;
  //Serial.print("j: ");Serial.print(j);
  }
    
    //reset
    matrix.fillScreen(0);
    matrix.swapBuffers(false);
    textX   = matrix.width();
    return;
}

void loop() {
  if(digitalRead(buttonPin) == HIGH && !messageShown)
  {
    //Serial.println("Button HIGH");
    showMessage();
    messageShown = true;
    lastDebounceTime = millis();
    
  }

  if ((millis() - lastDebounceTime) > debounceDelay) {
      messageShown = false;
  }
}
