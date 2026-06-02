from __future__ import annotations

import datetime as dt
import re
import subprocess
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class HookReportPaths:
    dated_report: Path
    dated_latest: Path
    global_latest: Path


def run_git(args: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        shell=False,
    )


def get_git_dir() -> Path:
    result = run_git(["rev-parse", "--git-dir"])

    if result.returncode != 0:
        # Fallback for non-git execution.
        return Path(".git")

    return Path(result.stdout.strip())


def default_log_root() -> Path:
    return get_git_dir() / "precommit-logs"


def sanitize_hook_id(hook_id: str) -> str:
    value = hook_id.strip().lower()
    value = re.sub(r"[^a-z0-9_.-]+", "-", value)
    value = value.strip("-")
    return value or "hook"


def build_report_paths(
    hook_id: str,
    explicit_log_file: str | None = None,
) -> HookReportPaths:
    safe_hook_id = sanitize_hook_id(hook_id)

    now = dt.datetime.now()
    date_folder = now.strftime("%Y-%m-%d")
    time_stamp = now.strftime("%H-%M-%S")

    log_root = default_log_root()
    dated_dir = log_root / date_folder
    global_latest_dir = log_root / "latest"

    if explicit_log_file:
        dated_report = Path(explicit_log_file)
    else:
        dated_report = dated_dir / f"{time_stamp}_{safe_hook_id}.md"

    dated_latest = dated_dir / f"latest_{safe_hook_id}.md"
    global_latest = global_latest_dir / f"{safe_hook_id}.md"

    dated_report.parent.mkdir(parents=True, exist_ok=True)
    dated_latest.parent.mkdir(parents=True, exist_ok=True)
    global_latest.parent.mkdir(parents=True, exist_ok=True)

    return HookReportPaths(
        dated_report=dated_report,
        dated_latest=dated_latest,
        global_latest=global_latest,
    )


def write_report_to_paths(paths: HookReportPaths, content: str) -> None:
    paths.dated_report.write_text(content, encoding="utf-8")
    paths.dated_latest.write_text(content, encoding="utf-8")
    paths.global_latest.write_text(content, encoding="utf-8")