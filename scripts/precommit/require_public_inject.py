from __future__ import annotations

import argparse
import datetime as dt
import re
import subprocess
from dataclasses import dataclass
from pathlib import Path

from precommit_log_paths import HookReportPaths, build_report_paths, write_report_to_paths


HOOK_ID = "require-public-vcontainer-inject"
SCRIPT_PATH = "scripts/precommit/require_public_inject.py"

PROJECT_ROOTS = (
    "Assets/_Project/Code/",
    "Assets/_Project/Tests/",
)

INJECT_METHOD_PATTERN = re.compile(
    r"""
    \[ \s* Inject \s* \]
    \s*
    (?P<mods>
        (?:
            public|private|internal|protected|
            static|virtual|override|sealed|async|
            new|\s
        )*
    )
    \s*
    (?P<return_type>
        void|[A-Za-z_][A-Za-z0-9_<>,\.\?\s]*
    )
    \s+
    (?P<name>[A-Za-z_][A-Za-z0-9_]*)
    \s*
    \(
    """,
    re.VERBOSE | re.MULTILINE,
)


@dataclass(frozen=True)
class Failure:
    path: Path
    line: int
    method_name: str
    modifiers: str


def run_git(args: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        shell=False,
    )


def normalize_path(path: Path | str) -> str:
    return str(path).replace("\\", "/")


def is_project_cs_file(path: Path) -> bool:
    normalized = normalize_path(path)
    return path.suffix == ".cs" and any(normalized.startswith(root) for root in PROJECT_ROOTS)


def collect_all_project_cs_files() -> list[Path]:
    result = run_git(["ls-files", "Assets/_Project"])

    if result.returncode != 0:
        print("[FAIL] Could not collect project C# files via git ls-files.")
        print(result.stderr)
        raise SystemExit(1)

    files: list[Path] = []

    for line in result.stdout.splitlines():
        path = Path(line.strip())
        if is_project_cs_file(path):
            files.append(path)

    return sorted(files, key=lambda p: normalize_path(p).lower())


def collect_staged_project_cs_files() -> list[Path]:
    result = run_git(["diff", "--cached", "--name-only", "--diff-filter=ACMR"])

    if result.returncode != 0:
        print("[FAIL] Could not collect staged files.")
        print(result.stderr)
        raise SystemExit(1)

    files: list[Path] = []

    for line in result.stdout.splitlines():
        path = Path(line.strip())
        if is_project_cs_file(path):
            files.append(path)

    return sorted(files, key=lambda p: normalize_path(p).lower())


def find_line_number(text: str, index: int) -> int:
    return text.count("\n", 0, index) + 1


def scan_file(path: Path) -> list[Failure]:
    if not path.exists():
        return []

    text = path.read_text(encoding="utf-8", errors="ignore")
    failures: list[Failure] = []

    for match in INJECT_METHOD_PATTERN.finditer(text):
        mods = match.group("mods") or ""
        method_name = match.group("name")
        normalized_mods = set(mods.split())

        if "public" not in normalized_mods:
            failures.append(
                Failure(
                    path=path,
                    line=find_line_number(text, match.start()),
                    method_name=method_name,
                    modifiers=mods.strip(),
                )
            )

    return failures


def write_markdown_report(
    report_paths: HookReportPaths,
    started: dt.datetime,
    finished: dt.datetime,
    mode: str,
    checked_files: list[Path],
    failures: list[Failure],
) -> None:
    result = "FAIL" if failures else "PASS"
    failed_paths = {failure.path for failure in failures}
    passed_files = [path for path in checked_files if path not in failed_paths]

    lines: list[str] = []

    lines.append(f"# Pre-commit Hook Report: {HOOK_ID}")
    lines.append("")
    lines.append(f"- Hook: `{HOOK_ID}`")
    lines.append(f"- Script: `{SCRIPT_PATH}`")
    lines.append(f"- Mode: `{mode}`")
    lines.append(f"- Started: `{started.isoformat()}`")
    lines.append(f"- Finished: `{finished.isoformat()}`")
    lines.append(f"- Result: `{result}`")
    lines.append(f"- Checked files: `{len(checked_files)}`")
    lines.append(f"- Passed files: `{len(passed_files)}`")
    lines.append(f"- Failed files: `{len(failed_paths)}`")
    lines.append(f"- Failures: `{len(failures)}`")
    lines.append("")
    lines.append("## Summary")
    lines.append("")
    lines.append("| Metric | Value |")
    lines.append("|---|---:|")
    lines.append(f"| Checked files | {len(checked_files)} |")
    lines.append(f"| Passed files | {len(passed_files)} |")
    lines.append(f"| Failed files | {len(failed_paths)} |")
    lines.append(f"| Failures | {len(failures)} |")
    lines.append(f"| Result | {result} |")
    lines.append("")

    if failures:
        lines.append("## Failures")
        lines.append("")

        for index, failure in enumerate(failures, start=1):
            lines.append(f"### {index}. `{normalize_path(failure.path)}`")
            lines.append("")
            lines.append(f"- Line: `{failure.line}`")
            lines.append(f"- Method: `{failure.method_name}`")
            lines.append(f"- Modifiers: `{failure.modifiers or '(none)'}`")
            lines.append("- Rule: `[Inject] method must be public`")
            lines.append(
                "- Reason: VContainer Source Generator generated injectors may not access non-public injection methods."
            )
            lines.append("")
            lines.append("Suggested fix:")
            lines.append("")
            lines.append("```csharp")
            lines.append("[Inject]")
            lines.append("public void Construct(IDependency dependency)")
            lines.append("{")
            lines.append("}")
            lines.append("```")
            lines.append("")
    else:
        lines.append("## Checked Files")
        lines.append("")
        for file_path in checked_files:
            lines.append(f"- `{normalize_path(file_path)}`")
        lines.append("")

    lines.append("## Policy")
    lines.append("")
    lines.append("- Plain C# services should prefer constructor injection.")
    lines.append("- Unity `MonoBehaviour` components may use `[Inject] public void Construct(...)`.")
    lines.append("- Non-public `[Inject]` methods are blocked because VContainer Source Generator generated injectors may not access them.")
    lines.append("")

    content = "\n".join(lines) + "\n"
    write_report_to_paths(report_paths, content)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--all",
        action="store_true",
        help="Scan all tracked project C# files under Assets/_Project/Code and Assets/_Project/Tests.",
    )
    parser.add_argument(
        "--staged",
        action="store_true",
        help="Scan staged project C# files only.",
    )
    parser.add_argument(
        "--log-file",
        default=None,
        help="Optional explicit Markdown report path.",
    )

    args = parser.parse_args()
    started = dt.datetime.now()

    if args.all and args.staged:
        print("[FAIL] Use only one mode: --all or --staged.")
        return 1

    if args.staged:
        mode = "staged"
        checked_files = collect_staged_project_cs_files()
    else:
        mode = "all"
        checked_files = collect_all_project_cs_files()

    failures: list[Failure] = []

    for file_path in checked_files:
        failures.extend(scan_file(file_path))

    finished = dt.datetime.now()
    report_paths = build_report_paths(HOOK_ID, args.log_file)

    write_markdown_report(
        report_paths=report_paths,
        started=started,
        finished=finished,
        mode=mode,
        checked_files=checked_files,
        failures=failures,
    )

    print(f"[INFO] Hook report: {report_paths.dated_report}")
    print(f"[INFO] Date latest report: {report_paths.dated_latest}")
    print(f"[INFO] Global latest report: {report_paths.global_latest}")
    print(f"[INFO] Checked files: {len(checked_files)}")

    for failure in failures:
        print(
            f"[FAIL] {normalize_path(failure.path)}:{failure.line}: "
            f"[Inject] method '{failure.method_name}' must be public."
        )

    if failures:
        print()
        print("Fix example:")
        print("  [Inject]")
        print("  public void Construct(IDependency dependency) { ... }")
        return 1

    print("[PASS] All VContainer [Inject] methods are public.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())