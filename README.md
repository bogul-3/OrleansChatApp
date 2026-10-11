# Orleans Chat App

A Discord-like chat application built using the **Microsoft Orleans** framework, supporting real-time private messaging, server creation, and group communication within server channels.

## ✨ Features

* **Private Messaging:** Direct 1-on-1 text communication between users.
* **Server Creation & Administration:** Create, manage, and configure custom chat servers.
* **Server Text Channels:** Dedicated group communication spaces within servers.
* **Virtual Actor Architecture:** Powered by Microsoft Orleans for seamless scalability and high performance.

## 🛠 Tech Stack

* **Backend Framework:** .NET / Microsoft Orleans
* **Database:** PostgreSQL (hosted via Docker)
* **Frontend / Client:** TypeScript
* **Language (Backend):** C#
* **Dependency Management:** NuGet
* **Containerization:** Docker

## 🚀 Getting Started

### Prerequisites

Make sure you have the following installed on your machine:
* [.NET SDK](https://dotnet.microsoft.com/download)
* [Docker](https://www.docker.com/)

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-username/orleans-chat-app.git
   cd orleans-chat-app
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Start the Database (PostgreSQL via Docker):**
   Run a PostgreSQL container using Docker (adjust credentials as needed):
   ```bash
   docker run --name orleans-postgres -e POSTGRES_PASSWORD=mysecretpassword -e POSTGRES_DB=orleanschat -p 5432:5432 -d postgres
   ```

## 💻 Running the Application

1. **Start the Orleans Silos (Cluster Nodes):**
   Open two separate terminal windows/tabs and run the cluster nodes on the respective ports:

   **Terminal 1:**
   ```bash
   dotnet run -- 11111 30000
   ```

   **Terminal 2:**
   ```bash
   dotnet run -- 11112 30001
   ```

2. **Run the Client:**
   Open a new terminal and start the client application (or run it via your IDE):
   ```bash
   dotnet run --project <Path-To-Client-Project>
   ```

