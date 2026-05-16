/*==========================================================
 * Smart Greenhouse — Automation Logic & State Machine
 * File: greenhouse_logic.h
 *==========================================================*/
#ifndef GREENHOUSE_LOGIC_H
#define GREENHOUSE_LOGIC_H

#include <stdint.h>

/*--- Automation Thresholds ---*/
#define SOIL_THRESHOLD_LOW    35   /* Below 35% → Pump ON  (soil is dry) */
#define SOIL_THRESHOLD_HIGH   50   /* Above 50% → Pump OFF (soil is wet) */
#define TEMP_THRESHOLD_HIGH   30   /* Above 30°C → Fan ON  */
#define TEMP_THRESHOLD_LOW    27   /* Below 27°C → Fan OFF (hysteresis) */

/*--- Hardware Config ---*/
#define RELAY_ACTIVE_LOW      1    /* Set to 0 for Active HIGH relays, 1 for Active LOW */

/*--- Override Modes ---*/
typedef enum {
    MODE_AUTO,       /* Automatic control (default) */
    MODE_MANUAL_ON,  /* Manually forced ON */
    MODE_MANUAL_OFF  /* Manually forced OFF */
} OverrideMode;

/*--- State Machine States ---*/
typedef enum {
    STATE_IDLE,
    STATE_READ_SENSORS,
    STATE_EVALUATE,
    STATE_ACTUATE,
    STATE_TRANSMIT
} SystemState;

/*--- Live Sensor Data ---*/
typedef struct {
    uint8_t soil;        /* Soil moisture (0-100%) */
    uint8_t temperature; /* Temperature (°C) */
    uint8_t humidity;    /* Humidity (%) */
    uint8_t light;       /* LDR: 1=dark, 0=light */
    uint8_t pump_on;     /* Pump status: 1=ON, 0=OFF */
    uint8_t fan_on;      /* Fan status: 1=ON, 0=OFF */
    uint8_t led_on;      /* LED status: 1=ON, 0=OFF */
} GreenhouseData;

/*--- Public API ---*/

/* Initialize greenhouse logic */
void Greenhouse_Init(void);

/* Run one cycle of the state machine (call from main loop) */
void Greenhouse_Update(void);

/* Get current sensor data (for display/debug) */
GreenhouseData Greenhouse_GetData(void);

/* Process incoming UART command string (e.g., "P:ON", "F:OFF", "A:AUTO") */
void Greenhouse_ProcessCommand(const char *cmd);

#endif /* GREENHOUSE_LOGIC_H */
