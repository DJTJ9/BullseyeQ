#!/bin/bash
# Syncs git snapshot for /codex:review (project uses Diversion, not git)
git add Assets/_Project Assets/Wingman Packages ProjectSettings .gitignore 2>/dev/null

if git diff --cached --quiet; then
  echo "Git already up to date."
else
  git -c user.email="review@tmp" -c user.name="tmp" commit -m "tmp: review snapshot"
  echo "Git synced."
fi

echo "Ready — run /codex:review"
