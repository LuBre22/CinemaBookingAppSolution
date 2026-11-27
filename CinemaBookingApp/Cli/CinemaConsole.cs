using System.Globalization;
using CinemaBookingApp.Application.Actors;
using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Authentification;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Payment;
using CinemaBooking = CinemaBookingApp.Application.Booking.Booking;
using CinemaProgram = CinemaBookingApp.Application.Scheduler.Program;

namespace CinemaBookingApp.Cli;

public sealed class CinemaConsole
{
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly FilmCatalog _catalog = new();
    private readonly Schedule _schedule = new();
    private readonly Bookings _bookings = new(new ExternalAuthenticator(true));
    private readonly List<Room> _rooms = new();
    private readonly Dictionary<Guid, List<CinemaBooking>> _carts = new();
    private readonly ExternalPaymentSystem _refundSystem = new(true);

    public CinemaConsole(TextReader input, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        _input = input;
        _output = output;
        SeedDemo();
    }

    public void Run()
    {
        _output.WriteLine("Cinema demo. Data is kept only until the application exits.");
        try
        {
            while (true)
            {
                _output.WriteLine("\nMode: 1 Manager | 2 Customer | 0 Exit");
                switch (Read("Choice: "))
                {
                    case "1": ManagerMenu(); break;
                    case "2": CustomerMenu(ReadCustomerId()); break;
                    case "0": _output.WriteLine("Goodbye."); return;
                    default: _output.WriteLine("Invalid choice."); break;
                }
            }
        }
        catch (EndOfInputException)
        {
            _output.WriteLine("\nGoodbye.");
        }
    }

    private void SeedDemo()
    {
        var seats = new[] { "A1", "A2", "A3", "A4", "A5", "B1", "B2", "B3", "B4", "B5" };
        _rooms.Add(Room.GetOrCreate(Guid.NewGuid(), seats));
        _rooms.Add(Room.GetOrCreate(Guid.NewGuid(), seats));
        Manager.CreateFilm(_catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        var film = _catalog.GetAllMovies().Single();
        Manager.AddShowing(_catalog, _schedule, _rooms[0], Guid.NewGuid(), film, DateTime.Now.AddHours(2), 12.50m);
    }

    private Guid ReadCustomerId()
    {
        while (true)
        {
            var value = Read("Customer ID (blank creates a new ID): ");
            if (value.Length == 0)
            {
                var created = Guid.NewGuid();
                _output.WriteLine($"Customer ID: {created} (save it to return to this account)");
                return created;
            }
            if (Guid.TryParse(value, out var id) && id != Guid.Empty)
            {
                _output.WriteLine($"Customer ID: {id}");
                return id;
            }
            _output.WriteLine("Enter a non-empty customer GUID, or leave blank.");
        }
    }

    private void ManagerMenu()
    {
        while (true)
        {
            _output.WriteLine("\nManager: 1 Create film | 2 Add showing | 3 View schedule | 4 Cancel showing | 0 Back");
            var choice = Read("Choice: ");
            if (choice == "0") return;
            try
            {
                switch (choice)
                {
                    case "1": CreateFilm(); break;
                    case "2": AddShowing(); break;
                    case "3": DisplaySchedule(); break;
                    case "4": CancelShowing(); break;
                    default: _output.WriteLine("Invalid choice."); break;
                }
            }
            catch (ArgumentException ex) { _output.WriteLine($"Operation failed: {ex.Message}"); }
            catch (InvalidOperationException ex) { _output.WriteLine($"Operation failed: {ex.Message}"); }
            catch (OverflowException ex) { _output.WriteLine($"Operation failed: {ex.Message}"); }
        }
    }

    private void CreateFilm()
    {
        var title = Read("Title: ");
        _output.WriteLine($"Genres: {string.Join(", ", Enum.GetNames<Genre>())}");
        var genreText = Read("Genre: ").Replace(" ", "", StringComparison.Ordinal);
        if (!Enum.TryParse<Genre>(genreText, true, out var genre) || !Enum.IsDefined(genre))
        {
            _output.WriteLine("Invalid genre.");
            return;
        }
        if (!int.TryParse(Read("Duration in minutes: "), NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
        {
            _output.WriteLine("Invalid duration.");
            return;
        }
        Manager.CreateFilm(_catalog, title, genre, TimeSpan.FromMinutes(minutes));
        _output.WriteLine($"Film created: {title}");
    }

    private void AddShowing()
    {
        var films = _catalog.GetAllMovies().ToList();
        _output.WriteLine("Films:");
        for (var i = 0; i < films.Count; i++)
            _output.WriteLine($"{i + 1}. {films[i].Title} ({films[i].Genre}, {films[i].Duration.TotalMinutes:0} min)");
        var film = Select(films, "Film number (0 to go back): ");
        if (film is null) return;

        _output.WriteLine("Rooms:");
        for (var i = 0; i < _rooms.Count; i++)
            _output.WriteLine($"{i + 1}. Room {i + 1} ({_rooms[i].Capacity} seats)");
        var room = Select(_rooms, "Room number (0 to go back): ");
        if (room is null) return;

        if (!DateTime.TryParseExact(Read("Start (yyyy-MM-dd HH:mm, local time): "), "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
        {
            _output.WriteLine("Invalid date or time.");
            return;
        }
        if (!decimal.TryParse(Read("Ticket price in EUR (e.g. 12.50): "), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var price))
        {
            _output.WriteLine("Invalid price.");
            return;
        }
        var id = Guid.NewGuid();
        Manager.AddShowing(_catalog, _schedule, room, id, film, start, price);
        _output.WriteLine($"Showing scheduled: {id}");
    }

    private List<Showing> DisplaySchedule()
    {
        var showings = _schedule.ScheduleEntries.Select(line => line.Showing).OrderBy(showing => showing.StartingTime).ToList();
        if (showings.Count == 0) _output.WriteLine("No scheduled showings.");
        for (var i = 0; i < showings.Count; i++)
        {
            var showing = showings[i];
            _output.WriteLine($"{i + 1}. {showing.Film.Title} | {showing.StartingTime:yyyy-MM-dd HH:mm} | Room {RoomNumber(showing.Room)} | {showing.Status} | {FormatPrice(showing.TicketPrice)} EUR");
        }
        return showings;
    }

    private void CancelShowing()
    {
        var showings = DisplaySchedule();
        if (showings.Count == 0) return;
        var showing = Select(showings, "Showing number to cancel (0 to go back): ");
        if (showing is null) return;
        Manager.CancelShowing(_schedule, showing.ShowingId, _refundSystem);
        _output.WriteLine("Showing cancelled. Associated bookings were cancelled and refunded.");
    }

    private void CustomerMenu(Guid customerId)
    {
        if (!_carts.ContainsKey(customerId)) _carts.Add(customerId, new List<CinemaBooking>());
        while (true)
        {
            _output.WriteLine("\nCustomer: 1 Today's program | 2 Next seven days | 3 Cart and payment | 4 My bookings | 5 Cancel booking | 0 Back");
            var choice = Read("Choice: ");
            if (choice == "0") return;
            try
            {
                switch (choice)
                {
                    case "1": BrowseProgram(customerId, 1); break;
                    case "2": BrowseProgram(customerId, 7); break;
                    case "3": PayCart(customerId); break;
                    case "4": DisplayBookings(customerId); break;
                    case "5": CancelBooking(customerId); break;
                    default: _output.WriteLine("Invalid choice."); break;
                }
            }
            catch (ArgumentException ex) { _output.WriteLine($"Operation failed: {ex.Message}"); }
            catch (InvalidOperationException ex) { _output.WriteLine($"Operation failed: {ex.Message}"); }
            catch (UnauthorizedAccessException ex) { _output.WriteLine($"Operation failed: {ex.Message}"); }
            catch (OverflowException ex) { _output.WriteLine($"Operation failed: {ex.Message}"); }
        }
    }

    private void BrowseProgram(Guid customerId, int days)
    {
        var today = DateTime.Today;
        var lines = CinemaProgram.GetProgram(_schedule, today, today.AddDays(days).AddTicks(-1));
        if (lines.Count == 0)
        {
            _output.WriteLine("No upcoming showings in this period.");
            return;
        }
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var showing = _schedule.GetShowing(line.ShowingId)!;
            var seats = line.AvailableSeats.Where(seat => seat.Value == SeatAvailabilityTypes.AVAILABLE).Select(seat => seat.Key);
            _output.WriteLine($"{i + 1}. {line.Title} | {line.StartTime:yyyy-MM-dd HH:mm} | Room {RoomNumber(line.Room)} | {FormatPrice(showing.TicketPrice)} EUR | Available: {string.Join(", ", seats)}");
        }
        var selected = Select(lines, "Showing number to book (0 to go back): ");
        if (selected is null) return;
        var seatInput = Read("Seat IDs separated by commas (e.g. A1,A2): ");
        var seatsSelected = seatInput.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var result = BookingTool.Receive(_bookings, _schedule, selected.ShowingId, customerId, seatsSelected);
        if (result.Booking is null)
        {
            _output.WriteLine($"Booking rejected: {result.DenialReason}.");
            return;
        }
        _carts[customerId].Add(result.Booking);
        _output.WriteLine($"Added to cart: {FormatPrice(result.Booking.Price)} EUR. Seats are not reserved until payment succeeds.");
    }

    private void PayCart(Guid customerId)
    {
        var cart = _carts[customerId];
        cart.RemoveAll(booking => booking.Status != BookingStatusTypes.RECEIVED);
        if (cart.Count == 0)
        {
            _output.WriteLine("Your cart is empty.");
            return;
        }
        for (var i = 0; i < cart.Count; i++)
            _output.WriteLine($"{i + 1}. {Describe(cart[i])}");
        var booking = Select(cart, "Cart item to pay (0 to go back): ");
        if (booking is null) return;
        var approve = ReadOutcome("Payment");
        try
        {
            BookingTool.CommitBooking(_schedule, booking, new ExternalPaymentSystem(approve));
            _output.WriteLine(booking.Status == BookingStatusTypes.CONFIRMED
                ? "Payment approved. Booking CONFIRMED."
                : "Payment declined. Booking DENIED.");
        }
        catch (InvalidOperationException ex)
        {
            _output.WriteLine($"Could not commit booking: {ex.Message} Status: {booking.Status}.");
        }
        finally
        {
            if (booking.Status != BookingStatusTypes.RECEIVED) cart.Remove(booking);
        }
    }

    private IReadOnlyList<CinemaBooking> DisplayBookings(Guid customerId)
    {
        var bookings = _bookings.ViewMyBookings(customerId);
        if (bookings.Count == 0) _output.WriteLine("You have no bookings.");
        for (var i = 0; i < bookings.Count; i++)
            _output.WriteLine($"{i + 1}. {Describe(bookings[i])}");
        return bookings;
    }

    private void CancelBooking(Guid customerId)
    {
        var bookings = DisplayBookings(customerId);
        if (bookings.Count == 0) return;
        var booking = Select(bookings, "Booking number to cancel (0 to go back): ");
        if (booking is null) return;
        var authenticate = ReadOutcome("Authentication");
        try
        {
            BookingTool.CancelBooking(booking, customerId, new ExternalAuthenticator(authenticate), _refundSystem);
            _output.WriteLine("Booking CANCELLED. Refund approved.");
        }
        catch (UnauthorizedAccessException ex) { _output.WriteLine($"Cancellation failed: {ex.Message}"); }
        catch (InvalidOperationException ex) { _output.WriteLine($"Cancellation failed: {ex.Message}"); }
    }

    private string Describe(CinemaBooking booking) =>
        $"{booking.Showing.Film.Title} | {booking.Showing.StartingTime:yyyy-MM-dd HH:mm} | Seats: {string.Join(", ", booking.SeatNumbers)} | {FormatPrice(booking.Price)} EUR | {booking.Status}";

    private static string FormatPrice(decimal price) => price.ToString("0.00", CultureInfo.InvariantCulture);

    private int RoomNumber(Room room) => _rooms.IndexOf(room) + 1;

    private bool ReadOutcome(string name)
    {
        while (true)
        {
            var outcome = Read($"{name} outcome (pass/fail): ");
            if (outcome.Equals("pass", StringComparison.OrdinalIgnoreCase)) return true;
            if (outcome.Equals("fail", StringComparison.OrdinalIgnoreCase)) return false;
            _output.WriteLine("Enter pass or fail.");
        }
    }

    private T? Select<T>(IReadOnlyList<T> items, string prompt) where T : class
    {
        var value = Read(prompt);
        if (value == "0") return null;
        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= 1 && number <= items.Count)
            return items[number - 1];
        _output.WriteLine("Invalid choice.");
        return null;
    }

    private string Read(string prompt)
    {
        _output.Write(prompt);
        _output.Flush();
        return (_input.ReadLine() ?? throw new EndOfInputException()).Trim();
    }

    private sealed class EndOfInputException : Exception;
}
