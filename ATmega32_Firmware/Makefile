#==========================================================
# Smart Greenhouse — Makefile for ATmega32
# Compiler: avr-gcc (from Arduino IDE toolchain)
# Programmer: Arduino Nano as ISP via avrdude
#==========================================================

# --- Toolchain Paths (Arduino IDE) ---
AVR_ROOT = C:/Users/moul/AppData/Local/Arduino15/packages/arduino/tools/avr-gcc/7.3.0-atmel3.6.1-arduino7
AVRDUDE_ROOT = C:/Users/moul/AppData/Local/Arduino15/packages/arduino/tools/avrdude/6.3.0-arduino17

CC       = "$(AVR_ROOT)/bin/avr-gcc"
OBJCOPY  = "$(AVR_ROOT)/bin/avr-objcopy"
SIZE     = "$(AVR_ROOT)/bin/avr-size"
AVRDUDE  = "$(AVRDUDE_ROOT)/bin/avrdude"
AVRDUDE_CONF = "$(AVRDUDE_ROOT)/etc/avrdude.conf"

# --- Target MCU ---
MCU      = atmega32
F_CPU    = 8000000UL

# --- COM Port (CHANGE THIS to your Arduino Nano port!) ---
PORT     = COM3

# --- Compiler Flags ---
CFLAGS   = -mmcu=$(MCU) -DF_CPU=$(F_CPU) -Os -Wall -Wextra -std=c99

# --- Source Files ---
SRCS     = main.c hal_adc.c hal_uart.c hal_dht11.c hal_gpio.c greenhouse_logic.c
OBJS     = $(SRCS:.c=.o)
TARGET   = greenhouse

# ==========================
# Build Rules
# ==========================

all: $(TARGET).hex size

# Compile .c to .o
%.o: %.c
	$(CC) $(CFLAGS) -c $< -o $@

# Link all objects to .elf
$(TARGET).elf: $(OBJS)
	$(CC) $(CFLAGS) -o $@ $(OBJS)

# Convert .elf to .hex (Intel HEX format for flashing)
$(TARGET).hex: $(TARGET).elf
	$(OBJCOPY) -O ihex -R .eeprom $< $@

# Display memory usage
size: $(TARGET).elf
	$(SIZE) --mcu=$(MCU) --format=avr $(TARGET).elf

# ==========================
# Flash Commands
# ==========================

# Set fuse bits (RUN ONCE ONLY — 8MHz internal clock)
fuses:
	$(AVRDUDE) -C $(AVRDUDE_CONF) -c arduino -p m32 -P $(PORT) -b 19200 \
		-U lfuse:w:0xE4:m -U hfuse:w:0xD9:m

# Upload firmware to ATmega32 via Arduino Nano ISP
flash: $(TARGET).hex
	$(AVRDUDE) -C $(AVRDUDE_CONF) -c arduino -p m32 -P $(PORT) -b 19200 \
		-U flash:w:$(TARGET).hex:i

# Read fuse bits (for verification)
readfuses:
	$(AVRDUDE) -C $(AVRDUDE_CONF) -c arduino -p m32 -P $(PORT) -b 19200 \
		-U lfuse:r:-:h -U hfuse:r:-:h

# ==========================
# Clean
# ==========================

clean:
	del /Q *.o *.elf *.hex 2>NUL

.PHONY: all fuses flash readfuses size clean
