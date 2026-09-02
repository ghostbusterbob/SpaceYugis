#include <BleKeyboard.h>

BleKeyboard bleKeyboard("Arcade Controller", "ESP32", 100);

// --- PIN DEFINITIONS (Fill in your GPIO numbers) ---
const int buttonPin_p1_move_left  = 33; // P1 Left  -> 'a'
const int buttonPin_p1_move_right = 25; // P1 Right -> 'd'
const int buttonPin_p1_attack     = 26; // P1 Attack -> 'w'

const int buttonPin_p2_move_left  = 14; // P2 Left  -> KEY_LEFT_ARROW
const int buttonPin_p2_move_right = 12; // P2 Right -> KEY_RIGHT_ARROW
const int buttonPin_p2_attack     = 13; // P2 Attack -> KEY_UP_ARROW

const int buttonPin_start         = 27; // Start     -> ' ' (Space)

// --- LAST BUTTON STATES ---
int lastButtonState_p1_move_left  = HIGH;
int lastButtonState_p1_move_right = HIGH;
int lastButtonState_p1_attack     = HIGH;

int lastButtonState_p2_move_left  = HIGH;
int lastButtonState_p2_move_right = HIGH;
int lastButtonState_p2_attack     = HIGH;

int lastButtonState_start         = HIGH;

void setup() {
  Serial.begin(19200);
  Serial.println("Starting BLE work!");
  bleKeyboard.begin();

  pinMode(buttonPin_p1_move_left, INPUT_PULLUP);
  pinMode(buttonPin_p1_move_right, INPUT_PULLUP);
  pinMode(buttonPin_p1_attack, INPUT_PULLUP);

  pinMode(buttonPin_p2_move_left, INPUT_PULLUP);
  pinMode(buttonPin_p2_move_right, INPUT_PULLUP);
  pinMode(buttonPin_p2_attack, INPUT_PULLUP);

  pinMode(buttonPin_start, INPUT_PULLUP);
}

void loop() {
  if (bleKeyboard.isConnected()) {

    // --- P1 MOVE LEFT ('a') ---
    int currentButtonState_p1_move_left = digitalRead(buttonPin_p1_move_left);
    if (currentButtonState_p1_move_left != lastButtonState_p1_move_left) {
      if (currentButtonState_p1_move_left == LOW) {
        bleKeyboard.press('a');
        Serial.println("P1 Left ingedrukt");
      } else {
        bleKeyboard.release('a');
        Serial.println("P1 Left losgelaten");
      }
      lastButtonState_p1_move_left = currentButtonState_p1_move_left;
      delay(15);
    }

    // --- P1 MOVE RIGHT ('d') ---
    int currentButtonState_p1_move_right = digitalRead(buttonPin_p1_move_right);
    if (currentButtonState_p1_move_right != lastButtonState_p1_move_right) {
      if (currentButtonState_p1_move_right == LOW) {
        bleKeyboard.press('d');
        Serial.println("P1 Right ingedrukt");
      } else {
        bleKeyboard.release('d');
        Serial.println("P1 Right losgelaten");
      }
      lastButtonState_p1_move_right = currentButtonState_p1_move_right;
      delay(15);
    }

    // --- P1 ATTACK ('w') ---
    int currentButtonState_p1_attack = digitalRead(buttonPin_p1_attack);
    if (currentButtonState_p1_attack != lastButtonState_p1_attack) {
      if (currentButtonState_p1_attack == LOW) {
        bleKeyboard.press('w');
        Serial.println("P1 Attack ingedrukt");
      } else {
        bleKeyboard.release('w');
        Serial.println("P1 Attack losgelaten");
      }
      lastButtonState_p1_attack = currentButtonState_p1_attack;
      delay(15);
    }

    // --- P2 MOVE LEFT (Left Arrow) ---
    int currentButtonState_p2_move_left = digitalRead(buttonPin_p2_move_left);
    if (currentButtonState_p2_move_left != lastButtonState_p2_move_left) {
      if (currentButtonState_p2_move_left == LOW) {
        bleKeyboard.press(KEY_LEFT_ARROW);
        Serial.println("P2 Left ingedrukt");
      } else {
        bleKeyboard.release(KEY_LEFT_ARROW);
        Serial.println("P2 Left losgelaten");
      }
      lastButtonState_p2_move_left = currentButtonState_p2_move_left;
      delay(15);
    }

    // --- P2 MOVE RIGHT (Right Arrow) ---
    int currentButtonState_p2_move_right = digitalRead(buttonPin_p2_move_right);
    if (currentButtonState_p2_move_right != lastButtonState_p2_move_right) {
      if (currentButtonState_p2_move_right == LOW) {
        bleKeyboard.press(KEY_RIGHT_ARROW);
        Serial.println("P2 Right ingedrukt");
      } else {
        bleKeyboard.release(KEY_RIGHT_ARROW);
        Serial.println("P2 Right losgelaten");
      }
      lastButtonState_p2_move_right = currentButtonState_p2_move_right;
      delay(15);
    }

    // --- P2 ATTACK (Up Arrow) ---
    int currentButtonState_p2_attack = digitalRead(buttonPin_p2_attack);
    if (currentButtonState_p2_attack != lastButtonState_p2_attack) {
      if (currentButtonState_p2_attack == LOW) {
        bleKeyboard.press(KEY_UP_ARROW);
        Serial.println("P2 Attack ingedrukt");
      } else {
        bleKeyboard.release(KEY_UP_ARROW);
        Serial.println("P2 Attack losgelaten");
      }
      lastButtonState_p2_attack = currentButtonState_p2_attack;
      delay(15);
    }

    // --- START (Space) ---
    int currentButtonState_start = digitalRead(buttonPin_start);
    if (currentButtonState_start != lastButtonState_start) {
      if (currentButtonState_start == LOW) {
        bleKeyboard.press(' ');
        Serial.println("Start ingedrukt");
      } else {
        bleKeyboard.release(' ');
        Serial.println("Start losgelaten");
      }
      lastButtonState_start = currentButtonState_start;
      delay(15);
    }

  }
}