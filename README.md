# Jaahd Logistics Management System

A bilingual (English/Arabic) Windows application for managing procurement workflows, inventory, and project budgets.

## Features
- Full Procurement Lifecycle: PR -> RFQ -> Bid Analysis -> PO -> GRN -> 3-Way Match.
- Role-based Access: Admin, PM, Logistics, Finance, Storekeeper, Head of Association.
- Budget Tracking: Automatic warnings if a request exceeds the project budget.
- Bilingual Support: Dynamic switching between English (LTR) and Arabic (RTL).
- Digital Signatures: Automatic placement of user signatures on approved documents.
- Cloud Sync: Local SQLite database with background synchronization capability.

## Technical Stack
- C# / .NET 8 (WPF)
- SQLite with Dapper ORM
- MVVM (CommunityToolkit.Mvvm)

## Setup Instructions
1. Open the solution in Visual Studio 2022.
2. Restore NuGet packages.
3. Build and Run.
4. Default credentials: User: `admin`, Password: `admin`

## Database
The application uses a local SQLite database (`jaahd.db`). The schema is defined in `schema.sql` and is automatically applied on first run.
