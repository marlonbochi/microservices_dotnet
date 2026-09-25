# Feature Specification: Store Order Flow (Products, Stock and Orders)

**Feature Branch**: `001-store-order-flow`

**Created**: 2026-09-24

**Status**: Draft

**Input**: User description: "A system with a web frontend and a backend split into microservices,
integrated through messaging, where I can register products and, on another screen, simulate the
whole ordering process of a product, controlling that product's stock. Everything runs locally so I
can study how divided microservices communicate."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manage the product catalog (Priority: P1)

As a store operator, I open the "Products" screen, register a new product (name, SKU, description,
price and initial stock quantity), and see it listed with its current available stock. I can also
edit name/description/price and add stock (restock) to an existing product.

**Why this priority**: Without products there is nothing to order. This slice alone already shows
two services cooperating: the catalog owns product data and the inventory owns stock, kept in sync
by events.

**Independent Test**: Create a product with initial stock 10 and verify it appears in the list with
"available: 10" within a few seconds; restock +5 and verify "available: 15".

**Acceptance Scenarios**:

1. **Given** the catalog is empty, **When** the operator registers "Mechanical Keyboard", SKU
   "KB-001", price 350.00, initial stock 10, **Then** the product appears in the list and, within
   5 seconds, shows 10 units available.
2. **Given** a product with SKU "KB-001" exists, **When** the operator registers another product
   with SKU "KB-001", **Then** the system rejects it with a clear "SKU already exists" message.
3. **Given** a product exists, **When** the operator submits a negative price or an empty name,
   **Then** the system rejects the input and explains which fields are invalid.
4. **Given** a product has 10 units available, **When** the operator restocks 5 units, **Then**
   available stock becomes 15.
5. **Given** a product exists, **When** the operator changes its price, **Then** new orders use the
   new price while existing orders keep the price they were placed with.

---

### User Story 2 - Place an order and watch it be processed (Priority: P1)

As a customer (simulated), I open the "Orders" screen, choose one or more products and quantities,
enter a customer name/e-mail and place the order. I then watch the order move through its
lifecycle in near real time: **Submitted → Stock Reserved → Payment Approved → Confirmed**, and see
the product's available stock decrease accordingly.

**Why this priority**: This is the core learning goal: a business process spanning several
services (orders, inventory, payment) coordinated through asynchronous messages.

**Independent Test**: With a product of stock 10, place an order for 3 units; verify the order ends
"Confirmed" and the product shows 7 available.

**Acceptance Scenarios**:

1. **Given** product A has 10 units, **When** a customer orders 3 units, **Then** the order reaches
   "Confirmed" and product A shows 7 units available.
2. **Given** an order was placed, **When** the customer views the order, **Then** they see each
   status transition with its timestamp (a timeline).
3. **Given** an order is placed, **When** its status changes, **Then** the screen reflects the new
   status without a manual page reload.
4. **Given** an order contains several items, **When** it is placed, **Then** the order total equals
   the sum of (unit price × quantity) using the prices at order time.

---

### User Story 3 - Stock shortage and payment failure with compensation (Priority: P2)

As a customer, when I order more units than are available the order is rejected with a reason; and
when the (simulated) payment is declined, any stock reserved for my order is released back, so no
units are "lost".

**Why this priority**: Shows failure handling and compensation (saga pattern), the hardest and most
instructive part of distributed processes.

**Independent Test**: Order 50 units of a product with 10 available → order "Rejected (insufficient
stock)", stock still 10. Order 2 units with "simulate payment failure" enabled → order "Cancelled
(payment declined)", stock returns to its original value.

**Acceptance Scenarios**:

1. **Given** product A has 10 units, **When** a customer orders 50 units, **Then** the order ends
   "Rejected" with reason "insufficient stock" and product A still shows 10 units.
2. **Given** product A has 10 units, **When** a customer orders 2 units with the payment-failure
   simulation enabled, **Then** stock is temporarily reserved (8 available), the payment is declined,
   the reservation is released (10 available), and the order ends "Cancelled" with reason
   "payment declined".
3. **Given** an order with 2 products where only one has enough stock, **When** it is placed,
   **Then** nothing is reserved (all-or-nothing) and the order ends "Rejected".
4. **Given** two customers simultaneously order the last unit of a product, **When** both orders
   are processed, **Then** exactly one is confirmed and the other is rejected; stock never goes
   negative.

---

### User Story 4 - Observe the distributed system (Priority: P3)

As a developer studying the system, I can open monitoring tools to see the messages flowing between
services, the queues, and an end-to-end trace of a single order across all services, plus health
status of each service.

**Why this priority**: The user's explicit goal is to learn how microservices work; seeing the
inner workings is valuable but not required for the store to function.

**Independent Test**: Place an order, open the tracing dashboard, find the trace for that order and
verify it spans the gateway, orders, inventory and payment services.

**Acceptance Scenarios**:

1. **Given** the system is running, **When** the developer opens the broker management UI, **Then**
   they see the exchanges/queues of each service.
2. **Given** an order was placed, **When** the developer opens the tracing dashboard, **Then** a
   single trace shows every service involved in that order.
3. **Given** a service's database is unreachable, **When** its readiness endpoint is queried,
   **Then** it reports "Unhealthy".

### Edge Cases

- Order with zero or negative quantity, empty item list, or unknown product → rejected at input with
  a clear validation message; nothing is published.
- A product that exists in the catalog but whose stock record has not yet been created (event still
  in flight) → treated as 0 available; the order is rejected for insufficient stock.
- The same message delivered twice by the broker → processed only once (no double reservation or
  double release).
- Payment service temporarily down → the order stays "Stock Reserved / Awaiting payment" and
  continues automatically when the service recovers (messages are durable).
- Orders service restarts in the middle of a process → the process resumes from its persisted state.
- The catalog is unreachable when an order is placed → order placement fails fast with a friendly
  "try again" error instead of hanging.
- Restock with zero or negative quantity → rejected.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow registering a product with name (1–200 chars), unique SKU
  (1–50 chars, letters, digits and dashes), optional description (≤ 1000 chars), price (> 0, two
  decimals) and initial stock (≥ 0).
- **FR-002**: System MUST list products with their price and currently available stock.
- **FR-003**: System MUST allow updating a product's name, description and price.
- **FR-004**: System MUST allow adding stock (restock) to a product with a positive quantity.
- **FR-005**: System MUST create the stock record for a new product automatically as a consequence
  of the product being registered.
- **FR-006**: System MUST allow placing an order with customer name, customer e-mail, one or more
  (product, quantity ≥ 1) items and an optional "simulate payment failure" flag.
- **FR-007**: System MUST capture each item's unit price and product name at order time.
- **FR-008**: System MUST reserve stock for all items of an order atomically (all-or-nothing).
- **FR-009**: System MUST reject an order when any item lacks available stock and record the reason.
- **FR-010**: System MUST request payment for the order total after stock is reserved; the payment
  simulation MUST decline when the failure flag is set, and approve otherwise.
- **FR-011**: System MUST release reserved stock when payment is declined (compensation) and mark
  the order "Cancelled" with the reason.
- **FR-012**: System MUST convert the reservation into a definitive stock deduction when payment
  is approved, and mark the order "Confirmed".
- **FR-013**: System MUST keep a timeline of status changes for each order and expose it.
- **FR-014**: System MUST push order status changes to the orders screen without a manual reload.
- **FR-015**: System MUST process each inter-service message exactly once from a business point of
  view (duplicates are ignored).
- **FR-016**: System MUST never let available stock become negative, including under concurrent
  orders.
- **FR-017**: System MUST expose all functionality to the frontend through a single entry point.
- **FR-018**: Each service MUST expose liveness and readiness health checks.
- **FR-019**: System MUST emit distributed traces that correlate one order across all services.
- **FR-020**: Invalid requests MUST return standardized, human-readable error details.
- **FR-021**: The entire system MUST start locally with a single command.

### Key Entities

- **Product** (Catalog): id, SKU, name, description, price, created/updated timestamps.
- **Stock Item** (Inventory): product id, SKU, quantity on hand, quantity reserved, available
  (= on hand − reserved).
- **Stock Reservation** (Inventory): order id, list of (product id, quantity), status
  (Reserved, Committed, Released).
- **Order** (Ordering): id, customer name/e-mail, items, total, status, failure reason, timeline.
- **Order Item** (Ordering): product id, product name, unit price, quantity, line total.
- **Order Status History** (Ordering): status, timestamp, note.
- **Payment** (Payment, transient): order id, amount, outcome (Approved/Declined).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can start the whole system with one command and reach the store screens
  in under 5 minutes on a first run (excluding image downloads).
- **SC-002**: A newly registered product shows its available stock within 5 seconds.
- **SC-003**: A successful order reaches "Confirmed" within 5 seconds of being placed under normal
  conditions.
- **SC-004**: In 100 concurrent orders for a product with 10 units of stock, exactly 10 units are
  sold and stock never goes below zero.
- **SC-005**: 100% of payment-declined orders restore stock to its prior value.
- **SC-006**: A single order can be followed end-to-end in one trace covering every service it
  touched.
- **SC-007**: Every functional requirement is covered by at least one automated test.

## Assumptions

- Single-tenant demo store; no authentication/authorization is required (out of scope, documented
  as a future improvement).
- A single currency (USD) is used; taxes, shipping and discounts are out of scope.
- Payment is simulated; no real payment provider is contacted.
- Order cancellation by the customer after confirmation, shipping and returns are out of scope.
- The system runs on a developer machine with Docker; production deployment (Kubernetes, cloud) is
  out of scope but the design must not preclude it.
- UI, documentation and code are in English (the repository is a public portfolio piece).
