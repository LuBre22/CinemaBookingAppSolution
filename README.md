# Cinema booking demo

Run the interactive application with:

```sh
dotnet run --project CinemaBookingApp/CinemaBookingApp.csproj
```

Choose manager or customer mode. The manager can create films, schedule showings in either of two demo rooms, view the schedule and cancel showings. The customer can browse today's program or the next seven days, add available seats to a cart, pay for individual cart items, view bookings and cancel confirmed bookings.

The demo starts with a film and a showing. Data is held in memory and is lost when the application exits. A new customer receives an ID; save it to return to the same customer when switching modes during that run. Cart items are received bookings but do not reserve seats until payment is approved and the booking is committed.

Payment and authentication are simulated: choose `pass` or `fail` when prompted. The cancellation choice controls authentication; the refund stub always succeeds. Cancelling a showing also cancels its received and confirmed bookings.
