# Release v3.0 — Documentation technique

## Diagramme de classes

Le diagramme suivant représente l'architecture complète d'EasySave v3.0, organisée autour du patron Observateur entre `BackupService` et ses abonnés, et du patron Stratégie pour les modes de sauvegarde et de chiffrement.

```mermaid
classDiagram
    class BackupService {
        -List~IStateObserver~ _observers
        -ILogWriter _logger
        -IEncryptionService _encryptionService
        -IBusinessSoftwareGuard _guard
        -ITransferCoordinator _transferCoordinator
        +Attach(IStateObserver)
        +Detach(IStateObserver)
        +Notify(BackupState)
        +RunJob(int, CancellationToken) Task
        +RunRange(IEnumerable~int~, CancellationToken) Task
        +PauseJobs(IEnumerable~int~)
        +ResumeJobs(IEnumerable~int~) bool
        +StopJob(int)
        +IsJobRunning(int) bool
        +IsGuardRunning() bool
        +AddJob(BackupJobConfig)
        +RemoveJob(int)
        +UpdateJob(int, BackupJobConfig)
        +GetJobs() IEnumerable~BackupJobConfig~
    }

    class IStateSubject {
        <<interface>>
        +Attach(IStateObserver)
        +Detach(IStateObserver)
        +Notify(BackupState)
    }

    class IStateObserver {
        <<interface>>
        +Update(BackupState)
    }

    class StateFileWriter {
        -string _statePath
        -IStateFormatter _formatter
        +Update(BackupState)
    }

    class BackupJob {
        -BackupJobConfig _config
        -IBackupStrategy _strategy
        -IEncryptionService _encryptionService
        -ITransferCoordinator _transferCoordinator
        +Execute(CancellationToken) IAsyncEnumerable~BackupResult~
    }

    class IBackupStrategy {
        <<interface>>
        +Execute(string, string, CancellationToken) Task~bool~
    }

    class FullBackupStrategy {
        +Execute(string, string, CancellationToken) Task~bool~
    }

    class DifferentialBackupStrategy {
        +Execute(string, string, CancellationToken) Task~bool~
    }

    class IEncryptionService {
        <<interface>>
        +Encrypt(string) ValueTuple~bool, long~
        +EncryptAsync(string, CancellationToken) Task
        +ShouldEncrypt(string) bool
    }

    class CryptoSoftEncryptionService {
        -SemaphoreSlim _cryptoSemaphore
        -string _cryptoSoftPath
        -string _encryptionKey
        -IReadOnlySet~string~ _encryptedExtensions
        +Encrypt(string) ValueTuple~bool, long~
        +EncryptAsync(string, CancellationToken) Task
        +ShouldEncrypt(string) bool
    }

    class NoEncryptionService {
        +Encrypt(string) ValueTuple~bool, long~
        +EncryptAsync(string, CancellationToken) Task
        +ShouldEncrypt(string) bool
    }

    class IBusinessSoftwareGuard {
        <<interface>>
        +IsRunning() bool
    }

    class ProcessBusinessSoftwareGuard {
        -IReadOnlyCollection~string~ _names
        +IsRunning() bool
    }

    class NoBusinessSoftwareGuard {
        +IsRunning() bool
    }

    class ITransferCoordinator {
        <<interface>>
        +WaitAsync(string, long, CancellationToken) Task
        +Release(string, long)
        +RegisterFile(string)
        +UnregisterFile(string)
    }

    class TransferCoordinator {
        -Func~AppSettings~ _getSettings
        +WaitAsync(string, long, CancellationToken) Task
        +Release(string, long)
        +RegisterFile(string)
        +UnregisterFile(string)
    }

    class ILogWriter {
        <<interface>>
        +Log(LogEntry)
    }

    class EasyLogger {
        -string _logDirectory
        -ILogFormatter _formatter
        +Log(LogEntry)
    }

    class SocketLogWriter {
        -string _host
        -int _port
        +Log(LogEntry)
    }

    class CompositeLogWriter {
        -IEnumerable~ILogWriter~ _writers
        +Log(LogEntry)
    }

    class BackupJobConfig {
        +string Name
        +string SourceDir
        +string TargetDir
        +BackupType Type
        +bool IsActive
    }

    class BackupResult {
        +string SourcePath
        +string DestPath
        +long FileSize
        +long TransferMs
        +bool Success
        +bool Skipped
        +long EncryptionMs
    }

    class AppSettings {
        +LogFormat LogFormat
        +LogTarget LogTarget
        +string CryptoSoftPath
        +string EncryptionKey
        +List~string~ EncryptedExtensions
        +List~string~ BusinessSoftwareNames
        +List~string~ PriorityExtensions
        +long MaxFileSizeForParallelTransferKb
        +int MaxParallelDegree
    }

    IStateSubject <|.. BackupService
    IStateObserver <|.. StateFileWriter
    BackupService --> ILogWriter
    BackupService --> IEncryptionService
    BackupService --> IBusinessSoftwareGuard
    BackupService --> ITransferCoordinator
    BackupService "1" --> "*" IStateObserver
    BackupService ..> BackupJob : crée
    BackupJob --> IBackupStrategy
    BackupJob --> IEncryptionService
    BackupJob --> ITransferCoordinator
    BackupJob ..> BackupResult : produit
    BackupService --> BackupJobConfig
    IBackupStrategy <|.. FullBackupStrategy
    IBackupStrategy <|.. DifferentialBackupStrategy
    IEncryptionService <|.. CryptoSoftEncryptionService
    IEncryptionService <|.. NoEncryptionService
    IBusinessSoftwareGuard <|.. ProcessBusinessSoftwareGuard
    IBusinessSoftwareGuard <|.. NoBusinessSoftwareGuard
    ITransferCoordinator <|.. TransferCoordinator
    ILogWriter <|.. EasyLogger
    ILogWriter <|.. SocketLogWriter
    ILogWriter <|.. CompositeLogWriter
    CompositeLogWriter --> ILogWriter
```

## Diagramme de cas d'utilisation

Le diagramme ci-dessous recense les interactions entre l'utilisateur et le système EasySave. Les cas d'utilisation système sont déclenchés automatiquement lors de l'exécution d'une tâche de sauvegarde.

```mermaid
graph LR
    User(["👤 Utilisateur"])
    Sys(["⚙️ Système"])

    subgraph Utilisateur
        UC1["Configurer les tâches\n(ajouter, modifier, supprimer)"]
        UC2["Exécuter une ou toutes les tâches"]
        UC3["Pause / Reprendre / Arrêter une tâche"]
        UC4["Configurer les paramètres\n(format log, langue, extensions,\nlogiciels métier, priorités)"]
    end

    subgraph Système
        UC5["Détecter le logiciel métier\net mettre en pause automatiquement"]
        UC6["Chiffrer les fichiers via CryptoSoft"]
        UC7["Écrire le journal quotidien\n(JSON ou XML)"]
        UC8["Mettre à jour le fichier d'état\nen temps réel"]
        UC9["Centraliser les logs\nvia serveur Docker"]
    end

    User --> UC1
    User --> UC2
    User --> UC3
    User --> UC4
    Sys --> UC5
    Sys --> UC6
    Sys --> UC7
    Sys --> UC8
    Sys --> UC9
    UC2 -.->|déclenche| UC6
    UC2 -.->|déclenche| UC7
    UC2 -.->|déclenche| UC8
    UC5 -.->|interrompt| UC2
```

## Diagramme de séquence

Ce diagramme décrit le cycle de vie complet d'une tâche de sauvegarde, de l'action utilisateur jusqu'à la notification de fin, en passant par la copie, le chiffrement et la journalisation. Les blocs `alt` et `opt` couvrent les cas de pause/reprise et d'arrêt.

```mermaid
sequenceDiagram
    actor User as Utilisateur
    participant MVM as MainViewModel
    participant BS as BackupService
    participant BJ as BackupJob
    participant PFC as PausableFileCopy
    participant CES as CryptoSoftEncryptionService
    participant EL as EasyLogger
    participant SFW as StateFileWriter

    User->>MVM: RunSelected()
    MVM->>BS: RunRange(indices, ct)
    BS->>BS: Énumérer les fichiers source
    BS->>BJ: Execute(ct)

    loop Pour chaque fichier
        BJ->>PFC: Copy(sourceFile, destFile, ct)
        PFC-->>BJ: Copie terminée

        opt Chiffrement requis
            BJ->>CES: EncryptAsync(destFile, ct)
            CES->>CES: Acquérir SemaphoreSlim
            CES->>CES: Process.Start(CryptoSoft.exe)
            CES-->>BJ: (Success, EncryptionMs)
        end

        BJ-->>BS: BackupResult
        BS->>EL: Log(LogEntry)
        BS->>SFW: Notify(BackupState.Running)
        SFW->>SFW: Écrire state.json

        alt Logiciel métier détecté
            BS->>MVM: Notify(BackupState.Paused)
            BS->>BS: Attendre fin du logiciel métier
            BS->>MVM: Notify(BackupState.Running)
        end

        alt Pause utilisateur
            User->>MVM: PauseSelected()
            MVM->>BS: PauseJobs(indices)
            BS->>MVM: Notify(BackupState.Paused)
            User->>MVM: ResumeSelected()
            MVM->>BS: ResumeJobs(indices)
            BS->>MVM: Notify(BackupState.Running)
        end

        opt Arrêt demandé
            User->>MVM: StopSelected()
            MVM->>BS: StopJob(index)
            BS->>BS: CancellationTokenSource.Cancel()
            BS->>MVM: Notify(BackupState.Interrupted)
        end
    end

    BS->>EL: Log(dernière entrée)
    BS->>SFW: Notify(BackupState.Completed)
    BS->>MVM: Notify(BackupState.Completed)
    MVM-->>User: Interface mise à jour
```

## Diagramme d'activité

Ce diagramme détaille le traitement interne d'un fichier individuel dans `BackupJob`, depuis la vérification de priorité jusqu'à l'écriture du résultat dans le canal de sortie. Les contrôles du logiciel métier et du flag de pause sont effectués après chaque fichier traité.

```mermaid
flowchart TD
    Start([Début]) --> CheckSamePath{sourceFile == destFile ?}
    CheckSamePath -- Oui --> WriteError[Écrire BackupResult\nTransferMs=-1, erreur]
    CheckSamePath -- Non --> CheckPriority{Extension prioritaire ?}
    CheckPriority -- Oui --> WaitNonPriority[Attendre la fin des\nfichiers non-prioritaires]
    CheckPriority -- Non --> CheckSize
    WaitNonPriority --> CheckSize{Fichier volumineux\nou MaxParallelDegree ?}
    CheckSize -- Oui --> WaitCoordinator[Acquérir le sémaphore\nTransferCoordinator]
    CheckSize -- Non --> Copy
    WaitCoordinator --> Copy[Copier le fichier\nchunk par chunk\nPausableFileCopy]
    Copy --> CopyOK{Copie réussie ?}
    CopyOK -- Non --> WriteFailed[Écrire BackupResult\nTransferMs=-1, Success=false]
    CopyOK -- Oui --> ReleaseCoord[Libérer TransferCoordinator]
    ReleaseCoord --> CheckEncrypt{ShouldEncrypt\ndestFile ?}
    CheckEncrypt -- Non --> ReadSize[Lire taille du fichier dest]
    CheckEncrypt -- Oui --> Encrypt[EncryptAsync\nCryptoSoft.exe]
    Encrypt --> EncryptOK{ExitCode >= 0 ?}
    EncryptOK -- Non --> ReadSizeFail[Lire taille\nencryptionFailed=true]
    EncryptOK -- Oui --> ReadSize
    ReadSizeFail --> WriteResult
    ReadSize --> WriteResult[Écrire BackupResult\ndans le canal]
    WriteError --> CheckGuard
    WriteFailed --> CheckGuard
    WriteResult --> CheckGuard{Logiciel métier\nactif ?}
    CheckGuard -- Oui --> WaitGuard[Pause automatique\nAttendre arrêt du logiciel]
    WaitGuard --> CheckPause
    CheckGuard -- Non --> CheckPause{Flag de pause\nutilisateur actif ?}
    CheckPause -- Oui --> WaitResume[Attendre reprise\nTask.Delay 100ms en boucle]
    WaitResume --> NextFile
    CheckPause -- Non --> NextFile{Autre fichier\ndans le canal ?}
    NextFile -- Oui --> CheckSamePath
    NextFile -- Non --> End([Fin])
```
