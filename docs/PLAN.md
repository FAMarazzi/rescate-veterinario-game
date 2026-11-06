# Rescate Veterinario – Plan de 20 días

Entrega: **18/11/2026 23:59**. Arranqué el 26/10. Voy de menos a más: commit todos los días y cada sistema tiene que andar antes de pasar al siguiente.

> Las fechas del cronograma son una estimación mía. Si algo se atrasa o se adelanta, actualizo la tabla y lo anoto en el registro de avance de abajo.

## Checklist de la consigna (todo esto tiene que estar al final)
- [x] Top view 45°, jugador se mueve libre (día 1)
- [x] Melee + Ranged (dardos con pool) (día 1)
- [x] Recibir / ocasionar daño, eliminar enemigos (`CorruptCat` con dardos/melee, se duerme con `Zzz`)
- [ ] Recolectar y usar ítems (botiquín, antídoto)
- [ ] Interactuar con el escenario (jaulas, puertas, palancas) (`IInteractable` y `Cage` listos en código, falta armar prefabs en escena)
- [ ] Feedback / notificaciones en pantalla
- [ ] Ganar, perder, respawn / reiniciar (respawn listo, falta condición de victoria/derrota)
- [x] Enemigos: detectar → acercarse → atacar → recibir daño → morir
- [ ] Escena de título: Play, Créditos, Opciones (volumen), Exit
- [ ] Música de fondo + efectos + feedback sonoro, volumen en Opciones
- [ ] 3+ minutos de gameplay efectivo
- [ ] Game Concept (2 carillas), backlog / Issues en GitHub, commits seguidos
- [ ] Build (Windows y, si da el tiempo, WebGL en itch.io) + PDF con controles y desafío

## Arquitectura (herencia + polimorfismo, manteniendo KISS)
- `IDamageable` → `Entity` (abstract) → `PlayerController`, `Enemy` (abstract), `Ally`
- `Enemy` → `CorruptCat` (sirve tanto para los gatos corruptos comunes como para el Gato Jefe gigante ajustando vida/daño/escala en el Inspector, sin duplicar scripts)
- `Ally` → `Dibu`, `Cuti` (salen de las jaulas, tienen aro verde, siguen al jugador y atacan a los corruptos)
- `IInteractable` → `Cage` (jaulas de gatitos) y armario de botiquines (decora la clínica y permite sacar/recolectar botiquines para usar)
- Barras de vida y `GameManager` (control de victoria, derrota, respawn y notificaciones en pantalla)
- Pool: `BulletPool` (ya está)

## Cronograma
| Día | Fecha | Programación | Unity / Arte / Audio |
|---|---|---|---|
| 1 | 26/10 | Repo + Entity + Player + cámara (HECHO) | Escena de prueba, pool de dardos, probar movimiento |
| 2 | 27/10 | Pulir Player (feel), barra de vida | Importar modelos de Meshy, prefabs Dibu / Cuti / gatos |
| 3 | 28/10 | `Enemy` abstract + máquina de estados | NavMesh, escena gris con salas |
| 4 | 29/10 | `CorruptCat` (melee) | Prefab enemigo, animación por código |
| 5 | 30/10 | `CorruptDog` (ranged, usa pool) | Prefab perro corrupto, material violeta |
| 6 | 31/10 | Melee con empuje contra enemigos | Probar balance |
| 7 | 1/11 | `Ally` + Dibu y Cuti siguen y atacan | Prefabs aliados |
| 8 | 2/11 | `IInteractable`, `Cage`, rescate de gatitos | Prefab jaula con modelo Meshy |
| 9 | 3/11 | `Door`, llaves o condición de apertura | Prefab puerta, armado del nivel 1 |
| 10 | 4/11 | `Pickup`: botiquín y antídoto | Modelos / íconos de ítems |
| 11 | 5/11 | `CorruptBoss` + cura con antídoto | Sala del jefe |
| 12 | 6/11 | `GameManager`, victoria / derrota / respawn | Armado final de niveles |
| 13 | 7/11 | UI: HUD, vida, gatitos, mensajes | Canvas y fuentes |
| 14 | 8/11 | Menú de título, Créditos, Opciones | Escena de título |
| 15 | 9/11 | `AudioManager` + volumen con slider | Música y efectos (freesound, etc.) |
| 16 | 10/11 | Partículas, feedback, screen shake | Efectos visuales |
| 17 | 11/11 | Nivel 2 o más contenido | Más salas |
| 18 | 12-13/11 | Bugs y balance, 3 minutos de juego | Pruebas |
| 19 | 14-15/11 | Build + WebGL + itch.io | Capturas |
| 20 | 16-17/11 | PDF, Game Concept, repo final | Margen |
| – | 18/11 | Entrega | – |

## Qué conseguir fuera de código
- **Música de fondo:** Pixabay, Freesound o Incompetech (revisar licencia, anotarla en créditos).
- **Efectos:** golpe, dardo, maullido, ladrido, puerta, jaula, pickup, victoria, derrota.
- **Fuente:** Google Fonts (por ejemplo una redondeada que combine con los gatos).
- **Animación:** por código (rebote, escala, rotación). Mixamo solo para el veterinario si es humanoide.
- **Créditos:** mi nombre, las herramientas de IA que usé (Meshy) y las fuentes del audio.

## Si me atraso
Si un día se me atrasa, recorto contenido (por ejemplo el nivel 2). Lo que no recorto es el menú, el audio ni la entrega.

## Hitos del profe vs. este plan
| Hito | Qué pide | Dónde cae en mi plan | Estado |
|---|---|---|---|
| 0 – Preproducción | Nombre, Game Concept, repo, backlog | Día 1 y 20 (Game Concept), Issues en GitHub | En curso |
| 1 – Core Gameplay | Movimiento, cámara, enemigo, daño, combate básico | Días 1 a 6 | **HECHO** |
| 2 – Vertical Slice | Melee, ranged, enemigos, items, objetivo, ganar o perder | Días 7 a 12 | **En curso (prioridad actual)** |
| 3 – Producción | Niveles, variantes, UI, audio | Días 13 a 17 | Pendiente |
| 4 – Feature Complete | Sistemas listos, bugs y balance | Día 18 | Pendiente |
| 5 – Release | Build, docs, créditos, flujo completo | Días 19 y 20 | Pendiente |

## Registro de avance
- **26/10 (día 1):** repo creado, `Entity` e `IDamageable`, jugador con movimiento, melee y dardos, pool de balas, cámara top view, escena de prueba.
- **27 y 28/10 (días 2 y 3):** no avancé. Los dos días de atraso los estoy recuperando entre hoy y mañana.
- **29/10 (día 4):** importé los modelos de Meshy a `Assets/Models` (veterinaria con animaciones, arma, dardo, jaula, piso, pared y pared con puerta). Borré los `.zip` y la jaula duplicada. Armé el prefab del jugador con el modelo como hijo (`Model`) y su Animator. Cambié el control del jugador (detalle abajo) y dejé todas las animaciones andando. Falta caminar hacia atrás y de costado: queda para el final y es poco probable que llegue a la entrega.
- **30/10 (día 5):**
  - Calibré el dardo: salida desde el `_shootPoint` hacia el punto apuntado y ajuste de ángulo `_aimYawOffset` para que coincida con la animación.
  - Armé herramienta de editor `RoomBuilderWindow.cs` para generar el piso modular 20x20 con rotación aleatoria de baldosas, paredes perimetrales y plano realista de clínica veterinaria (Recepción, Farmacia, Pasillo Central, Consultorios 1 y 2, Sala de Jaulas y Quirófano).
  - Creé materiales (`Mat_Piso`, `Mat_Pared`, `Mat_ParedPuerta`), ajusté la escala de las paredes y puertas, y horneé el `NavMeshSurface` sobre `Clinica_Piso` usando `Physics Colliders`.
- **5/11:**
  - Optimicé la escena: activé GPU Instancing en materiales, apagué sombras proyectadas de las 400 baldosas y agregué botón en `RoomBuilderWindow.cs` para marcar estáticos los módulos (`Static Batching`).
  - Integré el modelo 3D del dardo en `Dardo.prefab` (con `SphereCollider` como Trigger) y el modelo 3D del arma en la veterinaria (`_weaponIdle` y `_weaponMoving`, ocultándose sola en el bolsillo cuando no apunta).
  - Ajusté `PlayerAnimator.controller` y `AimAtMouse` (calculando el plano a la altura del arma activa) para que no quede trabada entre estados ni se desvíe el tiro al apuntar abajo.
  - Programé `Enemy.cs` (abstract, máquina de estados con `NavMeshAgent`) y `CorruptCat.cs` (melee con embestida).
- **6/11:**
  - Agregué tinte violeta automático de corrupción en `Enemy.cs` y lógica al curarse (`Die()`): recupera su color natural, se duerme con efecto `"Zzz"` flotante y respiración, o se vuelve aliado (`Ally.cs`) con aro verde en el piso, caminata relajada al seguir y modo combate contra corruptos.
  - Armé `GatoCorrupto.prefab` con `CatAnimator.controller` y ajusté colisiones/constraints en `Y` para que no eleve al jugador ni falle el respawn.
  - Creé `IInteractable.cs`, `Cage.cs` y la detección con tecla `E` en `PlayerController.cs`.

## Próximos pasos / TODOs (paso a paso, sin hacer todo de golpe)
1. **Modelos 3D nuevos y Prefabs:**
   - Terminar de generar e importar: **armario de botiquines**, objetos decorativos de la clínica y modelo del **Gato Corrupto Gigante (Jefe)** (que reutilizará `CorruptCat.cs` con más vida y daño para mantener el código simple - KISS).
   - Armar los prefabs de **`Dibu`**, **`Cuti`** y la **`Jaula`** (`Cage.cs`) para liberar aliados con la tecla `E`.
2. **Armario de Botiquines + Ítem Curativo (Recolectar y Utilizar items):**
   - El armario de botiquines va a cumplir doble función: decorar las salas (Farmacia / Consultorios) y permitir sacar/recolectar botiquines para usarlos cuando nos falte vida.
3. **Generador Automático de Decoración y Jaulas (`RoomBuilderWindow.cs`):**
   - Agregar la **Pestaña 4 ("4. Decoración y Jaulas")** en `Tools -> Generador de Nivel` para ubicar automáticamente los muebles, armarios de botiquines y jaulas en cada sala, y re-hornear el NavMesh.
4. **Barras de Vida + `GameManager` (Ganar, Perder, Notificaciones):**
   - Agregar barras de vida visuales y el `GameManager` para controlar el objetivo del nivel, las notificaciones en pantalla y el flujo de Victoria / Derrota / Reiniciar.
5. **Escena de Título y Audio (`AudioManager.cs`):**
   - Menú inicial (Play, Créditos, Opciones con slider de volumen, Exit) y sonidos/música.

