from pathlib import Path
import re


# ============================================================
# Configuration
# ============================================================

# Root directory of your C# solution
SOLUTION_DIR = Path(__file__).resolve().parent.parent

# --------------------------------------------------------------
# STRING LITERAL searches
#
# Finds occurrences of a value inside actual C# string literals
# ("...", @"...", $"...", $@"...", @$"...").
#
#   "file": count
#
# A file not listed here may not contain the value at all.
# 0 = explicitly forbidden
# --------------------------------------------------------------
ALLOWED_SOURCES = {
    "GameTimeNext": {
        "GameTimeNext/Core/Framework/Config/AppConfig.cs": 1,
        "GameTimeNext/App.xaml.cs": 1,
        "GameTimeNext/Core/Framework/Config/AppConfigOld.cs": 1000,
    },

    "GameTimeNXT": {
        "GameTimeNext/Core/Framework/Config/AppConfigOld.cs": 1000,
    },

    "MaxPra": {
        "GameTimeNext/Core/Framework/Config/AppConfig.cs": 1,
        "GameTimeNext/App.xaml.cs": 1,
    },
}


# --------------------------------------------------------------
# CODE searches
#
# Finds occurrences of a value in actual C# *code* — i.e.
# identifiers, member accesses, method calls, etc. Comments and
# string literal contents are excluded automatically, so this
# won't double-count things already caught by ALLOWED_SOURCES,
# and won't flag the term when it merely appears inside a
# comment or a string.
#
# Same semantics as ALLOWED_SOURCES:
#
#   "file": count
#
# A file not listed here may not contain the value at all.
# 0 = explicitly forbidden
#
# Matching uses word-boundaries on any side of the term that
# starts/ends with a word character, so searching for
# "DirectorySeparatorChar" won't match inside
# "AltDirectorySeparatorChar". Symbol-only terms (e.g. "=>")
# are matched as plain substrings.
# --------------------------------------------------------------
ALLOWED_CODE_SEARCHES = {
    # Example: only AppPaths.cs is allowed to talk to
    # Path.DirectorySeparatorChar directly, everywhere else
    # should go through the centralized helper.
    # "DirectorySeparatorChar": {
    #     "GameTimeNext/Core/Framework/Config/AppPaths.cs": 5,
    # },
    "DirectorySeparatorChar": {
        "GameTimeNext/Core/Framework/Config/AppConfigOld.cs": 1000,
        "GameTimeNext/Core/Framework/LauncherIntegration/SteamImportHelper.cs": 1,
        "GameTimeNext/Core/Framework/LauncherIntegration/SteamLocatorService.cs": 1,
        "GameTimeNext/Core/Framework/Utils/FnSystem.cs": 1,
    },
}


# File types to scan
EXTENSIONS = {
    ".cs",
}


# Directories to completely ignore
IGNORED_DIRECTORIES = {
    ".git",
    ".vs",
    "bin",
    "obj",
    "packages",
}


# ============================================================
# C# Source Tokenizer
# ============================================================

def tokenize_source(source: str):
    """
    Walks the source once and returns a list of token spans for:

        - Line comments      {"type": "comment", "start", "end"}
        - Block comments      {"type": "comment", "start", "end"}
        - String literals     {"type": "string", "start",
                                "content_start", "content_end", "end"}

    Supported string types:

        "GameTimeNext"
        @"GameTimeNext"
        $"GameTimeNext"
        $@"GameTimeNext"
        @$"GameTimeNext"

    "start"/"end" always describe the full span of the token
    (including quotes/prefix for strings, and // or /* */ markers
    for comments). "content_start"/"content_end" describe just the
    text between the quotes for strings.
    """

    tokens = []

    i = 0
    length = len(source)

    while i < length:
        char = source[i]
        next_char = source[i + 1] if i + 1 < length else ""

        # ----------------------------------------------------
        # Line comment
        # ----------------------------------------------------

        if char == "/" and next_char == "/":
            start = i
            i += 2

            while i < length and source[i] != "\n":
                i += 1

            tokens.append({
                "type": "comment",
                "start": start,
                "end": i,
            })

            continue

        # ----------------------------------------------------
        # Block comment
        # ----------------------------------------------------

        if char == "/" and next_char == "*":
            start = i
            i += 2

            while i < length and not (
                source[i] == "*"
                and i + 1 < length
                and source[i + 1] == "/"
            ):
                i += 1

            i = min(i + 2, length)

            tokens.append({
                "type": "comment",
                "start": start,
                "end": i,
            })

            continue

        # ----------------------------------------------------
        # Detect string prefix
        # ----------------------------------------------------

        is_string = False
        is_verbatim = False
        is_interpolated = False

        start = i

        # @"..."
        if char == "@" and next_char == '"':
            is_string = True
            is_verbatim = True
            i += 2

        # $"..."
        elif char == "$" and next_char == '"':
            is_string = True
            is_interpolated = True
            i += 2

        # $@"..."
        elif (
            char == "$"
            and next_char == "@"
            and i + 2 < length
            and source[i + 2] == '"'
        ):
            is_string = True
            is_verbatim = True
            is_interpolated = True
            i += 3

        # @$"..."
        elif (
            char == "@"
            and next_char == "$"
            and i + 2 < length
            and source[i + 2] == '"'
        ):
            is_string = True
            is_verbatim = True
            is_interpolated = True
            i += 3

        # "..."
        elif char == '"':
            is_string = True
            i += 1

        # ----------------------------------------------------
        # Read string content
        # ----------------------------------------------------

        if is_string:
            content_start = i

            while i < length:
                char = source[i]

                # --------------------------------------------
                # Verbatim strings
                #
                # "" = escaped quote
                # --------------------------------------------

                if is_verbatim:

                    if char == '"':

                        if (
                            i + 1 < length
                            and source[i + 1] == '"'
                        ):
                            i += 2
                            continue

                        break

                # --------------------------------------------
                # Normal strings
                #
                # \" = escaped quote
                # --------------------------------------------

                else:

                    if char == "\\":
                        i += 2
                        continue

                    if char == '"':
                        break

                i += 1

            content_end = i

            # Skip closing quote
            end = min(i + 1, length)

            expr_spans = (
                find_interpolation_expression_spans(
                    source, content_start, content_end
                )
                if is_interpolated
                else []
            )

            tokens.append({
                "type": "string",
                "start": start,
                "content_start": content_start,
                "content_end": content_end,
                "end": end,
                "expr_spans": expr_spans,
            })

            i = end

            continue

        i += 1

    return tokens


def extract_string_literals(source: str, tokens=None):
    """
    Returns just the string-literal tokens, in the same shape the
    string-search logic expects: start, content_start, content.
    """

    if tokens is None:
        tokens = tokenize_source(source)

    return [
        {
            "start": token["start"],
            "content_start": token["content_start"],
            "content": source[
                token["content_start"]:token["content_end"]
            ],
        }
        for token in tokens
        if token["type"] == "string"
    ]


def find_interpolation_expression_spans(source, content_start, content_end):
    """
    Given the content region of an interpolated string ($"...",
    $@"...", @$"..."), finds every top-level {expression} span
    and returns them as (expr_start, expr_end) tuples, where
    expr_start/expr_end bound the code *inside* the braces
    (braces themselves excluded).

    Handles:
        - {{ and }} escaped braces
        - nested braces, e.g. {items.Count()}
        - nested "..."/'...' literals inside the expression, so
          braces/colons inside a nested string don't confuse
          the parser

    Does not attempt to special-case format specifiers
    (e.g. {value:N2}) or alignment components
    (e.g. {value,10}) — that text is treated as part of the
    expression span too, which is harmless for search purposes.
    """

    spans = []
    i = content_start

    while i < content_end:
        char = source[i]

        if char == "{":
            if i + 1 < content_end and source[i + 1] == "{":
                i += 2
                continue

            expr_start = i + 1
            depth = 1
            j = i + 1
            in_quote = None

            while j < content_end and depth > 0:
                cj = source[j]

                if in_quote:
                    if cj == "\\" and in_quote == '"':
                        j += 2
                        continue

                    if cj == in_quote:
                        in_quote = None

                    j += 1
                    continue

                if cj == '"' or cj == "'":
                    in_quote = cj
                    j += 1
                    continue

                if cj == "{":
                    depth += 1
                elif cj == "}":
                    depth -= 1
                    if depth == 0:
                        break

                j += 1

            expr_end = j
            spans.append((expr_start, expr_end))
            i = j + 1
            continue

        if char == "}":
            if i + 1 < content_end and source[i + 1] == "}":
                i += 2
                continue
            i += 1
            continue

        i += 1

    return spans


def build_code_text(source: str, tokens=None):
    """
    Returns a copy of `source`, the same length, with every
    string-literal and comment character blanked out (replaced
    with a space, except newlines which are preserved). This
    leaves only actual code characters in place, at their
    original positions, so regex searches against it:

        - only match real code, never comments or string content
        - keep correct line/column numbers for reporting

    Exception: for interpolated strings ($"...", $@"...",
    @$"..."), the code inside {expression} braces is left
    intact rather than blanked, since that's genuine C# code
    (e.g. $"{Path.DirectorySeparatorChar}") and should still be
    reachable by ALLOWED_CODE_SEARCHES.
    """

    if tokens is None:
        tokens = tokenize_source(source)

    chars = list(source)

    def blank(start, end):
        for idx in range(start, end):
            if chars[idx] != "\n":
                chars[idx] = " "

    for token in tokens:

        if token["type"] == "comment":
            blank(token["start"], token["end"])
            continue

        if token["type"] == "string":
            expr_spans = token.get("expr_spans") or []

            pos = token["start"]

            for expr_start, expr_end in expr_spans:
                blank(pos, expr_start)
                pos = expr_end

            blank(pos, token["end"])

    return "".join(chars)


def build_search_pattern(term: str):
    """
    Builds a regex for a code search term. Applies a word-boundary
    on a side only if that side of the term is itself a word
    character, so identifier searches ("DirectorySeparatorChar")
    won't match inside longer identifiers
    ("AltDirectorySeparatorChar"), while symbol-only terms
    (e.g. "=>") still work as plain substring matches.
    """

    def is_word_char(c):
        return c.isalnum() or c == "_"

    prefix = r"\b" if term and is_word_char(term[0]) else ""
    suffix = r"\b" if term and is_word_char(term[-1]) else ""

    return re.compile(prefix + re.escape(term) + suffix)


# ============================================================
# File Scanner
# ============================================================

def _find_occurrences(search: str, allowed_map, relative_path,
                       source, lines, path, kind, positions):
    """
    Given a list of absolute character positions where `search`
    was found, checks them against the allowed count for this
    file and returns violation records (or an empty list).
    """

    results = []

    if not positions:
        return results

    allowed_count = allowed_map.get(relative_path, 0)
    actual_count = len(positions)

    if actual_count <= allowed_count:
        return results

    for position in positions:

        line_number = source.count("\n", 0, position) + 1

        line_start = source.rfind("\n", 0, position) + 1
        column = position - line_start + 1

        line = (
            lines[line_number - 1]
            if line_number <= len(lines)
            else ""
        )

        results.append({
            "search": search,
            "kind": kind,
            "file": path,
            "relative_path": relative_path,
            "line": line_number,
            "column": column,
            "text": line.strip(),
            "actual_count": actual_count,
            "allowed_count": allowed_count,
        })

    return results


def scan_file(path: Path):
    results = []

    try:
        source = path.read_text(
            encoding="utf-8-sig"
        )

    except (UnicodeDecodeError, OSError) as ex:
        print(
            f"WARNING: Could not read "
            f"{path}: {ex}"
        )

        return results

    relative_path = (
        path
        .relative_to(SOLUTION_DIR)
        .as_posix()
    )

    tokens = tokenize_source(source)
    literals = extract_string_literals(source, tokens)
    code_text = build_code_text(source, tokens)

    lines = source.splitlines()

    # --------------------------------------------------------
    # String literal searches
    # --------------------------------------------------------

    for search, allowed_map in ALLOWED_SOURCES.items():

        positions = []

        for literal in literals:
            for match in re.finditer(
                re.escape(search),
                literal["content"]
            ):
                positions.append(
                    literal["content_start"] + match.start()
                )

        results.extend(
            _find_occurrences(
                search, allowed_map, relative_path,
                source, lines, path, "string", positions,
            )
        )

    # --------------------------------------------------------
    # Code searches
    # --------------------------------------------------------

    for search, allowed_map in ALLOWED_CODE_SEARCHES.items():

        pattern = build_search_pattern(search)

        positions = [
            match.start()
            for match in pattern.finditer(code_text)
        ]

        results.extend(
            _find_occurrences(
                search, allowed_map, relative_path,
                source, lines, path, "code", positions,
            )
        )

    return results


# ============================================================
# Solution Scanner
# ============================================================

def scan_solution(root: Path):
    results = []

    for path in root.rglob("*"):

        # Ignore directories/files that aren't files
        if not path.is_file():
            continue

        # Only scan configured extensions
        if path.suffix.lower() not in EXTENSIONS:
            continue

        # Ignore configured directories
        if any(
            part in IGNORED_DIRECTORIES
            for part in path.parts
        ):
            continue

        results.extend(
            scan_file(path)
        )

    return results


# ============================================================
# Main
# ============================================================

def main():

    print(
        f"Scanning: {SOLUTION_DIR}\n"
    )

    results = scan_solution(
        SOLUTION_DIR
    )

    # --------------------------------------------------------
    # No violations
    # --------------------------------------------------------

    if not results:

        print(
            "Everything looks good."
        )

        return 0

    # --------------------------------------------------------
    # Violations
    # --------------------------------------------------------

    print(
        "=== HARDCODED VALUES "
        "OUTSIDE ALLOWED LIMITS ===\n"
    )

    # Group by kind + value + file
    grouped = {}

    for result in results:

        key = (
            result["kind"],
            result["search"],
            result["relative_path"],
        )

        grouped[key] = result

    # --------------------------------------------------------
    # Print violations
    # --------------------------------------------------------

    for result in grouped.values():

        kind_label = (
            "STRING"
            if result["kind"] == "string"
            else "CODE"
        )

        print(
            f'[{kind_label:<6}] '
            f'{result["search"]:<24} '
            f'{result["relative_path"]}'
        )

        print(
            f'                          '
            f'Found: {result["actual_count"]} '
            f'(allowed: {result["allowed_count"]})'
        )

        print()

        # Print every occurrence
        for occurrence in results:

            if (
                occurrence["kind"] == result["kind"]
                and
                occurrence["search"] == result["search"]
                and
                occurrence["relative_path"]
                == result["relative_path"]
            ):

                print(
                    f'                          '
                    f'Line {occurrence["line"]}, '
                    f'Column {occurrence["column"]}: '
                    f'{occurrence["text"]}'
                )

        print()

    print(
        f"Found {len(grouped)} violation(s)."
    )

    return 1


# ============================================================
# Entry Point
# ============================================================

if __name__ == "__main__":
    raise SystemExit(main())
