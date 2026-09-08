
# OtakuQuest

**OtakuQuest** is a gamified, RPG-style Task Management (Todo) application. Turn your daily chores and goals into epic quests, level up your character, and earn rewards to unlock new gear and avatars!

## Features

* **RPG Mechanics**: Complete real-world tasks to gain XP, level up, and increase your character's stats (HP, STR, INT, DEF, Gems).
* **Dynamic Quest Board**: Create, track, and complete your tasks as "Quests".
  * **Quest Types**: Study, Workout, Hobby, Social, Health.
  * **Difficulty Ranks**: From *E Rank* (Easy) up to *S Rank* (Boss-level).
* **Inventory & Equipment System**: Start with default gear and unlock new Avatars, Weapons, and Backgrounds to customize your profile.
* **Seamless UI/UX**: A modern, glassmorphism-inspired React frontend with smooth animations, collapsible quest cards, and silent background data fetching.
* **Secure Authentication**: Full user registration and login system protected by JWT (JSON Web Tokens) and BCrypt password hashing.
* **Type-Safe Architecture**: Fully typed communication between the C# backend and React frontend using OpenAPI generated DTOs.

## Tech Stack

**Frontend:**
* React (TypeScript)
* Custom CSS (Modern Glassmorphism & Flexbox Layouts)
* `openapi-typescript-codegen` (For auto-generating API clients and DTOs)

**Backend:**
* C# .NET 10 LTS (Web API)
* Entity Framework Core (EF Core)
* SQL Server (LocalDB for development)
* JWT Authentication & BCrypt

## Getting Started

Follow these instructions to get a copy of the project up and running on your local machine for development and testing.

### Prerequisites
* [Node.js](https://nodejs.org/) (v18 or higher)
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (stable; selected by `global.json`)
* [Visual Studio 2026](https://visualstudio.microsoft.com/) or VS Code with C# tooling
* SQL Server Express / LocalDB

### 1. Backend Setup (C# .NET)
1. Open the `OtakuQuest.Server` solution in Visual Studio.
2. Open the **Package Manager Console** and run the database migrations to build your local SQL database:
   ```powershell
   Update-Database
   ```

3.  Press `F5` or click **Run** to start the API. The Swagger documentation will open in your browser (usually `https://localhost:xxxx`).

### 2. Frontend Setup (React)

1.  Open a new terminal and navigate to the frontend client directory:
    ```bash
    cd OtakuQuest.Client
    ```
2.  Install the necessary dependencies:
    ```bash
    npm install
    ```
3.  *(Optional)* If you made changes to the backend API, regenerate the TypeScript client:
    ```bash
    npm run generate-api
    ```
4.  Start the development server:
    ```bash
    npm run dev
    ```
5.  Open your browser and navigate to `http://localhost:5173` (or the port Vite provides) to start your adventure\!

