---
name: mark-code
description: Mark code locations with a MARKEDBYCLAUDE comment so they show up in the Visual Studio Task List. Use whenever the user asks to mark, flag, tag or annotate places in the code (e.g. "mark all usages of X", "flag where we call the legacy API").
---

# Mark code locations

When the user asks to mark places, find the relevant locations first, then
insert a marker comment for each one using the token `MARKEDBYCLAUDE`.
Visual Studio picks the token up in the Task List.

## Rules

1. The comment goes on its own line directly ABOVE the relevant line, with
   the same indentation as that line.
2. Format: `<comment-open> MARKEDBYCLAUDE: <short reason> <comment-close>`
   The short reason is optional but preferred (max ~10 words).
3. Use the comment syntax of the file's language (table below).
4. Only ever insert comment lines. Never change, reformat or move existing code.
5. Do not add a marker if one is already directly above that line.
6. Never put a marker inside a string, a multi-line comment, a generated
   file (*.g.cs, *.designer.cs, obj/, bin/) or a third-party/vendor folder.
7. When done, reply with a list of file:line for every marker, plus every
   place you had to skip and why.

## Comment syntax by file type

| File type                              | Marker                                   |
|----------------------------------------|------------------------------------------|
| .cs, .fs, .ts, .js, .cpp, .h, .java    | `// MARKEDBYCLAUDE: reason`              |
| .xaml, .axaml, .xml, .csproj, .props, .html, .resx | `<!-- MARKEDBYCLAUDE: reason -->` |
| .cshtml, .razor                        | `@* MARKEDBYCLAUDE: reason *@`           |
| .vb                                    | `' MARKEDBYCLAUDE: reason`               |
| .sql                                   | `-- MARKEDBYCLAUDE: reason`              |
| .css, .scss                            | `/* MARKEDBYCLAUDE: reason */`           |
| .yml, .yaml, .ps1, .sh, .py, .editorconfig | `# MARKEDBYCLAUDE: reason`           |
| .json                                  | NO COMMENTS POSSIBLE, see below          |

## Special cases

- XAML/XML: an XML comment cannot go inside an element's opening tag
  (between attributes). Put it above the whole element instead.
- Strict .json files (package.json, most configs): do NOT edit. Report the
  location in your reply only. Exception: files that are known to allow
  comments (appsettings*.json, tsconfig.json, .jsonc) may use `// MARKEDBYCLAUDE`.
- Unknown file type: don't guess. Skip it and list it in the reply.