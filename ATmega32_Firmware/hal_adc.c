/*==========================================================
 * Smart Greenhouse — ADC Driver for ATmega32
 * File: hal_adc.c
 *==========================================================*/
#include "hal_adc.h"
#include <avr/io.h>

void ADC_Init(void)
{
    /* AVCC as reference (REFS0=1), right-adjusted result */
    ADMUX = (1 << REFS0);

    /* Enable ADC, prescaler = 64 → 8MHz/64 = 125kHz */
    ADCSRA = (1 << ADEN) | (1 << ADPS2) | (1 << ADPS1);
}

uint16_t ADC_Read(uint8_t channel)
{
    /* Select channel (0-7), keep reference setting */
    ADMUX = (ADMUX & 0xE0) | (channel & 0x07);

    /* Start single conversion */
    ADCSRA |= (1 << ADSC);

    /* Wait for conversion to complete */
    while (ADCSRA & (1 << ADSC));

    return ADC;  /* Read 10-bit result (ADCL + ADCH) */
}

uint8_t ADC_ReadSoilPercent(void)
{
    uint16_t raw = ADC_Read(0);  /* PA0 = channel 0 */

    /*
     * Soil Moisture Fork Sensor Module:
     * - Dry soil / Disconnected → High resistance → Voltage near 5V (~1023 raw)
     * - Wet soil                → Conduction      → Voltage pulled low
     * Inverted mapping: raw=1023 -> 0%, raw=0 -> 100%
     */
    uint16_t inverted = (raw > 1023) ? 0 : (1023 - raw);
    uint8_t percent = (uint8_t)(((uint32_t)inverted * 100) / 1023);

    if (percent > 100) percent = 100;

    return percent;
}
