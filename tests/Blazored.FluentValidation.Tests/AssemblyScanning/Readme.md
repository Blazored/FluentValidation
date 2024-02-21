### What does this test?
This test checks if the assembly scanning works. It leverages, that this test
assembly does not register any `AbstractValidator` by default.

 - Setting the `DisableAssemblyScanning` to `true` should not find any validators and ignore errors.
 - Setting the `DisableAssemblyScanning` to `false` or not setting the attribute at all, should
   find the validators in the assembly and validate normally.
