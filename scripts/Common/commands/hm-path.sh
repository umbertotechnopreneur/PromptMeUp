#!/usr/bin/env sh
# VBWR B
#
# Project: PromptMeUp
# Repository: https://github.com/umbertotechnopreneur/PromptMeUp
# Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
#
# VibeWare: Human intent, AI execution, and plenty of tokens
# Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
#
# Modified with AI: OpenAI Codex; added this header on 2026-10-10.
# Human guidance: Umberto Giacobbi; requested VibeWare branding.
#
# Copyright (c) 2026 Umberto Giacobbi
# License: MIT - see LICENSE
#
# VBWR E

# SPDX-License-Identifier: MIT

set -eu

action="${1:-status}"
case "$action" in
  install|remove|status) ;;
  *) echo "Usage: ./hm-path.sh [install|remove|status] [--yes]" >&2; exit 2 ;;
esac

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(dirname -- "$script_dir")
if [ -x "$script_dir/hm" ]; then
  executable="$script_dir/hm"
elif [ -x "$repository_root/PromptMeUp/bin/Release/net10.0/hm" ]; then
  executable="$repository_root/PromptMeUp/bin/Release/net10.0/hm"
elif command -v hm >/dev/null 2>&1; then
  executable=$(command -v hm)
else
  echo "hm was not found. Publish or build PromptMeUp first." >&2
  exit 1
fi

if [ "${2:-}" = "--yes" ]; then
  exec "$executable" "--path=$action" --yes
fi

exec "$executable" "--path=$action"
