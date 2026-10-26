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
