Feature: Ticket prices
  All ticket prices and booking totals are represented in euros.

  Rule: Money amounts must be positive and have no more than two decimal places

    Scenario: Accept a positive amount with two decimal places
      When a money amount of 12.50 EUR is created
      Then the amount is represented as 12.50 EUR

    Scenario: Normalize an amount to a scale of two
      When a money amount of 12 EUR is created
      Then the amount is represented as 12.00 EUR

    Scenario Outline: Reject a non-positive amount
      When a money amount of <amount> EUR is created
      Then the amount is rejected as invalid

      Examples:
        | amount |
        | 0      |
        | -1.25  |

    Scenario: Reject an amount with more than two decimal places
      When a money amount of 12.345 EUR is created
      Then the amount is rejected as invalid
