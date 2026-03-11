# HyperSalchicha - Arquitectura de Codigo (Guia Rapida)

Este documento resume como esta organizado el codigo para que otro agente/programador pueda continuar rapido sin releer todo el proyecto.

---

## 1) Mapa general de sistemas

- `Assets/Scripts/Player`
  - Movimiento FPS, camara, recoil, footsteps, stats del jugador.
- `Assets/Scripts/Weapons`
  - Inventario de 2 armas, disparo, recarga, swap, audio de armas, enhancements.
- `Assets/Scripts/Interaction`
  - Raycast de interaccion, interactuables con costo, compra/venta de armas.
- `Assets/Scripts/Items`
  - Inventario de item de 1 slot y pickups fisicos.
- `Assets/Scripts/Managers`
  - `GameManager` (rondas, cuajos, game over), `EffectsManager` (powerups temporales).
- `Assets/Scripts/UI`
  - UI ingame (ronda/cuajos/ammo/nombre arma), pausa/opciones, puente UI de armas.
- `Assets/Scripts/Enemies`
  - Vida, chase, melee, muerte.
- `Assets/Scripts/Bullets`
  - Lado proyectiles fisicos.

---

## 2) Flujo principal en runtime

1. `WeaponManager` crea/instala armas iniciales (slot 1 y slot 2 si existen).
2. `PlayerControllerAlt` maneja move/look/sprint/jump con Input System.
3. `WeaponManager` procesa input de combate y delega a `WeaponController` del arma activa.
4. `PlayerInteractor` muestra prompt + ejecuta `Interactable.Interact()`.
5. `Interactable` resuelve costo (Cuajos) y dispara UnityEvents.
6. UI se actualiza por eventos (`PlayerWeaponsUIBridge`, `InGameUIManager`) y por `GameManager`.

---

## 3) Jugador (movimiento/camara/audio de pasos)

### PlayerControllerAlt
Archivo: `Assets/Scripts/Player/PlayerControllerAlt.cs`

- Rigidbody-based movement.
- Sprint con stamina (toggle o hold).
- Air control + conservacion de impulso de salto.
- Ground check por `Physics.CheckSphere` usando **solo layer `Ground`**.
- Friccion dinamica por swap de materiales:
  - `movingPhysicMaterial`
  - `restBrakingPhysicMaterial`

### FirstPersonCameraRig
Archivo: `Assets/Scripts/Player/FirstPersonCameraRig.cs`

- Head bob (idle/walk/sprint), strafe roll, jump/land impulse.
- Presets de kick para eventos de arma.

### WeaponCameraRecoil
Archivo: `Assets/Scripts/Player/WeaponCameraRecoil.cs`

- Recoil/kick aditivo de armas.
- Presets con duracion de retorno configurable.

### FootstepManager
Archivo: `Assets/Scripts/Player/FootstepManager.cs`

- Steps por velocidad real del Rigidbody (no por animacion).
- Deteccion de suelo por CheckSphere.
- Resolucion de superficie por tag (`surfaceLibrary`).
- Sonido de landing y jump con multiplicadores de volumen/pitch.
- Audio pool interno (sin instanciar por paso).

---

## 4) Sistema de armas (core)

### Conceptos

- Inventario de 2 slots en `WeaponManager`.
- Cada slot guarda runtime:
  - `WeaponDefinition` (data)
  - `WeaponController` (instancia viva)

### WeaponDefinition (data)
Archivo: `Assets/Scripts/Weapons/WeaponDefinition.cs`

- ScriptableObject con stats y referencias:
  - daño, fire rate, ammo, reload mode, raycast distance, projectile, etc.
  - prefab de arma (`weaponPrefab`)
  - overrides opcionales para visuals de enhancements.

### WeaponManager (inventario + swap + API externa)
Archivo: `Assets/Scripts/Weapons/WeaponManager.cs`

- Inputs de armas: `Fire`, `Reload`, `EquipWeapon1`, `EquipWeapon2`, `ToggleWeapon`.
- Swap por estados/animator (con soporte de retarget/pending).
- API externa importante:
  - `EquipNewWeapon(WeaponDefinition definition)` -> compra/pickup/reemplazo.
  - `RemoveCurrentWeapon()` -> remueve arma activa y devuelve `WeaponDefinition`.
  - `AcquireWeapon(...)`, `ReplaceEquippedWeapon(...)`.
- Expone eventos para UI:
  - `OnWeaponChanged`
  - `OnAmmoChanged`

### WeaponController (logica de arma activa)
Archivo: `Assets/Scripts/Weapons/WeaponController.cs`

- Estado runtime de ammo, reload, fire cadence.
- Disparo hitscan o projectile.
- Integracion animator events:
  - `OnWeaponReadyToFire`
  - `OnBulletInserted`
  - `OnReloadAnimationFinished`
  - `OnCameraKick`
  - `OnAudioEvent`
  - `OnSharedAudioEvent`
- Enhancement state API:
  - `ActiveEnhancements`
  - `HasEnhancement/AddEnhancement/RemoveEnhancement/SetEnhancements`
- Conecta `WeaponEnhancementVisuals` y aplica overrides de `WeaponDefinition`.

### WeaponEnhancementVisuals
Archivo: `Assets/Scripts/Weapons/WeaponEnhancementVisuals.cs`

- Aplica materiales/GO/animator bool segun flags:
  - `Quantum`, `Heated`, `Overclocked`.
- Solo visual; la logica de estado vive en `WeaponController`.

### WeaponAudioController
Archivo: `Assets/Scripts/Weapons/WeaponAudioController.cs`

- Mapea `eventID -> AudioClip`.
- Pool fija de AudioSources.
- Audio Anchor externo para que sonidos no se corten al desactivar arma.
- Voice stealing cuando pool llena.

### Animator contract (armas)

- SMB:
  - `SMB_WeaponReady` -> habilita input de arma.
  - `SMB_WeaponHolstered` -> confirma holster al `WeaponManager`.
- Eventos de animacion deben llamar metodos en `WeaponController`.

### Scripts legacy/obsoletos

- `PlayerWeapons` (legacy, no usar).
- `WeaponAnimationRelay` (legacy relay, no usar para flujo nuevo).

---

## 5) Interacciones, economia y mundo

### PlayerInteractor
Archivo: `Assets/Scripts/Interaction/PlayerInteractor.cs`

- Solo SRP de interaccion:
  - Raycast
  - Prompt UI
  - `Interactable.Interact()`
- No maneja cobro de Cuajos.

### Interactable
Archivo: `Assets/Scripts/Interaction/Interactable.cs`

- Tiene `promptText`, `price`, `maxUses`, `onInteract`, `onInteractionFailed`.
- Si `price > 0`:
  - valida con `GameManager.Instance.cuajosActuales`
  - descuenta con `SubtractCuajos(price)`
  - luego `onInteract.Invoke()`.

### Compra/Venta de armas

- `WeaponDispenser` (`Assets/Scripts/Interaction/WeaponDispenser.cs`)
  - recibe `WeaponDefinition`.
  - `OnBuyInteraction()` -> `weaponManager.EquipNewWeapon(definition)`.
- `WeaponSeller` (`Assets/Scripts/Interaction/WeaponSeller.cs`)
  - `OnSellInteraction()` -> `RemoveCurrentWeapon()`.
  - si removio arma, suma `fixedSellValue` en Cuajos.

---

## 6) Sistema de items (1 slot)

### ItemData
Archivo: `Assets/Scripts/Items/ItemData.cs`

- SO de item:
  - `itemName`
  - `itemIcon`
  - `dropPrefab`

### PlayerItemInventory
Archivo: `Assets/Scripts/Items/PlayerItemInventory.cs`

- Un solo slot `currentItem`.
- `PickupItem(newItem, pickupTransform)`:
  - si habia item previo, dropea su `dropPrefab`.
  - guarda nuevo item.
  - actualiza UI Image.
- `ConsumeItem()` limpia slot y UI.
- `HasItem(item)` para validaciones de maquinas/recipes.

### ItemPickup
Archivo: `Assets/Scripts/Items/ItemPickup.cs`

- Se ejecuta desde UnityEvent de `Interactable`.
- Entrega item al `PlayerItemInventory`.
- Limpia prompt activo via `PlayerInteractor.ClearCurrentInteractionPrompt()`.
- Destruye pickup del mundo.

---

## 7) Managers y UI

### GameManager
Archivo: `Assets/Scripts/Managers/GameManager.cs`

- Singleton de partida.
- Rondas (`currentRound`, record).
- Economia (`cuajosActuales`, add/subtract).
- Game over y cambio de escenas.

### EffectsManager
Archivo: `Assets/Scripts/Managers/EffectsManager.cs`

- Powerups temporales sobre `WeaponManager`:
  - FireRate boost.
  - Infinite ammo temporal.

### UI principal

- `InGameUIManager`:
  - ronda, cuajos, ammo, nombre de arma con fade.
- `PlayerWeaponsUIBridge`:
  - escucha eventos de `WeaponManager` y actualiza UI.
- `PauseMenuController` + `PauseOptionsController`:
  - pausa con DOTween.
  - sensibilidad y volumen (persistidos en `PlayerPrefs`).

---

## 8) Enemigos y daño

- `WeaponController` hitscan filtra por layer `Enemy`.
- `EnemyScript.TakeDamage(float)` reduce HP y maneja muerte.
- IA/movimiento/ataque separados en:
  - `EnemyNavChase`
  - `EnemyMeleeAttack`

---

## 9) Contratos tecnicos importantes

- Layers requeridas:
  - `Ground` (movimiento/salto/footsteps).
  - `Enemy` (hitscan armas).
- Action Map esperado: `Gameplay`.
- Action names usadas:
  - `Move`, `Look`, `Jump`, `Sprint`
  - `Fire`, `Reload`, `EquipWeapon1`, `EquipWeapon2`, `ToggleWeapon`
- `GameManager.Instance` debe existir para economia/interactuables.

---

## 10) Recomendaciones para extender sin romper

- Nuevas armas: crear `WeaponDefinition` + prefab con `WeaponController`.
- Nuevos puntos de compra/venta: usar `Interactable` + `WeaponDispenser/WeaponSeller`.
- Nuevos enhancements: agregar flags/logica en `WeaponController`; visuals en `WeaponEnhancementVisuals`.
- Evitar logica de gameplay en `PlayerInteractor` (mantener SRP).
- Mantener wiring por Inspector en scripts de UI/audio; evitar fallbacks ocultos.

