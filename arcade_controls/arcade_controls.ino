#include <BleKeyboard.h>

BleKeyboard bleKeyboard("Arcade Controller", "ESP32", 100);

const int buttonPin_p2_attack = 33;
const int buttonPin_p2_move_right = 25;

int lastButtonState_p2_attack = HIGH;
int lastButtonState_p2_move_right = HIGH;

void setup() {
  Serial.begin(19200);
  Serial.println("Starting BLE work!");
  bleKeyboard.begin();

  pinMode(buttonPin_p2_attack, INPUT_PULLUP);
  pinMode(buttonPin_p2_move_right, INPUT_PULLUP);
}

void loop() {
  if (bleKeyboard.isConnected()) {
    int currentButtonState_p2_attack = digitalRead(buttonPin_p2_attack);

    if (currentButtonState_p2_attack != lastButtonState_p2_attack) {
      
      if (currentButtonState_p2_attack == LOW) {
        bleKeyboard.press('w');
        Serial.println("W ingedrukt");
      } 
      else {
        bleKeyboard.release('w');
        Serial.println("W losgelaten");
      }

      lastButtonState_p2_attack = currentButtonState_p2_attack;
      delay(15);
    }

    
    int currentButtonState_p2_move_right = digitalRead(buttonPin_p2_move_right);
    if (currentButtonState_p2_move_right != lastButtonState_p2_move_right) {
      
      if (currentButtonState_p2_move_right == LOW) {
        bleKeyboard.press('a');
        Serial.println("a ingedrukt");
      } 
      else {
        bleKeyboard.release('a');
        Serial.println("a losgelaten");
      }

      

      lastButtonState_p2_move_right = currentButtonState_p2_move_right;
      delay(15);
    }

  }
}