# Rescate Veterinario – Plan de 20 días

Entrega: **18/11/2026 23:59**. Hoy: 26/10. Reglas: de menos a más, commit todos los días, cada sistema funcionando antes de pasar al siguiente.

## Checklist de la consigna (todo esto tiene que estar al final)
- [ ] Top view 45°, jugador se mueve libre
- [ ] Melee + Ranged (dardos)
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
- **Créditos:** tu nombre, herramientas de IA (Meshy), fuentes de audio.

## Regla de oro
Si un día se atrasa, se **recorta contenido** (por ejemplo el nivel 2), nunca se corta el menú, el audio ni la entrega.
