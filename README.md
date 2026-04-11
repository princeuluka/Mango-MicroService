# 🥭 Mango Microservices — E-Commerce Platform

A full-featured e-commerce web application built with **.NET 7** and a **microservices architecture**. Each business domain is isolated into its own independently deployable API service, communicating asynchronously via **RabbitMQ** and synchronously via **HTTP/REST**.

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                    Mango.Web (MVC)                  │
│              ASP.NET Core Frontend Client           │
└──────────────┬──────────────────────────────────────┘
               │ HTTP (via IHttpClientFactory)
       ┌───────┴────────────────────────────────────┐
       │              API Services                  │
       │                                            │
  ┌────▼─────┐  ┌──────────┐  ┌──────────────────┐  │
  │ AuthAPI  │  │CouponAPI │  │   ProductAPI     │  │
  └──────────┘  └──────────┘  └──────────────────┘  │
  ┌────────────────────┐  ┌──────────────────────┐   │
  │  ShoppingCartAPI   │  │      EmailAPI        │   │
  └────────────────────┘  └──────────────────────┘   │
       └────────────────────────────────────────────┘
                         │
              ┌──────────▼──────────┐
              │  Mango.MessageBus   │
              │     (RabbitMQ)      │
              └─────────────────────┘
```

---

## 📦 Services & Projects

| Project | Description |
|---|---|
| `Mango.Web` | ASP.NET Core MVC frontend — the user-facing web application |
| `Mango.Services.AuthAPI` | Handles user registration, login, and JWT token generation via ASP.NET Core Identity |
| `Mango.Services.CouponAPI` | CRUD management for discount coupons, with Stripe integration for validation |
| `Mango.Services.ProductAPI` | Product catalog management — create, read, update, delete products |
| `Mango.Services.ShoppingCartAPI` | Cart management — add/remove items, apply coupons, and initiate checkout |
| `Mango.Service.EmailAPI` | Listens to RabbitMQ messages and sends transactional order confirmation emails |
| `Mango.MessageBus` | Shared library for publishing and consuming messages via RabbitMQ |

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| **Framework** | ASP.NET Core (.NET 8) |
| **Language** | C# |
| **ORM** | Entity Framework Core |
| **Database** | SQL Server (per-service, isolated databases) |
| **Authentication** | ASP.NET Core Identity + JWT Bearer Tokens |
| **Messaging** | RabbitMQ (async inter-service communication) |
| **Frontend** | ASP.NET Core MVC (Razor Views + Bootstrap) |
| **API Docs** | Swagger / OpenAPI |
| **Payments** | Stripe |

---

## ✨ Key Features

- **Microservices architecture** — each service owns its own database and can be deployed independently
- **JWT authentication** — secure token-based auth with role support (Admin / Customer)
- **Coupon & discount system** — apply coupon codes at checkout with real-time validation
- **Async messaging** — order and checkout events are published to RabbitMQ and consumed by downstream services (e.g., EmailAPI)
- **Email notifications** — automated order confirmation emails triggered by RabbitMQ messages
- **Stripe payments** — integrated payment session creation at checkout
- **Swagger UI** — every API service exposes interactive documentation at `/swagger`

---

## 🚀 Getting Started

### Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (or SQL Server Express / LocalDB)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code
- [RabbitMQ](https://www.rabbitmq.com/) (use Docker Compose for local development)
- A **Stripe** account (for payment features)

### 1. Clone the Repository

```bash
git clone https://github.com/princeuluka/Mango-MicroService.git
cd Mango-MicroService
```

### 2. Configure Each Service

Each API project has its own `appsettings.json`. Update the following values per service:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=Mango_ServiceName;Trusted_Connection=True;"
  },
  "ApiSettings": {
    "JwtOptions": {
      "Secret": "YOUR_SECRET_KEY_HERE",
      "Issuer": "mango-auth-server",
      "Audience": "mango-client"
    }
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  }
}
```

> **Note:** Each service uses its own database. EF Core migrations are included — run them per project.

### 3. Start RabbitMQ

Start RabbitMQ using Docker Compose:

```bash
docker-compose up -d
```

- **AMQP Port:** `5672` (for applications)
- **Management UI:** `http://localhost:15672` (guest/guest)

### 4. Apply Migrations

Run the following for each service that has a database:

```bash
# Example for CouponAPI
cd Mango.Services.CouponAPI
dotnet ef database update
```

Repeat for `AuthAPI`, `ProductAPI`, and `ShoppingCartAPI`.

### 5. Run the Services

Open the solution (`Mango.sln`) in Visual Studio and configure **Multiple Startup Projects**, selecting all API services and `Mango.Web`. Then press **F5**.

Alternatively, run each from the CLI:

```bash
dotnet run --project Mango.Services.AuthAPI
dotnet run --project Mango.Services.CouponAPI
dotnet run --project Mango.Services.ProductAPI
dotnet run --project Mango.Services.ShoppingCartAPI
dotnet run --project Mango.Service.EmailAPI
dotnet run --project Mango.Web
```

### 6. Access the Application

| Service | Default URL |
|---|---|
| Web App | `https://localhost:7001` |
| AuthAPI Swagger | `https://localhost:7002/swagger` |
| CouponAPI Swagger | `https://localhost:7003/swagger` |
| ProductAPI Swagger | `https://localhost:7004/swagger` |
| ShoppingCartAPI Swagger | `https://localhost:7005/swagger` |

> Port numbers may vary — check the `launchSettings.json` in each project.

---

## 🔐 Authentication Flow

1. User registers or logs in via the **Web** app, which calls `AuthAPI`.
2. `AuthAPI` returns a **JWT token**.
3. The Web app stores the token and attaches it to all subsequent API requests.
4. Each API validates the JWT using the shared secret configured in `appsettings.json`.

---

## 📬 Async Messaging Flow

1. When a cart checkout is initiated, `ShoppingCartAPI` publishes a message to **RabbitMQ**.
2. `EmailAPI` consumes the message from the queue and sends an order confirmation email to the customer.

---

## 📁 Solution Structure

```
Mango-MicroService/
├── Mango.MessageBus/           # Shared messaging library
├── Mango.Service.EmailAPI/     # Email notification service
├── Mango.Services.AuthAPI/     # Authentication & identity
├── Mango.Services.CouponAPI/   # Coupon management
├── Mango.Services.ProductAPI/  # Product catalog
├── Mango.Services.ShoppingCartAPI/ # Cart & checkout
├── Mango.Web/                  # MVC frontend
└── Mango.sln                   # Solution file
```

---

## 🤝 Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you'd like to change.

---

## 📄 License

This project is open-source and available under the [MIT License](LICENSE).
