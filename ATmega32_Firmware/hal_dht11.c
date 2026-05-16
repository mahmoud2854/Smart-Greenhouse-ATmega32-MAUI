/*==========================================================
 * Smart Greenhouse — DHT11 Driver for ATmega32
 * File: hal_dht11.c
 * Bit-banging on PA2 — reads 40-bit data frame
 *==========================================================*/
#include "hal_dht11.h"
#include <avr/io.h>
#include <util/delay.h>

/* DHT11 pin: PA2 */
#define DHT_DDR   DDRA
#define DHT_PORT  PORTA
#define DHT_PIN   PINA
#define DHT_BIT   2

/* Set PA2 as output */
static inline void dht_output(void)
{
    DHT_DDR |= (1 << DHT_BIT);
}

/* Set PA2 as input (with pull-up disabled, external 10k pull-up used) */
static inline void dht_input(void)
{
    DHT_DDR  &= ~(1 << DHT_BIT);
    DHT_PORT &= ~(1 << DHT_BIT);  /* No internal pull-up */
}

/* Read PA2 state */
static inline uint8_t dht_read_pin(void)
{
    return (DHT_PIN & (1 << DHT_BIT)) ? 1 : 0;
}

/* Wait for pin to reach expected level, with timeout */
static uint8_t dht_wait_for(uint8_t level, uint8_t max_us)
{
    uint8_t count = 0;
    while (dht_read_pin() != level) {
        _delay_us(1);
        count++;
        if (count >= max_us) return 0;  /* Timeout */
    }
    return 1;
}

DHT11_Data DHT11_Read(void)
{
    DHT11_Data result = {0, 0, 0};
    uint8_t data[5] = {0, 0, 0, 0, 0};
    uint8_t i, j;

    /*--- Start Signal ---*/
    /* Pull data line LOW for at least 18ms */
    dht_output();
    DHT_PORT &= ~(1 << DHT_BIT);
    _delay_ms(20);

    /* Release line (pull HIGH via external pull-up) */
    DHT_PORT |= (1 << DHT_BIT);
    dht_input();
    _delay_us(30);

    /*--- Wait for DHT11 Response ---*/
    /* DHT11 pulls LOW for ~80us */
    if (!dht_wait_for(0, 100)) return result;   /* Wait for LOW */
    if (!dht_wait_for(1, 100)) return result;   /* Wait for HIGH */
    if (!dht_wait_for(0, 100)) return result;   /* Wait for LOW (start of data) */

    /*--- Read 40 bits (5 bytes) ---*/
    for (i = 0; i < 5; i++) {
        for (j = 0; j < 8; j++) {
            /* Each bit starts with ~50us LOW */
            if (!dht_wait_for(1, 70)) return result;

            /* Measure HIGH duration to determine bit value */
            _delay_us(30);

            if (dht_read_pin()) {
                /* HIGH after 30us → bit is '1' (HIGH lasts ~70us total) */
                data[i] |= (1 << (7 - j));
                /* Wait for pin to go LOW again */
                if (!dht_wait_for(0, 50)) return result;
            }
            /* If LOW after 30us → bit is '0' (HIGH was only ~26us) */
        }
    }

    /*--- Validate Checksum ---*/
    /* Byte 4 = Byte0 + Byte1 + Byte2 + Byte3 */
    if (data[4] == ((data[0] + data[1] + data[2] + data[3]) & 0xFF)) {
        result.humidity    = data[0];   /* Humidity integer */
        result.temperature = data[2];   /* Temperature integer */
        result.valid       = 1;
    }

    return result;
}
