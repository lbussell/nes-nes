---
applyTo: '**/*.cs'
---

## Coding style

- Use collection expressions (`[]`) to initialize collections.
- Use `var` when the type is obvious or when the type is a primitive.
- `if`/`else` statements should always use curly braces.
- Use `is null` and `is not null` for null checks.
- Do not overuse try/catch blocks.
- Do not write ambiguous code. Always use variable names made of whole words.
- If an expression is getting complicated, break it into multiple lines.

## Organization

- Each file should only include one type.
- Always use file-scoped namespaces.
- Namespaces should match the directory structure.

## Coding guidelines

- Prefer records over classes for new data types.
