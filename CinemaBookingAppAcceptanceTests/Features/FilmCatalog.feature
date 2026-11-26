Feature: Film catalog
  Managers maintain the films that can be scheduled in the cinema program.

  Rule: A film must have a non-empty title and genre and a positive duration

    Scenario: Create a valid film
      Given the film catalog is empty
      When the manager creates a film with title "Arrival", genre "Science Fiction", and duration 116 minutes
      Then the film is added to the catalog
      And the film has a unique identifier

    Scenario Outline: Reject a film with invalid details
      Given the film catalog is empty
      When the manager creates a film with title "<title>", genre "<genre>", and duration <duration> minutes
      Then the film is rejected as invalid
      And the film catalog remains empty

      Examples:
        | title   | genre          | duration |
        |         | Drama          | 100      |
        | Arrival |                | 100      |
        | Arrival | Science Fiction| 0        |
        | Arrival | Science Fiction| -5       |
