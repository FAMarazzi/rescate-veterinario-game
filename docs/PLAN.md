# Rescate Veterinario – Plan de 20 días

Entrega: **18/11/2026 23:59**. Arranqué el 26/10. Voy de menos a más: commit todos los días y cada sistema tiene que andar antes de pasar al siguiente.

> Las fechas del cronograma son una estimación mía. Si algo se atrasa o se adelanta, actualizo la tabla y lo anoto en el registro de avance de abajo.

## Checklist de la consigna (todo esto tiene que estar al final)
- [x] Top view 45°, jugador se mueve libre (día 1)
- [x] Melee + Ranged (dardos con pool) (día 1, falta probarlos contra enemigos)
- [ ] Recibir / ocasionar daño, eliminar enemigos
- [ ] Recolectar y usar ítems (botiquín, antídoto)
- [ ] Interactuar con el escenario (jaulas, puertas, palancas)
- [ ] Feedback / notificaciones en pantalla
- [ ] Ganar, perder, respawn / reiniciar
- [ ] Enemigos: detectar → acercarse → atacar → recibir daño → morir
- [ ] Escena de título: Play, Créditos, Opciones (volumen), Exit
- [ ] Música de fondo + efectos + feedback sonoro, volumen en Opciones
- [ ] 3+ minutos de gameplay efectivo
- [ ] Game Concept (2 carillas), backlog / Issues en GitHub, commits seguidos
- [ ] Build (Windows y, si da el tiempo, WebGL en itch.io) + PDF con controles y desafío

## Arquitectura (herencia + polimorfismo)
- `IDamageable` → `Entity` (abstract) → `PlayerController`, `Enemy` (abstract), `Ally` (abstract)
- `Enemy` → `CorruptCat` (melee rápido), `CorruptDog` (ranged), `CorruptBoss` (jefe, se cura con antídoto)
- `Ally` → `Dibu`, `Cuti`, `Kitten` (siguen al jugador y atacan)
- `IInteractable` → `Cage`, `Door`, `Lever`
- `Pickup` (abstract) → `HealthKit`, `Antidote`
- Pool: `BulletPool` (ya está), más pool para efectos de partículas

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
| 1 – Core Gameplay | Movimiento, cámara, enemigo, daño, combate básico | Días 1 a 6 | En curso (falta enemigo) |
| 2 – Vertical Slice | Melee, ranged, enemigos, items, objetivo, ganar o perder | Días 7 a 12 | Pendiente |
| 3 – Producción | Niveles, variantes, UI, audio | Días 13 a 17 | Pendiente |
| 4 – Feature Complete | Sistemas listos, bugs y balance | Día 18 | Pendiente |
| 5 – Release | Build, docs, créditos, flujo completo | Días 19 y 20 | Pendiente |

## Registro de avance
- **26/10 (día 1):** repo creado, `Entity` e `IDamageable`, jugador con movimiento, melee y dardos, pool de balas, cámara top view, escena de prueba.
- **27 y 28/10 (días 2 y 3):** no avancé. Los dos días de atraso los estoy recuperando entre hoy y mañana.
- **29/10 (día 4):** importé los modelos de Meshy a `Assets/Models` (veterinaria con animaciones, arma, dardo, jaula, piso, pared y pared con puerta). Borré los `.zip` y la jaula duplicada. Armé el prefab del jugador con el modelo como hijo (`Model`) y su Animator. Cambié el control del jugador (detalle abajo) y dejé todas las animaciones andando. Falta caminar hacia atrás y de costado: queda para el final y es poco probable que llegue a la entrega.
- **30/10 (día 5 - Mañana):**
  - Calibré el dardo: salida desde el `_shootPoint` hacia el punto apuntado y ajuste de ángulo `_aimYawOffset` para que coincida con la animación.
  - Armé herramienta de editor `RoomBuilderWindow.cs` para generar el piso modular 20x20 con rotación aleatoria de baldosas y paredes perimetrales automáticas.
  - Generé el piso y el perímetro exterior de la clínica (guardado y pusheado al repo).
  - **Decisión de Escala Paredes:** La pared viene de 1.03m de alto por defecto (parece un mostrador bajo al lado de las baldosas de 1.9m y el jugador no entra por la puerta). La solución limpia es agrandar la altura de las paredes en el prefab: poner **Scale Z = `2` (o `2.2`)** en `Pared.prefab` y `ParedPuerta.prefab`. Así mantiene el ancho de 1.9m (calza exacto con el piso), la altura sube a ~2.1m (altura de puerta y pared real) y el veterinario pasa perfecto sin tener que achicarlo.

## Estado actual del jugador (30/10)
- **Movimiento:** WASD, el cuerpo gira suave hacia donde camina (`_turnSpeed`).
- **Apuntar:** click derecho mantenido. El cuerpo mira al mouse y camina más lento (`_aimSpeedMultiplier`).
- **Disparar:** click izquierdo mientras apunto. Sin apuntar, el click izquierdo es el golpe melee.
- **Animator:** el script manda `Speed` (Float), `Aiming` (Bool), `Melee` y `Shoot` (Trigger). Si un parámetro no existe en el controller, se ignora sin error.

## Notas técnicas (para retomar en otra PC)
- **Escala de Paredes (Para hacer al retomar a la tarde):**
  - En `Assets/Prefabs/Pared.prefab` y `ParedPuerta.prefab`, poner en Transform **Scale Z = `2`** (o 2.2) y acomodar el Box Collider.
  - Volver a tocar "Construir Perímetro de Paredes" en `Tools -> Generador de Nivel` para que tome la nueva altura.
- **Herramienta de Nivel:** En menú `Tools -> Generador de Nivel` se pueden regenerar pisos, perímetro y salas interiores con 1 click.
- **Animator en `Model`, no en `Player`.** Un solo Animator, con **Apply Root Motion desmarcado**. Si se marca, el modelo se separa del `Player` y parece que gira alrededor del centro del mapa.
- **FBX de la veterinaria:** uso `..._Meshy_Merged_Animations.fbx` (tiene malla y clips). Rig en Generic y **Loop Time** marcado en los clips de caminar, correr y apuntar. También hay un FBX con el clip `Rifle_Aim_Turn_Right`.
- **Animator sin Any State para apuntar.** Las flechas Idle/Walking/Running → Apuntar usan `Aiming` = true, y las de Idle/Walking/Running entre sí llevan además `Aiming` = false. Con Any State, el clip se reinicia o compite con otras flechas.
- **Rigidbody del jugador:** si la cámara tiembla, poner Interpolate. La cámara (`CameraFollow`) tiene que estar suelta en la raíz de la Hierarchy y no como hija del `Player`.
- **Peso de los modelos:** cada FBX de entorno pesa unos 14 MB. Los `.zip` no se suben al repo. GitHub rechaza archivos de más de 100 MB.
- **Materiales rosas:** Edit → Rendering → Materials → Convert Selected Built-in Materials to URP.

## Hoja de ruta para retomar a la tarde (30/10):
1. **Subir altura de paredes:** Poner `Scale Z = 2` en `Pared.prefab` y `ParedPuerta.prefab`, regenerar perímetro y salas interiores en `Tools -> Generador de Nivel`.
2. **NavMesh:** Instalar paquete `AI Navigation`, poner `NavMeshSurface` y hornear (Bake) el piso de la clínica.
3. **Enemigos (Core):**
   - Escribir `Enemy.cs` (abstract con máquina de estados: `Idle -> Chase -> Attack -> Dead`).
   - Crear `CorruptCat.cs` (enemigo melee rápido) con NavMeshAgent.
   - Probar recibir y hacer daño con el bate (melee) y los dardos del jugador.

