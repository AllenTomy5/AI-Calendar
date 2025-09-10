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
