/*==========================================================
 * Smart Greenhouse — Automation Logic & State Machine
 * File: greenhouse_logic.c
 *
 * State Machine Flow:
 * IDLE → READ_SENSORS → EVALUATE → ACTUATE → TRANSMIT → IDLE
 *
 * Features:
 * - Hysteresis on thresholds to prevent relay chattering
 * - Manual override per actuator via UART commands including Light
 * - Non-blocking design (no delays in state machine)
 *==========================================================*/
#include "greenhouse_logic.h"
#include "hal_adc.h"
#include "hal_uart.h"
#include "hal_dht11.h"
#include "hal_gpio.h"
#include <string.h>

/*--- Internal State ---*/
static SystemState    g_state = STATE_IDLE;
static GreenhouseData g_data  = {0, 0, 0, 0, 0, 0, 0};
static OverrideMode   g_pump_mode = MODE_AUTO;
static OverrideMode   g_fan_mode  = MODE_AUTO;
static OverrideMode   g_led_mode  = MODE_AUTO;

/* Last valid DHT11 readings (fallback if sensor fails) */
static uint8_t g_last_temp = 25;
static uint8_t g_last_hum  = 50;

void Greenhouse_Init(void)
{
    ADC_Init();
    UART_Init();
    GPIO_Init();

    g_state     = STATE_IDLE;
    g_pump_mode = MODE_AUTO;
    g_fan_mode  = MODE_AUTO;
    g_led_mode  = MODE_AUTO;
}

/*--- State: Read Sensors ---*/
static void state_read_sensors(void)
{
    /* Read soil moisture (ADC on PA0) */
    g_data.soil = ADC_ReadSoilPercent();

    /* Read DHT11 (PA2) — blocking ~25ms */
    DHT11_Data dht = DHT11_Read();
    if (dht.valid) {
        g_data.temperature = dht.temperature;
        g_data.humidity    = dht.humidity;
        g_last_temp = dht.temperature;
        g_last_hum  = dht.humidity;
    } else {
        /* Use last valid reading if sensor fails */
        g_data.temperature = g_last_temp;
        g_data.humidity    = g_last_hum;
    }

    /* Read LDR (PA1) */
    g_data.light = GPIO_ReadLDR();
}

/*--- State: Evaluate & Decide ---*/
static void state_evaluate(void)
{
    /*
     * Pump Logic (with hysteresis):
     * - Turn ON  when soil < 35%
     * - Turn OFF when soil > 50%
     * - Between 35-50%: maintain current state
     */
    if (g_pump_mode == MODE_AUTO) {
        if (g_data.soil < SOIL_THRESHOLD_LOW) {
            g_data.pump_on = 1;
        } else if (g_data.soil > SOIL_THRESHOLD_HIGH) {
            g_data.pump_on = 0;
        }
        /* else: keep current state (hysteresis zone) */
    } else {
        g_data.pump_on = (g_pump_mode == MODE_MANUAL_ON) ? 1 : 0;
    }

    /*
     * Fan Logic (with hysteresis):
     * - Turn ON  when temp > 45°C
     * - Turn OFF when temp < 40°C
     * - Between 40-45°C: maintain current state
     */
    if (g_fan_mode == MODE_AUTO) {
        if (g_data.temperature > TEMP_THRESHOLD_HIGH) {
            g_data.fan_on = 1;
        } else if (g_data.temperature < TEMP_THRESHOLD_LOW) {
            g_data.fan_on = 0;
        }
        /* else: keep current state (hysteresis zone) */
    } else {
        g_data.fan_on = (g_fan_mode == MODE_MANUAL_ON) ? 1 : 0;
    }

    /*
     * LED Logic: automatic by default (LDR based), overrideable via app
     * - Dark (LDR=1) → LED ON
     * - Light (LDR=0) → LED OFF
     */
    if (g_led_mode == MODE_AUTO) {
        g_data.led_on = g_data.light;
    } else {
        g_data.led_on = (g_led_mode == MODE_MANUAL_ON) ? 1 : 0;
    }
}

/*--- State: Actuate Outputs ---*/
static void state_actuate(void)
{
    /* Respect configured relay polarity */
#if RELAY_ACTIVE_LOW
    /* Active LOW: LOW turns relay ON, HIGH turns OFF */
    if (g_data.pump_on) GPIO_SetLow(PUMP_PIN);
    else GPIO_SetHigh(PUMP_PIN);

    if (g_data.fan_on) GPIO_SetLow(FAN_PIN);
    else GPIO_SetHigh(FAN_PIN);
#else
    /* Active HIGH: HIGH turns relay ON, LOW turns OFF */
    if (g_data.pump_on) GPIO_SetHigh(PUMP_PIN);
    else GPIO_SetLow(PUMP_PIN);

    if (g_data.fan_on) GPIO_SetHigh(FAN_PIN);
    else GPIO_SetLow(FAN_PIN);
#endif

    /* LED (direct drive, active HIGH) */
    if (g_data.led_on)
        GPIO_SetHigh(LED_PIN);
    else
        GPIO_SetLow(LED_PIN);
}

/*--- State: Transmit Data via UART ---*/
static void state_transmit(void)
{
    /* Format: S:{soil}%,T:{temp}C,H:{hum}%,L:{light},P:{pump},F:{fan}\r\n */

    UART_SendString("S:");
    UART_SendInt(g_data.soil);
    UART_SendString("%,T:");
    UART_SendInt(g_data.temperature);
    UART_SendString("C,H:");
    UART_SendInt(g_data.humidity);
    UART_SendString("%,L:");
    UART_SendString(g_data.led_on ? "ON" : "OFF");
    UART_SendString(",P:");
    UART_SendString(g_data.pump_on ? "ON" : "OFF");
    UART_SendString(",F:");
    UART_SendString(g_data.fan_on ? "ON" : "OFF");
    UART_SendString("\r\n");
}

/*--- Main State Machine Update ---*/
void Greenhouse_Update(void)
{
    switch (g_state) {
        case STATE_IDLE:
            g_state = STATE_READ_SENSORS;
            break;

        case STATE_READ_SENSORS:
            state_read_sensors();
            g_state = STATE_EVALUATE;
            break;

        case STATE_EVALUATE:
            state_evaluate();
            g_state = STATE_ACTUATE;
            break;

        case STATE_ACTUATE:
            state_actuate();
            g_state = STATE_TRANSMIT;
            break;

        case STATE_TRANSMIT:
            state_transmit();
            g_state = STATE_IDLE;
            break;

        default:
            g_state = STATE_IDLE;
            break;
    }
}

GreenhouseData Greenhouse_GetData(void)
{
    return g_data;
}

/*--- Process Override Commands from App ---*/
void Greenhouse_ProcessCommand(const char *cmd)
{
    /* Commands: "P:ON", "P:OFF", "F:ON", "F:OFF", "L:ON", "L:OFF", "A:AUTO" */

    if (cmd[0] == 'P' && cmd[1] == ':') {
        if (cmd[2] == 'O' && cmd[3] == 'N') {
            g_pump_mode = MODE_MANUAL_ON;
        } else if (cmd[2] == 'O' && cmd[3] == 'F') {
            g_pump_mode = MODE_MANUAL_OFF;
        }
    }
    else if (cmd[0] == 'F' && cmd[1] == ':') {
        if (cmd[2] == 'O' && cmd[3] == 'N') {
            g_fan_mode = MODE_MANUAL_ON;
        } else if (cmd[2] == 'O' && cmd[3] == 'F') {
            g_fan_mode = MODE_MANUAL_OFF;
        }
    }
    else if (cmd[0] == 'L' && cmd[1] == ':') {
        if (cmd[2] == 'O' && cmd[3] == 'N') {
            g_led_mode = MODE_MANUAL_ON;
        } else if (cmd[2] == 'O' && cmd[3] == 'F') {
            g_led_mode = MODE_MANUAL_OFF;
        }
    }
    else if (cmd[0] == 'A' && cmd[1] == ':') {
        /* Return all to automatic */
        g_pump_mode = MODE_AUTO;
        g_fan_mode  = MODE_AUTO;
        g_led_mode  = MODE_AUTO;
    }
}
