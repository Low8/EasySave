# Architecture EasySave — Référence technique

> Branche de référence : **fix/stop-behavior** (2026-05-13)
> Cible : .NET 10.0 (preview SDK 10.0.300)

---

## 1. Vue d'ensemble des projets

### 1.1 Projets de la solution (`EasySave.slnx`)

| Projet | Rôle | Framework |
|---|---|---|
| `EasyLog` | Bibliothèque de journalisation (formatters, writers, LogEntry) | net10.0 |
| `EasySave.Models` | Data contracts partagés entre tous les projets (DTOs, enums, interfaces légères) | net10.0 |
| `EasySave.Services` | Moteur métier : orchestration des sauvegardes, encryption, guard, coordination | net10.0 |
| `EasySave.Console` | Interface CLI interactive + mode ligne de commande | net10.0 (win-x64) |
| `GUI` | Interface graphique WPF, pattern MVVM | net10.0-windows |
| `EasySave.LogServer` | Serveur TCP centralisé pour recevoir les logs distants (Docker-ready) | net10.0 |
| `EasySave.Localization` | Service de localisation FR/EN (ressources .resx) | net10.0 |
| `EasySave.Tests` | Suite de tests xUnit (unitaires + intégration) | net10.0 |

### 1.2 Graphe de dépendances entre projets

```
EasySave.Models  <──────────────────────────────┐
     ▲                                           │
     │                                           │
EasyLog ◄─── EasySave.Services ◄─── EasySave.Console
                    ▲                       ▲
                    │                       │
                   GUI               EasySave.Tests
                    │
            EasySave.Localization (GUI + Console)

EasySave.LogServer  (autonome, dépend de EasyLog via code copié)
```

Règle : **EasySave.Models** et **EasyLog** ne dépendent de rien d'autre dans la solution. Tous les autres projets peuvent les consommer librement.

---

## 2. Architecture des services (`EasySave.Services`)

### 2.1 Patterns utilisés

| Pattern | Où | Rôle concret |
|---|---|---|
| **Observer** | `IStateSubject` / `IStateObserver` | `BackupService` notifie `MainViewModel`, `StateFileWriter`, `ConsoleObserver` à chaque fichier traité |
| **Strategy** | `IBackupStrategy` | Découple la logique de copie (`FullBackupStrategy`, `DifferentialBackupStrategy`) de l'orchestration |
| **Null Object** | `NoEncryptionService`, `NoBusinessSoftwareGuard` | Implémentations neutres utilisées quand la fonctionnalité est désactivée — élimine les `if` null partout |
| **Composite** | `CompositeLogWriter` | Écrit dans plusieurs `ILogWriter` simultanément (local + distant) |
| **Channel (producer/consumer)** | `BackupJob.Execute()` | Pipeline asynchrone isolant les workers du consumer (BackupService) |
| **Factory delegate** | `Func<BackupJobConfig, IBackupStrategy>? _strategyFactory` | Injection de stratégie personnalisée, notamment pour les tests (SlowFullBackupStrategy) |
| **Repository** | `IBackupJobRepository` / `JsonBackupJobRepository` | Sépare persistance JSON de la logique métier |

### 2.2 Dépendances entre classes

```
BackupService
├── IBackupJobRepository  ←  JsonBackupJobRepository (JSON sur disque)
├── ILogWriter            ←  EasyLogger | SocketLogWriter | CompositeLogWriter
├── IEncryptionService    ←  CryptoSoftEncryptionService | NoEncryptionService
├── IBusinessSoftwareGuard ← ProcessBusinessSoftwareGuard | NoBusinessSoftwareGuard
├── ITransferCoordinator  ←  TransferCoordinator
├── Func<AppSettings>     (lue à chaque job pour MaxParallelDegree, seuils, etc.)
└── Func<BackupJobConfig, IBackupStrategy>?  (optionnel, pour les tests)
    └── instancie BackupJob
            ├── IBackupStrategy  ←  FullBackupStrategy | DifferentialBackupStrategy
            │       └── PausableFileCopy.Copy() (lecture/écriture 64 KB par chunk)
            ├── IEncryptionService
            └── ITransferCoordinator
```

### 2.3 Rôle de chaque classe/interface

#### Interfaces (`EasySave.Services/Interfaces/`)

| Interface | Méthodes | Rôle |
|---|---|---|
| `IBackupStrategy` | `Task<bool> Execute(src, dst, ct)` | Contrat de copie ; retourne `true` si fichier copié, `false` si sauté |
| `ITransferCoordinator` | `RegisterFile`, `UnregisterFile`, `WaitAsync`, `Release` | Coordination inter-jobs : priorité et serialisation des gros fichiers |
| `IBackupJobRepository` | `GetAll()`, `Save()` | Persistance des configurations de jobs |
| `IStateSubject` | `Attach`, `Detach`, `Notify` | Producteur de l'Observer |

#### Modèles (`EasySave.Models/`)

| Classe/Enum | Description |
|---|---|
| `BackupJobConfig` | Config d'un job : Name, SourceDir, TargetDir, Type (Full/Differential), IsActive |
| `BackupState` | Snapshot d'état temps-réel : Status, TotalFiles, RemainingFiles, Progress, CurrentSource/Dest |
| `BackupStatus` | Enum : `Idle`, `Running`, `Paused`, `Completed`, `Interrupted`, `Error` |
| `AppSettings` | Paramètres globaux : MaxParallelDegree, seuils, extensions, LogFormat, LogTarget, serveur distant |
| `LogTarget` | Enum : `Local`, `Centralized`, `LocalAndCentralized` |
| `IStateObserver` | `void Update(BackupState)` — côté récepteur de l'Observer |

#### Services (`EasySave.Services/`)

| Classe | Description |
|---|---|
| `BackupService` | Façade principale. Gère la liste des jobs, orchestre `RunJob`/`RunRange`, contrôle Pause/Stop via `_pauseFlags` et `_stopCtsSources`, notifie les observers. |
| `BackupJob` | Implémente le pipeline Channel pour un job donné. Producteur de fichiers → workers → résultats. |
| `BackupResult` | Record : SourcePath, DestPath, FileSize, TransferMs, Success, Skipped, EncryptionMs |
| `FullBackupStrategy` | Copie inconditionnelle via `PausableFileCopy.Copy()` |
| `DifferentialBackupStrategy` | Copie uniquement si source plus récente que destination |
| `PausableFileCopy` | Copie chunk-by-chunk (64 KB) avec vérification du `CancellationToken` à chaque itération |
| `TransferCoordinator` | Mutex gros fichiers + blocage des non-prioritaires pendant qu'un fichier prioritaire est en transit |
| `StateFileWriter` | `IStateObserver` — écrit/met à jour `state.json` ou `state.xml` à chaque notification |
| `CryptoSoftEncryptionService` | Chiffrement via processus externe CryptoSoft ; supporte `EncryptAsync` avec `Kill()` sur annulation |
| `NoEncryptionService` | Null Object — `ShouldEncrypt` toujours `false` |
| `ProcessBusinessSoftwareGuard` | Polling `Process.GetProcessesByName` avec cache 2 s |
| `NoBusinessSoftwareGuard` | Null Object — `IsRunning` toujours `false` |
| `JsonBackupJobRepository` | Lit/écrit la liste des jobs dans un fichier JSON |
| `JsonStateFormatter` / `XmlStateFormatter` | Sérialisent `List<BackupState>` en JSON ou XML |

---

## 3. Mécanisme Pause/Stop

### 3.1 Principe fondamental

La pause est un **polling inter-fichiers** : le job ne s'arrête pas en cours de copie d'un fichier. Il finit le fichier courant, écrit le résultat dans le channel, puis avant de traiter le suivant, `BackupService.RunJob` boucle sur `_pauseFlags[index]` en attendant 100 ms entre chaque vérification.

```
[Worker] copie fichier N → écrit BackupResult dans channel
[BackupService] lit BackupResult ← déblocage du channel
[BackupService] while (_pauseFlags[index]) { await Task.Delay(100); }
                ↑ c'est ici que le job est "gelé"
[BackupService] lit BackupResult suivant quand flag remis à false
```

### 3.2 `_pauseFlags` — comment ça marche

```csharp
private readonly ConcurrentDictionary<int, bool> _pauseFlags = new();
```

- Clé : index du job dans `_jobs`.
- `PauseJobs(indices)` : met `_pauseFlags[index] = true`.
- `ResumeJobs(indices)` : remet `_pauseFlags[index] = false` (sauf si le guard est actif).
- La boucle de polling dans `RunJob` :
  ```csharp
  while (_pauseFlags.GetValueOrDefault(index, false))
  {
      linkedCts.Token.ThrowIfCancellationRequested();
      await Task.Delay(100, linkedCts.Token);
  }
  ```
- Le flag est initialisé à `false` à l'entrée de `RunJob` et supprimé du dictionnaire dans `finally`.

### 3.3 `_stopCtsSources` — comment ça marche

```csharp
private readonly ConcurrentDictionary<int, CancellationTokenSource> _stopCtsSources = new();
```

- Clé : index du job.
- `StopJob(index)` appelle `cts.Cancel()` sur le `CancellationTokenSource` lié.
- Ce CTS est créé avec `CancellationTokenSource.CreateLinkedTokenSource(ct)` où `ct` est le token externe.
- Annuler le CTS propage l'annulation à : la copie chunk-by-chunk dans `PausableFileCopy`, les `WaitAsync` du `TransferCoordinator`, le `WaitForExitAsync` de CryptoSoft, et la boucle de polling de pause.
- `IsJobRunning(index)` vérifie simplement si la clé est présente : `_stopCtsSources.ContainsKey(index)`.

### 3.4 Race condition éliminée — `linkedCts` avant le premier `await`

```csharp
var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
_stopCtsSources[index] = linkedCts;   // ← enregistrement AVANT tout await
_pauseFlags[index] = false;
```

Sans cet ordre, un appel `StopJob(index)` immédiatement après `RunJob(index, cts.Token)` pouvait ne pas trouver de CTS dans le dictionnaire (le job n'avait pas encore atteint l'enregistrement). Le fix consiste à enregistrer le CTS synchroniquement dès l'entrée dans la méthode, avant tout `await`.

### 3.5 `BoundedChannel(1)` — pourquoi et impact

Dans `BackupJob.Execute()`, le channel de résultats est borné à **1 slot** :

```csharp
var channel = Channel.CreateBounded<BackupResult>(
    new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.Wait, ... });
```

**Pourquoi :** Quand `BackupService.RunJob` est en pause (boucle sur `_pauseFlags`), il ne lit plus le channel. Sans borne, les workers continueraient d'écrire des résultats indéfiniment. Avec `Capacity=1`, le worker qui vient de terminer un fichier se bloque sur `WriteAsync` dès qu'un résultat est déjà en attente.

**Impact sur la pause :** L'overshoot maximum est structurellement limité à **fichier en cours + 1 slot** = au plus 2 fichiers supplémentaires après le signal de pause. Le test vérifie `InRange(copiedCount, 2, 5)` : 2 fichiers minimum (gate libérée après 2 `Running`), 5 fichiers maximum (marge de timing de 300 ms).

### 3.6 Guard `IsJobRunning` — doublon intentionnel

`IsJobRunning(int index)` et `_stopCtsSources.ContainsKey(index)` servent le même signal. `IsJobRunning` est exposé publiquement pour la GUI (`MainViewModel`) afin d'éviter de relancer un job déjà actif. `_stopCtsSources` sert en interne pour `StopJob`. Ce n'est pas une duplication de logique : c'est une API publique qui s'appuie sur l'état interne.

---

## 4. Pipeline `BackupJob`

### 4.1 Architecture générale

```
BackupJob.Execute(ct)
│
├── [Enumération] liste tous les fichiers source → List<string> files
├── [Enregistrement] RegisterFile() pour chaque fichier prioritaire
│
├── [Channel résultats] BoundedChannel<BackupResult>(1)   ← capacity 1
│
└── [Task.Run] producer loop
    │
    ├── [fileChannel] BoundedChannel<string>(maxDegree × 2)
    │       SingleWriter=true, SingleReader=false
    │
    ├── [Workers] Enumerable.Range(0, maxDegree) workers parallèles
    │       chacun : WaitToReadAsync → TryRead → WaitAsync → Execute → WriteAsync
    │
    ├── [Producteur] foreach(file) → fileChannel.Writer.WriteAsync
    │       puis fileChannel.Writer.TryComplete()
    │
    └── await Task.WhenAll(workers)
        puis channel.Writer.TryComplete()

[Caller / BackupService]
    await foreach (result in channel.Reader.ReadAllAsync(ct))
        → yield return result
```

### 4.2 `fileChannel` — rôle et capacité

- Capacité : `maxDegree * 2` (buffer limité pour éviter de charger tous les chemins en mémoire d'un coup).
- `FullMode = Wait` : le producteur se bloque si les workers ne consomment pas assez vite.
- `SingleWriter = true` (le producteur), `SingleReader = false` (N workers consomment en parallèle).

### 4.3 Channel résultats — rôle et capacité

- Capacité : **1**.
- `SingleWriter = false` (N workers écrivent), `SingleReader = true` (BackupService lit).
- Crée la backpressure nécessaire pour que la pause bloque effectivement les workers.

### 4.4 `MaxParallelDegree`

Configuré dans `AppSettings.MaxParallelDegree` (défaut : 3). Lu à l'entrée de `BackupJob.Execute` via `_getSettings().MaxParallelDegree`. Contrôle le nombre de workers créés. Dans les tests d'intégration, il est forcé à 1 pour garantir un ordre déterministe.

---

## 5. `EncryptAsync`

### 5.1 `WaitForExitAsync(ct)` + `Kill()`

```csharp
public async Task<(bool Success, long EncryptionMs)> EncryptAsync(string filePath, CancellationToken ct)
{
    ...
    using var process = Process.Start(psi);
    try
    {
        await process.WaitForExitAsync(ct);
    }
    catch (OperationCanceledException)
    {
        process.Kill();
        throw;
    }
    ...
}
```

Si le `CancellationToken` est déclenché pendant que CryptoSoft tourne, `WaitForExitAsync` lève `OperationCanceledException`, le processus est tué immédiatement, et l'exception est re-propagée. Cela garantit qu'un Stop ne reste pas bloqué en attente d'un processus externe.

### 5.2 `IEncryptionService`

```csharp
public interface IEncryptionService
{
    bool ShouldEncrypt(string filePath);
    (bool Success, long EncryptionMs) Encrypt(string filePath);

    // Implémentation par défaut — les stubs de test n'ont qu'à implémenter Encrypt.
    Task<(bool Success, long EncryptionMs)> EncryptAsync(string filePath, CancellationToken ct)
        => Task.FromResult(Encrypt(filePath));
}
```

L'implémentation par défaut de `EncryptAsync` délègue à `Encrypt` synchrone. Les stubs de test (`NoEncryptionService`, `NoOpEncryptionService`) n'ont donc pas à implémenter `EncryptAsync`. Seul `CryptoSoftEncryptionService` override `EncryptAsync` pour être vraiment asynchrone et annulable.

---

## 6. `TransferCoordinator`

### 6.1 Rôle

Deux responsabilités distinctes gérées dans la même classe :

1. **Priorité des extensions** : si `AppSettings.PriorityExtensions` est non vide, les fichiers non-prioritaires attendent qu'aucun fichier prioritaire ne soit en cours de transfert.
2. **Serialisation des gros fichiers** : si `AppSettings.MaxFileSizeForParallelTransferKb > 0`, les fichiers dont la taille dépasse ce seuil ne peuvent pas être transférés en parallèle (un seul à la fois via `SemaphoreSlim(1,1)`).

### 6.2 Mutex gros fichiers

```csharp
private readonly SemaphoreSlim _largeFileSemaphore = new(1, 1);
private readonly ConcurrentDictionary<string, byte> _largeFilesInProgress = new();

// Dans WaitAsync :
if (IsLargeFile(fileSizeBytes))
{
    await _largeFileSemaphore.WaitAsync(ct);
    _largeFilesInProgress.TryAdd(filePath, 0);
}

// Dans Release :
if (IsLargeFile(fileSizeBytes) && _largeFilesInProgress.TryRemove(filePath, out _))
    _largeFileSemaphore.Release();
```

Le `TryRemove` dans `Release` garantit que le sémaphore n'est relâché qu'une fois même si `Release` est appelé plusieurs fois (safe par design). Le seuil est en **Ko** (`MaxFileSizeForParallelTransferKb`).

### 6.3 Mécanisme de priorité

- `RegisterFile` / `UnregisterFile` incrémentent/décrémentent `_priorityFileCount` (sous lock).
- Un fichier non-prioritaire met en place un `TaskCompletionSource` dans `_nonPriorityWaiters` et attend que `_priorityFileCount` retombe à 0.
- Quand le dernier fichier prioritaire est unregistered, `ReleaseWaiters()` complète tous les TCS en attente.

---

## 7. Business Software Guard

### 7.1 Pattern polling

`ProcessBusinessSoftwareGuard.IsRunning()` est appelé par `BackupService.RunJob` **après chaque fichier traité** (dans la boucle `await foreach`). Ce n'est pas un watcher d'événements OS : c'est un polling synchrone.

Si le guard retourne `true`, le job émet un état `Paused`, entre dans une boucle d'attente de 500 ms, et reste bloqué jusqu'à ce que le logiciel métier soit fermé. Il n'y a pas de flag `_pauseFlags` impliqué — c'est une pause automatique distincte de la pause manuelle.

### 7.2 Cache 2 secondes

```csharp
private static readonly TimeSpan CacheInterval = TimeSpan.FromSeconds(2);

public bool IsRunning()
{
    if (DateTime.Now - _lastCheck < CacheInterval)
        return _cachedResult;

    _lastCheck = DateTime.Now;
    _cachedResult = _processNames.Any(name =>
        Process.GetProcessesByName(name).Length > 0);
    return _cachedResult;
}
```

`Process.GetProcessesByName` est coûteux (appel système). Le cache évite de le rappeler plus d'une fois toutes les 2 secondes. **Attention** : le cache n'est pas thread-safe (`_lastCheck` et `_cachedResult` ne sont pas protégés). En pratique l'impact est négligeable car le résultat légèrement périmé n'est pas critique.

---

## 8. `MainViewModel`

### 8.1 Commandes exposées

| Commande | `CanExecute` | Action |
|---|---|---|
| `RunSelectedCommand` | `SelectedJobs.Count > 0` | `RunSelected()` — lance les jobs sélectionnés non-déjà-en-cours via `IsJobRunning` |
| `RunAllCommand` | `Jobs.Any()` | `RunAll()` — lance tous les jobs non-déjà-en-cours |
| `PauseSelectedCommand` | `SelectedJobs.Count > 0` | `PauseSelected()` → `_service.PauseJobs(indices)` |
| `ResumeSelectedCommand` | `SelectedJobs.Count > 0` | `ResumeSelected()` → `_service.ResumeJobs(indices)` |
| `StopSelectedCommand` | `SelectedJobs.Count > 0` | `StopSelected()` → `_service.StopJob(index)` par index |
| `PauseAllCommand` | `Jobs.Any()` | `PauseAll()` → `_service.PauseJobs(0..N-1)` |
| `ResumeAllCommand` | `Jobs.Any()` | `ResumeAll()` → `_service.ResumeJobs(0..N-1)` |
| `StopAllCommand` | `Jobs.Any()` | `StopAll()` → `_service.StopJob(i)` pour tout i |
| `AddJobCommand` | toujours actif | Valide les champs et appelle `_service.AddJob` |
| `UpdateJobCommand` | `SelectedJob != null` | Met à jour le job sélectionné |
| `RemoveJobCommand` | `SelectedJob != null` | Supprime le job sélectionné |
| `BrowseNew/EditSource/TargetCommand` | toujours actif | Ouvre `FolderBrowserDialog` |

### 8.2 Flux Run/Pause/Stop

```
User clique RunSelected
  → RunSelected() [async void]
      → filtre par IsJobRunning → indices non-actifs
      → await Task.Run(() => _service.RunRange(indices, cts.Token))
          → BackupService.RunJob() pour chaque index (en parallèle)
              → notifie IStateObserver.Update() à chaque fichier
                  → MainViewModel.Update(state) [dispatcher.Invoke si besoin]
                      → job.UpdateFromState(state) → INotifyPropertyChanged → UI

User clique PauseSelected
  → _service.PauseJobs(indices)  [synchrone, instantané]
      → _pauseFlags[index] = true
          → BackupService.RunJob boucle sur await Task.Delay(100)

User clique StopSelected
  → _service.StopJob(index)  [synchrone]
      → linkedCts.Cancel()
          → OperationCanceledException propagée dans RunJob
              → catch → Notify(Interrupted) → finally nettoie _stopCtsSources/_pauseFlags
```

### 8.3 `CanExecute` actuels

Les `CanExecute` ne reflètent **pas** l'état Running/Paused des jobs individuels — ils vérifient uniquement si la sélection est non vide et si la liste contient des jobs. Il n'y a pas de désactivation automatique de "Run" quand un job est déjà en cours : c'est le filtre `IsJobRunning` dans `RunSelected`/`RunAll` qui empêche les doublons, pas `CanExecute`.

### 8.4 Reconfiguration des logs à chaud (`ApplyLogSettings`)

Quand les paramètres de log changent, `ApplyLogSettings` recrée entièrement le `BackupService` (nouveau logger, nouveau service d'encryption, nouveau guard) et rattache les observers. L'ancien service est détaché. Les jobs persistés sont rechargés depuis le fichier JSON.

---

## 9. Tests

### 9.1 Classes de test

| Classe | Type | Ce qu'elle couvre |
|---|---|---|
| `BackupServiceTests` | Unitaire | AddJob, RemoveJob, UpdateJob, persistance cross-instances |
| `BackupExecutionIntegrationTests` | Intégration | Copie réelle Full et Differential sur disque temp |
| `PauseResumeStopIntegrationTests` | Intégration | Pause, Resume, Stop avec vrai système de fichiers |
| `TransferCoordinatorTests` | Intégration | Mutex gros fichiers, priorité, séquentialité |
| `FullBackupStrategyTests` | Unitaire | Copie inconditionnelle, écrasement |
| `DifferentialBackupStrategyTests` | Unitaire | Skip si dest plus récent, copie si source plus récent |
| `CryptoSoftEncryptionServiceTests` | Unitaire | ShouldEncrypt, Encrypt (chemin absent, extension non ciblée) |
| `CryptoSoftEncryptionServiceAdvancedTests` | Unitaire | Injection de `processRunner` delegate pour bouchonner le process |
| `CommandLineParserTests` | Unitaire | Parse d'indices, ranges, point-virgule, cas dégénérés |
| `ProcessBusinessSoftwareGuardTests` | Unitaire | Processus existant, inexistant, insensibilité à la casse + .exe |
| `NoBusinessSoftwareGuardTests` | Unitaire | Toujours false |
| `RepositoryFormatterLoggerTests` | Unitaire | JsonBackupJobRepository, JsonLogFormatter, XmlLogFormatter, JsonStateFormatter |
| `RepositoryEdgeCasesTests` | Unitaire | Fichier corrompu, écrasement |
| `StateFileWriterTests` | Unitaire | Écriture, remplacement par nom |
| `XmlFormattersAndGuardTests` | Unitaire | XmlStateFormatter round-trip, XmlLogFormatter round-trip, NoBusinessSoftwareGuard |
| `UnitTest1` | Placeholder | Vide (squelette généré) |

### 9.2 Ce que couvre `PauseResumeStopIntegrationTests`

**Setup commun :** 10 fichiers de 1 Ko dans un répertoire temporaire, `MaxParallelDegree = 1`, `EasyLogger` réel, `NoEncryptionService`, `NoGuard`.

**`Pause_StopsTransferAfterCurrentFile`**
- Utilise `SlowFullBackupStrategy` (délai 50 ms par fichier) pour ralentir le pipeline.
- Une `SemaphoreSlim gate` est libérée après le 2e `Running` notifié.
- Appelle `PauseJobs([0])` après la gate.
- Attend 300 ms puis compte les fichiers dans `_dst`.
- Assert : `InRange(copiedCount, 2, 5)` — au minimum 2 fichiers (gate libérée après le 2e), au maximum 5 (1 fichier courant + 1 slot channel + marge de timing).
- Annule le CTS pour débloquer le job et attend la fin.

**`PauseThenResume_TransfersAllFiles`**
- Gate libérée après le 3e `Running`.
- `PauseJobs` puis 100 ms puis `ResumeJobs`.
- Attend `BackupStatus.Completed` sur une 2e `SemaphoreSlim`.
- Assert : 10 fichiers copiés (transfert intégral).

**`Stop_CancelsWithinTwoSeconds_NoCorruptedFiles`**
- Gate libérée après le 3e `Running`.
- `cts.Cancel()` direct.
- Assert : le `Task` du job termine en moins de 2 secondes.
- Assert : chaque fichier dans `_dst` a soit la taille complète de la source, soit 0 (pas de fichier partiellement écrit).

---

## 10. État des branches Git

### 10.1 Branches actives et leur rôle

| Branche | État | Rôle |
|---|---|---|
| `master` | Stable | Branche de référence pour les PRs |
| `fix/stop-behavior` *(courante)* | En cours | Correctif du mécanisme pause/stop + ajout `MachineName`/`UserName` dans les logs + `LogServer` Docker |
| `feature/run-guard` | Mergée dans master (PR #45) | Guard contre le double-lancement de job + réarchitecture du contrôle pause |
| `fix/v3-critical` | Mergée (PR #39) | Correctifs critiques thread-safety, `MaxParallelDegree`, guard business software |
| `develop` | Intégration | Branche d'intégration intermédiaire |

### 10.2 Commits récents significatifs sur `fix/stop-behavior`

```
fec0f73  fix(merge): resolve corrupted LogEntry block from bad merge resolution
79a3f39  Merge branch 'develop' into fix/stop-behavior
449be46  Merge pull request #45 from Low8/feature/run-guard
6abce6e  fix(stop): register linkedCts before first await to eliminate race condition
f28c317  fix(pause): bound result channel to enforce backpressure on pause
          - BoundedChannel(1) dans BackupJob
          - InRange(2,5) dans le test Pause_StopsTransferAfterCurrentFile
5469428  fix(run-guard): prevent duplicate job launch and fix stop responsiveness
f854c6e  fix(pause): rearchitect pause control to service layer
```

---

## Annexe — Fichiers de configuration runtime

| Fichier | Emplacement | Rôle |
|---|---|---|
| `settings.json` | racine de la solution | `AppSettings` sérialisé (format, langue, CryptoSoft, extensions, guard, serveur) |
| `config.json` | racine de la solution | Liste des `BackupJobConfig` sérialisée |
| `logs/daily/YYYY-MM-DD.json` (ou `.xml`) | racine/logs/daily | Logs journaliers des transferts |
| `logs/live/state.json` (ou `.xml`) | racine/logs/live | Snapshot d'état temps-réel de tous les jobs |
