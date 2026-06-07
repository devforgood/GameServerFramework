\# Coding Guidelines



\## Naming

\- Classes: PascalCase

\- Functions: snake\_case

\- Variables: snake\_case



\## C++

\- Use C++20

\- Prefer std::unique\_ptr over raw pointers

\- Avoid exceptions



\## Architecture

\- World logic must run on a single thread.

\- Communication between systems uses EventBroker.



\## Testing

\- Every new feature requires unit tests.

