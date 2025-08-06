# YouTube Live Chat Watcher

This project is a **web application that watches YouTube live chat messages in real-time** and filters them based on specific keywords. It is built with **.NET Core MVC** and utilizes the **YouTube Data API v3**.

## Features

- Real-time monitoring of live chat messages
- Keyword-based message filtering
- Display filtered messages on a web interface
- Supports multiple live stream IDs
- API rate limit handling (adaptive polling intervals)
- Simple and responsive web UI (Bootstrap or Tailwind CSS compatible)

## Tech Stack

- **.NET 5 / .NET 6 MVC**
- **YouTube Data API v3**
- **C# HttpClient** (for API requests)
- **SignalR** (for real-time UI updates) *(Optional but recommended)*
- **Entity Framework Core** (for managing watched streams & keyword filters)
- **SQL Server or SQLite** (Database)
- **Bootstrap / Tailwind CSS** (Frontend)

## Setup Instructions

### 1. Get YouTube API Key
- Go to [Google Cloud Console](https://console.cloud.google.com/), create a new project.
- Enable **YouTube Data API v3** and generate an **API Key**.

### 2. Clone the Repository
```bash
git clone https://github.com/yourusername/youtube-chat-watcher.git
cd youtube-chat-watcher
