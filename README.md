# Cinema Booking App

This .NET cinema demo lets managers create films, schedule and cancel showings, and view the schedule. Customers can browse upcoming showings, select seats, pay for bookings, view their booking history, and cancel eligible bookings.

**Payment and authentication are outside the system's scope.** The app uses configurable stubs to simulate success or failure; no money is transferred and customer identities are not verified. The CLI's customer ID and mode selection are for demonstration, not security. Refunds always succeed in the supplied stub. There is no production database: films, showings, carts and bookings exist only in memory and are lost when the process exits.

## Run the CLI

Install the .NET 10 SDK, then run from the repository root:

```sh
dotnet run --project CinemaBookingApp/CinemaBookingApp.csproj
```

The app starts with two demo rooms, a film and a showing. Select **manager** or **customer** mode:

- **Manager:** create films, add showings, view the current schedule or cancel a showing. Start times use local time in `yyyy-MM-dd HH:mm` format; enter EUR prices using a decimal point, such as `12.50`. A showing cancellation also cancels its received and confirmed bookings.
- **Customer:** leave the ID blank to generate one, or enter an existing customer GUID to return to that customer's bookings and cart during the same run. Browse today or the next seven calendar days, choose a showing and enter comma-separated seat IDs such as `A1,A2`.
- A selection enters the **cart** as a received booking. Pay for individual cart items by choosing `pass` or `fail` for the payment stub. Seats are not reserved until payment is approved and the booking is committed, so a seat can become unavailable while it is in a cart.
- View booking history or choose a confirmed booking to cancel. The cancellation `pass`/`fail` prompt controls the authentication stub, not the refund. Choose `0` to go back or exit.

## Architecture and design

The project separates its console interface, application operations and domain concepts into directories. The CLI translates user input into application calls; application classes coordinate the operations; domain classes and helpers hold concepts such as films, prices and time rules. This organization keeps the booking rules usable without the CLI, although it is not a fully isolated Clean Architecture: for example, the application directly uses the payment and authentication stubs, and the payment stub references the booking type.

The in-memory design keeps the demonstration self-contained. It also means the program has no durable storage, real service integrations or production authentication. Identifiers are GUIDs; the CLI uses numbered lists to make selection easier.

### Application flow

1. `Manager` creates catalog films and schedules a film in a room. A showing needs a future start and a valid price; the room checks for overlap with existing showings, including a 30-minute turnaround buffer.
2. The schedule contains individual showings. The customer program filters to planned showings in the requested date range, groups them by film and orders each film's showings by start time.
3. `BookingTool.Receive` validates the showing and the selected seats. A valid selection is stored as `RECEIVED`, with its price calculated from the ticket price and seat count; rejected initial requests are not saved.
4. `CommitBooking` consults the payment stub, rechecks the showing and all selected seats, and reserves them together. Success changes the booking to `CONFIRMED`; failed payment or an unavailable selection produces `DENIED`. If payment was approved but reservation fails, the refund stub is invoked.
5. A customer can cancel their own confirmed booking before the showing starts. This invokes the refund stub, releases its seats and sets `CANCELLED`. Cancelling a showing also cancels and refunds its received and confirmed bookings. Cancelled showings and created bookings remain available in memory for audit and history.

Seat availability belongs to each showing rather than the room itself, so bookings for one screening do not affect another. A room owns its seat layout; a showing takes its own availability map. The map is exposed read-only, while booking operations coordinate reservation and release. Locks protect the relevant in-process booking and seat changes; this is not a distributed or persistent concurrency solution.

Showing status is derived from local time as `PLANNED`, `RUNNING` or `OVER`, unless it has been explicitly cancelled. Booking history includes received, confirmed, denied and cancelled bookings, ordered by showing start. Money is represented in EUR using `decimal`: validation requires a positive amount with at most two decimal places, and normalization adds trailing zeroes without rounding.

### Directory guide

| Directory or file | Responsibility |
| --- | --- |
| `CinemaBookingApp/Program.cs` | Entry point; starts the console application. |
| `CinemaBookingApp/Cli/` | Interactive menus, demo data, input/output, customer carts and calls into the application. `CinemaConsole` accepts a reader and writer so its flows can be tested without an interactive terminal. |
| `CinemaBookingApp/Application/Actors/` | Manager operations for creating films and adding or cancelling showings. |
| `CinemaBookingApp/Application/Scheduler/` | The schedule and its individual `ScheduleLine` entries, cancelled-showing history, and the customer-facing program projection. |
| `CinemaBookingApp/Application/ShowRooms/` | Rooms, showing instances, seat layouts and per-showing seat availability. |
| `CinemaBookingApp/Application/BookingSystem/` | Booking data and statuses, validation results, the in-memory booking holder, and receive, commit and cancellation operations. |
| `CinemaBookingApp/Domain/Films/` | Film data, genres, film catalog and showing-status categories. |
| `CinemaBookingApp/Domain/Money/` | Price validation and two-decimal normalization helpers. |
| `CinemaBookingApp/Domain/Time/` | Time and scheduling helpers, including the turnaround-buffer check. |
| `CinemaBookingApp/Domain/Currency/` | The currency enum; the demo uses EUR. |
| `CinemaBookingApp/Domain/Payment/` | The configurable external-payment stub and its always-successful refund method. |
| `CinemaBookingApp/Domain/Authentification/` | The configurable external-authentication stub; it does not establish a real identity. |
| `CinemaBookingApp/Properties/` | Assembly settings that allow the test projects to access internal APIs. |
| `CinemaBookingAppTests/` | Unit and CLI tests, organized around the corresponding application and domain areas. |
| `CinemaBookingAppAcceptanceTests/Features/` | Gherkin feature files describing expected behavior; accompanying `.feature.cs` files are generated by Reqnroll. |
| `CinemaBookingAppAcceptanceTests/StepDefinitions/` | Bindings and scenario fixtures that exercise the application against the feature scenarios. |
| `CinemaBookingAppAcceptanceTests/Specifications.md` | The project specification and its stated goals. |
| `CinemaBookingAppSolution.slnx`, `global.json` | Solution definition and .NET test-runner configuration. |

## Testing

The unit suite uses xUnit, FluentAssertions and FakeItEasy where useful. Acceptance scenarios use Reqnroll with xUnit and exercise the feature flows; historical-showing fixtures use internal access because normal scheduling only allows future showings. Run the suites from the repository root:

```sh
dotnet test --project CinemaBookingAppTests/CinemaBookingAppTests.csproj
dotnet test --project CinemaBookingAppAcceptanceTests/CinemaBookingAppAcceptanceTests.csproj
```
