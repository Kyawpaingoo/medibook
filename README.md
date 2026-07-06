# 🏥 MediBook — Clinic Appointment Booking API

A production-style REST API for clinic appointment booking built with ASP.NET Core 8. Designed to handle real-world booking challenges — slot availability caching, double-booking prevention under concurrent load, and automated cloud deployment.

## Live Demo
> 🔗 `https://medibook-api-xxxxx.a.run.app/swagger` *(update after deploy)*

---

## Why I Built This

Most booking systems look simple on the surface but hide hard problems underneath — what happens when two patients book the same slot at exactly the same time? How do you keep availability data fast without hitting the database on every request? This project was built to answer those questions with production-grade solutions, not just happy-path CRUD.

---

## Architecture

The solution is split into shared projects with a clear dependency flow — Data has no project references, Infra depends only on Data, and MediBookAPI depends on both but never touches BookingDBContext or Repository<T> directly:

```
Client
  │
  │  GET /api/doctors (fetch doctors & slots)
  ▼
MediBookAPI
  ├── Controllers (DoctorController)
  │
  └── Services (DoctorServices/)
        │
        ├── IDoctorService / DoctorService
        │         │
        │         └── query via Unit of Work → return doctor data
        │
        └── IBookingUnitOfWork (injected into the service layer)
              │
              ├── IRepository<T> ─────────┐
              └── BookingUnitOfWork ───────┤── Transaction Management
                                          │   (EF Core + SaveChangesAsync)
                                          ▼
                              Update slot state:
                              Available → Reserved → Confirmed → Cancelled
                                          │
                                          ▼
                              Data (BookingDBContext)
                              ├── tbDoctors, tbPatients, tbUsers
                              ├── tbAppointments, tbSlots
                              └── tbAppointmentStatusHistory
                                          │
                                          ▼
                                   PostgreSQL (source of truth)
                                          │
                                          ▼
                                GitHub Actions CI/CD
                                          │
                                          ▼
                                GCP Cloud Run (live deployment)
```

---

## Tech Stack

| Layer | Technology                  |
|---|-----------------------------|
| API | ASP.NET Core 10             |
| Database | PostgreSQL (via EF Core)    |
| Cache | Redis (cache-aside pattern) |
| Containerisation | Docker (multi-stage build)  |
| CI/CD | GitHub Actions              |
| Cloud | GCP Cloud Run               |

---

## Key Features

### 1. Redis Cache-Aside for Slot Availability
Available slots are cached in Redis on first request. Cache is invalidated immediately on every booking or cancellation — so users always see accurate availability without hitting the database on every request.

### 2. Optimistic Concurrency — No Double Bookings
Two patients booking the same slot at the same time is the core race condition of any booking system. Solved using EF Core row-versioning — if two requests try to book the same slot simultaneously, one succeeds and the other gets a clear conflict response.

### 3. Slot State Machine
Every appointment slot moves through a defined lifecycle:

```
Available → Reserved → Confirmed → Cancelled
```

Clear state transitions with a full audit trail — no ambiguous or orphaned bookings.

### 4. Keyless CI/CD via Workload Identity Federation
No long-lived service account keys stored as GitHub secrets. Authentication between GitHub Actions and GCP uses Workload Identity Federation — a more secure, keyless approach.

---

## Project Structure

```
MediBook/
├── MediBook.Data/
│   ├── Models/
│   │   ├── tbAppointment.cs
│   │   ├── tbDoctor.cs
│   │   └── tbSlot.cs
│   │   └── tbPatients.cs
│   │   └── tbUsers.cs
│   │   └── tbAppointmentStatusHistory.cs
│   │   └── BookingDBContext.cs
│   ├── Dtos/
│   │   ├── AppointmentDtos.cs
│   │   └── DoctorDtos.cs
│   ├── Migrations/
│   └── MediBook.Data.csproj
│
├── MediBook.Infra/
│   ├── Repositories/
│   │   ├── IRepository.cs
│   │   ├── Repository.cs
│   ├── UnitOfWork/
│   │   ├── IUnitOfWork.cs
│   │   └── UnitOfWork.cs
│   ├── Caching/
│   │   ├── ICacheService.cs
│   │   └── RedisCacheService.cs
│   ├── Helpers/
│   │   └── SlotAvailabilityHelper.cs
│   └── MediBook.Infra.csproj
│
├── MediBook.Api/
│   ├── Controllers/
│   │   ├── AppointmentsController.cs
│   │   ├── DoctorsController.cs
│   │   └── HealthController.cs
│   ├── Services/
│   │   ├── IAppointmentService.cs
│   │   ├── AppointmentService.cs
│   │   └── HealthServices/
│   │       ├── IHealthService.cs
│   │       └── HealthService.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── MediBook.Api.csproj
│
├── MediBook.Tests/
│   ├── AppointmentServiceTests.cs
│   ├── AppointmentRepositoryTests.cs
│   └── MediBook.Tests.csproj
│
├── .github/
│   └── workflows/
│       └── ci-cd.yml
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
├── .gitignore
├── MediBook.sln
└── README.md
```

---

## API Endpoints

| Method | Route | Description                                   |
|---|---|-----------------------------------------------|
| `GET` | `/api/doctor/getbypaging` | List doctors with paging, sorting, and search |
| `GET` | `/api/doctor/getbyid?id={guid}` | Get a single doctor by ID                     |
| `POST` | `/api/doctor/create` | Create a new doctor                           |
| `PUT` | `/api/doctor/update` | Update doctor data                            |
| `DELETE` | `/api/doctor/softdelete?id={guid}` | Deactivate a doctor (soft delete)             |
| `DELETE` | `/api/doctor/harddelete?id={guid}` | Permanently delete a doctor                   |
| `POST` | `/api/appointments` | Book a slot                                   |
| `GET` | `/api/appointments/{id}` | Get booking details                           |
| `DELETE` | `/api/appointments/{id}` | Cancel a booking                              |
| `GET` | `/health` | Liveness probe                                |
| `GET` | `/health/ready` | Readiness — checks PostgreSQL + Redis         |

---

## Running Locally

```bash
git clone https://github.com/Kyawpaingoo/medibook.git
cd medibook
docker-compose up
```

API available at `http://localhost:8080`
Swagger UI at `http://localhost:8080/swagger`

---

## Example Request

```bash
# Book an appointment
curl -X POST http://localhost:8080/api/appointments \
  -H "Content-Type: application/json" \
  -d '{
    "doctorId": 1,
    "slotId": 42,
    "patientName": "John Doe",
    "patientEmail": "john@example.com"
  }'
```

```json
{
  "appointmentId": "appt-001",
  "status": "Reserved",
  "doctorName": "Dr. Smith",
  "slotTime": "2026-07-01T10:00:00Z",
  "createdAt": "2026-06-29T08:00:00Z"
}
```

---

## What I Would Add With More Time

- Integration tests using Testcontainers (real PostgreSQL + Redis in CI)
- Email/SMS notification on booking confirmation
- Admin dashboard for clinic staff built in React

---

Built by Kevin · [LinkedIn](https://linkedin.com/in/kyaw-paing-oo-dev) · [GitHub](https://github.com/Kyawpaingoo)
