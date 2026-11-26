Feature: Showtime programming
  Managers schedule and cancel showtimes, and customers browse the upcoming program.

  Rule: Only future showtimes with an existing film and room and a positive EUR price can be scheduled

    Scenario: Schedule a valid future showtime
      Given a film and a room exist
      And the current time is before the requested showtime
      When the manager schedules the film in that room at the requested time for 12.50 EUR
      Then the showtime is scheduled

    Scenario: Reject a showtime that is not in the future
      Given a film and a room exist
      And the requested showtime is at or before the current time
      When the manager schedules the film in that room at the requested time for 12.50 EUR
      Then the showtime is rejected
      And no showtime is saved

    Scenario: Reject scheduling with an unknown film or room
      Given either the selected film or room does not exist
      When the manager schedules a showtime using that selection
      Then the showtime is rejected
      And no showtime is saved

    Scenario: Reject a non-positive ticket price
      Given a film and a room exist
      When the manager schedules the film in that room for 0 EUR
      Then the showtime is rejected
      And no showtime is saved

  Rule: Scheduled showtimes in the same room must not overlap, including the 30-minute turnaround buffer

    Scenario: Reject a showtime that overlaps an existing scheduled showtime
      Given a scheduled showtime occupies the selected room
      And its occupied interval includes the film duration and a 30-minute buffer
      When the manager schedules another showtime whose interval overlaps it
      Then the new showtime is rejected
      And the existing showtime remains scheduled

    Scenario: Allow showtimes in different rooms at the same time
      Given a scheduled showtime occupies a room
      When the manager schedules a showtime in a different room at the same time
      Then the new showtime is scheduled

    Scenario: Ignore cancelled showtimes when checking room availability
      Given a cancelled showtime previously occupied the selected room
      When the manager schedules an overlapping showtime in that room
      Then the new showtime is scheduled

  Rule: A showtime can only be cancelled while it is scheduled and its start is in the future

    Scenario: Cancel a future scheduled showtime
      Given a future showtime is scheduled
      When the manager cancels the showtime
      Then the showtime is cancelled
      And its confirmed bookings are cancelled
      And the showtime and bookings remain available for audit

    Scenario: Reject cancellation after the showtime has started
      Given a scheduled showtime has started
      When the manager cancels the showtime
      Then the cancellation is rejected
      And the showtime remains scheduled

  Rule: The program contains only future scheduled showtimes, grouped by film and ordered by start time

    Scenario: Browse the upcoming program
      Given the program contains future scheduled, past scheduled, and cancelled showtimes
      When a customer browses the program
      Then only future scheduled showtimes are shown
      And showtimes are grouped by film
      And showtimes in each group are ordered by start time
