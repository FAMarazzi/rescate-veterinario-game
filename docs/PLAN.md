# Rescate Veterinario – Plan y Bitácora de Avance

Entrega: **18/11/2026 23:59**. Voy de menos a más: commit seguido y cada sistema tiene que andar antes de pasar al siguiente.

> Las fechas y notas de abajo son mi registro personal de avance a medida que fui cerrando cada parte del juego.

## Checklist de la consigna (todo esto tiene que estar al final)
- [x] Top view 45°, jugador se mueve libre
- [x] Melee + Ranged (dardos con pool)
- [x] Recibir / ocasionar daño, eliminar enemigos (`CorruptCat` con dardos/melee, se duerme con `Zzz`)
- [x] Recolectar y usar ítems (sacar botiquines con `E` de los armarios `MedicineCabinet` y usarlos con `Q`)
- [x] Interactuar con el escenario (`IInteractable`: jaulas `Cage` y armarios médicos `MedicineCabinet`)
- [x] Feedback / notificaciones en pantalla y barras de vida (`WorldHealthBar` y avisos en `GameManager`)
- [x] Ganar, perder, respawn / reiniciar (sistema de 3 vidas + animación de muerte + respawn con escudo en `PlayerController` + pantallas de Victoria y Game Over)
- [x] Enemigos: detectar → acercarse → atacar → recibir daño → morir (con dificultad progresiva por sala y línea de visión)
- [x] Escena de título: Play (con intro de 3 diapositivas), Créditos, Opciones (volumen), Exit
- [x] Música de fondo + efectos + feedback sonoro, volumen en Opciones
- [x] 3+ minutos de gameplay efectivo
- [x] Game Concept (2 carillas), backlog / Issues en GitHub, commits seguidos
- [x] Build de Windows + PDF con controles y desafío

## Arquitectura (herencia + polimorfismo, manteniendo KISS)
- `IDamageable` → `Entity` (abstract) → `PlayerController`, `Enemy` (abstract), `Ally`
- `Enemy` → `CorruptCat` (sirve tanto para los gatos corruptos comunes como para el Gato Jefe gigante ajustando vida/daño/escala en el Inspector, sin duplicar scripts)
- `Ally` → `Dibu`, `Cuti` (salen de las jaulas, tienen aro verde, siguen al jugador y atacan a los corruptos; nunca mueren: si les bajan la vida se vuelven corruptos un rato hasta que los sedo con dardos)
- `IInteractable` → `Cage` (jaulas de gatitos) y `MedicineCabinet` (armario de botiquines: decora la clínica y permite sacar/recolectar botiquines para usar con `Q`)
- Barras de vida (`WorldHealthBar`) y `GameManager` (control de victoria, derrota, respawn, menús y notificaciones en pantalla)
- Pools: `BulletPool`, `EnemyPool` y `KittenPool`

## Qué conseguí / armé fuera de código
- **Música de fondo:** tema propio en La menor (`Rescate Veterinario master simple.wav`), puesto en loop desde el menú y bajando un poquito el volumen al entrar a jugar.
- **Efectos:** disparo de dardo, empujón melee, abrir jaula, curarse con botiquín, gato curado y soniditos de hover/click en los botones afinados en La menor.
- **Modelos y animaciones:** modelos 3D generados con Meshy AI (veterinaria, Dibu, Cuti, gatos, jaulas, camillas, carritos, armarios, piso y paredes) y animaciones de Mixamo / Meshy para la veterinaria (correr, apuntar, morir) y los michis.
- **Créditos:** mi nombre, mención a mis michis reales (Dibu y Cuti) y las herramientas que usé.

## Hitos del profe vs. mi avance
| Hito | Qué pide | Estado |
|---|---|---|
| 0 – Preproducción | Nombre, Game Concept, repo, backlog | **HECHO** |
| 1 – Core Gameplay | Movimiento, cámara, enemigo, daño, combate básico | **HECHO** |
| 2 – Vertical Slice | Melee, ranged, enemigos, items, objetivo, ganar o perder | **HECHO** |
| 3 – Producción | Niveles, variantes, UI, audio | **HECHO** |
| 4 – Feature Complete | Sistemas listos, bugs y balance | **HECHO** |
| 5 – Release | Build, docs, créditos, flujo completo | **HECHO** |

## Registro de avance
- **Arranque (Base y Core Gameplay):** repo creado, `Entity` e `IDamageable`, jugador con movimiento, melee y dardos, pool de balas (`BulletPool`), cámara top view a 45° (`CameraFollow`) traída y adaptada del proyecto anterior.
- **29/10:** importé los modelos de Meshy a `Assets/Models` (veterinaria con animaciones, arma, dardo, jaula, piso, pared y pared con puerta). Borré los `.zip` y la jaula duplicada. Armé el prefab del jugador con el modelo como hijo (`Model`) y su Animator. Cambié el control del jugador y dejé las animaciones andando.
- **30/10:**
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
- **11/11:**
  - Importé los muebles decorativos (camillas, carritos, mostrador, sillas, mesa quirúrgica y armarios médicos) y agregué `MedicineCabinet.cs` para sacar botiquines con `E` y curarse con `Q`.
  - Armé `WorldHealthBar.cs` (barras de vida flotantes con patrón Observer), `GameManager.cs` (HUD, objetivos, notificaciones, pausa, victoria y derrota) y `AudioManager.cs` con slider de volumen guardado en `PlayerPrefs`.
  - Sumé `EnemyPool.cs` y `KittenPool.cs` para que los enemigos y los gatitos rescatados que corren a la puerta también usen Object Pooling, y `ClinicLightEffects.cs` para el parpadeo de los tubos fluorescentes rotos.
- **12/11 y 13/11:**
  - Armé las pantallas ilustradas del **Menú de Inicio** (`Assets/MainMenu`) y **Créditos** (`Assets/Creditos`) con el efecto de hover turquesa al 110% en `MenuButtonHover.cs`, y la intro de 3 diapositivas (*"Informe de Emergencia"*) al darle a Jugar.
  - Ajusté las animaciones de los gatos, el piso exterior nocturno en `CameraFollow.cs` y las jaulas de **Dibu** (en Internación) y **Cuti** (en Consultorio 2).
- **16/11 al 18/11:**
  - Ajusté el centrado y orientación de Cuti al salir de la jaula con su FBX animado nuevo (`CutiAnimator.controller`) y puse material sin fricción para que ni el jugador ni los gatos se traben en los marcos de las puertas.
  - Sumé la animación de muerte de la veterinaria (`Dead`) con transición de respawn, onda expansiva turquesa y 2,5 s de escudo invulnerable titilando, y validé que los botiquines solo se puedan sacar desde el frente del armario y nunca a través de una pared.
  - Armé la pantalla ilustrada de **Game Over** (`Assets/GameOver`), integré mi tema de música original en La menor con bajada suave de volumen al entrar a partida, probé la Build de Windows y cerré los PDFs de *Game Concept* y *Documentación de Release*.
