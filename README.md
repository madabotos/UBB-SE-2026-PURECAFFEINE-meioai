# BoardRent (Property and Management)

BoardRent is a WinUI desktop app for renting board games between users, with live cross-instance notifications.

## Project structure

- `Property_and_Management/` — main app (UI + logic + DB access)
- `NotificationServer/` — UDP relay server for live notifications
- `ServerCommunication/` — shared message contract between app and server
- `Property_and_Management/Scripts/` — SQL setup/reset scripts + 2-user demo script

## App layers (`Property_and_Management/src`)

- `Model` — business entities (`Game`, `Request`, `Rental`, `Notification`, `User`)
- `DTO` — transport objects between layers/UI
- `Interface` — service/repository contracts
- `Repository` — SQL data access
- `Service` — business rules (requests, rentals, notifications)
- `Service/Listeners` — UDP notification client
- `Viewmodels` — page presentation logic
- `Views` — WinUI pages
- `Utilities` — UI converters/helpers

## Component interactions (important)

- **Chat/request flow:** `ChatView` actions approve/deny requests; this updates requests/rentals and triggers notifications.
- **Notification flow:** app saves notification -> sends UDP message -> `NotificationServer` forwards by user ID -> target app updates notifications UI and toast.
- **Shared contract:** `ServerCommunication` defines message types and serialization for both sides.

## Database

- DB name: `BoardRent`
- Connection string: `Property_and_Management/App.config` (default `localhost\\SQLEXPRESS`)
- Scripts in `Property_and_Management/Scripts/`:
  - `generate_db.sql` (create schema)
  - `drop_all_tables.sql` (clean reset)
  - `reset_and_insert_test_data.sql` (seed demo data)

## 2-user demo (quick)

1. In SSMS run, in order:
   1) `drop_all_tables.sql` (optional)  
   2) `generate_db.sql`  
   3) `reset_and_insert_test_data.sql`

2. In PowerShell from `Property_and_Management/Scripts/` run:

```powershell
./demo-run-2-users.ps1
```

This script builds the app, starts `NotificationServer`, and launches two instances:
- user 1 (`...exe 1`)
- user 2 (`...exe 2`)

## Requirements

- Windows
- .NET 8 SDK
- SQL Server + SSMS
