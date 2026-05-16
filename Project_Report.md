# اسم المشروع: [اكتب اسم المشروع هنا]
### مشروع تخرج — [اكتب القسم/الكلية هنا]

## فريق العمل:
1. [اسم الطالب الأول]
2. [اسم الطالب الثاني]
3. [اسم الطالب الثالث]
4. [اسم الطالب الرابع]

---

## 📌 نبذة عن المشروع (Abstract)
هذا المشروع عبارة عن نظام متكامل لإدارة صوبة زراعية ذكية (Smart Greenhouse). يتكون من جزأين رئيسيين:
1. **الهاردوير (ATmega32):** يقوم بقراءة بيانات البيئة المحيطة بالنبات (درجة الحرارة، نسبة الرطوبة الجوية، رطوبة التربة، والإضاءة) باستخدام حساسات (DHT11, Soil Moisture, LDR). ثم يتخذ قرارات تلقائية لحماية النبات.
2. **تطبيق الموبايل (MAUI App):** يتصل بالميكروكنترولر عبر البلوتوث لعرض القراءات بشكل حي ومباشر على شاشة جذابة. كما يوفر إمكانية التحكم اليدوي في الأجهزة (مضخة، مروحة، إضاءة).

---

## 📐 رسم هيكلي للمشروع (Block Diagram)

- **المدخلات (Sensors):** حساس حرارة ورطوبة (DHT11) + حساس رطوبة تربة (Analog) + حساس إضاءة (LDR).
- **المعالجة (Microcontroller):** ATmega32 يعمل بتردد 8MHz.
- **المخرجات (Actuators):** ريلاي لمضخة مياه + ريلاي لمروحة تبريد + لمبة إضاءة LED عبر ترانزستور.
- **الاتصال (Communication):** وحدة بلوتوث HC-05 ترسل وتستقبل البيانات إلى/من تطبيق الموبايل (.NET MAUI).

---

## 🔌 التوصيلات الكهربائية وبروتوس (Proteus Wiring)

- **حساس رطوبة التربة:** يتصل بطرف `PA0` (ADC). 
- **حساس الضوء (LDR):** يتصل بطرف `PA1` مع مقاومة سحب لأسفل 10kΩ.
- **حساس الحرارة والرطوبة (DHT11):** يتصل بطرف `PA2` مع مقاومة رفع 4.7kΩ.
- **ريلاي المضخة (Pump):** يتصل بطرف `PB0` (Active LOW).
- **ريلاي المروحة (Fan):** يتصل بطرف `PB1` (Active LOW).
- **الإضاءة (LED):** تتصل بطرف `PB2` عبر ترانزستور كـ Switch مع مقاومة 4.7kΩ للـ Base.
- **وحدة البلوتوث HC-05:** يتم توصيل `RXD` بطرف `TXD (PD1)`، و `TXD` بطرف `RXD (PD0)`.

---

## 💻 أكواد المشروع الأساسية

### 1. كود الهاردوير (ATmega32 - Logic Layer)
هذا الجزء يوضح محرك القرار (State Machine) وكيفية التعامل مع الحساسات والمشغلات:

```c
/* جزء من ملف greenhouse_logic.c */
static void state_evaluate(void)
{
    /* Pump Logic (with hysteresis) */
    if (g_pump_mode == MODE_AUTO) {
        if (g_data.soil < SOIL_THRESHOLD_LOW) {
            g_data.pump_on = 1;
        } else if (g_data.soil > SOIL_THRESHOLD_HIGH) {
            g_data.pump_on = 0;
        }
    } else {
        g_data.pump_on = (g_pump_mode == MODE_MANUAL_ON) ? 1 : 0;
    }

    /* Fan Logic (with hysteresis) */
    if (g_fan_mode == MODE_AUTO) {
        if (g_data.temperature > TEMP_THRESHOLD_HIGH) {
            g_data.fan_on = 1;
        } else if (g_data.temperature < TEMP_THRESHOLD_LOW) {
            g_data.fan_on = 0;
        }
    } else {
        g_data.fan_on = (g_fan_mode == MODE_MANUAL_ON) ? 1 : 0;
    }

    /* LED Logic */
    if (g_led_mode == MODE_AUTO) {
        g_data.led_on = g_data.light;
    } else {
        g_data.led_on = (g_led_mode == MODE_MANUAL_ON) ? 1 : 0;
    }
}
```

### 2. كود تطبيق الموبايل (MAUI - DashboardViewModel)
هذا الجزء يوضح كيفية معالجة أوامر التحكم اليدوي وربطها بالواجهة:

```csharp
/* جزء من ملف DashboardViewModel.cs */
private void TogglePump()
{
    string cmd = IsPumpOn ? "P:OFF" : "P:ON";
    _bluetooth.SendCommand(cmd);
}

private void ToggleFan()
{
    string cmd = IsFanOn ? "F:OFF" : "F:ON";
    _bluetooth.SendCommand(cmd);
}

private void ToggleLight()
{
    string cmd = IsLightOn ? "L:OFF" : "L:ON";
    _bluetooth.SendCommand(cmd);
}

private void ResetAuto()
{
    _bluetooth.SendCommand("A:AUTO");
    StatusMessage = "🔄 Switched to AUTO mode";
}
```

---

## ❓ أسئلة المناقشة المتوقعة للمشروع

### س1: لماذا تم بناء الكود باستخدام `State Machine` بدلًا من `delay()`؟
**الإجابة:** لأن استخدام `delay` يوقف المعالج تمامًا، مما يمنع استقبال أوامر البلوتوث (Manual Override). الـ State Machine تجعل النظام (Non-blocking) ومستجيب دائمًا.

### س2: كيف تم تنفيذ خاصية الـ (Hysteresis) في المروحة والمضخة؟
**الإجابة:** بوضع عتبتين مختلفتين للتشغيل والإيقاف (مثل التشغيل عند 30°C والإيقاف عند 27°C) لمنع الريلاي من الارتعاش وتخريب الأجهزة عند تذبذب الحرارة.

### س3: ما هي بنية `MVVM` المستخدمة في تطبيق الموبايل؟
**الإجابة:** هي فصل التصميم (View) عن منطق التشغيل (ViewModel). هذا يسمح بتحديث الواجهة تلقائياً بمجرد وصول داتا من البلوتوث باستخدام خاصية (Data Binding).

### س4: كيف تمكّن التطبيق من قراءة بيانات البلوتوث دون أن يتجمد (Freezing)؟
**الإجابة:** قراءة البلوتوث تتم في مسار خلفي (`Task.Run`). عندما تصل البيانات، تُمرر للواجهة عبر دالة `MainThread.BeginInvokeOnMainThread` لتحديث الـ UI بأمان.

---
*نهاية المستند.*
