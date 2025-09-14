# Step 1: API Style Evaluation for AICalendar

## API Styles Considered
We evaluated **REST**, **GraphQL**, and **gRPC** against the domain needs of AICalendar:  
- Events, calendars, attendees, reminders, time zones, recurring rules, invitations  
- Clients: mobile apps, web apps, and server-to-server communication  
- Network patterns: latency, payload size, streaming needs  
- Operational concerns: versioning, rollout, observability, and security  

---

## Comparison Matrix

| Criteria                  | REST (.NET Core Web API) | GraphQL (HotChocolate)  | gRPC (.NET gRPC) |
|---------------------------|--------------------------|--------------------------|------------------|
| **Ease of Integration**   | ⭐⭐⭐ Widely supported    | ⭐⭐ Needs tooling        | ⭐ Limited for web |
| **Performance**           | ⭐⭐ Moderate             | ⭐⭐⭐ Optimized queries   | ⭐⭐⭐ High (binary) |
| **Streaming Support**     | ❌ Polling only          | ✅ Subscriptions        | ✅ Native |
| **Tooling in .NET**       | Huge (Swagger, EF Core) | Good (HotChocolate)     | Strong (ProtoBuf) |
| **Versioning**            | ✅ Mature & simple       | ⚠️ Schema evolution hard| ✅ Proto evolution |
| **Security**              | ✅ JWT/OAuth supported   | ⚠️ Field-level tricky   | ✅ TLS + JWT/mTLS |
| **Best for**              | Public APIs, integrations| Mobile/Web clients      | Microservices, real-time |

---

## Decision
For **Phase 1**, we will implement **REST with ASP.NET Core Web API**.  
- Simple and fast to develop.  
- Best support for external integrations (Google Calendar, Outlook).  
- Rich tooling (Swagger/OpenAPI, Postman, EF Core).  
- Easier onboarding for new developers.  

In **later phases**, we may adopt a **hybrid model**:  
- **GraphQL** for flexible mobile/web queries.  
- **gRPC** for internal microservices and real-time reminders.  

---

## Risks & Mitigations
- **Risk:** REST may cause over-fetching/under-fetching.  
  - *Mitigation:* Use pagination, filtering, and projection (select specific fields).  
- **Risk:** Limited real-time support.  
  - *Mitigation:* Use SignalR/WebSockets initially; later migrate to gRPC for streaming.  
- **Risk:** Future schema evolution may require breaking changes.  
  - *Mitigation:* Version APIs using `/api/v1/`, `/api/v2/` routes and deprecation policy.  

---

## Security
- Use **JWT/OAuth2** for authentication.  
- Enforce **rate limiting** at the API gateway level.  
- Apply **role-based access control (RBAC)** to differentiate organizers vs attendees.  

---

## Scalability & Cost
- REST scales well in ASP.NET Core with load balancers.  
- Costs remain predictable due to stateless design.  
- Hybrid model can be introduced later without breaking existing REST APIs.

---

## Day 2 Progress

### Chosen API Style and Justification

We have implemented a **RESTful API** using ASP.NET Core Web API.  
**Justification:**  
- REST is simple, widely supported, and ideal for public APIs and integrations (e.g., Google Calendar, Outlook).
- ASP.NET Core provides robust tooling (Swagger/OpenAPI, EF Core, Serilog).
- REST enables rapid development and onboarding for new developers.
- Future extensibility: We can add GraphQL or gRPC endpoints later if needed.

### Local Development Setup

1. **Clone the repository:**
   ```sh
   git clone <your-repo-url>
   cd AiCalendar
   ```

2. **Run the API:**
   ```sh
   dotnet run --project AiCalendar.Api
   ```

3. **Swagger UI:**  
   Visit [http://localhost:5000/swagger](http://localhost:5000/swagger) (or the port shown in your terminal) for interactive API docs.

### Example API Calls

#### Get Event by ID

```sh
curl -X GET "http://localhost:5000/api/Calendar/1" -H "accept: application/json"
```

#### Create a New Event

```sh
curl -X POST "http://localhost:5000/api/Calendar" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Team Meeting",
    "startDate": "2024-07-01T10:00:00Z",
    "endDate": "2024-07-01T11:00:00Z",
    "location": "Conference Room",
    "description": "Monthly sync-up"
  }'
```

#### Parse Natural Language Event

```sh
curl -X POST "http://localhost:5000/api/Calendar/parse" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{ "text": "Lunch with Alex tomorrow at noon" }'
```

---

**Note:**  
Replace `localhost:5000` with your actual host/port if different.  
Authentication headers may be required if JWT is enabled.

---




