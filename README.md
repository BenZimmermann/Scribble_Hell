# Scribble Hell

## Kurzbeschreibung des Spiels
- Multiplayer 2D Game  
- Spielt in der Fantasie eines Kindes  
- Überlebe und bezwinge verschiedene Gegner-Wellen  

---

## Anleitung zum Starten von Host und Client

### Host starten
- Projekt in Unity öffnen  
- Szene starten  
- Host-Modus im NetworkManager auswählen  
- Spiel startet als **Server + Client**

---

## Technischer Überblick

### Verwendete RPCs

#### ServerRpc
- `private void ShootBulletServerRpc(Vector2 direction)`
- `private void SelectUpgradeServerRpc(int upgradeIndex, NetworkConnection sender = null)`
- `public void SetReadyStateServerRpc(string name)`
- `private void MoveServerRpc(Vector2 input)`
- `public void TakeDamageServerRpc()`
- `public void Initialize(BulletData data, Vector2 shootDirection)`
- `public void StartWaveSystem()`
- `private void InitializeEnemy()`
- `private List<UpgradeData> GetRandomUpgrades(int count)`
- `private EnemyPrefabEntry GetWeightedRandomEnemy()`

#### ObserversRpc
- `private void HideLobbyCanvasClientRpc()`
- `private void PlayShootSoundClientRpc()`

#### TargetRpc
- `public void ShowPlayerNameReady(NetworkConnection con, string name)`
- `public void DisableNameField(NetworkConnection con, bool isOff)`
- `private void HideUpgradeUITargetRpc(NetworkConnection conn)`
- `private void NotifyOutOfBoundsTargetRpc(NetworkConnection conn)`

---

### Verwendete SyncVars
- `public readonly SyncVar<string> Player1`
- `public readonly SyncVar<float> countdown`
- `public readonly SyncVar<int> currentScore`
- `public readonly SyncVar<int> Player1Lives`
- `private readonly SyncVar<bool> isUpgradePhase`
- `private readonly SyncVar<string> player1UpgradeChoice`
- `private readonly SyncVar<int> currentWave`
- `private readonly SyncVar<int> enemiesKilled`
- `private readonly SyncVar<int> totalEnemiesThisWave`
- `private readonly SyncVar<bool> isWaveActive`
- `private readonly SyncVar<float> syncSpeed`

---

## Bullet-Logik
- Bullets werden **serverseitig gespawnt**
- Bewegung erfolgt über **Rigidbody2D**
- Bullet-Daten stammen aus **ScriptableObjects (BulletData)**

---

## Gegner-Logik
- Individualisierung durch **ScriptableObjects**
- Verhalten und Werte werden über **EnemyData ScriptableObjects** gesteuert

---

## Übersicht der Bonusfeatures
- Wave-System mit immer größer werdenden Wellen  
- Alle 5 Runden: Upgrade-Phase  
- Lobby mit Spielstart  
- Verschiedene Gegner mit unterschiedlichen Sprites  
- Alle Sprites selbst in **Photoshop** erstellt  
- Scribble Shader  
- Map-Border mit Benachrichtigung  

---

## Bekannte Bugs / Einschränkungen
- Nicht alle Upgrades sind erreichbar  
- Kein persistentes Scoreboard  
