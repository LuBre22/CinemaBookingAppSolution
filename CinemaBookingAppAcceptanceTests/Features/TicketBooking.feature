Feature: Ticket booking
  Customers select seats, book tickets, cancel their bookings, and view their booking history.

  Rule: Only future scheduled showtimes can be booked, and a booking must contain at least one valid available seat

    Scenario: Book available seats for a future showtime
      Given a future showtime is scheduled with available seats A1 and A2
      And the ticket price is 12.50 EUR per seat
      When the customer books seats A1 and A2
      Then the booking is confirmed
      And the total price is 25.00 EUR
      And seats A1 and A2 are no longer available for that showtime

    Scenario: Reject an empty seat selection
      Given a future showtime is scheduled
      When the customer books no seats
      Then the booking is rejected
      And no booking is saved

    Scenario Outline: Reject a seat that is invalid or unavailable
      Given a future showtime is scheduled with seat A1 already confirmed
      When the customer attempts to book seat "<seat>"
      Then the booking is rejected
      And no booking is saved
      And the customer's other selected seats remain unbooked

      Examples:
        | seat |
        | A1   |
        | Z99  |

    Scenario: Reject booking for a showtime that has started
      Given a scheduled showtime has started
      When the customer attempts to book an available seat
      Then the booking is rejected
      And no booking is saved

  Rule: Only confirmed bookings make seats unavailable

    Scenario: Make seats available again after their booking is cancelled
      Given a future showtime has a confirmed booking for seat A1
      When the customer cancels that booking
      Then the booking is cancelled
      And seat A1 is available for that showtime

  Rule: Customers can cancel only their own confirmed booking for a scheduled future showtime

    Scenario: Reject cancellation of another customer's booking
      Given another customer owns a confirmed booking for a future scheduled showtime
      When the customer attempts to cancel that booking
      Then the cancellation is rejected as unauthorized
      And the booking remains confirmed

    Scenario: Reject cancellation of an already cancelled booking
      Given the customer has a cancelled booking
      When the customer attempts to cancel that booking again
      Then the cancellation is rejected
      And the booking remains cancelled

    Scenario: Reject cancellation after the showtime has started
      Given the customer has a confirmed booking for a showtime that has started
      When the customer attempts to cancel that booking
      Then the cancellation is rejected
      And the booking remains confirmed

  Rule: Customers can view all of their bookings, including cancelled bookings, ordered by showtime start

    Scenario: View booking history in showtime order
      Given the customer has confirmed and cancelled bookings for multiple showtimes
      When the customer views their bookings
      Then all of those bookings are shown
      And the bookings are ordered by showtime start
