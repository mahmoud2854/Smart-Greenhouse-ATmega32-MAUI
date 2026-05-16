/*==========================================================
 * Smart Greenhouse — Main Entry Point
 * File: main.c
 *
 * ATmega32 @ 8MHz Internal RC Oscillator
 * Non-blocking super-loop with Timer0 for 1.5s scheduling
 *
 * Author: Mahmoud — Computer Engineering Dept.
 *==========================================================*/
#include <avr/io.h>
#include <avr/interrupt.h>
#include "greenhouse_logic.h"
#include "hal_uart.h"

/*--- Timing ---*/
#define CYCLE_INTERVAL_MS   1500   /* Run state machine every 1.5 seconds */

volatile uint32_t g_millis = 0;    /* Millisecond counter */

/*--- UART Command Buffer ---*/
#define CMD_BUF_SIZE  16
static char    cmd_buf[CMD_BUF_SIZE];
static uint8_t cmd_idx = 0;

/*==========================================================
 * Timer0 — Compare Match Interrupt (every 1ms)
 *
 * 8MHz / 64 prescaler = 125,000 ticks/sec
 * CTC mode: OCR0 = 124 → 125,000 / 125 = 1000 Hz = 1ms
 *==========================================================*/
ISR(TIMER0_COMP_vect)
{
    g_millis++;
}

static void Timer0_Init(void)
{
    /* CTC mode (WGM01=1), prescaler = 64 (CS01+CS00=1) */
    TCCR0 = (1 << WGM01) | (1 << CS01) | (1 << CS00);

    /* Compare value for 1ms interrupt */
    OCR0 = 124;

    /* Enable Timer0 Compare Match interrupt */
    TIMSK |= (1 << OCIE0);
}

static uint32_t millis(void)
{
    uint32_t ms;
    cli();
    ms = g_millis;
    sei();
    return ms;
}

/*==========================================================
 * UART Command Processing
 * Reads incoming bytes and buffers until '\n' is received
 *==========================================================*/
static void process_uart_input(void)
{
    while (UART_Available()) {
        uint8_t c = UART_Receive();

        if (c == '\n' || c == '\r') {
            if (cmd_idx > 0) {
                cmd_buf[cmd_idx] = '\0';
                Greenhouse_ProcessCommand(cmd_buf);
                cmd_idx = 0;
            }
        } else {
            if (cmd_idx < CMD_BUF_SIZE - 1) {
                cmd_buf[cmd_idx++] = (char)c;
            }
        }
    }
}

/*==========================================================
 * Main — Super Loop
 *==========================================================*/
int main(void)
{
    /* Initialize all subsystems */
    Greenhouse_Init();
    Timer0_Init();

    /* Enable global interrupts */
    sei();

    /* Send startup message */
    UART_SendString("=== Smart Greenhouse v1.0 ===\r\n");

    uint32_t last_cycle = 0;

    /* Non-blocking main loop */
    while (1) {
        /* Check for incoming commands from app */
        process_uart_input();

        /* Run state machine cycle every 1.5 seconds */
        uint32_t now = millis();
        if (now - last_cycle >= CYCLE_INTERVAL_MS) {
            last_cycle = now;

            /* Run all 5 states in sequence:
             * READ → EVALUATE → ACTUATE → TRANSMIT → IDLE */
            Greenhouse_Update();  /* IDLE → READ_SENSORS */
            Greenhouse_Update();  /* READ_SENSORS → EVALUATE */
            Greenhouse_Update();  /* EVALUATE → ACTUATE */
            Greenhouse_Update();  /* ACTUATE → TRANSMIT */
            Greenhouse_Update();  /* TRANSMIT → IDLE */
        }
    }

    return 0;
}
