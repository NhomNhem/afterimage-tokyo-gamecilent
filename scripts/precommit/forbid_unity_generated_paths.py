from __future__ import annotations

import argparse
import datetime as dt
import subprocess
from dataclasses import dataclass
from pathlib import Path

from precommit_log_paths import HookReportPaths, build_report_paths, write_report_to_paths


HOOK_ID = "forbid-unity-generated-folders"
SCRIPT_PATH = "scripts/precommit/forbid_unity_generated_paths.py"

FORBIDDEN_PARTS = {
    "Library",
    "library",
    "Temp",
    "temp",
    "Obj",
    "obj",
    "Build",
    "build",
    "Builds",
    "builds",
    "Logs",
    "logs",
    ".vs",
    ".idea",
}


@dataclass(frozen=True)
class Failure:
    path: Path
    forbidden_part: str


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


def collect_staged_files() -> list[Path]:
    result = run_git(["diff", "--cached", "--name-only", "--diff-filter=ACMR"])

    if result.returncode != 0:
        print("[FAIL] Could not collect staged files.")
        print(result.stderr)
        raise SystemExit(1)

    files: list[Path] = []

    for line in result.stdout.splitlines():
        value = line.strip()
        if value:
            files.append(Path(value))

    return sorted(files, key=lambda p: normalize_path(p).lower())


def collect_all_tracked_files() -> list[Path]:
    result = run_git(["ls-files"])

    if result.returncode != 0:
        print("[FAIL] Could not collect tracked files.")
        print(result.stderr)
        raise SystemExit(1)

    files: list[Path] = []

    for line in result.stdout.splitlines():
        value = line.strip()
        if value:
            files.append(Path(value))

    return sorted(files, key=lambda p: normalize_path(p).lower())


def scan_path(path: Path) -> Failure | None:
    parts = set(path.parts)
    hits = parts & FORBIDDEN_PARTS

    if not hits:
        return None

    return Failure(path=path, forbidden_part=sorted(hits)[0])


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
            lines.append(f"- Forbidden part: `{failure.forbidden_part}`")
            lines.append("- Rule: Unity generated/cache/IDE folders must not be committed.")
            lines.append("")
            lines.append("Suggested fixes:")
            lines.append("")
            lines.append("```powershell")
            lines.append("git restore --staged .idea")
            lines.append("git restore .idea")
            lines.append("git clean -fd .idea")
            lines.append("```")
            lines.append("")
    else:
        lines.append("## Checked Files")
        lines.append("")
        for file_path in checked_files:
            lines.append(f"- `{normalize_path(file_path)}`")
        lines.append("")

    lines.append("## Forbidden Parts")
    lines.append("")
    for part in sorted(FORBIDDEN_PARTS):
        lines.append(f"- `{part}`")
    lines.append("")
    lines.append("## Policy")
    lines.append("")
    lines.append("- Do not commit Unity generated/cache folders.")
    lines.append("- Do not commit IDE workspace folders such as `.idea` and `.vs`.")
    lines.append("- If a folder is already tracked, remove it with `git rm -r --cached <path>` and add it to `.gitignore`.")
    lines.append("")

    content = "\n".join(lines) + "\n"
    write_report_to_paths(report_paths, content)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--staged", action="store_true")
    parser.add_argument("--all", action="store_true")
    parser.add_argument("--log-file", default=None)

    args = parser.parse_args()
    started = dt.datetime.now()

    if args.all and args.staged:
        print("[FAIL] Use only one mode: --all or --staged.")
        return 1

    if args.all:
        mode = "all"
        checked_files = collect_all_tracked_files()
    else:
        mode = "staged"
        checked_files = collect_staged_files()

    failures: list[Failure] = []

    for file_path in checked_files:
        failure = scan_path(file_path)
        if failure is not None:
            failures.append(failure)

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
            f"[FAIL] Forbidden Unity/generated path staged: "
            f"{normalize_path(failure.path)}"
        )

    if failures:
        print()
        print("Do not commit Unity generated/cache/IDE folders.")
        return 1

    print("[PASS] No Unity generated/cache/IDE folders found.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())