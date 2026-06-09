# Coding Guidelines

## General

* Avoid unnecessary comments.
* Prefer self-explanatory code over comments.

## Naming

* Types (classes, structs, enums, aliases): `PascalCase`
* Functions and methods: `PascalCase`
* Local variables and parameters: `snake_case`
* Member variables: `snake_case_`
* Constants: `kPascalCase`
* Macros: `ALL_CAPS_WITH_UNDERSCORES`
* Namespaces: `snake_case`
* File names: `snake_case`

## C++

* Use C++20.
* Prefer `std::unique_ptr` over raw pointers for ownership.
* Avoid exceptions.
* Prefer standard library facilities over custom implementations when practical.

## Architecture

* World logic must run on a single thread.
* Communication between systems must use `EventBroker`.
* Keep system boundaries explicit and loosely coupled.

## Testing

* Every new feature requires unit tests.
* Bug fixes should include regression tests when applicable.
