# JobApplicationManager

**Author:** [@NiklasKrogell](https://github.com/NiklasKrogell)

A small, local, single-user CRM for keeping a job search under control. It's a Windows desktop app
(WPF + EF Core + SQLite): applications, companies, industries, an activity log for every application,
and statistics that show how the search is going.

The UI of the app is in Finnish, but the code and this README are in English.

---

## Why this exists

During my own job search I felt that I needed a tool to keep track of my applications. 
I also was curious about the amounts of my applications, so this project is a way to learn while solving a real problem.

## Features

### Applications
- List with sortable columns (company, position, location, applied date, status).
- Search across position, company, location, contact person and notes, plus a status filter.
- A form for the selected application: company, position, applied date, status, location, posting
  link (opens in the browser, http/https only), contact name, email and phone, and notes.
- Add a company inline ("+ Uusi yritys"). An existing company with the same name is reused
  instead of creating a duplicate.
- **Activity log (Tapahtumat)** for each application: add notes, emails, calls, interviews and
  other events with a date. Status changes appear automatically. Newest entries are shown first.
- **CSV export** of the visible (filtered) list. The file opens correctly in Finnish Excel:
  semicolon separators, UTF-8 with a BOM, and formula injection neutralised.
- **Database backup** to a file of your choice. It uses SQLite `VACUUM INTO`, so the copy is
  consistent even while the app is running.

### Companies
- Company list with industry and number of applications, plus search.
- Company form (name, industry) and the company's applications. "Näytä hakemus" or a double
  click opens the application on the Applications tab.
- Industry management: add, rename and delete industries, each shown with its company count.
- Deleting a company or an industry warns about related rows. Applications and companies are
  kept and only lose the reference. Nothing is deleted in cascade by surprise.

### Statistics
- KPI cards:
  - **Total:** all applications.
  - **Active:** applications whose status is Applied, Interviewing or Offered.
  - **Response rate:** applications that have *ever* reached Interviewing, Offered, Accepted or
    Rejected, as a share of all applications.
  - **Interview rate:** applications that have ever been Interviewing, or have an Interview
    activity in the log.
- Bar charts by status, by month (last 12 months) and by industry, drawn with plain WPF shapes
  and no chart library.
- History comes from the activity log. An application that was withdrawn *after* an interview
  still counts as answered and interviewed. "Withdrawn" alone does not count as a response,
  because it is your decision, not the employer's.

---

## Getting started

### Requirements
- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/) (includes the desktop runtime the app needs)

### Run from source
```powershell
dotnet run --project JobApplicationManager.csproj
```
Development runs use a **separate development database** (see [Development database](#development-database)),
and the window title shows `[job_applications.dev.db]`.

### Install or update on your machine
```powershell
.\publish.ps1
```
The same command installs the app the first time and updates it later. It:

1. Stops if the app is running.
2. Backs up the database to `%LOCALAPPDATA%\JobApplicationManager\backups\` and keeps the 10
   newest copies.
3. Publishes a Release build to a temporary folder and mirrors it to
   `%LOCALAPPDATA%\Programs\Tyonhaku-CRM`. If the build fails, the installed version is left
   untouched.
4. Creates a Start menu shortcut "Työnhaku-CRM" if it doesn't exist yet.

Pending database migrations run automatically the next time the app starts, so your data is
upgraded in place.

Optional parameters: `-Destination <folder>`, `-SkipBackup`, `-NoShortcut`.

> **"File is not digitally signed"?** Your PowerShell execution policy only allows signed scripts.
> Either run it once with `powershell -ExecutionPolicy Bypass -File .\publish.ps1`, or allow
> local scripts for your user with `Set-ExecutionPolicy RemoteSigned -Scope CurrentUser`.
> The second option changes a security setting, so make sure that's what you want.

### Where the data lives

| What | Path |
|---|---|
| Real data | `%LOCALAPPDATA%\JobApplicationManager\job_applications.db` |
| Development data | `%LOCALAPPDATA%\JobApplicationManager\job_applications.dev.db` |
| Backups from `publish.ps1` | `%LOCALAPPDATA%\JobApplicationManager\backups\` |
| Installed app | `%LOCALAPPDATA%\Programs\Tyonhaku-CRM\` |

The database is deliberately *not* stored next to the executable, so that rebuilding, cleaning
`bin/` or reinstalling never touches your data.

**Restoring a backup:** close the app, then copy the backup file over `job_applications.db` in the
folder above.

---

## How it's built

### Tech stack

| UI | WPF on .NET 10 (`net10.0-windows`) |
| MVVM | [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) 8.4 (`[ObservableProperty]`, `[RelayCommand]`) |
| Data | EF Core 10 + SQLite, lazy-loading proxies, code-first migrations |
| Tooling | `dotnet-ef` as a local tool (`dotnet-tools.json`) |

There are no other dependencies, and the charts are plain XAML.

### Project structure
```
Model/          Entities: JobApplication, Company, Industry, Activity (+ enums)
Context/        AppDbContext (configuration, validation, automatic activity log)
Migrations/     EF Core migrations
ViewModels/     MainViewModel + one ViewModel per tab
Views/          One UserControl per tab
Services/       Statistics, CSV export, dialogs, shared save helper
Converters/     Enum → Finnish text, null → bool
Helpers/        Enum [Description] lookup
publish.ps1     Install/update script
```

---

## Ideas for later

- Reminders and follow-up dates ("call back on Friday").
- A separate contacts table, if the same recruiter appears across many applications.
- An xUnit project for the statistics, validation and CSV export.
- Optional auto-update (ClickOnce or Velopack) if the app is ever used on several machines.
- AI features: summarise notes, suggest next steps, or generate a cover letter from the job description.
