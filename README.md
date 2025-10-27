# 🧱 Age of the Brick - Unity Prototype

**Age of the Brick** es un prototipo de juego de estrategia en tiempo real inspirado en *Age of Empires*, pero ambientado en un mundo hecho completamente de **LEGO**.  
Este proyecto busca combinar mecánicas clásicas de RTS (construcción, recolección, combate y evolución) con una estética modular basada en piezas LEGO, tanto a nivel visual como en el diseño de las unidades y edificios.

---

## 🎮 Características actuales

✅ **Selección de unidades:** sistema de selección visual basado en la clase `Selectable`.  
✅ **Control de cámara:** movimiento fluido mediante los bordes de la pantalla.  
✅ **Sistema de jugadores:** registro de jugadores, propiedad de unidades y gestión de eras.  
✅ **Unidad base (`Unit`):** control de movimiento, ataque, salud y comportamiento por estados.  
✅ **Villager funcional:** unidad capaz de desplazarse, recibir daño y responder a comandos.  
✅ **Health Bar (barra de vida):** UI en espacio mundial con color dinámico según el estado de salud.  
✅ **Navegación (NavMeshAgent):** integración completa con el sistema de movimiento por agentes de Unity.  


---

## ⚙️ Requisitos técnicos

- **Motor:** Unity 2022.3 LTS o superior  
- **Lenguaje:** C#  
- **Sistema de navegación:** Unity NavMesh  
- **Render:** URP o Built-in compatible  

---

## 🧠 Diseño de gameplay

- Cada unidad está construida sobre la clase base `Unit`, que hereda de `Selectable`.  
- El comportamiento de las unidades se maneja mediante una **máquina de estados interna** (`Idle`, `Moving`, `Attacking`, etc.).  
- Las estadísticas se almacenan en `ScriptableObjects` (`UnitStats`) que permiten evolución por eras.  
- El sistema de salud se muestra mediante una **barra de vida en espacio mundial**, la cual se activa solo cuando la unidad recibe daño o se cura.  

---

## 🚧 Próximos pasos

🔹 Migrar el sistema de **HealthBar** a un prefab UI reutilizable gestionado por un **WorldCanvasManager**.  
🔹 Prototipar el sistema de **combate entre unidades** (melee/rango).  
🔹 Integrar **construcción de edificios** básicos. 
🔹 Integrar **recursos** básicos. 
🔹 Implementar **recolección de recursos** para los aldeanos.  
🔹 Añadir **interfaz de selección múltiple** y comandos por grupo.  

---

## 📂 Cómo probar

1. Clona el repositorio:
   ```bash
   git clone https://github.com/<tu-usuario>/AgeOfTheBrick-UnityPrototype.git

