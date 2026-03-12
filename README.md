# 📦 Last-Mile Delivery Tracking System

## 📖 Project Description

The **Last-Mile Delivery Tracking System** is a backend API designed to manage and monitor the final stage of product delivery from distribution hubs to customers.

The system supports:

* Delivery assignment and tracking
* Route optimization
* Customer notifications
* Proof of delivery
* Billing and payment management

The application follows the **MVC architecture** and is implemented using **ASP.NET Core with Entity Framework Core**.

It also supports **role-based authentication** for:

* Admin
* Delivery Agent
* Customer

---

# 🚀 Features

## 1️⃣ Delivery Assignment & Tracking

Assign deliveries to agents and track delivery status.

**Controller**

* `DeliveryController`

  * `assignDelivery()`
  * `updateDeliveryStatus()`
  * `getDeliveryDetails()`

**Service**

* `DeliveryService`

  * Integrates with GPS for real-time tracking

---

## 2️⃣ Route Optimization

Calculates the fastest and shortest routes for delivery agents.

**Controller**

* `RouteController`

  * `generateOptimizedRoute()`
  * `getRouteDetails()`

**Service**

* `RouteService`

---

## 3️⃣ Customer Notification & Proof of Delivery

Send delivery updates and capture delivery proof.

**Controllers**

* `NotificationController`

  * `sendDeliveryUpdate()`
  * `sendOTPForDelivery()`

* `ProofController`

  * `captureProofOfDelivery()`

**Services**

* `NotificationService`
* `ProofService`

---

## 4️⃣ Billing & Payment

Handles delivery billing and payment processing.

**Controllers**

* `BillingController`

  * `generateInvoice()`
  * `getInvoiceDetails()`

* `PaymentController`

  * `makePayment()`
  * `getPaymentDetails()`

**Services**

* `BillingService`
* `PaymentService`

---

## 5️⃣ User Authentication & Authorization

Provides secure login and role-based access control.

**Controller**

* `AuthController`

  * `login()`
  * `registerUser()`
  * `logout()`
  * `getUserProfile()`

**Service**

* `AuthService`

---

# 🗄️ Database Schema

### User

| Column   | Description                       |
| -------- | --------------------------------- |
| userId   | Primary Key                       |
| username | Unique username                   |
| password | Hashed password                   |
| email    | User email                        |
| role     | ADMIN / DELIVERY_AGENT / CUSTOMER |

### Delivery

| Column          | Description                             |
| --------------- | --------------------------------------- |
| deliveryId      | Primary Key                             |
| customerId      | FK → User                               |
| address         | Delivery address                        |
| assignedAgentId | FK → User                               |
| status          | ASSIGNED / OUT_FOR_DELIVERY / DELIVERED |

### Route

| Column        | Description             |
| ------------- | ----------------------- |
| routeId       | Primary Key             |
| deliveryId    | FK → Delivery           |
| distance      | Route distance          |
| estimatedTime | Estimated delivery time |

### ProofOfDelivery

| Column     | Description        |
| ---------- | ------------------ |
| proofId    | Primary Key        |
| deliveryId | FK → Delivery      |
| photoURL   | Delivery photo     |
| signature  | Customer signature |
| timestamp  | Delivery time      |

### Invoice

| Column     | Description     |
| ---------- | --------------- |
| invoiceId  | Primary Key     |
| deliveryId | FK → Delivery   |
| amount     | Delivery charge |
| status     | PENDING / PAID  |

### Payment

| Column        | Description      |
| ------------- | ---------------- |
| paymentId     | Primary Key      |
| invoiceId     | FK → Invoice     |
| paymentAmount | Amount paid      |
| paymentDate   | Date             |
| paymentStatus | SUCCESS / FAILED |

---

# 🛠️ Tech Stack

**Backend**

* ASP.NET Core
* Entity Framework Core

**Database**

* SQL Server / MySQL

**Architecture**

* MVC
* REST API

**Authentication**

* Role-Based Access Control

---

# 📂 ASP.NET Project Folder Structure

```
LastMileDelivery.API
│
├── Controllers
│   ├── AuthController.cs
│   ├── DeliveryController.cs
│   ├── RouteController.cs
│   ├── NotificationController.cs
│   ├── ProofController.cs
│   ├── BillingController.cs
│   └── PaymentController.cs
│
├── Models
│   ├── User.cs
│   ├── Delivery.cs
│   ├── Route.cs
│   ├── ProofOfDelivery.cs
│   ├── Invoice.cs
│   └── Payment.cs
│
├── Services
│   ├── AuthService.cs
│   ├── DeliveryService.cs
│   ├── RouteService.cs
│   ├── NotificationService.cs
│   ├── ProofService.cs
│   ├── BillingService.cs
│   └── PaymentService.cs
│
├── Data
│   └── ApplicationDbContext.cs
│
├── Migrations
│
├── DTOs
│
├── Program.cs
├── appsettings.json
└── README.md
```

---

# 🗺️ ER Diagram

```
User
-----
userId (PK)
username
password
email
role

      1
      |
      | customerId
      |
Delivery
---------
deliveryId (PK)
customerId (FK)
assignedAgentId (FK)
address
status

      |
      | deliveryId
      |
Route
-----
routeId (PK)
deliveryId (FK)
distance
estimatedTime

      |
      | deliveryId
      |
ProofOfDelivery
---------------
proofId (PK)
deliveryId (FK)
photoURL
signature
timestamp

      |
      | deliveryId
      |
Invoice
-------
invoiceId (PK)
deliveryId (FK)
amount
status

      |
      | invoiceId
      |
Payment
-------
paymentId (PK)
invoiceId (FK)
paymentAmount
paymentDate
paymentStatus
```

---

# ⚙️ Installation & Setup

### 1️⃣ Clone Repository

```bash
git clone https://github.com/yourusername/last-mile-delivery-tracking.git
```

### 2️⃣ Configure Database

Edit `appsettings.json`

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=LastMileDB;Trusted_Connection=True;"
}
```

### 3️⃣ Run Migrations

```
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4️⃣ Run Application

```
dotnet run
```

---

# 📡 API Modules

* Delivery Management API
* Route Optimization API
* Notification API
* Billing & Payment API
* Authentication API

---

# 🎯 Conclusion

The **Last-Mile Delivery Tracking System** provides a modular backend system for efficient delivery management. It ensures secure authentication, optimized routing, real-time notifications, and seamless billing and payment processing.

---

