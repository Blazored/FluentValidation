### What does this test?
This test checks if the `GetFailuresFromLastValidation` method works correctly. It does so by both using `Validate`
and `ValidateAsync`. The failures given back are then checked for severity.

To test warnings, the age of a person can be set to 69.
