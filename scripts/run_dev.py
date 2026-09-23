#!/usr/bin/env python3
"""Development launcher for the HomeCharts budgeting app.

Runs the Avalonia desktop app (`src/HomeCharts.App`) via `dotnet run`,
mirroring `build/run-app.ps1` but cross-platform.

Usage:
    python scripts/run_dev.py                 # build (implicit) + run, Debug
    python scripts/run_dev.py -c Release       # run in Release
    python scripts/run_dev.py --build          # `dotnet build` the solution first
    python scripts/run_dev.py --no-restore     # skip NuGet restore
    python scripts/run_dev.py -- --some-arg    # pass args after `--` to the app
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
SOLUTION = REPO_ROOT / "HomeCharts.sln"
APP_PROJECT = REPO_ROOT / "src" / "HomeCharts.App" / "HomeCharts.App.csproj"


def run(cmd: list[str]) -> int:
    print(f"> {' '.join(cmd)}", flush=True)
    return subprocess.run(cmd, cwd=REPO_ROOT).returncode


def main() -> int:
    parser = argparse.ArgumentParser(description="Run HomeCharts in development mode.")
    parser.add_argument(
        "-c",
        "--configuration",
        default="Debug",
        help="Build configuration (default: Debug).",
    )
    parser.add_argument(
        "--build",
        action="store_true",
        help="Build the full solution before running.",
    )
    parser.add_argument(
        "--no-restore",
        action="store_true",
        help="Skip the implicit NuGet restore.",
    )
    parser.add_argument(
        "app_args",
        nargs=argparse.REMAINDER,
        help="Arguments after `--` are forwarded to the app.",
    )
    args = parser.parse_args()

    if shutil.which("dotnet") is None:
        print(
            "error: the .NET SDK ('dotnet') was not found on PATH.\n"
            "Install .NET 9 SDK: https://dotnet.microsoft.com/download",
            file=sys.stderr,
        )
        return 1

    if not APP_PROJECT.exists():
        print(f"error: app project not found at {APP_PROJECT}", file=sys.stderr)
        return 1

    restore_flag = ["--no-restore"] if args.no_restore else []

    if args.build:
        code = run(["dotnet", "build", str(SOLUTION), "-c", args.configuration, *restore_flag])
        if code != 0:
            print(f"error: dotnet build failed (exit {code}).", file=sys.stderr)
            return code

    # argparse.REMAINDER keeps the leading "--"; drop it before forwarding.
    forwarded = [a for a in args.app_args if a != "--"]

    cmd = ["dotnet", "run", "--project", str(APP_PROJECT), "-c", args.configuration, *restore_flag]
    if forwarded:
        cmd += ["--", *forwarded]

    return run(cmd)


if __name__ == "__main__":
    raise SystemExit(main())
