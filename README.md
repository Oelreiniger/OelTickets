# OelTickets

OelTickets is a simple ticketing system built with **ASP.NET Core** and **Entity Framework Core**.

It includes:
- Managing projects
- Creating and tracking tickets
- Writing comments
- User authentication and role management via **ASP.NET Core Identity**

## Prerequisites
- .NET SDK installed
- Docker installed

## How to Setup

### Install required .NET tools
Go into the `OelTicketsBackend` folder and restore the local tools:

```shell
dotnet tool restore
```

### Setup the Database
Start the MySQL + phpMyAdmin containers:

```shell
docker compose up -d
```

Apply the Entity Framework migrations to create/update the database schema:

```shell
dotnet ef database update
```

### Start
That’s it.

- Run the `OelTicketsBackend` solution.
- Open `/swagger` in your browser to view and test the available API endpoints (Swagger UI).
- The `OelTickets` folder contains a basic frontend to use and test the system.
